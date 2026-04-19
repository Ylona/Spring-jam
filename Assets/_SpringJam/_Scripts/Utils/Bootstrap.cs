using System.Collections.Generic;
using System.Linq;
using SpringJam2026.Audio;
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
            var services = new List<IGameService>();
            
            // Scan the scene for any GOs that implements IGameService
            var sceneServices = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None)
                .OfType<IGameService>();
            
            services.AddRange(sceneServices);
            
            // Manual addition of C# scripts that extend IGameService (no mono)
            services.Add(new AudioService());
            
            services = services.OrderBy(s => s.Priority).ToList();
            
            // Register the services first before initializing in case there are dependency
            foreach (var service in services)
            {
                ServiceLocator.Register(service.GetType(), service);
            }
            
            // Seconds we initialize
            foreach (var service in services)
                service.Initialize();
            
            // Lastly we can subscribe to events
            foreach (var service in services)
                service.Bind();
        }

        private void InitializeSystems()
        {
            ServiceLocator.DebugDumpServices();
            
            // Not the best place to put this but just for testing
            // ServiceLocator.Get<AudioService>()?.StartMusic();
        }
    }
}