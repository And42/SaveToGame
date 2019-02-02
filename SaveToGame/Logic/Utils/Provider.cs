using Autofac;
using JetBrains.Annotations;

namespace SaveToGameWpf.Logic.Utils
{
    public class Provider<T>
    {
        [NotNull]
        private readonly ILifetimeScope _lifetimeScope;
        
        public Provider([NotNull] ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public T Get() => _lifetimeScope.Resolve<T>();
    }
}