using System;
using System.Collections.Generic;

namespace Unity.BossRoom.Infrastructure
{
    public class DisposableGroup : IDisposable
    {
        readonly List<IDisposable> _mDisposables = new List<IDisposable>();

        public void Dispose()
        {
            foreach (var disposable in _mDisposables)
            {
                disposable.Dispose();
            }

            _mDisposables.Clear();
        }

        public void Add(IDisposable disposable)
        {
            _mDisposables.Add(disposable);
        }
    }
}
