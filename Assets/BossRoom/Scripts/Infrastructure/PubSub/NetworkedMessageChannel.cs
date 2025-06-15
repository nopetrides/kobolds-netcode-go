using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace Unity.BossRoom.Infrastructure
{
    /// <summary>
    /// This type of message channel allows the server to publish a message that will be sent to clients as well as
    /// being published locally. Clients and the server both can subscribe to it.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class NetworkedMessageChannel<T> : MessageChannel<T> where T : unmanaged, INetworkSerializeByMemcpy
    {
        NetworkManager _mNetworkManager;

        string _mName;

        public NetworkedMessageChannel()
        {
            _mName = $"{typeof(T).FullName}NetworkMessageChannel";
        }

        [Inject]
        void InjectDependencies(NetworkManager networkManager)
        {
            _mNetworkManager = networkManager;
            _mNetworkManager.OnClientConnectedCallback += OnClientConnected;
            if (_mNetworkManager.IsListening)
            {
                RegisterHandler();
            }
        }

        public override void Dispose()
        {
            if (!IsDisposed)
            {
                if (_mNetworkManager != null && _mNetworkManager.CustomMessagingManager != null)
                {
                    _mNetworkManager.CustomMessagingManager.UnregisterNamedMessageHandler(_mName);
                }
            }
            base.Dispose();
        }

        void OnClientConnected(ulong clientId)
        {
            RegisterHandler();
        }

        void RegisterHandler()
        {
            // Only register message handler on clients
            if (!_mNetworkManager.IsServer)
            {
                _mNetworkManager.CustomMessagingManager.RegisterNamedMessageHandler(_mName, ReceiveMessageThroughNetwork);
            }
        }

        public override void Publish(T message)
        {
            if (_mNetworkManager.IsServer)
            {
                // send message to clients, then publish locally
                SendMessageThroughNetwork(message);
                base.Publish(message);
            }
            else
            {
                Debug.LogError("Only a server can publish in a NetworkedMessageChannel");
            }
        }

        void SendMessageThroughNetwork(T message)
        {
            // Avoid throwing an exception if you are in the middle of shutting down and either
            // NetworkManager no longer exists or the CustomMessagingManager no longer exists.
            if (_mNetworkManager == null || _mNetworkManager.CustomMessagingManager == null)
            {
                return;
            }
            var writer = new FastBufferWriter(FastBufferWriter.GetWriteSize<T>(), Allocator.Temp);
            writer.WriteValueSafe(message);
            _mNetworkManager.CustomMessagingManager.SendNamedMessageToAll(_mName, writer);
        }

        void ReceiveMessageThroughNetwork(ulong clientID, FastBufferReader reader)
        {
            reader.ReadValueSafe(out T message);
            base.Publish(message);
        }
    }
}
