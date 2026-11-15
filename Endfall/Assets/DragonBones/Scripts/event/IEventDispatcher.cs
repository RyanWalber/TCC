namespace DragonBones
{
    public delegate void ListenerDelegate<T>(string type, T eventObject);

    public interface IEventDispatcher<T>
    {

        bool HasDBEventListener(string type);

        void DispatchDBEvent(string type, T eventObject);

        void AddDBEventListener(string type, ListenerDelegate<T> listener);

        void RemoveDBEventListener(string type, ListenerDelegate<T> listener);
    }
}
