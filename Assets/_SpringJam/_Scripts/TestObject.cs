using System.Collections;
using SpringJam2026.Events;
using SpringJam2026.Utils;
using UnityEngine;

namespace SpringJam2026
{
    public class TestObject : MonoBehaviour, IGameService
    {
        public int Priority => 100;
        public void Initialize()
        {
            // Do nothing
        }

        public void Bind()
        {
            // Do nothing
            StartCoroutine(TestEvent());
        }

        private IEnumerator TestEvent()
        {
            yield return new WaitForSeconds(3);
            
            EventHub.Broadcast_GamePaused(false);
        }
    }   
}
