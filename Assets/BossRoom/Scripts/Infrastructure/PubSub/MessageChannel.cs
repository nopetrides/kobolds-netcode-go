using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.BossRoom.Infrastructure
{
    public class MessageChannel<T> : IMessageChannel<T>
    {
        readonly List<Action<T>> _mMessageHandlers = new List<Action<T>>();

        /// This dictionary of handlers to be either added or removed is used to prevent problems from immediate
        /// modification of the list of subscribers. It could happen if one decides to unsubscribe in a message handler
        /// etc.A true value means this handler should be added, and a false one means it should be removed
        readonly Dictionary<Action<T>, bool> _mPendingHandlers = new Dictionary<Action<T>, bool>();

        public bool IsDisposed { get; private set; } = false;

        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
                _mMessageHandlers.Clear();
                _mPendingHandlers.Clear();
            }
        }

        public virtual void Publish(T message)
        {
            foreach (var handler in _mPendingHandlers.Keys)
            {
                if (_mPendingHandlers[handler])
                {
                    _mMessageHandlers.Add(handler);
                }
                else
                {
                    _mMessageHandlers.Remove(handler);
                }
            }
            _mPendingHandlers.Clear();

            foreach (var messageHandler in _mMessageHandlers)
            {
                if (messageHandler != null)
                {
                    messageHandler.Invoke(message);
                }
            }
        }

        public virtual IDisposable Subscribe(Action<T> handler)
        {
            Assert.IsTrue(!IsSubscribed(handler), "Attempting to subscribe with the same handler more than once");

            if (_mPendingHandlers.ContainsKey(handler))
            {
                if (!_mPendingHandlers[handler])
                {
                    _mPendingHandlers.Remove(handler);
                }
            }
            else
            {
                _mPendingHandlers[handler] = true;
            }

            var subscription = new DisposableSubscription<T>(this, handler);
            return subscription;
        }

        public void Unsubscribe(Action<T> handler)
        {
            if (IsSubscribed(handler))
            {
                if (_mPendingHandlers.ContainsKey(handler))
                {
                    if (_mPendingHandlers[handler])
                    {
                        _mPendingHandlers.Remove(handler);
                    }
                }
                else
                {
                    _mPendingHandlers[handler] = false;
                }
            }
        }

        bool IsSubscribed(Action<T> handler)
        {
            var isPendingRemoval = _mPendingHandlers.ContainsKey(handler) && !_mPendingHandlers[handler];
            var isPendingAdding = _mPendingHandlers.ContainsKey(handler) && _mPendingHandlers[handler];
            return _mMessageHandlers.Contains(handler) && !isPendingRemoval || isPendingAdding;
        }
    }
}
