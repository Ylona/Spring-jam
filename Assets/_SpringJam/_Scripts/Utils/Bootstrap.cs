using System.Linq;
using SpringJam2026.Events;
using UnityEngine;

namespace SpringJam2026.Utils
{
    public class Bootstrap : MonoBehaviour
    {
        private bool _initialized;
        
        private void Awake()
        {
            if (_initialized) return;
            _initialized = true;
            
            InitializeServices();
            InitializeSystems();
        }

        private void InitializeServices()
        {
            // Scan the scene for any GOs that implements IGameService
            // Order them by the priority (added in the skript)
            var services = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IGameService>()
                .OrderBy(s => s.Priority)
                .ToList();
            
            // First we will initialize these
            foreach (var service in services)
            {
                service.Initialize();

                ServiceLocator.Register(service);

                Debug.Log($"[Bootstrap] Initialized: {service.GetType().Name} (Priority: {service.Priority})");
            }
            
            // Second we will subscribe to events
            foreach (var service in services)
            {
                service.Bind();

                Debug.Log($"[Bootstrap] Bound: {service.GetType().Name}");
            }
        }

        private void InitializeSystems()
        {
            Debug.Log("Game Initialized");
        }
    }
}