using System;
using System.Collections.Generic;
using System.Text;

namespace DragonBones
{

    public class DragonBonesData : BaseObject
    {
        public bool autoSearch;

        public uint frameRate;

        public string version;

        public string name;
        public ArmatureData stage;
        public readonly List<uint> frameIndices = new List<uint>();
        public readonly List<float> cachedFrames = new List<float>();

        public readonly List<string> armatureNames = new List<string>();
        public readonly Dictionary<string, ArmatureData> armatures = new Dictionary<string, ArmatureData>();
        internal byte[] binary;
        internal short[] intArray;
        internal float[] floatArray;
        internal short[] frameIntArray;
        internal float[] frameFloatArray;
        internal short[] frameArray;
        internal ushort[] timelineArray;
        internal UserData userData = null;

        protected override void _OnClear()
        {
            foreach (var k in this.armatures.Keys)
            {
                this.armatures[k].ReturnToPool();
            }

            if (this.userData != null)
            {
                this.userData.ReturnToPool();
            }

            this.autoSearch = false;
            this.frameRate = 0;
            this.version = "";
            this.name = "";
            this.stage = null;
            this.frameIndices.Clear();
            this.cachedFrames.Clear();
            this.armatureNames.Clear();
            this.armatures.Clear();
            this.binary = null;
            this.intArray = null;
            this.floatArray = null;
            this.frameIntArray = null;
            this.frameFloatArray = null;
            this.frameArray = null;
            this.timelineArray = null;
            this.userData = null;
        }

        public void AddArmature(ArmatureData value)
        {
            if (this.armatures.ContainsKey(value.name))
            {
                Helper.Assert(false, "Same armature: " + value.name);
                this.armatures[value.name].ReturnToPool();
            }

            value.parent = this;
            this.armatures[value.name] = value;
            this.armatureNames.Add(value.name);
        }


        public ArmatureData GetArmature(string armatureName)
        {
            return this.armatures.ContainsKey(armatureName) ? this.armatures[armatureName] : null;
        }
    }
}
