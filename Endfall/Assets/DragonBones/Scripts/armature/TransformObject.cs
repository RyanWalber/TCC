using System;
using System.Collections.Generic;
using System.Text;

namespace DragonBones
{

    public abstract class TransformObject : BaseObject
    {
        protected static readonly Matrix _helpMatrix  = new Matrix();
        protected static readonly Transform _helpTransform  = new Transform();
        protected static readonly Point _helpPoint = new Point();

        public readonly Matrix globalTransformMatrix = new Matrix();

        public readonly Transform global = new Transform();

        public readonly Transform offset = new Transform();
        public Transform origin;
        public object userData;
        protected bool _globalDirty;
        internal Armature _armature;
        protected override void _OnClear()
        {
            this.globalTransformMatrix.Identity();
            this.global.Identity();
            this.offset.Identity();
            this.origin = null;
            this.userData = null;

            this._globalDirty = false;
            this._armature = null;
        }

        public void UpdateGlobalTransform()
        {
            if (this._globalDirty)
            {
                this._globalDirty = false;
                this.global.FromMatrix(this.globalTransformMatrix);
            }
        }

        public Armature armature
        {
            get{ return this._armature; }
        }
    }
}
