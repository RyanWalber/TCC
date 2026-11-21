using System;
using System.Collections.Generic;

namespace DragonBones
{

    public class ArmatureData : BaseObject
    {
        public ArmatureType type;

        public uint frameRate;
        public uint cacheFrameRate;
        public float scale;

        public string name;
        public readonly Rectangle aabb = new Rectangle();

        public readonly List<string> animationNames = new List<string>();
        public readonly List<BoneData> sortedBones = new List<BoneData>();
        public readonly List<SlotData> sortedSlots = new List<SlotData>();
        public readonly List<ActionData> defaultActions = new List<ActionData>();
        public readonly List<ActionData> actions = new List<ActionData>();
        public readonly Dictionary<string, BoneData> bones = new Dictionary<string, BoneData>();
        public readonly Dictionary<string, SlotData> slots = new Dictionary<string, SlotData>();

        public readonly Dictionary<string, ConstraintData> constraints = new Dictionary<string, ConstraintData>();
        public readonly Dictionary<string, SkinData> skins = new Dictionary<string, SkinData>();
        public readonly Dictionary<string, AnimationData> animations = new Dictionary<string, AnimationData>();


        public SkinData defaultSkin = null;

        public AnimationData defaultAnimation = null;
        public CanvasData canvas = null;
        public UserData userData = null;
        public DragonBonesData parent;
        protected override void _OnClear()
        {
            foreach (var action in this.defaultActions)
            {
                action.ReturnToPool();
            }

            foreach (var action in this.actions)
            {
                action.ReturnToPool();
            }

            foreach (var k in this.bones.Keys)
            {
                this.bones[k].ReturnToPool();
            }

            foreach (var k in this.slots.Keys)
            {
                this.slots[k].ReturnToPool();
            }

            foreach (var k in this.constraints.Keys)
            {
                this.constraints[k].ReturnToPool();
            }

            foreach (var k in this.skins.Keys)
            {
                this.skins[k].ReturnToPool();
            }

            foreach (var k in this.animations.Keys)
            {
                this.animations[k].ReturnToPool();
            }

            if (this.canvas != null)
            {
                this.canvas.ReturnToPool();
            }

            if (this.userData != null)
            {
                this.userData.ReturnToPool();
            }

            this.type = ArmatureType.Armature;
            this.frameRate = 0;
            this.cacheFrameRate = 0;
            this.scale = 1.0f;
            this.name = "";
            this.aabb.Clear();
            this.animationNames.Clear();
            this.sortedBones.Clear();
            this.sortedSlots.Clear();
            this.defaultActions.Clear();
            this.actions.Clear();
            this.bones.Clear();
            this.slots.Clear();
            this.constraints.Clear();
            this.skins.Clear();
            this.animations.Clear();
            this.defaultSkin = null;
            this.defaultAnimation = null;
            this.canvas = null;
            this.userData = null;
            this.parent = null;
        }

        public void SortBones()
        {
            var total = this.sortedBones.Count;
            if (total <= 0)
            {
                return;
            }

            var sortHelper = this.sortedBones.ToArray();
            var index = 0;
            var count = 0;
            this.sortedBones.Clear();
            while (count < total)
            {
                var bone = sortHelper[index++];
                if (index >= total)
                {
                    index = 0;
                }

                if (this.sortedBones.Contains(bone))
                {
                    continue;
                }

                var flag = false;
                foreach (var constraint in this.constraints.Values)
                {
                    if (constraint.root == bone && !this.sortedBones.Contains(constraint.target))
                    {
                        flag = true;
                        break;
                    }
                }

                if (flag)
                {
                    continue;
                }
                if (bone.parent != null && !this.sortedBones.Contains(bone.parent))
                {
                    continue;
                }

                this.sortedBones.Add(bone);
                count++;
            }
        }

        public void CacheFrames(uint frameRate)
        {
            if (this.cacheFrameRate > 0)
            {
                return;
            }

            this.cacheFrameRate = frameRate;
            foreach (var k in this.animations.Keys)
            {
                this.animations[k].CacheFrames(this.cacheFrameRate);
            }
        }

        public int SetCacheFrame(Matrix globalTransformMatrix, Transform transform)
        {
            var dataArray = this.parent.cachedFrames;
            var arrayOffset = dataArray.Count;

            dataArray.ResizeList(arrayOffset + 10, 0.0f);

            dataArray[arrayOffset] = globalTransformMatrix.a;
            dataArray[arrayOffset + 1] = globalTransformMatrix.b;
            dataArray[arrayOffset + 2] = globalTransformMatrix.c;
            dataArray[arrayOffset + 3] = globalTransformMatrix.d;
            dataArray[arrayOffset + 4] = globalTransformMatrix.tx;
            dataArray[arrayOffset + 5] = globalTransformMatrix.ty;
            dataArray[arrayOffset + 6] = transform.rotation;
            dataArray[arrayOffset + 7] = transform.skew;
            dataArray[arrayOffset + 8] = transform.scaleX;
            dataArray[arrayOffset + 9] = transform.scaleY;

            return arrayOffset;
        }

        public void GetCacheFrame(Matrix globalTransformMatrix, Transform transform, int arrayOffset)
        {
            var dataArray = this.parent.cachedFrames;
            globalTransformMatrix.a = dataArray[arrayOffset];
            globalTransformMatrix.b = dataArray[arrayOffset + 1];
            globalTransformMatrix.c = dataArray[arrayOffset + 2];
            globalTransformMatrix.d = dataArray[arrayOffset + 3];
            globalTransformMatrix.tx = dataArray[arrayOffset + 4];
            globalTransformMatrix.ty = dataArray[arrayOffset + 5];
            transform.rotation = dataArray[arrayOffset + 6];
            transform.skew = dataArray[arrayOffset + 7];
            transform.scaleX = dataArray[arrayOffset + 8];
            transform.scaleY = dataArray[arrayOffset + 9];
            transform.x = globalTransformMatrix.tx;
            transform.y = globalTransformMatrix.ty;
        }

        public void AddBone(BoneData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.bones.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same bone: " + value.name);
                    this.bones[value.name].ReturnToPool();
                }

                this.bones[value.name] = value;
                this.sortedBones.Add(value);
            }
        }
        public void AddSlot(SlotData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.slots.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same slot: " + value.name);
                    this.slots[value.name].ReturnToPool();
                }

                this.slots[value.name] = value;
                this.sortedSlots.Add(value);
            }
        }
        public void AddConstraint(ConstraintData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.constraints.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same constraint: " + value.name);
                    this.slots[value.name].ReturnToPool();
                }

                this.constraints[value.name] = value;
            }
        }
        public void AddSkin(SkinData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.skins.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same slot: " + value.name);
                    this.skins[value.name].ReturnToPool();
                }

                value.parent = this;
                this.skins[value.name] = value;
                if (this.defaultSkin == null)
                {
                    this.defaultSkin = value;
                }

                if (value.name == "default")
                {
                    this.defaultSkin = value;
                }
            }
        }
        public void AddAnimation(AnimationData value)
        {
            if (value != null && !string.IsNullOrEmpty(value.name))
            {
                if (this.animations.ContainsKey(value.name))
                {
                    Helper.Assert(false, "Same animation: " + value.name);
                    this.animations[value.name].ReturnToPool();
                }

                value.parent = this;
                this.animations[value.name] = value;
                this.animationNames.Add(value.name);
                if (this.defaultAnimation == null)
                {
                    this.defaultAnimation = value;
                }
            }
        }
        internal void AddAction(ActionData value, bool isDefault)
        {
            if (isDefault)
            {
                this.defaultActions.Add(value);
            }
            else
            {
                this.actions.Add(value);
            }
        }


        public BoneData GetBone(string boneName)
        {
            return (!string.IsNullOrEmpty(boneName) && bones.ContainsKey(boneName)) ? bones[boneName] : null;
        }

        public SlotData GetSlot(string slotName)
        {
            return (!string.IsNullOrEmpty(slotName) && slots.ContainsKey(slotName)) ? slots[slotName] : null;
        }
        public ConstraintData GetConstraint(string constraintName)
        {
            return this.constraints.ContainsKey(constraintName) ? this.constraints[constraintName] : null;
        }

        public SkinData GetSkin(string skinName)
        {
            return !string.IsNullOrEmpty(skinName) ? (skins.ContainsKey(skinName) ? skins[skinName] : null) : defaultSkin;
        }

        public MeshDisplayData GetMesh(string skinName, string slotName, string meshName)
        {
            var skin = this.GetSkin(skinName);
            if (skin == null)
            {
                return null;
            }

            return skin.GetDisplay(slotName, meshName) as MeshDisplayData;
        }

        public AnimationData GetAnimation(string animationName)
        {
            return !string.IsNullOrEmpty(animationName) ? (animations.ContainsKey(animationName) ? animations[animationName] : null) : defaultAnimation;
        }
    }


    public class BoneData : BaseObject
    {
        public bool inheritTranslation;
        public bool inheritRotation;
        public bool inheritScale;
        public bool inheritReflection;

        public float length;

        public string name;
        public readonly Transform transform = new Transform();
        public UserData userData = null;

        public BoneData parent = null;

        protected override void _OnClear()
        {
            if (this.userData != null)
            {
                this.userData.ReturnToPool();
            }

            this.inheritTranslation = false;
            this.inheritRotation = false;
            this.inheritScale = false;
            this.inheritReflection = false;
            this.length = 0.0f;
            this.name = "";
            this.transform.Identity();
            this.userData = null;
            this.parent = null;
        }
    }

    public class SurfaceData : BoneData
    {
        public float vertexCountX;
        public float vertexCountY;
        public readonly List<float> vertices = new List<float>();
        protected override void _OnClear()
        {
            base._OnClear();

            this.vertexCountX = 0;
            this.vertexCountY = 0;
            this.vertices.Clear();
        }
    }


    public class SlotData : BaseObject
    {
        public static readonly ColorTransform DEFAULT_COLOR = new ColorTransform();

        public static ColorTransform CreateColor()
        {
            return new ColorTransform();
        }

        public BlendMode blendMode;
        public int displayIndex;
        public int zOrder;

        public string name;
        public ColorTransform color = null;
        public UserData userData = null;

        public BoneData parent;
        protected override void _OnClear()
        {
            if (this.userData != null)
            {
                this.userData.ReturnToPool();
            }

            this.blendMode = BlendMode.Normal;
            this.displayIndex = 0;
            this.zOrder = 0;
            this.name = "";
            this.color = null;
            this.userData = null;
            this.parent = null;
        }
    }
}