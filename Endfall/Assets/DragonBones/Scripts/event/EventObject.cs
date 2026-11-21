namespace DragonBones
{

    public class EventObject : BaseObject
    {

        public const string START = "start";

        public const string LOOP_COMPLETE = "loopComplete";

        public const string COMPLETE = "complete";

        public const string FADE_IN = "fadeIn";

        public const string FADE_IN_COMPLETE = "fadeInComplete";

        public const string FADE_OUT = "fadeOut";

        public const string FADE_OUT_COMPLETE = "fadeOutComplete";

        public const string FRAME_EVENT = "frameEvent";

        public const string SOUND_EVENT = "soundEvent";



        public static void ActionDataToInstance(ActionData data, EventObject instance, Armature armature)
        {
            if (data.type == ActionType.Play)
            {
                instance.type = EventObject.FRAME_EVENT;
            }
            else
            {
                instance.type = data.type == ActionType.Frame ? EventObject.FRAME_EVENT : EventObject.SOUND_EVENT;
            }

            instance.name = data.name;
            instance.armature = armature;
            instance.actionData = data;
            instance.data = data.data;

            if (data.bone != null)
            {
                instance.bone = armature.GetBone(data.bone.name);
            }

            if (data.slot != null)
            {
                instance.slot = armature.GetSlot(data.slot.name);
            }
        }


        public float time;

        public string type;

        public string name;
        public Armature armature;

        public Bone bone;

        public Slot slot;

        public AnimationState animationState;
        public ActionData actionData;
        public UserData data;

        protected override void _OnClear()
        {
            this.time = 0.0f;
            this.type = string.Empty;
            this.name = string.Empty;
            this.armature = null;
            this.bone = null;
            this.slot = null;
            this.animationState = null;
            this.actionData = null;
            this.data = null;
        }
    }
}
