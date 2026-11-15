using System;
using System.Collections.Generic;

namespace DragonBones
{

    public class Armature : BaseObject, IAnimatable
    {
        private static int _OnSortSlots(Slot a, Slot b)
        {
            if (a._zOrder > b._zOrder)
            {
                return 1;
            }
            else if (a._zOrder < b._zOrder)
            {
                return -1;
            }

            return 0;
        }


        public bool inheritAnimation;
        public object userData;

        private bool _lockUpdate;
        private bool _slotsDirty;
        private bool _zOrderDirty;
        private bool _flipX;
        private bool _flipY;

        internal int _cacheFrameIndex;
        private readonly List<Bone> _bones = new List<Bone>();
        private readonly List<Slot> _slots = new List<Slot>();
        internal readonly List<Constraint> _constraints = new List<Constraint>();
        private readonly List<EventObject> _actions = new List<EventObject>();
        public ArmatureData _armatureData;
        private Animation _animation = null;
        private IArmatureProxy _proxy = null;
        private object _display;
        internal TextureAtlasData _replaceTextureAtlasData = null;
        private object _replacedTexture;
        internal DragonBones _dragonBones;
        private WorldClock _clock = null;

        internal Slot _parent;
        protected override void _OnClear()
        {
            if (this._clock != null)
            {
                this._clock.Remove(this);
            }

            foreach (var bone in this._bones)
            {
                bone.ReturnToPool();
            }

            foreach (var slot in this._slots)
            {
                slot.ReturnToPool();
            }

            foreach (var constraint in this._constraints)
            {
                constraint.ReturnToPool();
            }

            if (this._animation != null)
            {
                this._animation.ReturnToPool();
            }

            if (this._proxy != null)
            {
                this._proxy.DBClear();
            }

            if (this._replaceTextureAtlasData != null)
            {
                this._replaceTextureAtlasData.ReturnToPool();
            }

            this.inheritAnimation = true;
            this.userData = null;

            this._lockUpdate = false;
            this._slotsDirty = false;
            this._zOrderDirty = false;
            this._flipX = false;
            this._flipY = false;
            this._cacheFrameIndex = -1;
            this._bones.Clear();
            this._slots.Clear();
            this._constraints.Clear();
            this._actions.Clear();
            this._armatureData = null;
            this._animation = null;
            this._proxy = null;
            this._display = null;
            this._replaceTextureAtlasData = null;
            this._replacedTexture = null;
            this._dragonBones = null;
            this._clock = null;
            this._parent = null;
        }

        internal void _SortZOrder(short[] slotIndices, int offset)
        {
            var slotDatas = this._armatureData.sortedSlots;
            var isOriginal = slotIndices == null;

            if (this._zOrderDirty || !isOriginal)
            {
                for (int i = 0, l = slotDatas.Count; i < l; ++i)
                {
                    var slotIndex = isOriginal ? i : slotIndices[offset + i];
                    if (slotIndex < 0 || slotIndex >= l)
                    {
                        continue;
                    }

                    var slotData = slotDatas[slotIndex];
                    var slot = this.GetSlot(slotData.name);
                    if (slot != null)
                    {
                        slot._SetZorder(i);
                    }
                }

                this._slotsDirty = true;
                this._zOrderDirty = !isOriginal;
            }
        }
        internal void _AddBone(Bone value)
        {
            if (!this._bones.Contains(value))
            {
                this._bones.Add(value);
            }
        }
        internal void _AddSlot(Slot value)
        {
            if (!this._slots.Contains(value))
            {
                this._slotsDirty = true;
                this._slots.Add(value);
            }
        }

        internal void _AddConstraint(Constraint value)
        {
            if (!this._constraints.Contains(value))
            {
                this._constraints.Add(value);
            }
        }
        internal void _BufferAction(EventObject action, bool append)
        {
            if (!this._actions.Contains(action))
            {
                if (append)
                {
                    this._actions.Add(action);
                }
                else
                {
                    this._actions.Insert(0, action);
                }
            }
        }

        public void Dispose()
        {
            if (this._armatureData != null)
            {
                this._lockUpdate = true;

                if (this._dragonBones != null)
                {
                    this._dragonBones.BufferObject(this);
                }
            }
        }
        internal void Init(ArmatureData armatureData, IArmatureProxy proxy, object display, DragonBones dragonBones)
        {
            if (this._armatureData != null)
            {
                return;
            }

            this._armatureData = armatureData;
            this._animation = BaseObject.BorrowObject<Animation>();
            this._proxy = proxy;
            this._display = display;
            this._dragonBones = dragonBones;

            this._proxy.DBInit(this);
            this._animation.Init(this);
            this._animation.animations = this._armatureData.animations;
        }
        public void AdvanceTime(float passedTime)
        {
            if (this._lockUpdate)
            {
                return;
            }

            if (this._armatureData == null)
            {
                Helper.Assert(false, "The armature has been disposed.");
                return;
            }
            else if (this._armatureData.parent == null)
            {
                Helper.Assert(false, "The armature data has been disposed.\nPlease make sure dispose armature before call factory.clear().");
                return;
            }

            var prevCacheFrameIndex = this._cacheFrameIndex;

            this._animation.AdvanceTime(passedTime);

            if (this._slotsDirty)
            {
                this._slotsDirty = false;
                this._slots.Sort(Armature._OnSortSlots);
            }

            if (this._cacheFrameIndex < 0 || this._cacheFrameIndex != prevCacheFrameIndex)
            {
                int i = 0, l = 0;
                for (i = 0, l = this._bones.Count; i < l; ++i)
                {
                    this._bones[i].Update(this._cacheFrameIndex);
                }

                for (i = 0, l = this._slots.Count; i < l; ++i)
                {
                    this._slots[i].Update(this._cacheFrameIndex);
                }
            }

            if (this._actions.Count > 0)
            {
                this._lockUpdate = true;
                foreach (var action in this._actions)
                {
                    var actionData = action.actionData;
                    if (actionData != null)
                    {
                        if (actionData.type == ActionType.Play)
                        {
                            if (action.slot != null)
                            {
                                var childArmature = action.slot.childArmature;
                                if (childArmature != null)
                                {
                                    childArmature.animation.FadeIn(actionData.name);
                                }
                            }
                            else if (action.bone != null)
                            {
                                foreach (var slot in this.GetSlots())
                                {
                                    if (slot.parent == action.bone)
                                    {
                                        var childArmature = slot.childArmature;
                                        if (childArmature != null)
                                        {
                                            childArmature.animation.FadeIn(actionData.name);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                this._animation.FadeIn(actionData.name);
                            }
                        }
                    }

                    action.ReturnToPool();
                }

                this._actions.Clear();
                this._lockUpdate = false;
            }

            this._proxy.DBUpdate();
        }

        public void InvalidUpdate(string boneName = null, bool updateSlot = false)
        {
            if (!string.IsNullOrEmpty(boneName))
            {
                Bone bone = this.GetBone(boneName);
                if (bone != null)
                {
                    bone.InvalidUpdate();

                    if (updateSlot)
                    {
                        foreach (var slot in this._slots)
                        {
                            if (slot.parent == bone)
                            {
                                slot.InvalidUpdate();
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var bone in this._bones)
                {
                    bone.InvalidUpdate();
                }

                if (updateSlot)
                {
                    foreach (var slot in this._slots)
                    {
                        slot.InvalidUpdate();
                    }
                }
            }
        }

        public Slot ContainsPoint(float x, float y)
        {
            foreach (var slot in this._slots)
            {
                if (slot.ContainsPoint(x, y))
                {
                    return slot;
                }
            }

            return null;
        }

        public Slot IntersectsSegment(float xA, float yA, float xB, float yB,
                                       Point intersectionPointA = null,
                                       Point intersectionPointB = null,
                                       Point normalRadians = null)
        {
            var isV = xA == xB;
            var dMin = 0.0f;
            var dMax = 0.0f;
            var intXA = 0.0f;
            var intYA = 0.0f;
            var intXB = 0.0f;
            var intYB = 0.0f;
            var intAN = 0.0f;
            var intBN = 0.0f;
            Slot intSlotA = null;
            Slot intSlotB = null;

            foreach (var slot in this._slots)
            {
                var intersectionCount = slot.IntersectsSegment(xA, yA, xB, yB, intersectionPointA, intersectionPointB, normalRadians);
                if (intersectionCount > 0)
                {
                    if (intersectionPointA != null || intersectionPointB != null)
                    {
                        if (intersectionPointA != null)
                        {
                            var d = isV ? intersectionPointA.y - yA : intersectionPointA.x - xA;
                            if (d < 0.0f)
                            {
                                d = -d;
                            }

                            if (intSlotA == null || d < dMin)
                            {
                                dMin = d;
                                intXA = intersectionPointA.x;
                                intYA = intersectionPointA.y;
                                intSlotA = slot;

                                if (normalRadians != null)
                                {
                                    intAN = normalRadians.x;
                                }
                            }
                        }

                        if (intersectionPointB != null)
                        {
                            var d = intersectionPointB.x - xA;
                            if (d < 0.0f)
                            {
                                d = -d;
                            }

                            if (intSlotB == null || d > dMax)
                            {
                                dMax = d;
                                intXB = intersectionPointB.x;
                                intYB = intersectionPointB.y;
                                intSlotB = slot;

                                if (normalRadians != null)
                                {
                                    intBN = normalRadians.y;
                                }
                            }
                        }
                    }
                    else
                    {
                        intSlotA = slot;
                        break;
                    }
                }
            }

            if (intSlotA != null && intersectionPointA != null)
            {
                intersectionPointA.x = intXA;
                intersectionPointA.y = intYA;

                if (normalRadians != null)
                {
                    normalRadians.x = intAN;
                }
            }

            if (intSlotB != null && intersectionPointB != null)
            {
                intersectionPointB.x = intXB;
                intersectionPointB.y = intYB;

                if (normalRadians != null)
                {
                    normalRadians.y = intBN;
                }
            }

            return intSlotA;
        }

        public Bone GetBone(string name)
        {
            foreach (var bone in this._bones)
            {
                if (bone.name == name)
                {
                    return bone;
                }
            }

            return null;
        }

        public Bone GetBoneByDisplay(object display)
        {
            var slot = this.GetSlotByDisplay(display);

            return slot != null ? slot.parent : null;
        }

        public Slot GetSlot(string name)
        {
            foreach (var slot in this._slots)
            {
                if (slot.name == name)
                {
                    return slot;
                }
            }

            return null;
        }

        public Slot GetSlotByDisplay(object display)
        {
            if (display != null)
            {
                foreach (var slot in this._slots)
                {
                    if (slot.display == display)
                    {
                        return slot;
                    }
                }
            }

            return null;
        }

        public List<Bone> GetBones()
        {
            return this._bones;
        }

        public List<Slot> GetSlots()
        {
            return this._slots;
        }


        public bool flipX
        {
            get { return this._flipX; }
            set
            {
                if (this._flipX == value)
                {
                    return;
                }

                this._flipX = value;
                this.InvalidUpdate();
            }
        }

        public bool flipY
        {
            get { return this._flipY; }
            set
            {
                if (this._flipY == value)
                {
                    return;
                }

                this._flipY = value;
                this.InvalidUpdate();
            }
        }

        public uint cacheFrameRate
        {
            get { return this._armatureData.cacheFrameRate; }
            set
            {
                if (this._armatureData.cacheFrameRate != value)
                {
                    this._armatureData.CacheFrames(value);

                    foreach (var slot in this._slots)
                    {
                        var childArmature = slot.childArmature;
                        if (childArmature != null)
                        {
                            childArmature.cacheFrameRate = value;
                        }
                    }
                }
            }
        }

        public string name
        {
            get { return this._armatureData.name; }
        }

        public ArmatureData armatureData
        {
            get { return this._armatureData; }
        }

        public Animation animation
        {
            get { return this._animation; }
        }
        public IArmatureProxy proxy
        {
            get { return this._proxy; }
        }


        public IEventDispatcher<EventObject> eventDispatcher
        {
            get { return this._proxy; }
        }

        public object display
        {
            get { return this._display; }
        }
        public object replacedTexture
        {
            get { return this._replacedTexture; }
            set
            {
                if (this._replacedTexture == value)
                {
                    return;
                }

                if (this._replaceTextureAtlasData != null)
                {
                    this._replaceTextureAtlasData.ReturnToPool();
                    this._replaceTextureAtlasData = null;
                }

                this._replacedTexture = value;

                foreach (var slot in this._slots)
                {
                    slot.InvalidUpdate();
                    slot.Update(-1);
                }
            }
        }
        public WorldClock clock
        {
            get { return this._clock; }
            set
            {
                if (this._clock == value)
                {
                    return;
                }

                if (this._clock != null)
                {
                    this._clock.Remove(this);
                }

                this._clock = value;

                if (this._clock != null)
                {
                    this._clock.Add(this);
                }

                foreach (var slot in this._slots)
                {
                    var childArmature = slot.childArmature;
                    if (childArmature != null)
                    {
                        childArmature.clock = this._clock;
                    }
                }
            }
        }

        public Slot parent
        {
            get { return this._parent; }
        }
    }
}
