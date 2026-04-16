using UnityEngine;
using SpringJam.Systems.DayLoop;
using SpringJam2026.Audio;
using SpringJam2026.Utils;

namespace SpringJam2026.Systems
{
    public class DayLoopAudioHandler : MonoBehaviour, IGameService
    {
        private DayLoopRuntime runtime;
        private AudioService audio;
    
        private bool outroPlayed;
    
        private const float OUTRO_DURATION = 6f;
        
        public int Priority => 60;
        public void Initialize()
        {
            runtime = DayLoopRuntime.Instance;
            audio = ServiceLocator.Get<AudioService>();
        }

        public void Bind()
        {
            if (runtime == null) return;
    
            runtime.LoopStarted += OnLoopStarted;
            runtime.LoopEnded += OnLoopEnded;
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
                PlayNightOutro();
                outroPlayed = true;
            }
        }
    
        private void OnLoopStarted(DayLoopSnapshot snapshot)
        {
            outroPlayed = false;
    
            PlayMorningIntro();
        }
    
        private void OnLoopEnded(DayLoopEndContext context)
        {
            outroPlayed = false;
        }
    
        private void PlayMorningIntro()
        {
            audio.PlayMorningIntro();
        }
    
        private void PlayNightOutro()
        {
            audio.PlayNightOutro();
        }
    }   
}