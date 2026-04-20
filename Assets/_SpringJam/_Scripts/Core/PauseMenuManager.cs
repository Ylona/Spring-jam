using System.Collections;
using SpringJam.Dialogue;
using SpringJam2026.Audio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SpringJam2026.Utils;
using UnityEngine.EventSystems;

namespace SpringJam2026.Core
{
    public class PauseMenuManager : MonoBehaviour, IGameService
    {
        [Header("UI")] 
        [SerializeField] private UIDocument uiDocument;

        [Header("Input")] 
        [SerializeField] private InputActionAsset inputActions;

        private VisualElement root;
        private VisualElement menu;

        private Button resumeButton;
        private Button restartButton;
        private Toggle muteToggle;
        private Slider volumeSlider;

        private InputAction playerPauseAction;
        private InputAction uiPauseAction;

        private InputActionMap playerMap;
        private InputActionMap uiMap;

        private bool isPaused;
        private int lastPauseFrame = -1;
        
        private UIDocument dialogueUI;
        private AudioService audioService;

        public int Priority => 60;

        public void Initialize()
        {
            root = uiDocument.rootVisualElement.Q<VisualElement>("root");
            menu = root.Q<VisualElement>("pause-menu");

            resumeButton = root.Q<Button>("resumeButton");
            restartButton = root.Q<Button>("restartButton");
            muteToggle = root.Q<Toggle>("muteToggle");
            volumeSlider = root.Q<Slider>("volumeSlider");

            muteToggle.RegisterValueChangedCallback(OnMuteChanged);
            volumeSlider.RegisterValueChangedCallback(OnVolumeChanged);
            
            menu.pickingMode = PickingMode.Position;

            playerMap = inputActions.FindActionMap("Player");
            uiMap = inputActions.FindActionMap("UI");

            playerPauseAction = playerMap.FindAction("Pause");
            uiPauseAction = uiMap.FindAction("Pause");
            
            // Temp
            dialogueUI = FindFirstObjectByType<DialogueRuntimeController>()?.GetComponent<UIDocument>();
            
            audioService = ServiceLocator.Get<AudioService>();

            if (audioService != null)
            {
                float volume = audioService.GetMasterVolume();
                volumeSlider.SetValueWithoutNotify(volume);
                muteToggle.SetValueWithoutNotify(volume <= 0f);   
            }
            
            resumeButton.clicked += () =>
            {
                PlayReturn();
                TogglePause();
            };

            resumeButton.RegisterCallback<MouseEnterEvent>(_ => PlayHover());
            
            restartButton.clicked += () =>
            {
                PlaySelect();
                RestartGame();
            };

            restartButton.RegisterCallback<MouseEnterEvent>(_ => PlayHover());
            
            playerMap.Disable();
            uiMap.Disable();
        }

        public void Bind()
        {
            playerMap.Enable();
            uiMap.Enable();
            
            playerPauseAction.performed += OnPausePressed;
            uiPauseAction.performed += OnPausePressed;

            playerPauseAction.Enable();
            uiPauseAction.Enable();
        }
        
        private IEnumerator Start()
        {
            yield return null;
            
            playerMap.Enable();
            uiMap.Enable();
            playerPauseAction.Enable();
            uiPauseAction.Enable();
            
            if (audioService != null)
            {
                float volume = audioService.GetMasterVolume();
                volumeSlider.SetValueWithoutNotify(volume);
                muteToggle.SetValueWithoutNotify(volume <= 0f);   
            }
        }

        private void OnDestroy()
        {
            playerPauseAction.performed -= OnPausePressed;
            uiPauseAction.performed -= OnPausePressed;
        }

        private void OnPausePressed(InputAction.CallbackContext ctx)
        {
            // Pause is being double triggered. I suspect it could be both action maps being enabled??
            if (lastPauseFrame == Time.frameCount)
                return;
            
            lastPauseFrame = Time.frameCount;
            
            if (DialogueRuntimeController.ConsumedInputThisFrame)
                return;

            TogglePause();
        }

        public void TogglePause()
        {
            isPaused = !isPaused;

            if (isPaused)
                Pause();
            else
                Resume();
        }

        private void Pause()
        {
            GamePause.SetPaused(true);

            if (dialogueUI != null)
                dialogueUI.rootVisualElement.style.display = DisplayStyle.None;

            Time.timeScale = 0f;
            
            root.RemoveFromClassList("hidden");

            playerMap.Disable();
            uiMap.Enable();

            EventSystem.current.SetSelectedGameObject(null);

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }

        private void Resume()
        {
            GamePause.SetPaused(false);

            if (dialogueUI != null)
                dialogueUI.rootVisualElement.style.display = DisplayStyle.Flex;

            Time.timeScale = 1f;
            
            root.AddToClassList("hidden");

            uiMap.Disable();
            playerMap.Enable();

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }

        private void RestartGame()
        {
            var audio = ServiceLocator.Get<AudioService>();
            audio?.StopAllLoops();
            
            // Temp
            // Ideally we should have a gamemanager handling this since I see we have some dontdestroyonload
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnMuteChanged(ChangeEvent<bool> evt)
        {
            audioService.MuteMasterVolume(evt.newValue);

            if (!evt.newValue)
            {
                float volume = audioService.GetMasterVolume();
                volumeSlider.SetValueWithoutNotify(volume);
            }
        }

        private void OnVolumeChanged(ChangeEvent<float> evt)
        {
            float value = evt.newValue;
            
            if (value > 0f)
            {
                audioService.MuteMasterVolume(false);
                muteToggle.SetValueWithoutNotify(false);
            }
            else
            {
                audioService.MuteMasterVolume(true);
                muteToggle.SetValueWithoutNotify(true);
            }
            
            audioService.SetMasterVolume(value);
        }
        
        #region Audio Events
        
        private void PlayHover()
        {
            audioService?.PlayUIHover();
        }

        private void PlaySelect()
        {
            audioService?.PlayUISelect();
        }

        private void PlayReturn()
        {
            audioService?.PlayUIReturn();
        }
        
        #endregion
    }
}