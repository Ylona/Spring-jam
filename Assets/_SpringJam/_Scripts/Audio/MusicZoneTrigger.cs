using UnityEngine;
using SpringJam2026.Utils;

namespace SpringJam2026.Audio
{
    [RequireComponent(typeof(Collider))]
    public class MusicZoneTrigger : MonoBehaviour, IGameService
    {
        private AudioService audioService;

        public int Priority => 45;
        
        public void Initialize()
        {
            audioService = ServiceLocator.Get<AudioService>();
        }

        public void Bind()
        {
            // Silence is golden
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            audioService.SetMusicZone(1);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            audioService.SetMusicZone(0);
        }
    }    
}
