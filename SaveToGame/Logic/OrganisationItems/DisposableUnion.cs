using System;
using UsefulFunctionsLib;

namespace SaveToGameWpf.Logic.OrganisationItems
{
    public class DisposableUnion : IDisposable
    {
        private readonly IDisposable[] _items;
        private bool _disposed;

        public DisposableUnion(params IDisposable[] items)
        {
            _items = items;
        }

        public void Dispose()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(DisposableUnion));

            _disposed = true;
            _items.ForEach(it => it.Dispose());
        }
    }
}
