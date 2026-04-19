using System.Collections;
using UnityEngine;
using SpringJam.Systems.DayLoop;
using SpringJam2026.Utils;

namespace SpringJam2026.Audio
{
    public class DayLoopAudioHandler : MonoBehaviour, IGameService
    {
        private DayLoopRuntime runtime;
        private AudioService audioService;
        private const float OUTRO_DURATION = 6f;
        private const float INTRO_DURATION = 5f;
        private bool outroPlayed;
        private bool musicStarted;
        
        public int Priority => 60;
        public void Initialize()
        {
            runtime = DayLoopRuntime.Instance;
            audioService = ServiceLocator.Get<AudioService>();
        }

        public void Bind()
        {
            if (runtime == null) return;
    
            runtime.LoopStarted += OnLoopStarted;
            runtime.LoopEnded += OnLoopEnded;
            
            SyncWithCurrentState();
        }
    
        void OnDisable()
        {
            if (runtime == null) return;
    
            runtime.LoopStarted -= OnLoopStarted;
            runtime.LoopEnded -= OnLoopEnded;
        }
    
        void Update()
        {
            if (runtime == null) return;
    
            var snapshot = runtime.CurrentSnapshot;
    
            if (snapshot == null || !snapshot.IsPlayablePhase)
                return;
    
            float remaining = snapshot.RemainingSeconds;
    
            // Try play the outro before the day ends, like +- 6 seconds I recon
            if (!outroPlayed && remaining <= OUTRO_DURATION)
            {
                if (musicStarted)
                {
                    audioService.StopMusic();
                    musicStarted = false;
                }
                
                PlayNightOutro();
                outroPlayed = true;
            }
        }
        
        private void SyncWithCurrentState()
        {
            var snapshot = runtime.CurrentSnapshot;

            if (snapshot == null)
                return;
            
            if (snapshot.IsPlayablePhase)
            {
                OnLoopStarted(snapshot);
            }
        }
    
        private void OnLoopStarted(DayLoopSnapshot snapshot)
        {
            Debug.Log("[DayLoopAudioHandler] OnLoopStarted");
            outroPlayed = false;
            musicStarted = false;

            PlayMorningIntro();
            StartCoroutine(StartMusicAfterIntro());
        }
    
        private void OnLoopEnded(DayLoopEndContext context)
        {
            outroPlayed = false;
        }
    
        private void PlayMorningIntro()
        {
            audioService.PlayMorningIntro();
        }
    
        private void PlayNightOutro()
        {
            audioService.PlayNightOutro();
        }
        
        private IEnumerator StartMusicAfterIntro()
        {
            yield return new WaitForSeconds(INTRO_DURATION);

            if (audioService != null)
            {
                audioService.StartMusic();
                musicStarted = true;
            }
        }
    }   
}