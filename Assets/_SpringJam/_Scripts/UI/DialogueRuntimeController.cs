using System;
using System.Collections.Generic;
using SpringJam.Systems.DayLoop;
using SpringJam.UI;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace SpringJam.Dialogue
{
    [DefaultExecutionOrder(-200)]
    public sealed class DialogueRuntimeController : MonoBehaviour
    {
        private const string OverlayResourcePath = "UI/DialogueOverlay";
        private const string PanelSettingsResourcePath = "UI/DialoguePanelSettings";
        private const int PetalCount = 5;

        private static DialogueRuntimeController instance;
        private static bool isShuttingDown;

        private readonly DialogueSession session = new DialogueSession();
        private readonly Dictionary<string, TaskRowBinding> taskRows = new Dictionary<string, TaskRowBinding>(StringComparer.Ordinal);
        private readonly List<VisualElement> petalElements = new List<VisualElement>(PetalCount);

        private InputSystem_Actions controls;
        private PanelSettings panelSettings;
        private bool ownsPanelSettings;
        private PanelTextSettings panelTextSettings;
        private bool ownsPanelTextSettings;
        private FontAsset runtimeFontAsset;
        private UIDocument uiDocument;
        private VisualElement promptShell;
        private VisualElement dialogueShell;
        private Label promptLabel;
        private Label speakerLabel;
        private Label bodyLabel;
        private Label footerLabel;
        private VisualElement journalShell;
        private Label journalTitleLabel;
        private VisualElement skyBand;
        private VisualElement sunTrack;
        private VisualElement sunDisc;
        private VisualElement petalStrip;
        private Label timeHintLabel;
        private VisualElement taskList;
        private string worldPromptText = string.Empty;
        private int consumedInputFrame = -1;
        private DayLoopRuntime subscribedDayLoop;
        private DayLoopSnapshot currentDayLoopSnapshot;

        public static bool IsDialogueOpen => instance != null && instance.session.IsOpen;
        public static bool ConsumedInputThisFrame => instance != null && instance.consumedInputFrame == Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            isShuttingDown = false;
            EnsureInstance();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            isShuttingDown = false;
            DontDestroyOnLoad(gameObject);

            controls = new InputSystem_Actions();
            BuildUi();
            TrySubscribeDayLoop();
            RefreshUi();
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnEnable()
        {
            controls?.Enable();
            TrySubscribeDayLoop();
            RefreshTaskJournal();
        }

        private void OnDisable()
        {
            controls?.Disable();
            UnsubscribeDayLoop();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                isShuttingDown = true;
            }

            UnsubscribeDayLoop();

            if (ownsPanelSettings && panelSettings != null)
            {
                DestroyOwnedObject(panelSettings);
            }

            if (ownsPanelTextSettings && panelTextSettings != null)
            {
                DestroyOwnedObject(panelTextSettings);
            }

            if (runtimeFontAsset != null)
            {
                DestroyOwnedObject(runtimeFontAsset);
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
            TrySubscribeDayLoop();
            RefreshTaskJournalTime();

            if (!session.IsOpen)
            {
                return;
            }

            if (controls.Player.Previous.WasPressedThisFrame() && session.MovePrevious())
            {
                ConsumeInput();
                RefreshUi();
                return;
            }

            if (controls.UI.Cancel.WasPressedThisFrame() && session.TryClose())
            {
                ConsumeInput();
                RefreshUi();
                return;
            }

            if (controls.Player.Interact.WasPressedThisFrame()
                || controls.Player.Next.WasPressedThisFrame()
                || controls.UI.Submit.WasPressedThisFrame())
            {
                DialogueAdvanceResult result = session.Advance();
                if (result != DialogueAdvanceResult.None)
                {
                    ConsumeInput();
                    RefreshUi();
                }
            }
        }

        public static bool TryStartConversation(DialogueConversation conversation)
        {
            DialogueRuntimeController controller = EnsureInstance();
            return controller != null && controller.TryStartConversationInternal(conversation);
        }

        public static void SetInteractionPrompt(string promptTextValue)
        {
            DialogueRuntimeController controller = EnsureInstance();
            if (controller != null)
            {
                controller.SetInteractionPromptInternal(promptTextValue);
            }
        }

        private bool TryStartConversationInternal(DialogueConversation conversation)
        {
            if (!session.TryOpen(conversation))
            {
                return false;
            }

            ConsumeInput();
            RefreshUi();
            return true;
        }

        private void SetInteractionPromptInternal(string promptTextValue)
        {
            string nextPromptText = string.IsNullOrWhiteSpace(promptTextValue) ? string.Empty : promptTextValue.Trim();
            if (worldPromptText == nextPromptText)
            {
                return;
            }

            worldPromptText = nextPromptText;

            if (!session.IsOpen)
            {
                RefreshUi();
            }
        }

        private void ConsumeInput()
        {
            consumedInputFrame = Time.frameCount;
        }

        private void BuildUi()
        {
            VisualTreeAsset overlayAsset = Resources.Load<VisualTreeAsset>(OverlayResourcePath);
            StyleSheet overlayStyleSheet = Resources.Load<StyleSheet>(OverlayResourcePath);
            if (overlayAsset == null)
            {
                Debug.LogError($"Dialogue overlay UXML is missing at Resources/{OverlayResourcePath}.uxml", this);
                return;
            }

            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            panelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            ownsPanelSettings = panelSettings == null;
            if (panelSettings == null)
            {
                panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
                panelSettings.name = "DialoguePanelSettingsRuntime";
                panelSettings.hideFlags = HideFlags.DontSave;
            }

            panelTextSettings = EnsurePanelTextSettings(panelSettings);
            ConfigurePanelSettings(panelSettings);

            uiDocument.panelSettings = panelSettings;
            uiDocument.sortingOrder = 1000;

            VisualElement root = uiDocument.rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;
            StretchToParent(root);

            if (overlayStyleSheet != null)
            {
                root.styleSheets.Add(overlayStyleSheet);
            }

            TemplateContainer overlayRoot = overlayAsset.CloneTree();
            overlayRoot.pickingMode = PickingMode.Ignore;
            StretchToParent(overlayRoot);
            root.Add(overlayRoot);

            promptShell = root.Q<VisualElement>("prompt-shell");
            dialogueShell = root.Q<VisualElement>("dialogue-shell");
            promptLabel = root.Q<Label>("prompt-label");
            speakerLabel = root.Q<Label>("speaker-label");
            bodyLabel = root.Q<Label>("body-label");
            footerLabel = root.Q<Label>("footer-label");

            BuildTaskJournal(root);
            ApplyRuntimeTextDefaults(root);
        }

        private void BuildTaskJournal(VisualElement root)
        {
            journalShell = new VisualElement { name = "journal-shell" };
            journalShell.AddToClassList("journal-shell");
            root.Add(journalShell);

            journalTitleLabel = new Label("Spring Weave");
            journalTitleLabel.name = "journal-title";
            journalTitleLabel.AddToClassList("journal-title");
            journalShell.Add(journalTitleLabel);

            VisualElement timeShell = new VisualElement { name = "time-shell" };
            timeShell.AddToClassList("time-shell");
            journalShell.Add(timeShell);

            skyBand = new VisualElement { name = "sky-band" };
            skyBand.AddToClassList("sky-band");
            skyBand.AddToClassList("time-band--dawn");
            timeShell.Add(skyBand);

            sunTrack = new VisualElement { name = "sun-track" };
            sunTrack.AddToClassList("sun-track");
            skyBand.Add(sunTrack);

            sunDisc = new VisualElement { name = "sun-disc" };
            sunDisc.AddToClassList("sun-disc");
            sunTrack.Add(sunDisc);

            petalStrip = new VisualElement { name = "petal-strip" };
            petalStrip.AddToClassList("petal-strip");
            timeShell.Add(petalStrip);

            timeHintLabel = new Label("Dawn hush");
            timeHintLabel.name = "time-hint-label";
            timeHintLabel.AddToClassList("time-hint-label");
            timeShell.Add(timeHintLabel);

            taskList = new VisualElement { name = "task-list" };
            taskList.AddToClassList("task-list");
            journalShell.Add(taskList);

            BuildPetals();
        }

        private void BuildPetals()
        {
            petalElements.Clear();
            petalStrip?.Clear();

            if (petalStrip == null)
            {
                return;
            }

            for (int i = 0; i < PetalCount; i++)
            {
                VisualElement petal = new VisualElement();
                petal.AddToClassList("time-petal");
                petalStrip.Add(petal);
                petalElements.Add(petal);
            }
        }

        private void RefreshUi()
        {
            bool showDialogue = session.IsOpen;
            SetVisibility(dialogueShell, showDialogue);

            if (showDialogue && speakerLabel != null && bodyLabel != null && footerLabel != null)
            {
                DialogueLine line = session.CurrentLine;
                speakerLabel.text = string.IsNullOrWhiteSpace(line.SpeakerName) ? "Caretaker" : line.SpeakerName;
                bodyLabel.text = line.Body;
                footerLabel.text = BuildFooterText();
            }

            bool showPrompt = !showDialogue && !string.IsNullOrWhiteSpace(worldPromptText);
            SetVisibility(promptShell, showPrompt);

            if (showPrompt && promptLabel != null)
            {
                promptLabel.text = $"E  {worldPromptText}";
            }

            RefreshTaskJournal();
        }

        private string BuildFooterText()
        {
            return session.IsOnLastLine ? "E Close    Esc Cancel" : "E Continue    Esc Cancel";
        }

        private void ApplyRuntimeTextDefaults(VisualElement root)
        {
            FontAsset fontAsset = EnsureRuntimeFontAsset();
            if (fontAsset == null)
            {
                return;
            }

            StyleFontDefinition fontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(fontAsset));
            root.style.unityFontDefinition = fontDefinition;

            ApplyFontToLabel(promptLabel, fontDefinition);
            ApplyFontToLabel(speakerLabel, fontDefinition);
            ApplyFontToLabel(bodyLabel, fontDefinition);
            ApplyFontToLabel(footerLabel, fontDefinition);
            ApplyFontToLabel(journalTitleLabel, fontDefinition);
            ApplyFontToLabel(timeHintLabel, fontDefinition);
        }

        private static void ApplyFontToLabel(Label label, StyleFontDefinition fontDefinition)
        {
            if (label == null)
            {
                return;
            }

            label.style.unityFontDefinition = fontDefinition;
            label.enableRichText = false;
        }

        private void TrySubscribeDayLoop()
        {
            DayLoopRuntime runtime = DayLoopRuntime.Instance;
            if (runtime == null)
            {
                runtime = FindFirstObjectByType<DayLoopRuntime>();
            }

            if (runtime == subscribedDayLoop)
            {
                return;
            }

            UnsubscribeDayLoop();
            subscribedDayLoop = runtime;

            if (subscribedDayLoop == null)
            {
                currentDayLoopSnapshot = null;
                RefreshTaskJournal();
                return;
            }

            subscribedDayLoop.LoopStarted += HandleLoopSnapshotChanged;
            subscribedDayLoop.PhaseChanged += HandleLoopSnapshotChanged;
            subscribedDayLoop.TaskChanged += HandleDayLoopTaskChanged;

            currentDayLoopSnapshot = subscribedDayLoop.CurrentSnapshot;
            RefreshTaskJournal();
        }

        private void UnsubscribeDayLoop()
        {
            if (subscribedDayLoop == null)
            {
                return;
            }

            subscribedDayLoop.LoopStarted -= HandleLoopSnapshotChanged;
            subscribedDayLoop.PhaseChanged -= HandleLoopSnapshotChanged;
            subscribedDayLoop.TaskChanged -= HandleDayLoopTaskChanged;
            subscribedDayLoop = null;
        }

        private void HandleLoopSnapshotChanged(DayLoopSnapshot snapshot)
        {
            currentDayLoopSnapshot = snapshot;
            RefreshTaskJournal();
        }

        private void HandleDayLoopTaskChanged(DayLoopTaskSnapshot task)
        {
            if (task == null)
            {
                return;
            }

            currentDayLoopSnapshot = subscribedDayLoop != null ? subscribedDayLoop.CurrentSnapshot : currentDayLoopSnapshot;

            if (taskRows.TryGetValue(task.TaskId, out TaskRowBinding row))
            {
                ApplyTaskState(row, task);
            }
            else
            {
                RefreshTaskJournal();
            }
        }

        private void RefreshTaskJournal()
        {
            bool hasSnapshot = currentDayLoopSnapshot != null;
            SetVisibility(journalShell, hasSnapshot);
            if (!hasSnapshot)
            {
                return;
            }

            if (RequiresTaskRowRebuild(currentDayLoopSnapshot.Tasks))
            {
                RebuildTaskRows(currentDayLoopSnapshot.Tasks);
            }

            foreach (DayLoopTaskSnapshot task in currentDayLoopSnapshot.Tasks)
            {
                if (task != null && taskRows.TryGetValue(task.TaskId, out TaskRowBinding row))
                {
                    ApplyTaskState(row, task);
                }
            }

            RefreshTaskJournalTime();
        }

        private void RefreshTaskJournalTime()
        {
            if (subscribedDayLoop == null || journalShell == null || journalShell.resolvedStyle.display == DisplayStyle.None)
            {
                return;
            }

            TaskJournalTimeBand timeBand = TaskJournalPresenter.GetTimeBand(
                subscribedDayLoop.CurrentPhase,
                subscribedDayLoop.ElapsedSeconds,
                subscribedDayLoop.DayDurationSeconds);
            ApplyTimeBandClass(timeBand);

            if (timeHintLabel != null)
            {
                timeHintLabel.text = TaskJournalPresenter.GetTimeLabel(
                    subscribedDayLoop.CurrentPhase,
                    subscribedDayLoop.ElapsedSeconds,
                    subscribedDayLoop.DayDurationSeconds);
            }

            int closedPetals = TaskJournalPresenter.GetClosedPetalCount(
                subscribedDayLoop.CurrentPhase,
                subscribedDayLoop.ElapsedSeconds,
                subscribedDayLoop.DayDurationSeconds,
                petalElements.Count);
            for (int i = 0; i < petalElements.Count; i++)
            {
                petalElements[i].EnableInClassList("is-closed", i < closedPetals);
            }

            if (sunDisc == null || sunTrack == null)
            {
                return;
            }

            float progress = TaskJournalPresenter.GetSunProgress(
                subscribedDayLoop.CurrentPhase,
                subscribedDayLoop.ElapsedSeconds,
                subscribedDayLoop.DayDurationSeconds);
            float trackWidth = sunTrack.resolvedStyle.width;
            float discWidth = sunDisc.resolvedStyle.width;
            if (trackWidth <= 0f || discWidth <= 0f)
            {
                return;
            }

            float maxTravel = Mathf.Max(0f, trackWidth - discWidth);
            sunDisc.style.left = maxTravel * progress;
            sunDisc.style.top = 18f - (Mathf.Sin(progress * Mathf.PI) * 12f);
        }

        private bool RequiresTaskRowRebuild(IReadOnlyList<DayLoopTaskSnapshot> tasks)
        {
            if (tasks == null)
            {
                return taskRows.Count > 0;
            }

            if (taskRows.Count != tasks.Count)
            {
                return true;
            }

            foreach (DayLoopTaskSnapshot task in tasks)
            {
                if (task == null || !taskRows.ContainsKey(task.TaskId))
                {
                    return true;
                }
            }

            return false;
        }

        private void RebuildTaskRows(IReadOnlyList<DayLoopTaskSnapshot> tasks)
        {
            taskRows.Clear();
            taskList?.Clear();
            if (taskList == null || tasks == null)
            {
                return;
            }

            foreach (DayLoopTaskSnapshot task in tasks)
            {
                if (task == null)
                {
                    continue;
                }

                TaskJournalTaskPresentation presentation = TaskJournalPresenter.GetTaskPresentation(task.TaskId);
                VisualElement row = new VisualElement();
                row.AddToClassList("task-card");
                row.AddToClassList(presentation.ThemeClass);

                Label markLabel = new Label(presentation.BadgeText);
                markLabel.AddToClassList("task-mark");
                row.Add(markLabel);

                VisualElement copy = new VisualElement();
                copy.AddToClassList("task-copy");

                Label titleLabel = new Label(task.DisplayName);
                titleLabel.AddToClassList("task-title");
                copy.Add(titleLabel);

                Label stateLabel = new Label();
                stateLabel.AddToClassList("task-state");
                copy.Add(stateLabel);

                row.Add(copy);
                taskList.Add(row);
                taskRows[task.TaskId] = new TaskRowBinding(row, titleLabel, stateLabel);
            }
        }

        private void ApplyTaskState(TaskRowBinding row, DayLoopTaskSnapshot task)
        {
            TaskJournalTaskState state = TaskJournalPresenter.GetTaskState(task);
            row.TitleLabel.text = task.DisplayName;
            row.StateLabel.text = TaskJournalPresenter.GetTaskStateText(task);
            row.Root.EnableInClassList("is-locked", state == TaskJournalTaskState.Locked);
            row.Root.EnableInClassList("is-ready", state == TaskJournalTaskState.Ready);
            row.Root.EnableInClassList("is-complete", state == TaskJournalTaskState.Complete);
        }

        private void ApplyTimeBandClass(TaskJournalTimeBand timeBand)
        {
            if (skyBand == null)
            {
                return;
            }

            skyBand.EnableInClassList("time-band--dawn", timeBand == TaskJournalTimeBand.Dawn);
            skyBand.EnableInClassList("time-band--morning", timeBand == TaskJournalTimeBand.Morning);
            skyBand.EnableInClassList("time-band--high-sun", timeBand == TaskJournalTimeBand.HighSun);
            skyBand.EnableInClassList("time-band--long-light", timeBand == TaskJournalTimeBand.LongLight);
            skyBand.EnableInClassList("time-band--dusk", timeBand == TaskJournalTimeBand.Dusk);
        }

        private PanelTextSettings EnsurePanelTextSettings(PanelSettings settings)
        {
            PanelTextSettings resolvedTextSettings = settings.textSettings;
            ownsPanelTextSettings = resolvedTextSettings == null;
            if (resolvedTextSettings == null)
            {
                resolvedTextSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                resolvedTextSettings.name = "DialoguePanelTextSettingsRuntime";
                resolvedTextSettings.hideFlags = HideFlags.DontSave;
            }

            FontAsset fontAsset = EnsureRuntimeFontAsset();
            if (fontAsset != null && resolvedTextSettings.defaultFontAsset == null)
            {
                resolvedTextSettings.defaultFontAsset = fontAsset;
            }

            settings.textSettings = resolvedTextSettings;
            return resolvedTextSettings;
        }

        private FontAsset EnsureRuntimeFontAsset()
        {
            if (runtimeFontAsset != null)
            {
                return runtimeFontAsset;
            }

            Font font = LoadRuntimeFont();
            if (font == null)
            {
                return null;
            }

            runtimeFontAsset = FontAsset.CreateFontAsset(font);
            if (runtimeFontAsset != null)
            {
                runtimeFontAsset.name = $"DialogueRuntime-{font.name}";
                runtimeFontAsset.hideFlags = HideFlags.DontSave;
            }

            return runtimeFontAsset;
        }

        private static Font LoadRuntimeFont()
        {
            Font font = TryLoadBuiltinFont("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            font = TryLoadBuiltinFont("Arial.ttf");
            if (font != null)
            {
                return font;
            }

            return Font.CreateDynamicFontFromOSFont("Arial", 18);
        }

        private static Font TryLoadBuiltinFont(string fontName)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(fontName);
            }
            catch
            {
                return null;
            }
        }

        private static void ConfigurePanelSettings(PanelSettings settings)
        {
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 0.5f;
            settings.sortingOrder = 1000;
            settings.clearColor = false;
        }

        private static void StretchToParent(VisualElement element)
        {
            if (element == null)
            {
                return;
            }

            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.top = 0f;
            element.style.right = 0f;
            element.style.bottom = 0f;
        }

        private static void SetVisibility(VisualElement element, bool isVisible)
        {
            if (element == null)
            {
                return;
            }

            element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static void DestroyOwnedObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
                return;
            }

            DestroyImmediate(target);
        }

        private static DialogueRuntimeController EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            if (isShuttingDown || !Application.isPlaying)
            {
                return null;
            }

            instance = FindFirstObjectByType<DialogueRuntimeController>();
            if (instance != null)
            {
                return instance;
            }

            GameObject runtimeObject = new GameObject("DialogueRuntimeController");
            return runtimeObject.AddComponent<DialogueRuntimeController>();
        }

        private sealed class TaskRowBinding
        {
            public TaskRowBinding(VisualElement root, Label titleLabel, Label stateLabel)
            {
                Root = root;
                TitleLabel = titleLabel;
                StateLabel = stateLabel;
            }

            public VisualElement Root { get; }
            public Label TitleLabel { get; }
            public Label StateLabel { get; }
        }
    }
}
