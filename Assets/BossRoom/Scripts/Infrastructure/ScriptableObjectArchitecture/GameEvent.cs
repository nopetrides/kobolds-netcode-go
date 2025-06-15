using System.Collections.Generic;
using UnityEngine;

namespace Unity.BossRoom.Infrastructure
{
    /// <summary>
    /// Class for encapsulating game-related events within ScriptableObject instances. This class defines a List of
    /// GameEventListeners, which will be notified whenever this GameEvent's Raise() method is fired.
    /// </summary>
    [CreateAssetMenu]
    public class GameEvent : ScriptableObject
    {
        List<IGameEventListenable> _mListeners = new List<IGameEventListenable>();

        public void Raise()
        {
            for (int i = _mListeners.Count - 1; i >= 0; i--)
            {
                if (_mListeners[i] == null)
                {
                    _mListeners.RemoveAt(i);
                    continue;
                }

                _mListeners[i].EventRaised();
            }
        }

        public void RegisterListener(IGameEventListenable listener)
        {
            for (int i = 0; i < _mListeners.Count; i++)
            {
                if (_mListeners[i] == listener)
                {
                    return;
                }
            }

            _mListeners.Add(listener);
        }

        public void DeregisterListener(IGameEventListenable listener)
        {
            _mListeners.Remove(listener);
        }
    }
}
