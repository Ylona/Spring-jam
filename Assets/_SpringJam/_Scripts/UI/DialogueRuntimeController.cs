using UnityEngine;
using UnityEngine.UI;

namespace SpringJam.Dialogue
{
    [DefaultExecutionOrder(-200)]
    public sealed class DialogueRuntimeController : MonoBehaviour
    {
        private static readonly Color PromptBackgroundColor = new Color32(22, 40, 65, 215);
        private static readonly Color DialogueBackgroundColor = new Color32(16, 11, 39, 232);
        private static readonly Color AccentColor = new Color32(243, 181, 168, 255);
        private static readonly Color BodyColor = new Color32(255, 221, 203, 255);

        private static DialogueRuntimeController instance;

        private readonly DialogueSession session = new DialogueSession();

        private InputSystem_Actions controls;
        private GameObject promptRoot;
        private GameObject dialogueRoot;
        private Text promptText;
        private Text speakerText;
        private Text bodyText;
        private Text footerText;
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

        private void RefreshUi()
        {
            bool showDialogue = session.IsOpen;
            if (dialogueRoot != null)
            {
                dialogueRoot.SetActive(showDialogue);
            }

            if (showDialogue)
            {
                DialogueLine line = session.CurrentLine;
                speakerText.text = string.IsNullOrWhiteSpace(line.SpeakerName) ? "Caretaker" : line.SpeakerName;
                bodyText.text = line.Body;
                footerText.text = BuildFooterText();
            }

            bool showPrompt = !showDialogue && !string.IsNullOrWhiteSpace(worldPromptText);
            if (promptRoot != null)
            {
                promptRoot.SetActive(showPrompt);
            }

            if (showPrompt)
            {
                promptText.text = string.Format("E  {0}", worldPromptText);
            }
        }

        private string BuildFooterText()
        {
            return session.IsOnLastLine ? "E Close    Esc Cancel" : "E Continue    Esc Cancel";
        }

        private void BuildUi()
        {
            Font font = LoadFont();
            Transform canvasTransform = CreateCanvasRoot();

            promptRoot = CreatePanel(
                canvasTransform,
                "InteractionPrompt",
                PromptBackgroundColor,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 68f),
                new Vector2(360f, 52f));
            promptText = CreateText(promptRoot.transform, "PromptText", font, 21, TextAnchor.MiddleCenter, BodyColor);
            Stretch(promptText.rectTransform, new Vector2(14f, 8f), new Vector2(-14f, -8f));

            dialogueRoot = CreatePanel(
                canvasTransform,
                "DialoguePanel",
                DialogueBackgroundColor,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 36f),
                new Vector2(1100f, 220f));

            speakerText = CreateText(dialogueRoot.transform, "SpeakerText", font, 24, TextAnchor.UpperLeft, AccentColor);
            SetRect(speakerText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(20f, -18f), new Vector2(-40f, 30f));

            bodyText = CreateText(dialogueRoot.transform, "BodyText", font, 28, TextAnchor.UpperLeft, BodyColor);
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(bodyText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), new Vector2(20f, -56f), new Vector2(-40f, -102f));

            footerText = CreateText(dialogueRoot.transform, "FooterText", font, 18, TextAnchor.LowerRight, AccentColor);
            SetRect(footerText.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 16f), new Vector2(-40f, 26f));
        }

        private Transform CreateCanvasRoot()
        {
            GameObject canvasObject = new GameObject("DialogueCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvasObject.transform;
        }

        private static GameObject CreatePanel(
            Transform parent,
            string name,
            Color backgroundColor,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject panelObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelObject.transform.SetParent(parent, false);

            Image image = panelObject.GetComponent<Image>();
            image.color = backgroundColor;

            RectTransform rectTransform = panelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = size;
            return panelObject;
        }

        private static Text CreateText(Transform parent, string name, Font font, int fontSize, TextAnchor alignment, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void Stretch(RectTransform rectTransform, Vector2 insetMin, Vector2 insetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.offsetMin = insetMin;
            rectTransform.offsetMax = insetMax;
        }

        private static void SetRect(
            RectTransform rectTransform,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = pivot;
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;
        }

        private static Font LoadFont()
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

