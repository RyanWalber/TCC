using System;
using System.Collections.Generic;

namespace DragonBones
{

    public abstract class Slot : TransformObject
    {

        public string displayController;
        protected bool _displayDirty;
        protected bool _zOrderDirty;
        protected bool _visibleDirty;
        protected bool _blendModeDirty;
        internal bool _colorDirty;
        internal bool _transformDirty;
        protected bool _visible;
        internal BlendMode _blendMode;
        protected int _displayIndex;
        protected int _animationDisplayIndex;
        internal int _zOrder;
        protected int _cachedFrameIndex;
        internal float _pivotX;
        internal float _pivotY;
        protected readonly Matrix _localMatrix = new Matrix();
        internal readonly ColorTransform _colorTransform = new ColorTransform();
        internal readonly List<DisplayData> _displayDatas = new List<DisplayData>();
        protected readonly List<object> _displayList = new List<object>();
        internal SlotData _slotData;
        protected List<DisplayData> _rawDisplayDatas;
        protected DisplayData _displayData;
        protected BoundingBoxData _boundingBoxData;
        protected TextureData _textureData;
        public DeformVertices _deformVertices;
        protected object _rawDisplay;
        protected object _meshDisplay;
        protected object _display;
        protected Armature _childArmature;
        protected Bone _parent;
        internal List<int> _cachedFrameIndices = new List<int>();
        public Slot()
        {
        }
        protected override void _OnClear()
        {
            base._OnClear();

            var disposeDisplayList = new List<object>();
            for (int i = 0, l = _displayList.Count; i < l; ++i)
            {
                var eachDisplay = _displayList[i];
                if (eachDisplay != _rawDisplay && eachDisplay != _meshDisplay && !disposeDisplayList.Contains(eachDisplay))
                {
                    disposeDisplayList.Add(eachDisplay);
                }
            }

            for (int i = 0, l = disposeDisplayList.Count; i < l; ++i)
            {
                var eachDisplay = disposeDisplayList[i];
                if (eachDisplay is Armature)
                {
                    (eachDisplay as Armature).Dispose();
                }
                else
                {
                    this._DisposeDisplay(eachDisplay, true);
                }
            }

            if (this._deformVertices != null)
            {
                this._deformVertices.ReturnToPool();
            }

            if (this._meshDisplay != null && this._meshDisplay != this._rawDisplay)
            {
                this._DisposeDisplay(this._meshDisplay, false);
            }

            if (this._rawDisplay != null)
            {
                this._DisposeDisplay(this._rawDisplay, false);
            }

            this.displayController = null;

            this._displayDirty = false;
            this._zOrderDirty = false;
            this._blendModeDirty = false;
            this._colorDirty = false;
            this._transformDirty = false;
            this._visible = true;
            this._blendMode = BlendMode.Normal;
            this._displayIndex = -1;
            this._animationDisplayIndex = -1;
            this._zOrder = 0;
            this._cachedFrameIndex = -1;
            this._pivotX = 0.0f;
            this._pivotY = 0.0f;
            this._localMatrix.Identity();
            this._colorTransform.Identity();
            this._displayList.Clear();
            this._displayDatas.Clear();
            this._slotData = null;
            this._rawDisplayDatas = null;
            this._displayData = null;
            this._boundingBoxData = null;
            this._textureData = null;
            this._deformVertices = null;
            this._rawDisplay = null;
            this._meshDisplay = null;
            this._display = null;
            this._childArmature = null;
            this._parent = null;
            this._cachedFrameIndices = null;
        }

        protected abstract void _InitDisplay(object value, bool isRetain);
        protected abstract void _DisposeDisplay(object value, bool isRelease);
        protected abstract void _OnUpdateDisplay();
        protected abstract void _AddDisplay();
        protected abstract void _ReplaceDisplay(object value);
        protected abstract void _RemoveDisplay();
        protected abstract void _UpdateZOrder();
        internal abstract void _UpdateVisible();
        internal abstract void _UpdateBlendMode();
        protected abstract void _UpdateColor();
        protected abstract void _UpdateFrame();
        protected abstract void _UpdateMesh();
        protected abstract void _UpdateTransform();
        protected abstract void _IdentityTransform();

        protected DisplayData _GetDefaultRawDisplayData(int displayIndex)
        {
            var defaultSkin = this._armature._armatureData.defaultSkin;
            if (defaultSkin != null)
            {
                var defaultRawDisplayDatas = defaultSkin.GetDisplays(this._slotData.name);
                if (defaultRawDisplayDatas != null)
                {
                    return displayIndex < defaultRawDisplayDatas.Count ? defaultRawDisplayDatas[displayIndex] : null;
                }
            }

            return null;
        }

        protected void _UpdateDisplayData()
        {
            var prevDisplayData = this._displayData;
            var prevVerticesData = this._deformVertices != null ? this._deformVertices.verticesData : null;
            var prevTextureData = this._textureData;

            DisplayData rawDisplayData = null;
            VerticesData currentVerticesData = null;

            this._displayData = null;
            this._boundingBoxData = null;
            this._textureData = null;

            if (this._displayIndex >= 0)
            {
                if (this._rawDisplayDatas != null)
                {
                    rawDisplayData = this._displayIndex < this._rawDisplayDatas.Count ? this._rawDisplayDatas[this._displayIndex] : null;
                }

                if (rawDisplayData == null)
                {
                    rawDisplayData = this._GetDefaultRawDisplayData(this._displayIndex);
                }

                if (this._displayIndex < this._displayDatas.Count)
                {
                    this._displayData = this._displayDatas[this._displayIndex];
                }
            }

            if (this._displayData != null)
            {
                if (this._displayData.type == DisplayType.Mesh)
                {
                    currentVerticesData = (this._displayData as MeshDisplayData).vertices;
                }
                else if (this._displayData.type == DisplayType.Path)
                {
                    currentVerticesData = (this._displayData as PathDisplayData).vertices;
                }
                else if (rawDisplayData != null)
                {
                    if (rawDisplayData.type == DisplayType.Mesh)
                    {
                        currentVerticesData = (rawDisplayData as MeshDisplayData).vertices;
                    }
                    else if (rawDisplayData.type == DisplayType.Path)
                    {
                        currentVerticesData = (rawDisplayData as PathDisplayData).vertices;
                    }
                }

                if (this._displayData.type == DisplayType.BoundingBox)
                {
                    this._boundingBoxData = (this._displayData as BoundingBoxDisplayData).boundingBox;
                }
                else if (rawDisplayData != null)
                {
                    if (rawDisplayData.type == DisplayType.BoundingBox)
                    {
                        this._boundingBoxData = (rawDisplayData as BoundingBoxDisplayData).boundingBox;
                    }
                }

                if (this._displayData.type == DisplayType.Image)
                {
                    this._textureData = (this._displayData as ImageDisplayData).texture;
                }
                else if (this._displayData.type == DisplayType.Mesh)
                {
                    this._textureData = (this._displayData as MeshDisplayData).texture;
                }
            }

            if (this._displayData != prevDisplayData || currentVerticesData != prevVerticesData || this._textureData != prevTextureData)
            {
                if (currentVerticesData == null && this._textureData != null)
                {
                    var imageDisplayData = this._displayData as ImageDisplayData;
                    var scale = this._textureData.parent.scale * this._armature._armatureData.scale;
                    var frame = this._textureData.frame;

                    this._pivotX = imageDisplayData.pivot.x;
                    this._pivotY = imageDisplayData.pivot.y;

                    var rect = frame != null ? frame : this._textureData.region;
                    var width = rect.width;
                    var height = rect.height;

                    if (this._textureData.rotated && frame == null)
                    {
                        width = rect.height;
                        height = rect.width;
                    }

                    this._pivotX *= width * scale;
                    this._pivotY *= height * scale;

                    if (frame != null)
                    {
                        this._pivotX += frame.x * scale;
                        this._pivotY += frame.y * scale;
                    }

                    if (this._displayData != null && rawDisplayData != null && this._displayData != rawDisplayData)
                    {
                        rawDisplayData.transform.ToMatrix(Slot._helpMatrix);
                        Slot._helpMatrix.Invert();
                        Slot._helpMatrix.TransformPoint(0.0f, 0.0f, Slot._helpPoint);
                        this._pivotX -= Slot._helpPoint.x;
                        this._pivotY -= Slot._helpPoint.y;

                        this._displayData.transform.ToMatrix(Slot._helpMatrix);
                        Slot._helpMatrix.Invert();
                        Slot._helpMatrix.TransformPoint(0.0f, 0.0f, Slot._helpPoint);
                        this._pivotX += Slot._helpPoint.x;
                        this._pivotY += Slot._helpPoint.y;
                    }

                    if (!DragonBones.yDown)
                    {
                        this._pivotY = (this._textureData.rotated ? this._textureData.region.width : this._textureData.region.height) * scale - this._pivotY;
                    }
                }
                else
                {
                    this._pivotX = 0.0f;
                    this._pivotY = 0.0f;
                }

                if (rawDisplayData != null)
                {
                    this.origin = rawDisplayData.transform;
                }
                else if (this._displayData != null)
                {
                    this.origin = this._displayData.transform;
                }
                else
                {
                    this.origin = null;
                }

                if (currentVerticesData != prevVerticesData)
                {
                    if (this._deformVertices == null)
                    {
                        this._deformVertices = BaseObject.BorrowObject<DeformVertices>();
                    }

                    this._deformVertices.init(currentVerticesData, this._armature);
                }
                else if (this._deformVertices != null && this._textureData != prevTextureData)
                {
                    this._deformVertices.verticesDirty = true;
                }

                this._displayDirty = true;
                this._transformDirty = true;
            }
        }

        protected void _UpdateDisplay()
        {
            var prevDisplay = this._display != null ? this._display : this._rawDisplay;
            var prevChildArmature = this._childArmature;

            if (this._displayIndex >= 0 && this._displayIndex < this._displayList.Count)
            {
                this._display = this._displayList[this._displayIndex];
                if (this._display != null && this._display is Armature)
                {
                    this._childArmature = this._display as Armature;
                    this._display = this._childArmature.display;
                }
                else
                {
                    this._childArmature = null;
                }
            }
            else
            {
                this._display = null;
                this._childArmature = null;
            }

            var currentDisplay = this._display != null ? this._display : this._rawDisplay;
            if (currentDisplay != prevDisplay)
            {
                this._OnUpdateDisplay();
                this._ReplaceDisplay(prevDisplay);

                this._transformDirty = true;
                this._visibleDirty = true;
                this._blendModeDirty = true;
                this._colorDirty = true;
            }

            if (currentDisplay == this._rawDisplay || currentDisplay == this._meshDisplay)
            {
                this._UpdateFrame();
            }

            if (this._childArmature != prevChildArmature)
            {
                if (prevChildArmature != null)
                {
                    prevChildArmature._parent = null;
                    prevChildArmature.clock = null;
                    if (prevChildArmature.inheritAnimation)
                    {
                        prevChildArmature.animation.Reset();
                    }
                }

                if (this._childArmature != null)
                {
                    this._childArmature._parent = this;
                    this._childArmature.clock = this._armature.clock;
                    if (this._childArmature.inheritAnimation)
                    {
                        if (this._childArmature.cacheFrameRate == 0)
                        {
                            var cacheFrameRate = this._armature.cacheFrameRate;
                            if (cacheFrameRate != 0)
                            {
                                this._childArmature.cacheFrameRate = cacheFrameRate;
                            }
                        }

                        List<ActionData> actions = null;
                        if (this._displayData != null && this._displayData.type == DisplayType.Armature)
                        {
                            actions = (this._displayData as ArmatureDisplayData).actions;
                        }
                        else if (this._displayIndex >= 0 && this._rawDisplayDatas != null)
                        {
                            var rawDisplayData = this._displayIndex < this._rawDisplayDatas.Count ? this._rawDisplayDatas[this._displayIndex] : null;

                            if (rawDisplayData == null)
                            {
                                rawDisplayData = this._GetDefaultRawDisplayData(this._displayIndex);
                            }

                            if (rawDisplayData != null && rawDisplayData.type == DisplayType.Armature)
                            {
                                actions = (rawDisplayData as ArmatureDisplayData).actions;
                            }
                        }

                        if (actions != null && actions.Count > 0)
                        {
                            foreach (var action in actions)
                            {
                                var eventObject = BaseObject.BorrowObject<EventObject>();
                                EventObject.ActionDataToInstance(action, eventObject, this._armature);
                                eventObject.slot = this;
                                this._armature._BufferAction(eventObject, false);
                            }
                        }
                        else
                        {
                            this._childArmature.animation.Play();
                        }
                    }
                }
            }
        }

        protected void _UpdateGlobalTransformMatrix(bool isCache)
        {
            this.globalTransformMatrix.CopyFrom(this._localMatrix);
            this.globalTransformMatrix.Concat(this._parent.globalTransformMatrix);
            if (isCache)
            {
                this.global.FromMatrix(this.globalTransformMatrix);
            }
            else
            {
                this._globalDirty = true;
            }
        }
        internal bool _SetDisplayIndex(int value, bool isAnimation = false)
        {
            if (isAnimation)
            {
                if (this._animationDisplayIndex == value)
                {
                    return false;
                }

                this._animationDisplayIndex = value;
            }

            if (this._displayIndex == value)
            {
                return false;
            }

            this._displayIndex = value;
            this._displayDirty = true;

            this._UpdateDisplayData();

            return this._displayDirty;
        }

        internal bool _SetZorder(int value)
        {
            if (this._zOrder == value)
            {
            }

            this._zOrder = value;
            this._zOrderDirty = true;

            return this._zOrderDirty;
        }

        internal bool _SetColor(ColorTransform value)
        {
            this._colorTransform.CopyFrom(value);
            this._colorDirty = true;

            return this._colorDirty;
        }
        internal bool _SetDisplayList(List<object> value)
        {
            if (value != null && value.Count > 0)
            {
                if (this._displayList.Count != value.Count)
                {
                    this._displayList.ResizeList(value.Count);
                }

                for (int i = 0, l = value.Count; i < l; ++i)
                {
                    var eachDisplay = value[i];
                    if (eachDisplay != null &&
                        eachDisplay != this._rawDisplay &&
                        eachDisplay != this._meshDisplay &&
                        !(eachDisplay is Armature) && this._displayList.IndexOf(eachDisplay) < 0)
                    {
                        this._InitDisplay(eachDisplay, true);
                    }

                    this._displayList[i] = eachDisplay;
                }
            }
            else if (this._displayList.Count > 0)
            {
                this._displayList.Clear();
            }

            if (this._displayIndex >= 0 && this._displayIndex < this._displayList.Count)
            {
                this._displayDirty = this._display != this._displayList[this._displayIndex];
            }
            else
            {
                this._displayDirty = this._display != null;
            }

            this._UpdateDisplayData();

            return this._displayDirty;
        }

        internal virtual void Init(SlotData slotData, Armature armatureValue, object rawDisplay, object meshDisplay)
        {
            if (this._slotData != null)
            {
                return;
            }

            this._slotData = slotData;
            this._visibleDirty = true;
            this._blendModeDirty = true;
            this._colorDirty = true;
            this._blendMode = this._slotData.blendMode;
            this._zOrder = this._slotData.zOrder;
            this._colorTransform.CopyFrom(this._slotData.color);
            this._rawDisplay = rawDisplay;
            this._meshDisplay = meshDisplay;

            this._armature = armatureValue;

            var slotParent = this._armature.GetBone(this._slotData.parent.name);
            if (slotParent != null)
            {
                this._parent = slotParent;
            }
            else
            {
            }

            this._armature._AddSlot(this);

            this._InitDisplay(this._rawDisplay, false);
            if (this._rawDisplay != this._meshDisplay)
            {
                this._InitDisplay(this._meshDisplay, false);
            }

            this._OnUpdateDisplay();
            this._AddDisplay();

        }

        internal void Update(int cacheFrameIndex)
        {
            if (this._displayDirty)
            {
                this._displayDirty = false;
                this._UpdateDisplay();

                if (this._transformDirty)
                {
                    if (this.origin != null)
                    {
                        this.global.CopyFrom(this.origin).Add(this.offset).ToMatrix(this._localMatrix);
                    }
                    else
                    {
                        this.global.CopyFrom(this.offset).ToMatrix(this._localMatrix);
                    }
                }
            }

            if (this._zOrderDirty)
            {
                this._zOrderDirty = false;
                this._UpdateZOrder();
            }

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
                else if (this._transformDirty || this._parent._childrenTransformDirty)
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
            else if (this._transformDirty || this._parent._childrenTransformDirty)
            {
                cacheFrameIndex = -1;
                this._transformDirty = true;
                this._cachedFrameIndex = -1;
            }

            if (this._display == null)
            {
                return;
            }

            if (this._visibleDirty)
            {
                this._visibleDirty = false;
                this._UpdateVisible();
            }

            if (this._blendModeDirty)
            {
                this._blendModeDirty = false;
                this._UpdateBlendMode();
            }

            if (this._colorDirty)
            {
                this._colorDirty = false;
                this._UpdateColor();
            }

            if (this._deformVertices != null && this._deformVertices.verticesData != null && this._display == this._meshDisplay)
            {
                var isSkinned = this._deformVertices.verticesData.weight != null;

                if (this._deformVertices.verticesDirty ||
                    (isSkinned && this._deformVertices.isBonesUpdate()))
                {
                    this._deformVertices.verticesDirty = false;
                    this._UpdateMesh();
                }

                if (isSkinned)
                {
                    return;
                }
            }

            if (this._transformDirty)
            {
                this._transformDirty = false;

                if (this._cachedFrameIndex < 0)
                {
                    var isCache = cacheFrameIndex >= 0;
                    this._UpdateGlobalTransformMatrix(isCache);

                    if (isCache && this._cachedFrameIndices != null)
                    {
                        this._cachedFrameIndex = this._cachedFrameIndices[cacheFrameIndex] = this._armature._armatureData.SetCacheFrame(this.globalTransformMatrix, this.global);
                    }
                }
                else
                {
                    this._armature._armatureData.GetCacheFrame(this.globalTransformMatrix, this.global, this._cachedFrameIndex);
                }

                this._UpdateTransform();
            }
        }

        public void UpdateTransformAndMatrix()
        {
            if (this._transformDirty)
            {
                this._transformDirty = false;
                this._UpdateGlobalTransformMatrix(false);
            }
        }

        internal void ReplaceDisplayData(DisplayData value, int displayIndex = -1)
        {
            if (displayIndex < 0)
            {
                if (this._displayIndex < 0)
                {
                    displayIndex = 0;
                }
                else
                {
                    displayIndex = this._displayIndex;
                }
            }

            if (this._displayDatas.Count <= displayIndex)
            {
                this._displayDatas.ResizeList(displayIndex + 1);

                for (int i = 0, l = this._displayDatas.Count; i < l; ++i)
                {
                    this._displayDatas[i] = null;
                }
            }

            this._displayDatas[displayIndex] = value;
        }


        public bool ContainsPoint(float x, float y)
        {
            if (this._boundingBoxData == null)
            {
                return false;
            }

            this.UpdateTransformAndMatrix();

            Slot._helpMatrix.CopyFrom(this.globalTransformMatrix);
            Slot._helpMatrix.Invert();
            Slot._helpMatrix.TransformPoint(x, y, Slot._helpPoint);

            return this._boundingBoxData.ContainsPoint(Slot._helpPoint.x, Slot._helpPoint.y);
        }


        public int IntersectsSegment(float xA, float yA, float xB, float yB,
                                    Point intersectionPointA = null,
                                    Point intersectionPointB = null,
                                    Point normalRadians = null)
        {
            if (this._boundingBoxData == null)
            {
                return 0;
            }

            this.UpdateTransformAndMatrix();
            Slot._helpMatrix.CopyFrom(this.globalTransformMatrix);
            Slot._helpMatrix.Invert();
            Slot._helpMatrix.TransformPoint(xA, yA, Slot._helpPoint);
            xA = Slot._helpPoint.x;
            yA = Slot._helpPoint.y;
            Slot._helpMatrix.TransformPoint(xB, yB, Slot._helpPoint);
            xB = Slot._helpPoint.x;
            yB = Slot._helpPoint.y;

            var intersectionCount = this._boundingBoxData.IntersectsSegment(xA, yA, xB, yB, intersectionPointA, intersectionPointB, normalRadians);
            if (intersectionCount > 0)
            {
                if (intersectionCount == 1 || intersectionCount == 2)
                {
                    if (intersectionPointA != null)
                    {
                        this.globalTransformMatrix.TransformPoint(intersectionPointA.x, intersectionPointA.y, intersectionPointA);

                        if (intersectionPointB != null)
                        {
                            intersectionPointB.x = intersectionPointA.x;
                            intersectionPointB.y = intersectionPointA.y;
                        }
                    }
                    else if (intersectionPointB != null)
                    {
                        this.globalTransformMatrix.TransformPoint(intersectionPointB.x, intersectionPointB.y, intersectionPointB);
                    }
                }
                else
                {
                    if (intersectionPointA != null)
                    {
                        this.globalTransformMatrix.TransformPoint(intersectionPointA.x, intersectionPointA.y, intersectionPointA);
                    }

                    if (intersectionPointB != null)
                    {
                        this.globalTransformMatrix.TransformPoint(intersectionPointB.x, intersectionPointB.y, intersectionPointB);
                    }
                }

                if (normalRadians != null)
                {
                    this.globalTransformMatrix.TransformPoint((float)Math.Cos(normalRadians.x), (float)Math.Sin(normalRadians.x), Slot._helpPoint, true);
                    normalRadians.x = (float)Math.Atan2(Slot._helpPoint.y, Slot._helpPoint.x);

                    this.globalTransformMatrix.TransformPoint((float)Math.Cos(normalRadians.y), (float)Math.Sin(normalRadians.y), Slot._helpPoint, true);
                    normalRadians.y = (float)Math.Atan2(Slot._helpPoint.y, Slot._helpPoint.x);
                }
            }

            return intersectionCount;
        }


        public void InvalidUpdate()
        {
            this._displayDirty = true;
            this._transformDirty = true;
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
                this._UpdateVisible();
            }
        }

        public int displayIndex
        {
            get { return this._displayIndex; }
            set
            {
                if (this._SetDisplayIndex(value))
                {
                    this.Update(-1);
                }
            }
        }


        public string name
        {
            get { return this._slotData.name; }
        }


        public List<object> displayList
        {
            get { return new List<object>(_displayList.ToArray()); }
            set
            {
                var backupDisplayList = _displayList.ToArray();
                var disposeDisplayList = new List<object>();

                if (this._SetDisplayList(value))
                {
                    this.Update(-1);
                }

                foreach (var eachDisplay in backupDisplayList)
                {
                    if (eachDisplay != null &&
                        eachDisplay != this._rawDisplay &&
                        eachDisplay != this._meshDisplay &&
                        this._displayList.IndexOf(eachDisplay) < 0 &&
                        disposeDisplayList.IndexOf(eachDisplay) < 0)
                    {
                        disposeDisplayList.Add(eachDisplay);
                    }
                }

                foreach (var eachDisplay in disposeDisplayList)
                {
                    if (eachDisplay is Armature)
                    {
                    }
                    else
                    {
                        this._DisposeDisplay(eachDisplay, true);
                    }
                }
            }
        }

        public SlotData slotData
        {
            get { return this._slotData; }
        }

        public List<DisplayData> rawDisplayDatas
        {
            get { return this._rawDisplayDatas; }
            set
            {
                if (this._rawDisplayDatas == value)
                {
                    return;
                }

                this._displayDirty = true;
                this._rawDisplayDatas = value;

                if (this._rawDisplayDatas != null)
                {
                    this._displayDatas.ResizeList(this._rawDisplayDatas.Count);
                    for (int i = 0, l = this._displayDatas.Count; i < l; ++i)
                    {
                        var rawDisplayData = this._rawDisplayDatas[i];

                        if (rawDisplayData == null)
                        {
                            rawDisplayData = this._GetDefaultRawDisplayData(i);
                        }

                        this._displayDatas[i] = rawDisplayData;
                    }
                }
                else
                {
                    this._displayDatas.Clear();
                }
            }
        }

        public BoundingBoxData boundingBoxData
        {
            get { return this._boundingBoxData; }
        }
        public object rawDisplay
        {
            get { return this._rawDisplay; }
        }

        public object meshDisplay
        {
            get { return this._meshDisplay; }
        }

        public object display
        {
            get { return this._display; }
            set
            {
                if (this._display == value)
                {
                    return;
                }

                var displayListLength = this._displayList.Count;
                if (this._displayIndex < 0 && displayListLength == 0)
                {
                    this._displayIndex = 0;
                }

                if (this._displayIndex < 0)
                {
                    return;
                }
                else
                {
                    var replaceDisplayList = this.displayList;
                    if (displayListLength <= this._displayIndex)
                    {
                        replaceDisplayList.ResizeList(this._displayIndex + 1);
                    }

                    replaceDisplayList[this._displayIndex] = value;
                    this.displayList = replaceDisplayList;
                }
            }
        }

        public Armature childArmature
        {
            get { return this._childArmature; }
            set
            {
                if (this._childArmature == value)
                {
                    return;
                }

                this.display = value;
            }
        }


        public Bone parent
        {
            get { return this._parent; }
        }
    }
}
