using SpringJam2026.Utils;
using UnityEngine;

namespace SpringJam2026.Audio
{
    public class FlowerAudioHandler : MonoBehaviour, IGameService
    {
        private AudioService audioService;

        public int Priority => 61;
        public void Initialize()
        {
            audioService = ServiceLocator.Get<AudioService>();
        }

        public void Bind()
        {
            // Silence is golden
        }

        public void PlayFlowerInteract()
        {
            audioService.PlayFlowerInteract();
        }

        public void PlayFlowerWrong()
        {
            audioService.PlayFlowerWrong();
        }

        public void PlayFlowerRight()
        {
            audioService.PlayFlowerRight();
        }
    }  
}
