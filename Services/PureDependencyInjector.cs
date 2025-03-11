using System;
using System.Collections.Generic;

namespace HumbleKeys.Services
{
    public class PureDependencyInjector
    {
        private readonly Dictionary<Type, object> resolutions = new Dictionary<Type, object>();

        public void Register<T>(Func<PureDependencyInjector, T> factory)
        {
            resolutions.Add(typeof(T), factory);
        }

        public T Resolve<T>()
        {
            var factory = (Func<PureDependencyInjector, T>)resolutions[typeof(T)];
            return factory(this);
        }
    }

}
