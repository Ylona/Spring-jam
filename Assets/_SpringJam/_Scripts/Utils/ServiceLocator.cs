using System;
using System.Collections.Generic;

namespace SpringJam2026.Utils
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> _services = new();

        public static void Register(Type type, object instance)
        {
            if (type == null || instance == null)
                return;

            _services[type] = instance;
        }
        
        public static void Register<T>(T service)
        {
            var type = typeof(T);
            _services[type] = service;
        }
        
        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
                return (T)service;

            throw new Exception($"Service not found: {typeof(T)}");
        }
        
        public static void DebugDumpServices()
        {
            if (_services == null || _services.Count == 0)
            {
                UnityEngine.Debug.Log("[ServiceLocator] No services registered.");
                return;
            }

            UnityEngine.Debug.Log($"[ServiceLocator] Dumping {_services.Count} services:");

            foreach (var kvp in _services)
            {
                var keyType = kvp.Key;
                var instance = kvp.Value;
                var concreteType = instance?.GetType();

                UnityEngine.Debug.Log(
                    $"[ServiceLocator] Key: {keyType.FullName} | Instance: {concreteType?.FullName}"
                );
            }
        }
    }   
}
