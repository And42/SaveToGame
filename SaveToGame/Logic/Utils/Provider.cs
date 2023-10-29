using Autofac;

namespace SaveToGameWpf.Logic.Utils
{
    public class Provider<T>
    {
        private readonly ILifetimeScope _lifetimeScope;
        
        public Provider(ILifetimeScope lifetimeScope)
        {
            _lifetimeScope = lifetimeScope;
        }

        public T Get() => _lifetimeScope.Resolve<T>();
    }
}