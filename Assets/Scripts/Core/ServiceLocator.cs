using UnityEngine;
using System;
using System.Collections.Generic;

namespace TacticalGame.Core
{
    /// <summary>
    /// Service Locator pattern for accessing game managers without expensive Find operations.
    /// Register managers on Awake, access them anywhere via ServiceLocator.Get<T>()
    /// </summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
        private static bool isQuitting = false;

        /// <summary>
        /// Register a service. Call this in Awake() of your managers.
        /// </summary>
        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            
            if (services.ContainsKey(type))
            {
                Debug.LogWarning($"ServiceLocator: {type.Name} is already registered. Replacing.");
                services[type] = service;
            }
            else
            {
                services.Add(type, service);
            }
        }

        /// <summary>
        /// Unregister a service. Call this in OnDestroy() of your managers.
        /// </summary>
        public static void Unregister<T>() where T : class
        {
            if (isQuitting) return;
            
            var type = typeof(T);
            if (services.ContainsKey(type))
            {
                services.Remove(type);
            }
        }

        /// <summary>
        /// Get a registered service. Returns null if not found.
        /// </summary>
        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            
            if (services.TryGetValue(type, out var service))
            {
                return service as T;
            }
            
            Debug.LogWarning($"ServiceLocator: {type.Name} not found. Make sure it's registered in Awake().");
            return null;
        }

        /// <summary>
        /// Try to get a service. Returns false if not found.
        /// </summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);
            
            if (services.TryGetValue(type, out var obj))
            {
                service = obj as T;
                return true;
            }
            
            service = null;
            return false;
        }

        /// <summary>
        /// Check if a service is registered.
        /// </summary>
        public static bool IsRegistered<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Clear all services. Useful for scene transitions.
        /// </summary>
        public static void Clear()
        {
            services.Clear();
        }

        /// <summary>
        /// Call this when application is quitting to prevent errors.
        /// </summary>
        public static void OnApplicationQuit()
        {
            isQuitting = true;
            Clear();
        }
    }

    /// <summary>
    /// Attach this to a GameObject in your scene to handle cleanup.
    /// </summary>
    public class ServiceLocatorCleanup : MonoBehaviour
    {
        private void OnApplicationQuit()
        {
            ServiceLocator.OnApplicationQuit();
        }
    }
}