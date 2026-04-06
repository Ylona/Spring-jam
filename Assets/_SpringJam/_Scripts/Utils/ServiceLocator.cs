using System;
using System.Collections.Generic;

namespace SpringJam2026.Utils
{
    public static class ServiceLocator
    {
        private static Dictionary<Type, object> _services = new();

        public static void Register<T>(T service)
        {
            var type = typeof(T);
            _services[type] = service;
        }
        
        public static T Get<T>()
        {
            var type = typeof(T);

            if (_services.TryGetValue(type, out var service))
                return (T)service;
            
            throw new Exception($"Service not found: {type}");
        }
    }   
}
