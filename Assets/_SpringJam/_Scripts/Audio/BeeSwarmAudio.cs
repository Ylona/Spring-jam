using SpringJam2026.Utils;
using UnityEngine;

namespace SpringJam2026.Audio
{
    public class BeeSwarmAudio : MonoBehaviour, IGameService
    {
        public int Priority => 44;

        [SerializeField] private BeeSwarmAnchorMover swarm;

        private AudioController controller;
        private const string BEE_SWARM_ID = "beeSwarm";

        public void Initialize()
        {
            controller = ServiceLocator.Get<AudioController>();
        }

        public void Bind()
        {
            controller.PlayLoop3D(BEE_SWARM_ID, controller.library.beeSwarmLoop, swarm.transform);
            controller.SetLoopVolume(BEE_SWARM_ID, 0.5f);
        }
        
        private void OnDestroy()
        {
            controller.StopLoop(BEE_SWARM_ID);
        }
    }
}