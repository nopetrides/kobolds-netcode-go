using System;

namespace Unity.BossRoom.Infrastructure
{
    /// <summary>
    /// This class is a handle to an active Message Channel subscription and when disposed it unsubscribes from said channel.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DisposableSubscription<T> : IDisposable
    {
        Action<T> _mHandler;
        bool _mIsDisposed;
        IMessageChannel<T> _mMessageChannel;

        public DisposableSubscription(IMessageChannel<T> messageChannel, Action<T> handler)
        {
            _mMessageChannel = messageChannel;
            _mHandler = handler;
        }

        public void Dispose()
        {
            if (!_mIsDisposed)
            {
                _mIsDisposed = true;

                if (!_mMessageChannel.IsDisposed)
                {
                    _mMessageChannel.Unsubscribe(_mHandler);
                }

                _mHandler = null;
                _mMessageChannel = null;
            }
        }
    }
}
