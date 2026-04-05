using SpringJam.Dialogue;
using SpringJam.Systems.DayLoop;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace SpringJam.Journal
{
    [DefaultExecutionOrder(-190)]
    public sealed class MemoryJournalRuntimeController : MonoBehaviour
    {
        private const string OverlayResourcePath = "UI/MemoryJournalOverlay";
        private const string PanelSettingsResourcePath = "UI/DialoguePanelSettings";
        private const string ToggleHintText = "C  Journal";
        private const string FooterText = "C Close    Esc Close";

        private static MemoryJournalRuntimeController instance;
        private static bool isShuttingDown;

        private InputSystem_Actions controls;
        private DayLoopRuntime observedRuntime;
        private bool isOpen;
        private bool isDirty = true;
        private float previousTimeScale = 1f;

        private PanelSettings panelSettings;
        private bool ownsPanelSettings;
        private PanelTextSettings panelTextSettings;
        private bool ownsPanelTextSettings;
        private FontAsset runtimeFontAsset;
        private UIDocument uiDocument;
        private VisualElement toggleShell;
        private Label toggleLabel;
        private VisualElement journalScrim;
        private VisualElement journalShell;
        private Label phaseLabel;
        private ScrollView tasksList;
        private Label tasksEmptyLabel;
        private ScrollView cluesList;
        private Label cluesEmptyLabel;
        private Label footerLabel;

        public static bool IsJournalOpen => instance != null && instance.isOpen;

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
            RefreshUi();
        }

        private void OnEnable()
        {
            controls?.Enable();
        }

        private void OnDisable()
        {
            controls?.Disable();
            UnbindRuntime();

            if (isOpen)
            {
                RestoreTimeScale();
                isOpen = false;
            }
        }

        private void OnApplicationQuit()
        {
            isShuttingDown = true;
        }

        private void OnDestroy()
        {
            RestoreTimeScale();
            UnbindRuntime();

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
                isShuttingDown = true;
            }
        }

        private void Update()
        {
            TryBindRuntime();

            if (isOpen && DialogueRuntimeController.IsDialogueOpen)
            {
                SetJournalOpen(false);
            }

            bool togglePressed = controls != null && controls.Player.Crouch.WasPressedThisFrame();
            bool cancelPressed = controls != null && controls.UI.Cancel.WasPressedThisFrame();

            if (isOpen)
            {
                if (togglePressed || cancelPressed)
                {
                    SetJournalOpen(false);
                }
            }
            else if (togglePressed && !DialogueRuntimeController.IsDialogueOpen)
            {
                SetJournalOpen(true);
            }

            if (isDirty)
            {
                RefreshUi();
            }
        }

        private void BuildUi()
        {
            VisualTreeAsset overlayAsset = Resources.Load<VisualTreeAsset>(OverlayResourcePath);
            StyleSheet overlayStyleSheet = Resources.Load<StyleSheet>(OverlayResourcePath);
            if (overlayAsset == null)
            {
                Debug.LogError($"Memory journal overlay UXML is missing at Resources/{OverlayResourcePath}.uxml", this);
                return;
            }

            uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                uiDocument = gameObject.AddComponent<UIDocument>();
            }

            PanelSettings loadedPanelSettings = Resources.Load<PanelSettings>(PanelSettingsResourcePath);
            panelSettings = loadedPanelSettings != null
                ? Instantiate(loadedPanelSettings)
                : ScriptableObject.CreateInstance<PanelSettings>();
            ownsPanelSettings = true;
            panelSettings.name = "MemoryJournalPanelSettingsRuntime";
            panelSettings.hideFlags = HideFlags.DontSave;

            panelTextSettings = EnsurePanelTextSettings(panelSettings);
            ConfigurePanelSettings(panelSettings);

            uiDocument.panelSettings = panelSettings;
            uiDocument.sortingOrder = 980;

            VisualElement root = uiDocument.rootVisualElement;
            root.Clear();
            StretchToParent(root);

            if (overlayStyleSheet != null)
            {
                root.styleSheets.Add(overlayStyleSheet);
            }

            TemplateContainer journalTree = overlayAsset.CloneTree();
            StretchToParent(journalTree);
            root.Add(journalTree);

            toggleShell = root.Q<VisualElement>("toggle-shell");
            toggleLabel = root.Q<Label>("toggle-label");
            journalScrim = root.Q<VisualElement>("journal-scrim");
            journalShell = root.Q<VisualElement>("journal-shell");
            phaseLabel = root.Q<Label>("phase-label");
            tasksList = root.Q<ScrollView>("tasks-list");
            tasksEmptyLabel = root.Q<Label>("tasks-empty-label");
            cluesList = root.Q<ScrollView>("clues-list");
            cluesEmptyLabel = root.Q<Label>("clues-empty-label");
            footerLabel = root.Q<Label>("footer-label");

            ApplyRuntimeTextDefaults(root);
        }

        private void RefreshUi()
        {
            isDirty = false;

            bool showToggleHint = !isOpen && !DialogueRuntimeController.IsDialogueOpen;
            SetVisibility(toggleShell, showToggleHint);
            if (toggleLabel != null)
            {
                toggleLabel.text = ToggleHintText;
            }

            SetVisibility(journalScrim, isOpen);
            SetVisibility(journalShell, isOpen);
            if (!isOpen)
            {
                return;
            }

            MemoryJournalPageData pageData = MemoryJournalPresentationBuilder.Build(observedRuntime != null ? observedRuntime.CurrentSnapshot : null);

            if (phaseLabel != null)
            {
                phaseLabel.text = pageData.PhaseLine;
            }

            if (footerLabel != null)
            {
                footerLabel.text = FooterText;
            }

            PopulateTasks(pageData);
            PopulateClues(pageData);
        }

        private void PopulateTasks(MemoryJournalPageData pageData)
        {
            if (tasksList == null || tasksEmptyLabel == null)
            {
                return;
            }

            tasksList.contentContainer.Clear();

            foreach (MemoryJournalTaskEntry task in pageData.Tasks)
            {
                tasksList.contentContainer.Add(CreateTaskCard(task));
            }

            bool hasTasks = pageData.Tasks.Count > 0;
            SetVisibility(tasksList, hasTasks);
            SetVisibility(tasksEmptyLabel, !hasTasks);
            tasksEmptyLabel.text = pageData.TasksEmptyMessage;
        }

        private void PopulateClues(MemoryJournalPageData pageData)
        {
            if (cluesList == null || cluesEmptyLabel == null)
            {
                return;
            }

            cluesList.contentContainer.Clear();

            foreach (MemoryJournalClueEntry clue in pageData.Clues)
            {
                cluesList.contentContainer.Add(CreateClueCard(clue));
            }

            bool hasClues = pageData.Clues.Count > 0;
            SetVisibility(cluesList, hasClues);
            SetVisibility(cluesEmptyLabel, !hasClues);
            cluesEmptyLabel.text = pageData.CluesEmptyMessage;
        }

        private VisualElement CreateTaskCard(MemoryJournalTaskEntry task)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("journal-card");
            card.AddToClassList("task-card");

            VisualElement header = new VisualElement();
            header.AddToClassList("journal-card-header");

            Label title = new Label(task.Title);
            title.AddToClassList("journal-card-title");
            header.Add(title);

            Label badge = new Label(task.StatusLabel);
            badge.AddToClassList("task-badge");
            badge.AddToClassList(GetTaskStateClass(task.State));
            header.Add(badge);

            card.Add(header);

            Label body = new Label(task.Summary);
            body.AddToClassList("journal-card-body");
            card.Add(body);

            return card;
        }

        private VisualElement CreateClueCard(MemoryJournalClueEntry clue)
        {
            VisualElement card = new VisualElement();
            card.AddToClassList("journal-card");
            card.AddToClassList("clue-card");

            Label category = new Label(clue.CategoryLabel);
            category.AddToClassList("clue-category");
            card.Add(category);

            Label title = new Label(clue.Title);
            title.AddToClassList("journal-card-title");
            card.Add(title);

            Label body = new Label(clue.Summary);
            body.AddToClassList("journal-card-body");
            card.Add(body);

            return card;
        }

        private void SetJournalOpen(bool shouldOpen)
        {
            if (isOpen == shouldOpen)
            {
                return;
            }

            isOpen = shouldOpen;
            if (isOpen)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else
            {
                RestoreTimeScale();
            }

            isDirty = true;
        }

        private void RestoreTimeScale()
        {
            if (Time.timeScale == 0f)
            {
                Time.timeScale = previousTimeScale;
            }
        }

        private void TryBindRuntime()
        {
            DayLoopRuntime runtime = DayLoopRuntime.Instance;
            if (observedRuntime == runtime)
            {
                return;
            }

            UnbindRuntime();
            observedRuntime = runtime;

            if (observedRuntime != null)
            {
                observedRuntime.LoopStarted += HandleLoopUpdated;
                observedRuntime.PhaseChanged += HandleLoopUpdated;
                observedRuntime.LoopEnded += HandleLoopEnded;
                observedRuntime.TaskChanged += HandleTaskChanged;
                observedRuntime.KnowledgeLearned += HandleKnowledgeLearned;
            }

            isDirty = true;
        }

        private void UnbindRuntime()
        {
            if (observedRuntime == null)
            {
                return;
            }

            observedRuntime.LoopStarted -= HandleLoopUpdated;
            observedRuntime.PhaseChanged -= HandleLoopUpdated;
            observedRuntime.LoopEnded -= HandleLoopEnded;
            observedRuntime.TaskChanged -= HandleTaskChanged;
            observedRuntime.KnowledgeLearned -= HandleKnowledgeLearned;
            observedRuntime = null;
            isDirty = true;
        }

        private void HandleLoopUpdated(DayLoopSnapshot snapshot)
        {
            isDirty = true;
        }

        private void HandleLoopEnded(DayLoopEndContext context)
        {
            isDirty = true;
        }

        private void HandleTaskChanged(DayLoopTaskSnapshot taskSnapshot)
        {
            isDirty = true;
        }

        private void HandleKnowledgeLearned(string knowledgeId)
        {
            isDirty = true;
        }

        private static string GetTaskStateClass(MemoryJournalTaskState state)
        {
            return state switch
            {
                MemoryJournalTaskState.Sleeping => "task-badge-sleeping",
                MemoryJournalTaskState.Ready => "task-badge-ready",
                _ => "task-badge-complete",
            };
        }

        private void ApplyRuntimeTextDefaults(VisualElement root)
        {
            FontAsset fontAsset = EnsureRuntimeFontAsset();
            if (fontAsset == null)
            {
                return;
            }

            root.style.unityFontDefinition = new StyleFontDefinition(FontDefinition.FromSDFFont(fontAsset));
        }

        private PanelTextSettings EnsurePanelTextSettings(PanelSettings settings)
        {
            PanelTextSettings resolvedTextSettings = settings.textSettings;
            ownsPanelTextSettings = resolvedTextSettings == null;
            if (resolvedTextSettings == null)
            {
                resolvedTextSettings = ScriptableObject.CreateInstance<PanelTextSettings>();
                resolvedTextSettings.name = "MemoryJournalPanelTextSettingsRuntime";
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
                runtimeFontAsset.name = $"MemoryJournalRuntime-{font.name}";
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
            settings.sortingOrder = 980;
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

        private static void DestroyOwnedObject(Object target)
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

        private static MemoryJournalRuntimeController EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            if (isShuttingDown || !Application.isPlaying)
            {
                return null;
            }

            instance = FindFirstObjectByType<MemoryJournalRuntimeController>();
            if (instance != null)
            {
                return instance;
            }

            GameObject runtimeObject = new GameObject("MemoryJournalRuntimeController");
            return runtimeObject.AddComponent<MemoryJournalRuntimeController>();
        }
    }
}

