using System;
using System.Collections.Generic;
using _001_Scripts.Interface;

namespace _001_Scripts.Core
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, IManager> _managers = new Dictionary<Type, IManager>();

        public static T GetService<T>() where T : IManager
        {
            if (!_managers.ContainsKey(typeof(T)))
                throw new Exception($"Service {typeof(T).Name} not registered");

            return (T)_managers[typeof(T)];
        }

        public static void RegisterService<T>(T service) where T : IManager => _managers.Add(typeof(T), service);
    }
}