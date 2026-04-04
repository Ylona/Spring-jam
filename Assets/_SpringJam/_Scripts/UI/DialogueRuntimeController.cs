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

        private static DialogueRuntimeController instance;

        private readonly DialogueSession session = new DialogueSession();

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
        private string worldPromptText = string.Empty;
        private int consumedInputFrame = -1;

        public static bool IsDialogueOpen => instance != null && instance.session.IsOpen;
        public static bool ConsumedInputThisFrame => instance != null && instance.consumedInputFrame == Time.frameCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
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
        }

        private void OnDestroy()
        {
            if (ownsPanelSettings && panelSettings != null)
            {
                Destroy(panelSettings);
            }

            if (ownsPanelTextSettings && panelTextSettings != null)
            {
                Destroy(panelTextSettings);
            }

            if (runtimeFontAsset != null)
            {
                Destroy(runtimeFontAsset);
            }

            if (instance == this)
            {
                instance = null;
            }
        }

        private void Update()
        {
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
            return EnsureInstance().TryStartConversationInternal(conversation);
        }

        public static void SetInteractionPrompt(string promptTextValue)
        {
            EnsureInstance().SetInteractionPromptInternal(promptTextValue);
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
            worldPromptText = string.IsNullOrWhiteSpace(promptTextValue) ? string.Empty : promptTextValue.Trim();

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
            }

            panelTextSettings = EnsurePanelTextSettings(panelSettings);
            ConfigurePanelSettings(panelSettings);

            uiDocument.panelSettings = panelSettings;
            uiDocument.sortingOrder = 1000;

            VisualElement root = uiDocument.rootVisualElement;
            root.Clear();
            root.pickingMode = PickingMode.Ignore;

            if (overlayStyleSheet != null)
            {
                root.styleSheets.Add(overlayStyleSheet);
            }

            TemplateContainer overlayRoot = overlayAsset.CloneTree();
            overlayRoot.pickingMode = PickingMode.Ignore;
            overlayRoot.style.flexGrow = 1f;
            overlayRoot.style.width = Length.Percent(100);
            overlayRoot.style.height = Length.Percent(100);
            root.Add(overlayRoot);

            promptShell = root.Q<VisualElement>("prompt-shell");
            dialogueShell = root.Q<VisualElement>("dialogue-shell");
            promptLabel = root.Q<Label>("prompt-label");
            speakerLabel = root.Q<Label>("speaker-label");
            bodyLabel = root.Q<Label>("body-label");
            footerLabel = root.Q<Label>("footer-label");

            ApplyRuntimeTextDefaults(root);
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

        private static void SetVisibility(VisualElement element, bool isVisible)
        {
            if (element == null)
            {
                return;
            }

            element.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static DialogueRuntimeController EnsureInstance()
        {
            if (instance != null)
            {
                return instance;
            }

            instance = FindFirstObjectByType<DialogueRuntimeController>();
            if (instance != null)
            {
                return instance;
            }

            GameObject runtimeObject = new GameObject("DialogueRuntimeController");
            return runtimeObject.AddComponent<DialogueRuntimeController>();
        }
    }
}
