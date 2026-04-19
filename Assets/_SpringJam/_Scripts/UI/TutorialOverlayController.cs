using UnityEngine;
using UnityEngine.UIElements;

public class TutorialOverlayController : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;

    private VisualElement root;
    private Button closeButton;

    private void Awake()
    {
        root = uiDocument.rootVisualElement;
        root.pickingMode = PickingMode.Position;

        closeButton = root.Q<Button>("CloseButton");
        if (closeButton != null)
        {
            closeButton.clicked -= Hide;
            closeButton.clicked += Hide;
            Hide();
        }
    }

    private void OnDisable()
    {
        if (closeButton != null)
            closeButton.clicked -= Hide;
    }

    public void Show()
    {
        if (root != null)
            root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        if (root != null)
            root.style.display = DisplayStyle.None;
    }
}
