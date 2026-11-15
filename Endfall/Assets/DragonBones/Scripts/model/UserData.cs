using System.Collections.Generic;

namespace DragonBones
{

    public class UserData : BaseObject
    {

        public readonly List<int> ints = new List<int>();

        public readonly List<float> floats = new List<float>();

        public readonly List<string> strings = new List<string>();

        protected override void _OnClear()
        {
            this.ints.Clear();
            this.floats.Clear();
            this.strings.Clear();
        }

        internal void AddInt(int value)
        {
            this.ints.Add(value);
        }
        internal void AddFloat(float value)
        {
            this.floats.Add(value);
        }
        internal void AddString(string value)
        {
            this.strings.Add(value);
        }


        public int GetInt(int index = 0)
        {
            return index >= 0 && index < this.ints.Count ? this.ints[index] : 0;
        }

        public float GetFloat(int index = 0)
        {
            return index >= 0 && index < this.floats.Count ? this.floats[index] : 0.0f;
        }

        public string GetString(int index = 0)
        {
            return index >= 0 && index < this.strings.Count ? this.strings[index] : string.Empty;
        }
    }

    public class ActionData : BaseObject
    {
        public ActionType type;
        public string name;
        public BoneData bone;
        public SlotData slot;
        public UserData data;

        protected override void _OnClear()
        {
            if (this.data != null)
            {
                this.data.ReturnToPool();
            }

            this.type = ActionType.Play;
            this.name = "";
            this.bone = null;
            this.slot = null;
            this.data = null;
        }
    }
}
