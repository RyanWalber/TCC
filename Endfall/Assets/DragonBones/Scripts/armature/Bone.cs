using System;
using System.Collections.Generic;

namespace DragonBones
{

    public class Bone : TransformObject
    {

        internal OffsetMode offsetMode;
        internal readonly Transform animationPose = new Transform();
        internal bool _transformDirty;
        internal bool _childrenTransformDirty;
        private bool _localDirty;

        internal bool _hasConstraint;
        private bool _visible;
        private int _cachedFrameIndex;
        internal readonly BlendState _blendState = new BlendState();
        internal BoneData _boneData;
        protected Bone _parent;
        internal List<int> _cachedFrameIndices = new List<int>();
        protected override void _OnClear()
        {
            base._OnClear();

            this.offsetMode = OffsetMode.Additive;
            this.animationPose.Identity();

            this._transformDirty = false;
            this._childrenTransformDirty = false;
            this._localDirty = true;
            this._hasConstraint = false;
            this._visible = true;
            this._cachedFrameIndex = -1;
            this._blendState.Clear();
            this._boneData = null;
            this._parent = null;
            this._cachedFrameIndices = null;
        }
        private void _UpdateGlobalTransformMatrix(bool isCache)
        {
            var boneData = this._boneData;
            var parent = this._parent;
            var flipX = this._armature.flipX;
            var flipY = this._armature.flipY == DragonBones.yDown;
            var rotation = 0.0f;
            var global = this.global;
            var inherit = parent != null;
            var globalTransformMatrix = this.globalTransformMatrix;

            if (this.offsetMode == OffsetMode.Additive)
            {
                if (this.origin != null)
                {
                    global.x = this.origin.x + this.offset.x + this.animationPose.x;
                    global.y = this.origin.y + this.offset.y + this.animationPose.y;
                    global.skew = this.origin.skew + this.offset.skew + this.animationPose.skew;
                    global.rotation = this.origin.rotation + this.offset.rotation + this.animationPose.rotation;
                    global.scaleX = this.origin.scaleX * this.offset.scaleX * this.animationPose.scaleX;
                    global.scaleY = this.origin.scaleY * this.offset.scaleY * this.animationPose.scaleY;
                }
                else
                {
                    global.CopyFrom(this.offset).Add(this.animationPose);
                }

            }
            else if (this.offsetMode == OffsetMode.None)
            {
                if (this.origin != null)
                {
                    global.CopyFrom(this.origin).Add(this.animationPose);
                }
                else
                {
                    global.CopyFrom(this.animationPose);
                }
            }
            else
            {
                inherit = false;
                global.CopyFrom(this.offset);
            }

            if (inherit)
            {
                var parentMatrix = parent.globalTransformMatrix;
                if (boneData.inheritScale)
                {
                    if (!boneData.inheritRotation)
                    {
                        parent.UpdateGlobalTransform();

                        if (flipX && flipY)
                        {
                            rotation = global.rotation - (parent.global.rotation + Transform.PI);
                        }
                        else if (flipX)
                        {
                            rotation = global.rotation + parent.global.rotation + Transform.PI;
                        }
                        else if (flipY)
                        {
                            rotation = global.rotation + parent.global.rotation;
                        }
                        else
                        {
                            rotation = global.rotation - parent.global.rotation;
                        }

                        global.rotation = rotation;
                    }

                    global.ToMatrix(globalTransformMatrix);
                    globalTransformMatrix.Concat(parentMatrix);

                    if (this._boneData.inheritTranslation)
                    {
                        global.x = globalTransformMatrix.tx;
                        global.y = globalTransformMatrix.ty;
                    }
                    else
                    {
                        globalTransformMatrix.tx = global.x;
                        globalTransformMatrix.ty = global.y;
                    }

                    if (isCache)
                    {
                        global.FromMatrix(globalTransformMatrix);
                    }
                    else
                    {
                        this._globalDirty = true;
                    }
                }
                else
                {
                    if (boneData.inheritTranslation)
                    {
                        var x = global.x;
                        var y = global.y;
                        global.x = parentMatrix.a * x + parentMatrix.c * y + parentMatrix.tx;
                        global.y = parentMatrix.b * x + parentMatrix.d * y + parentMatrix.ty;
                    }
                    else
                    {
                        if (flipX)
                        {
                            global.x = -global.x;
                        }

                        if (flipY)
                        {
                            global.y = -global.y;
                        }
                    }

                    if (boneData.inheritRotation)
                    {
                        parent.UpdateGlobalTransform();
                        if (parent.global.scaleX < 0.0)
                        {
                            rotation = global.rotation + parent.global.rotation + Transform.PI;
                        }
                        else
                        {
                            rotation = global.rotation + parent.global.rotation;
                        }

                        if (parentMatrix.a * parentMatrix.d - parentMatrix.b * parentMatrix.c < 0.0)
                        {
                            rotation -= global.rotation * 2.0f;

                            if (flipX != flipY || boneData.inheritReflection)
                            {
                                global.skew += Transform.PI;
                            }
                        }

                        global.rotation = rotation;
                    }
                    else if (flipX || flipY)
                    {
                        if (flipX && flipY)
                        {
                            rotation = global.rotation + Transform.PI;
                        }
                        else
                        {
                            if (flipX)
                            {
                                rotation = Transform.PI - global.rotation;
                            }
                            else
                            {
                                rotation = -global.rotation;
                            }

                            global.skew += Transform.PI;
                        }

                        global.rotation = rotation;
                    }

                    global.ToMatrix(globalTransformMatrix);
                }
            }
            else
            {
                if (flipX || flipY)
                {
                    if (flipX)
                    {
                        global.x = -global.x;
                    }

                    if (flipY)
                    {
                        global.y = -global.y;
                    }

                    if (flipX && flipY)
                    {
                        rotation = global.rotation + Transform.PI;
                    }
                    else
                    {
                        if (flipX)
                        {
                            rotation = Transform.PI - global.rotation;
                        }
                        else
                        {
                            rotation = -global.rotation;
                        }

                        global.skew += Transform.PI;
                    }

                    global.rotation = rotation;
                }

                global.ToMatrix(globalTransformMatrix);
            }
        }
        internal void Init(BoneData boneData, Armature armatureValue)
        {
            if (this._boneData != null)
            {
                return;
            }

            this._boneData = boneData;
            this._armature = armatureValue;

            if (this._boneData.parent != null)
            {
                this._parent = this._armature.GetBone(this._boneData.parent.name);
            }

            this._armature._AddBone(this);
            this.origin = this._boneData.transform;
        }
        internal void Update(int cacheFrameIndex)
        {
            this._blendState.dirty = false;

            if (cacheFrameIndex >= 0 && this._cachedFrameIndices != null)
            {
                var cachedFrameIndex = this._cachedFrameIndices[cacheFrameIndex];

                if (cachedFrameIndex >= 0 && this._cachedFrameIndex == cachedFrameIndex)
                {
                    this._transformDirty = false;
                }
                else if (cachedFrameIndex >= 0)
                {
                    this._transformDirty = true;
                    this._cachedFrameIndex = cachedFrameIndex;
                }
                else
                {
                    if (this._hasConstraint)
                    {
                        foreach (var constraint in this._armature._constraints)
                        {
                            if (constraint._root == this)
                            {
                                constraint.Update();
                            }
                        }
                    }

                    if (this._transformDirty || (this._parent != null && this._parent._childrenTransformDirty))
                    {
                        this._transformDirty = true;
                        this._cachedFrameIndex = -1;
                    }
                    else if (this._cachedFrameIndex >= 0)
                    {
                        this._transformDirty = false;
                        this._cachedFrameIndices[cacheFrameIndex] = this._cachedFrameIndex;
                    }
                    else
                    {
                        this._transformDirty = true;
                        this._cachedFrameIndex = -1;
                    }
                }
            }
            else
            {
                if (this._hasConstraint)
                {
                    foreach (var constraint in this._armature._constraints)
                    {
                        if (constraint._root == this)
                        {
                            constraint.Update();
                        }
                    }
                }

                if (this._transformDirty || (this._parent != null && this._parent._childrenTransformDirty))
                {
                    cacheFrameIndex = -1;
                    this._transformDirty = true;
                    this._cachedFrameIndex = -1;
                }
            }

            if (this._transformDirty)
            {
                this._transformDirty = false;
                this._childrenTransformDirty = true;

                if (this._cachedFrameIndex < 0)
                {
                    var isCache = cacheFrameIndex >= 0;
                    if (this._localDirty)
                    {
                        this._UpdateGlobalTransformMatrix(isCache);
                    }

                    if (isCache && this._cachedFrameIndices != null)
                    {
                        this._cachedFrameIndex = this._cachedFrameIndices[cacheFrameIndex] = this._armature._armatureData.SetCacheFrame(this.globalTransformMatrix, this.global);
                    }
                }
                else
                {
                    this._armature._armatureData.GetCacheFrame(this.globalTransformMatrix, this.global, this._cachedFrameIndex);
                }
            }
            else if (this._childrenTransformDirty)
            {
                this._childrenTransformDirty = false;
            }

            this._localDirty = true;
        }
        internal void UpdateByConstraint()
        {
            if (this._localDirty)
            {
                this._localDirty = false;

                if (this._transformDirty || (this._parent != null && this._parent._childrenTransformDirty))
                {
                    this._UpdateGlobalTransformMatrix(true);
                }

                this._transformDirty = true;
            }
        }

        public void InvalidUpdate()
        {
            this._transformDirty = true;
        }

        public bool Contains(Bone value)
        {
            if (value == this)
            {
                return false;
            }

            Bone ancestor = value;
            while (ancestor != this && ancestor != null)
            {
                ancestor = ancestor.parent;
            }

            return ancestor == this;
        }

        public BoneData boneData
        {
            get { return this._boneData; }
        }


        public bool visible
        {
            get { return this._visible; }
            set
            {
                if (this._visible == value)
                {
                    return;
                }

                this._visible = value;

                foreach (var slot in this._armature.GetSlots())
                {
                    if (slot.parent == this)
                    {
                        slot._UpdateVisible();
                    }
                }
            }
        }


        public string name
        {
            get { return this._boneData.name; }
        }


        public Bone parent
        {
            get { return this._parent; }
        }

        [System.Obsolete("")]
        public Slot slot
        {
            get
            {
                foreach (var slot in this._armature.GetSlots())
                {
                    if (slot.parent == this)
                    {
                        return slot;
                    }
                }

                return null;
            }
        }
    }
}
