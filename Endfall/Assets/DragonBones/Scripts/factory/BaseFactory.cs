using System.Collections.Generic;

namespace DragonBones
{
    public class BuildArmaturePackage
    {
        public string dataName = "";
        public string textureAtlasName = "";
        public DragonBonesData data;
        public ArmatureData armature;
        public SkinData skin;
    }

    public abstract class BaseFactory
    {
        protected static ObjectDataParser _objectParser = null;
        protected static BinaryDataParser _binaryParser = null;
        public bool autoSearch = false;
        protected readonly Dictionary<string, DragonBonesData> _dragonBonesDataMap = new Dictionary<string, DragonBonesData>();
        protected readonly Dictionary<string, List<TextureAtlasData>> _textureAtlasDataMap = new Dictionary<string, List<TextureAtlasData>>();
        public DragonBones _dragonBones = null;
        protected DataParser _dataParser = null;

        public BaseFactory(DataParser dataParser = null)
        {
            if (BaseFactory._objectParser == null)
            {
                BaseFactory._objectParser = new ObjectDataParser();
            }

            if (BaseFactory._binaryParser == null)
            {
                BaseFactory._binaryParser = new BinaryDataParser();
            }

            this._dataParser = dataParser != null ? dataParser : BaseFactory._objectParser;
        }
        protected bool _IsSupportMesh()
        {
            return true;
        }
        protected TextureData _GetTextureData(string textureAtlasName, string textureName)
        {
            if (this._textureAtlasDataMap.ContainsKey(textureAtlasName))
            {
                foreach (var textureAtlasData in this._textureAtlasDataMap[textureAtlasName])
                {
                    var textureData = textureAtlasData.GetTexture(textureName);
                    if (textureData != null)
                    {
                        return textureData;
                    }
                }
            }

            if (this.autoSearch)
            {
                foreach (var values in this._textureAtlasDataMap.Values)
                {
                    foreach (var textureAtlasData in values)
                    {
                        if (textureAtlasData.autoSearch)
                        {
                            var textureData = textureAtlasData.GetTexture(textureName);
                            if (textureData != null)
                            {
                                return textureData;
                            }
                        }
                    }
                }
            }

            return null;
        }
        protected bool _FillBuildArmaturePackage(BuildArmaturePackage dataPackage,
                                                string dragonBonesName,
                                                string armatureName,
                                                string skinName,
                                                string textureAtlasName)
        {
            DragonBonesData dragonBonesData = null;
            ArmatureData armatureData = null;

            var isAvailableName = !string.IsNullOrEmpty(dragonBonesName);
            if (isAvailableName)
            {
                if (this._dragonBonesDataMap.ContainsKey(dragonBonesName))
                {
                    dragonBonesData = this._dragonBonesDataMap[dragonBonesName];
                    armatureData = dragonBonesData.GetArmature(armatureName);
                }
            }

            if (armatureData == null && (!isAvailableName || this.autoSearch))
            {
                foreach (var key in this._dragonBonesDataMap.Keys)
                {
                    dragonBonesData = this._dragonBonesDataMap[key];
                    if (!isAvailableName || dragonBonesData.autoSearch)
                    {
                        armatureData = dragonBonesData.GetArmature(armatureName);
                        if (armatureData != null)
                        {
                            dragonBonesName = key;
                            break;
                        }
                    }
                }
            }

            if (armatureData != null)
            {
                dataPackage.dataName = dragonBonesName;
                dataPackage.textureAtlasName = textureAtlasName;
                dataPackage.data = dragonBonesData;
                dataPackage.armature = armatureData;
                dataPackage.skin = null;

                if (!string.IsNullOrEmpty(skinName))
                {
                    dataPackage.skin = armatureData.GetSkin(skinName);
                    if (dataPackage.skin == null && this.autoSearch)
                    {
                        foreach (var k in this._dragonBonesDataMap.Keys)
                        {
                            var skinDragonBonesData = this._dragonBonesDataMap[k];
                            var skinArmatureData = skinDragonBonesData.GetArmature(skinName);
                            if (skinArmatureData != null)
                            {
                                dataPackage.skin = skinArmatureData.defaultSkin;
                                break;
                            }
                        }
                    }
                }

                if (dataPackage.skin == null)
                {
                    dataPackage.skin = armatureData.defaultSkin;
                }

                return true;
            }

            return false;
        }
        protected void _BuildBones(BuildArmaturePackage dataPackage, Armature armature)
        {
            var bones = dataPackage.armature.sortedBones;
            for (int i = 0, l = bones.Count; i < l; ++i)
            {
                var boneData = bones[i];
                var bone = BaseObject.BorrowObject<Bone>();
                bone.Init(boneData, armature);
            }
        }
        protected void _BuildSlots(BuildArmaturePackage dataPackage, Armature armature)
        {
            var currentSkin = dataPackage.skin;
            var defaultSkin = dataPackage.armature.defaultSkin;
            if (currentSkin == null || defaultSkin == null)
            {
                return;
            }

            Dictionary<string, List<DisplayData>> skinSlots = new Dictionary<string, List<DisplayData>>();
            foreach (var key in defaultSkin.displays.Keys)
            {
                var displays = defaultSkin.GetDisplays(key);
                skinSlots[key] = displays;
            }

            if (currentSkin != defaultSkin)
            {
                foreach (var k in currentSkin.displays.Keys)
                {
                    var displays = currentSkin.GetDisplays(k);
                    skinSlots[k] = displays;
                }
            }

            foreach (var slotData in dataPackage.armature.sortedSlots)
            {
                var displayDatas = skinSlots.ContainsKey(slotData.name) ? skinSlots[slotData.name] : null;
                var slot = this._BuildSlot(dataPackage, slotData, armature);
                slot.rawDisplayDatas = displayDatas;

                if (displayDatas != null)
                {
                    var displayList = new List<object>();
                    for (int i = 0, l = displayDatas.Count; i < l; ++i)
                    {
                        var displayData = displayDatas[i];

                        if (displayData != null)
                        {
                            displayList.Add(this._GetSlotDisplay(dataPackage, displayData, null, slot));
                        }
                        else
                        {
                            displayList.Add(null);
                        }
                    }

                    slot._SetDisplayList(displayList);
                }

                slot._SetDisplayIndex(slotData.displayIndex, true);
            }
        }

        protected void _BuildConstraints(BuildArmaturePackage dataPackage, Armature armature)
        {
            var constraints = dataPackage.armature.constraints;
            foreach (var constraintData in constraints.Values)
            {
                var constraint = BaseObject.BorrowObject<IKConstraint>();
                constraint.Init(constraintData, armature);
                armature._AddConstraint(constraint);
            }
        }

        protected virtual Armature _BuildChildArmature(BuildArmaturePackage dataPackage, Slot slot, DisplayData displayData)
        {
            return this.BuildArmature(displayData.path, dataPackage != null ? dataPackage.dataName : "", "", dataPackage != null ? dataPackage.textureAtlasName : "");
        }
        protected object _GetSlotDisplay(BuildArmaturePackage dataPackage, DisplayData displayData, DisplayData rawDisplayData, Slot slot)
        {
            var dataName = dataPackage != null ? dataPackage.dataName : displayData.parent.parent.parent.name;
            object display = null;
            switch (displayData.type)
            {
                case DisplayType.Image:
                    {
                        var imageDisplayData = displayData as ImageDisplayData;
                        if (imageDisplayData.texture == null)
                        {
                            imageDisplayData.texture = this._GetTextureData(dataName, displayData.path);
                        }
                        else if (dataPackage != null && !string.IsNullOrEmpty(dataPackage.textureAtlasName))
                        {
                            imageDisplayData.texture = this._GetTextureData(dataPackage.textureAtlasName, displayData.path);
                        }

                        if (rawDisplayData != null && rawDisplayData.type == DisplayType.Mesh && this._IsSupportMesh())
                        {
                            display = slot.meshDisplay;
                        }
                        else
                        {
                            display = slot.rawDisplay;
                        }
                    }
                    break;
                case DisplayType.Mesh:
                    {
                        var meshDisplayData = displayData as MeshDisplayData;
                        if (meshDisplayData.texture == null)
                        {
                            meshDisplayData.texture = this._GetTextureData(dataName, meshDisplayData.path);
                        }
                        else if (dataPackage != null && !string.IsNullOrEmpty(dataPackage.textureAtlasName))
                        {
                            meshDisplayData.texture = this._GetTextureData(dataPackage.textureAtlasName, meshDisplayData.path);
                        }

                        if (this._IsSupportMesh())
                        {
                            display = slot.meshDisplay;
                        }
                        else
                        {
                            display = slot.rawDisplay;
                        }
                    }
                    break;
                case DisplayType.Armature:
                    {
                        var armatureDisplayData = displayData as ArmatureDisplayData;
                        var childArmature = this._BuildChildArmature(dataPackage, slot, displayData);
                        if (childArmature != null)
                        {
                            childArmature.inheritAnimation = armatureDisplayData.inheritAnimation;
                            if (!childArmature.inheritAnimation)
                            {
                                var actions = armatureDisplayData.actions.Count > 0 ? armatureDisplayData.actions : childArmature.armatureData.defaultActions;
                                if (actions.Count > 0)
                                {
                                    foreach (var action in actions)
                                    {
                                        var eventObject = BaseObject.BorrowObject<EventObject>();
                                        EventObject.ActionDataToInstance(action, eventObject, slot.armature);
                                        eventObject.slot = slot;
                                        slot.armature._BufferAction(eventObject, false);
                                    }
                                }
                                else
                                {
                                    childArmature.animation.Play();
                                }
                            }

                            armatureDisplayData.armature = childArmature.armatureData;
                        }

                        display = childArmature;
                    }
                    break;
                case DisplayType.BoundingBox:
                    break;
            }

            return display;
        }
        protected abstract TextureAtlasData _BuildTextureAtlasData(TextureAtlasData textureAtlasData, object textureAtlas);
        protected abstract Armature _BuildArmature(BuildArmaturePackage dataPackage);
        protected abstract Slot _BuildSlot(BuildArmaturePackage dataPackage, SlotData slotData, Armature armature);

        public DragonBonesData ParseDragonBonesData(object rawData, string name = null, float scale = 1.0f)
        {
            var dataParser = rawData is byte[] ? BaseFactory._binaryParser : this._dataParser;
            DragonBonesData dragonBonesData = dataParser.ParseDragonBonesData(rawData, scale);

            while (true)
            {
                var textureAtlasData = this._BuildTextureAtlasData(null, null);
                if (dataParser.ParseTextureAtlasData(null, textureAtlasData, scale))
                {
                    this.AddTextureAtlasData(textureAtlasData, name);
                }
                else
                {
                    textureAtlasData.ReturnToPool();
                    break;
                }
            }

            if (dragonBonesData != null)
            {
                this.AddDragonBonesData(dragonBonesData, name);
            }

            return dragonBonesData;
        }

        public TextureAtlasData ParseTextureAtlasData(Dictionary<string, object> rawData, object textureAtlas, string name = null, float scale = 1.0f)
        {
            var textureAtlasData = this._BuildTextureAtlasData(null, null);
            this._dataParser.ParseTextureAtlasData(rawData, textureAtlasData, scale);
            this._BuildTextureAtlasData(textureAtlasData, textureAtlas);
            this.AddTextureAtlasData(textureAtlasData, name);

            return textureAtlasData;
        }
        public void UpdateTextureAtlasData(string name, List<object> textureAtlases)
        {
            var textureAtlasDatas = this.GetTextureAtlasData(name);
            if (textureAtlasDatas != null)
            {
                for (int i = 0, l = textureAtlasDatas.Count; i < l; ++i)
                {
                    if (i < textureAtlases.Count)
                    {
                        this._BuildTextureAtlasData(textureAtlasDatas[i], textureAtlases[i]);
                    }
                }
            }
        }

        public DragonBonesData GetDragonBonesData(string name)
        {
            return this._dragonBonesDataMap.ContainsKey(name) ? this._dragonBonesDataMap[name] : null;
        }

        public void AddDragonBonesData(DragonBonesData data, string name = null)
        {
            name = !string.IsNullOrEmpty(name) ? name : data.name;
            if (this._dragonBonesDataMap.ContainsKey(name))
            {
                if (this._dragonBonesDataMap[name] == data)
                {
                    return;
                }

                Helper.Assert(false, "Can not add same name data: " + name);
                return;
            }

            this._dragonBonesDataMap[name] = data;
        }

        public virtual void RemoveDragonBonesData(string name, bool disposeData = true)
        {
            if (this._dragonBonesDataMap.ContainsKey(name))
            {
                if (disposeData)
                {
                    this._dragonBones.BufferObject(this._dragonBonesDataMap[name]);
                }

                this._dragonBonesDataMap.Remove(name);
            }
        }

        public List<TextureAtlasData> GetTextureAtlasData(string name)
        {
            return this._textureAtlasDataMap.ContainsKey(name) ? this._textureAtlasDataMap[name] : null;
        }

        public void AddTextureAtlasData(TextureAtlasData data, string name = null)
        {
            name = !string.IsNullOrEmpty(name) ? name : data.name;
            var textureAtlasList = (this._textureAtlasDataMap.ContainsKey(name)) ?
                                    this._textureAtlasDataMap[name] :
                                    (this._textureAtlasDataMap[name] = new List<TextureAtlasData>());
            if (!textureAtlasList.Contains(data))
            {
                textureAtlasList.Add(data);
            }
        }

        public virtual void RemoveTextureAtlasData(string name, bool disposeData = true)
        {
            if (this._textureAtlasDataMap.ContainsKey(name))
            {
                var textureAtlasDataList = this._textureAtlasDataMap[name];
                if (disposeData)
                {
                    foreach (var textureAtlasData in textureAtlasDataList)
                    {
                        this._dragonBones.BufferObject(textureAtlasData);
                    }
                }

                this._textureAtlasDataMap.Remove(name);
            }
        }

        public virtual ArmatureData GetArmatureData(string name, string dragonBonesName = "")
        {
            var dataPackage = new BuildArmaturePackage();
            if (!this._FillBuildArmaturePackage(dataPackage, dragonBonesName, name, "", ""))
            {
                return null;
            }

            return dataPackage.armature;
        }

        public virtual void Clear(bool disposeData = true)
        {
            if (disposeData)
            {
                foreach (var dragonBoneData in this._dragonBonesDataMap.Values)
                {
                    this._dragonBones.BufferObject(dragonBoneData);
                }

                foreach (var textureAtlasDatas in this._textureAtlasDataMap.Values)
                {
                    foreach (var textureAtlasData in textureAtlasDatas)
                    {
                        this._dragonBones.BufferObject(textureAtlasData);
                    }
                }
            }

            _dragonBonesDataMap.Clear();
            _textureAtlasDataMap.Clear();
        }

        public virtual Armature BuildArmature(string armatureName, string dragonBonesName = "", string skinName = null, string textureAtlasName = null)
        {
            var dataPackage = new BuildArmaturePackage();
            if (!this._FillBuildArmaturePackage(dataPackage, dragonBonesName, armatureName, skinName, textureAtlasName))
            {
                Helper.Assert(false, "No armature data: " + armatureName + ", " + (dragonBonesName != "" ? dragonBonesName : ""));
                return null;
            }

            var armature = this._BuildArmature(dataPackage);
            this._BuildBones(dataPackage, armature);
            this._BuildSlots(dataPackage, armature);
            this._BuildConstraints(dataPackage, armature);
            armature.InvalidUpdate(null, true);
            armature.AdvanceTime(0.0f);

            return armature;
        }
        public virtual void ReplaceDisplay(Slot slot, DisplayData displayData, int displayIndex = -1)
        {
            if (displayIndex < 0)
            {
                displayIndex = slot.displayIndex;
            }

            if (displayIndex < 0)
            {
                displayIndex = 0;
            }

            slot.ReplaceDisplayData(displayData, displayIndex);

            var displayList = slot.displayList;
            if (displayList.Count <= displayIndex)
            {
                displayList.ResizeList(displayIndex + 1);

                for (int i = 0, l = displayList.Count; i < l; ++i)
                {
                    displayList[i] = null;
                }
            }

            if (displayData != null)
            {
                var rawDisplayDatas = slot.rawDisplayDatas;
                DisplayData rawDisplayData = null;

                if (rawDisplayDatas != null)
                {
                    if (displayIndex < rawDisplayDatas.Count)
                    {
                        rawDisplayData = rawDisplayDatas[displayIndex];
                    }
                }

                displayList[displayIndex] = this._GetSlotDisplay(null, displayData, rawDisplayData, slot);
            }
            else
            {
                displayList[displayIndex] = null;
            }

            slot.displayList = displayList;
        }

        public bool ReplaceSlotDisplay(string dragonBonesName,
                                        string armatureName,
                                        string slotName,
                                        string displayName,
                                        Slot slot, int displayIndex = -1)
        {
            var armatureData = this.GetArmatureData(armatureName, dragonBonesName);
            if (armatureData == null || armatureData.defaultSkin == null)
            {
                return false;
            }

            var displayData = armatureData.defaultSkin.GetDisplay(slotName, displayName);
            if (displayData == null)
            {
                return false;
            }

            this.ReplaceDisplay(slot, displayData, displayIndex);

            return true;
        }
        public bool ReplaceSlotDisplayList(string dragonBonesName, string armatureName, string slotName, Slot slot)
        {
            var armatureData = this.GetArmatureData(armatureName, dragonBonesName);
            if (armatureData == null || armatureData.defaultSkin == null)
            {
                return false;
            }

            var displays = armatureData.defaultSkin.GetDisplays(slotName);
            if (displays == null)
            {
                return false;
            }

            var displayIndex = 0;
            for (int i = 0, l = displays.Count; i < l; ++i)
            {
                var displayData = displays[i];
                this.ReplaceDisplay(slot, displayData, displayIndex++);
            }

            return true;
        }

        public bool ReplaceSkin(Armature armature, SkinData skin, bool isOverride = false, List<string> exclude = null)
        {
            var success = false;
            var defaultSkin = skin.parent.defaultSkin;

            foreach (var slot in armature.GetSlots())
            {
                if (exclude != null && exclude.Contains(slot.name))
                {
                    continue;
                }

                var displays = skin.GetDisplays(slot.name);
                if (displays == null)
                {
                    if (defaultSkin != null && skin != defaultSkin)
                    {
                        displays = defaultSkin.GetDisplays(slot.name);
                    }

                    if (displays == null)
                    {
                        if (isOverride)
                        {
                            slot.rawDisplayDatas = null;
                            slot.displayList.Clear();
                        }

                        continue;
                    }
                }
                var displayCount = displays.Count;
                var displayList = slot.displayList;
                displayList.ResizeList(displayCount);
                for (int i = 0, l = displayCount; i < l; ++i)
                {
                    var displayData = displays[i];
                    if (displayData != null)
                    {
                        displayList[i] = this._GetSlotDisplay(null, displayData, null, slot);
                    }
                    else
                    {
                        displayList[i] = null;
                    }
                }

                success = true;
                slot.rawDisplayDatas = displays;
                slot.displayList = displayList;
            }

            return success;
        }

        public bool ReplaceAnimation(Armature armature,
                                    ArmatureData armatureData,
                                    bool isOverride = true)
        {

            var skinData = armatureData.defaultSkin;
            if (skinData == null)
            {
                return false;
            }

            if (isOverride)
            {
                armature.animation.animations = armatureData.animations;
            }
            else
            {
                var rawAnimations = armature.animation.animations;
                Dictionary<string, AnimationData> animations = new Dictionary<string, AnimationData>();

                foreach (var k in rawAnimations.Keys)
                {
                    animations[k] = rawAnimations[k];
                }

                foreach (var k in armatureData.animations.Keys)
                {
                    animations[k] = armatureData.animations[k];
                }

                armature.animation.animations = animations;
            }

            foreach (var slot in armature.GetSlots())
            {
                var index = 0;
                foreach (var display in slot.displayList)
                {
                    if (display is Armature)
                    {
                        var displayDatas = skinData.GetDisplays(slot.name);
                        if (displayDatas != null && index < displayDatas.Count)
                        {
                            var displayData = displayDatas[index];
                            if (displayData != null && displayData.type == DisplayType.Armature)
                            {
                                var childArmatureData = this.GetArmatureData(displayData.path, displayData.parent.parent.parent.name);

                                if (childArmatureData != null)
                                {
                                    this.ReplaceAnimation(display as Armature, childArmatureData, isOverride);
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        public Dictionary<string, DragonBonesData> GetAllDragonBonesData()
        {
            return this._dragonBonesDataMap;
        }
        public Dictionary<string, List<TextureAtlasData>> GetAllTextureAtlasData()
        {
            return this._textureAtlasDataMap;
        }


        public WorldClock clock
        {
            get { return this._dragonBones.clock; }
        }

        [System.Obsolete("")]
        public bool ChangeSkin(Armature armature, SkinData skin, List<string> exclude = null)
        {
            return ReplaceSkin(armature, skin, false, exclude);
        }
    }
}
