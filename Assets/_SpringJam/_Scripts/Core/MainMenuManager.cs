using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpringJam2026.Audio;
using SpringJam2026.Utils;

namespace SpringJam2026.Core
{
    public class MainMenuManager : MonoBehaviour, IGameService
    {
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private string gameSceneName = "Map";
        [SerializeField] private InputActionAsset inputActions;

        private InputAction submitAction;
        private InputActionMap uiMap;
        private Button playButton;
        private AudioService audioService;

        public int Priority => 45;
        public void Initialize()
        {
            audioService = ServiceLocator.Get<AudioService>();
            
            var root = uiDocument.rootVisualElement;

            playButton = root.Q<Button>("playButton");
            
            uiMap = inputActions.FindActionMap("UI");
            submitAction = uiMap.FindAction("Submit");

            uiMap.Enable();
            
            Time.timeScale = 1f;
            
            GamePause.SetPaused(false);

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        
        public void Bind()
        {
            submitAction.performed += OnSubmit;
            playButton.clicked += OnPlayClicked;
            
            playButton.RegisterCallback<MouseEnterEvent>(_ => audioService.PlayUIHover());
        }

        private void Start()
        {
            if (audioService != null)
                audioService.StartMenuMusic();
        }
        
        private void OnDestroy()
        {
            submitAction.performed -= OnSubmit;
            playButton.clicked -= OnPlayClicked;
        }

        private void OnPlayClicked()
        {
            audioService.StopMenuMusic();
            audioService.PlayUIStartGame();
            SceneManager.LoadScene(gameSceneName);
        }
        
        private void OnSubmit(InputAction.CallbackContext ctx)
        {
            OnPlayClicked();
        }
    }   
}