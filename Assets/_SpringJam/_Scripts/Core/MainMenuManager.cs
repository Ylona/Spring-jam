using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace SpringJam2026.Core
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string gameSceneName = "Map";
        [SerializeField] private InputActionAsset inputActions;

        private InputAction submitAction;
        private InputActionMap uiMap;
        private Button playButton;

        private void Awake()
        {
            var root = uiDocument.rootVisualElement;

            playButton = root.Q<Button>("playButton");
            playButton.clicked += OnPlayClicked;
            
            uiMap = inputActions.FindActionMap("UI");
            submitAction = uiMap.FindAction("Submit");
            
            submitAction.performed += OnSubmit;

            uiMap.Enable();
            
            Time.timeScale = 1f;
            
            SpringJam2026.Core.GamePause.SetPaused(false);

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        
        private void OnDestroy()
        {
            submitAction.performed -= OnSubmit;
            playButton.clicked -= OnPlayClicked;
        }

        private void OnPlayClicked()
        {
            SceneManager.LoadScene(gameSceneName);
        }
        
        private void OnSubmit(InputAction.CallbackContext ctx)
        {
            OnPlayClicked();
        }
    }   
}