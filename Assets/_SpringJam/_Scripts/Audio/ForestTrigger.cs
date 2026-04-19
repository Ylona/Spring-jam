using UnityEngine;
using SpringJam2026.Utils;

namespace SpringJam2026.Audio
{
    public class ForestTrigger : MonoBehaviour, IGameService
    {
        public int Priority => 43;
        
        private AudioService audioService;

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

            audioService.EnterForest();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            audioService.ExitForest();
        }
    }
}
