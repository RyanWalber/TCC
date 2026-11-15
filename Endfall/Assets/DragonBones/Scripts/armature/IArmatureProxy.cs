namespace DragonBones
{

    public interface IArmatureProxy : IEventDispatcher<EventObject>
    {
        void DBInit(Armature armature);
        void DBClear();
        void DBUpdate();

        void Dispose(bool disposeProxy);

         Armature armature { get; }

         Animation animation { get; }
    }
}
