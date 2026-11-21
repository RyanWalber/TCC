using System.Collections.Generic;
using UnityEngine;

namespace DragonBones
{
    public class UnityEventDispatcher<T> : MonoBehaviour
    {
        private readonly Dictionary<string, ListenerDelegate<T>> _listeners = new Dictionary<string, ListenerDelegate<T>>();
        public UnityEventDispatcher()
        {
        }
        public void DispatchEvent(string type, T eventObject)
        {
            if (!_listeners.ContainsKey(type))
            {
                return;
            }
            else
            {
                _listeners[type](type, eventObject);
            }
        }
        public bool HasEventListener(string type)
        {
            return _listeners.ContainsKey(type);
        }
        public void AddEventListener(string type, ListenerDelegate<T> listener)
        {
            if (_listeners.ContainsKey(type))
            {
                var delegates = _listeners[type].GetInvocationList();
                for (int i = 0, l = delegates.Length; i < l; ++i)
                {
                    if (listener == delegates[i] as ListenerDelegate<T>)
                    {
                        return;
                    }
                }

                _listeners[type] += listener;
            }
            else
            {
                _listeners.Add(type, listener);
            }
        }
        public void RemoveEventListener(string type, ListenerDelegate<T> listener)
        {
            if (!_listeners.ContainsKey(type))
            {
                return;
            }

            var delegates = _listeners[type].GetInvocationList();
            for (int i = 0, l = delegates.Length; i < l; ++i)
            {
                if (listener == delegates[i] as ListenerDelegate<T>)
                {
                    _listeners[type] -= listener;
                    break;
                }
            }

            if (_listeners[type] == null)
            {
                _listeners.Remove(type);
            }
        }
    }

    [DisallowMultipleComponent]
    public class DragonBoneEventDispatcher : UnityEventDispatcher<EventObject>, IEventDispatcher<EventObject>
    {
        public void AddDBEventListener(string type, ListenerDelegate<EventObject> listener)
        {
            AddEventListener(type, listener);
        }

        public void DispatchDBEvent(string type, EventObject eventObject)
        {
            DispatchEvent(type, eventObject);
        }

        public bool HasDBEventListener(string type)
        {
            return HasEventListener(type);
        }

        public void RemoveDBEventListener(string type, ListenerDelegate<EventObject> listener)
        {
            RemoveEventListener(type, listener);
        }
    }
}
