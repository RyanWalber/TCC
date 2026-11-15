using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DragonBones
{
    internal class ClockHandler : MonoBehaviour
    {
        void Update()
        {
            UnityFactory.factory._dragonBones.AdvanceTime(Time.deltaTime);
        }
    }

    public class UnityFactory : BaseFactory
    {
        internal const string defaultShaderName = "Sprites/Default";
        internal const string defaultUIShaderName = "UI/Default";

        internal static DragonBones _dragonBonesInstance = null;
        private static UnityFactory _factory = null;
        private static GameObject _gameObject = null;

        private GameObject _armatureGameObject = null;
        private bool _isUGUI = false;

        private readonly List<UnityDragonBonesData> _cacheUnityDragonBonesData = new List<UnityDragonBonesData>();


        public static UnityFactory factory
        {
            get
            {
                if (_factory == null)
                {
                    _factory = new UnityFactory();
                }

                return _factory;
            }
        }

        public UnityFactory(DataParser dataParser = null) : base(dataParser)
        {
            Init();
        }

        private void Init()
        {
            if (Application.isPlaying)
            {
                if (_gameObject == null)
                {
                    _gameObject = GameObject.Find("DragonBones Object");
                    if (_gameObject == null)
                    {
                        _gameObject = new GameObject("DragonBones Object", typeof(ClockHandler));

                        _gameObject.isStatic = true;
                        _gameObject.hideFlags = HideFlags.HideInHierarchy;
                    }
                }

                GameObject.DontDestroyOnLoad(_gameObject);

                var clockHandler = _gameObject.GetComponent<ClockHandler>();
                if (clockHandler == null)
                {
                    _gameObject.AddComponent<ClockHandler>();
                }

                var eventManager = _gameObject.GetComponent<DragonBoneEventDispatcher>();
                if (eventManager == null)
                {
                    eventManager = _gameObject.AddComponent<DragonBoneEventDispatcher>();
                }

                if (_dragonBonesInstance == null)
                {
                    _dragonBonesInstance = new DragonBones(eventManager);
                    DragonBones.yDown = false;
                }
            }
            else
            {
                if (_dragonBonesInstance == null)
                {
                    _dragonBonesInstance = new DragonBones(null);
                    DragonBones.yDown = false;
                }
            }

            _dragonBones = _dragonBonesInstance;
        }

        protected override TextureAtlasData _BuildTextureAtlasData(TextureAtlasData textureAtlasData, object textureAtlas)
        {
            if (textureAtlasData != null)
            {
                if (textureAtlas != null)
                {

                    (textureAtlasData as UnityTextureAtlasData).uiTexture = (textureAtlas as UnityDragonBonesData.TextureAtlas).uiMaterial;
                    (textureAtlasData as UnityTextureAtlasData).texture = (textureAtlas as UnityDragonBonesData.TextureAtlas).material;
                }
            }
            else
            {
                textureAtlasData = BaseObject.BorrowObject<UnityTextureAtlasData>();
            }

            return textureAtlasData;
        }

        protected override Armature _BuildArmature(BuildArmaturePackage dataPackage)
        {
            var armature = BaseObject.BorrowObject<Armature>();
            var armatureDisplay = _armatureGameObject == null ? new GameObject(dataPackage.armature.name) : _armatureGameObject;
            var armatureComponent = armatureDisplay.GetComponent<UnityArmatureComponent>();
            if (armatureComponent == null)
            {
                armatureComponent = armatureDisplay.AddComponent<UnityArmatureComponent>();
                armatureComponent.isUGUI = _isUGUI;

                if (armatureComponent.isUGUI)
                {
                    armatureComponent.transform.localScale = Vector2.one * (1.0f / dataPackage.armature.scale);
                }
            }
            else
            {
                var slotRoot = armatureDisplay.transform.Find("Slots");
                if (slotRoot != null)
                {
                    for (int i = slotRoot.transform.childCount; i > 0; i--)
                    {
                        var childSlotDisplay = slotRoot.transform.GetChild(i - 1);
                        childSlotDisplay.transform.SetParent(armatureDisplay.transform, false);
                    }

                    UnityFactoryHelper.DestroyUnityObject(slotRoot.gameObject);
                }
            }

            armatureComponent._armature = armature;
            armature.Init(dataPackage.armature, armatureComponent, armatureDisplay, this._dragonBones);
            _armatureGameObject = null;

            return armature;
        }

        protected override Armature _BuildChildArmature(BuildArmaturePackage dataPackage, Slot slot, DisplayData displayData)
        {
            var childDisplayName = slot.slotData.name + " (" + displayData.path + ")";
            var proxy = slot.armature.proxy as UnityArmatureComponent;
            var childTransform = proxy.transform.Find(childDisplayName);
            Armature childArmature = null;
            if (childTransform == null)
            {
                if (dataPackage != null)
                {
                    childArmature = BuildArmature(displayData.path, dataPackage.dataName);
                }
                else
                {
                    childArmature = BuildArmature(displayData.path, displayData.parent.parent.parent.name);
                }
            }
            else
            {
                if (dataPackage != null)
                {
                    childArmature = BuildArmatureComponent(displayData.path, dataPackage != null ? dataPackage.dataName : "", null, dataPackage.textureAtlasName, childTransform.gameObject).armature;
                }
                else
                {
                    childArmature = BuildArmatureComponent(displayData.path, null, null, null, childTransform.gameObject).armature;
                }
            }

            if (childArmature == null)
            {
                return null;
            }

            var childArmatureDisplay = childArmature.display as GameObject;
            childArmatureDisplay.GetComponent<UnityArmatureComponent>().isUGUI = proxy.GetComponent<UnityArmatureComponent>().isUGUI;
            childArmatureDisplay.name = childDisplayName;
            childArmatureDisplay.transform.SetParent(proxy.transform, false);
            childArmatureDisplay.gameObject.hideFlags = HideFlags.HideInHierarchy;
            childArmatureDisplay.SetActive(false);
            return childArmature;
        }

        protected override Slot _BuildSlot(BuildArmaturePackage dataPackage, SlotData slotData, Armature armature)
        {
            var slot = BaseObject.BorrowObject<UnitySlot>();
            var armatureDisplay = armature.display as GameObject;
            var transform = armatureDisplay.transform.Find(slotData.name);
            var gameObject = transform == null ? null : transform.gameObject;
            var isNeedIngoreCombineMesh = false;
            if (gameObject == null)
            {
                gameObject = new GameObject(slotData.name);
            }
            else
            {
                if (gameObject.hideFlags == HideFlags.None)
                {
                    var combineMeshs = (armature.proxy as UnityArmatureComponent).GetComponent<UnityCombineMeshs>();
                    if (combineMeshs != null)
                    {
                        isNeedIngoreCombineMesh = !combineMeshs.slotNames.Contains(slotData.name);
                    }
                }
            }

            slot.Init(slotData, armature, gameObject, gameObject);

            if (isNeedIngoreCombineMesh)
            {
                slot.DisallowCombineMesh();
            }

            return slot;
        }

        public UnityArmatureComponent BuildArmatureComponent(string armatureName, string dragonBonesName = "", string skinName = "", string textureAtlasName = "", GameObject gameObject = null, bool isUGUI = false)
        {
            _armatureGameObject = gameObject;
            _isUGUI = isUGUI;
            var armature = BuildArmature(armatureName, dragonBonesName, skinName, textureAtlasName);

            if (armature != null)
            {
                _dragonBones.clock.Add(armature);

                var armatureDisplay = armature.display as GameObject;
                var armatureComponent = armatureDisplay.GetComponent<UnityArmatureComponent>();

                return armatureComponent;
            }


            return null;
        }
        public GameObject GetTextureDisplay(string textureName, string textureAtlasName = null)
        {




            return null;
        }
        protected void _RefreshTextureAtlas(UnityTextureAtlasData textureAtlasData, bool isUGUI, bool isEditor = false)
        {
            Material material = null;
            if (isUGUI && textureAtlasData.uiTexture == null)
            {
                if (isEditor)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        material = AssetDatabase.LoadAssetAtPath<Material>(textureAtlasData.imagePath + "_UI_Mat.mat");
                    }
#endif
                }
                else
                {
                    material = Resources.Load<Material>(textureAtlasData.imagePath + "_UI_Mat");
                }

                if (material == null)
                {
                    Texture2D textureAtlas = null;

                    if (isEditor)
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            textureAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAtlasData.imagePath + ".png");
                        }
#endif
                    }
                    else
                    {
                        textureAtlas = Resources.Load<Texture2D>(textureAtlasData.imagePath);
                    }

                    material = UnityFactoryHelper.GenerateMaterial(defaultUIShaderName, textureAtlas.name + "_UI_Mat", textureAtlas);
                    if (textureAtlasData.width < 2)
                    {
                        textureAtlasData.width = (uint)textureAtlas.width;
                    }

                    if (textureAtlasData.height < 2)
                    {
                        textureAtlasData.height = (uint)textureAtlas.height;
                    }

                    textureAtlasData._disposeEnabled = true;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        string path = AssetDatabase.GetAssetPath(textureAtlas);
                        path = path.Substring(0, path.Length - 4);
                        AssetDatabase.CreateAsset(material, path + "_UI_Mat.mat");
                        AssetDatabase.SaveAssets();
                    }
#endif
                }

                textureAtlasData.uiTexture = material;
            }
            else if (!isUGUI && textureAtlasData.texture == null)
            {
                if (isEditor)
                {
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        material = AssetDatabase.LoadAssetAtPath<Material>(textureAtlasData.imagePath + "_Mat.mat");
                    }
#endif
                }
                else
                {
                    material = Resources.Load<Material>(textureAtlasData.imagePath + "_Mat");
                }

                if (material == null)
                {
                    Texture2D textureAtlas = null;
                    if (isEditor)
                    {
#if UNITY_EDITOR
                        if (!Application.isPlaying)
                        {
                            textureAtlas = AssetDatabase.LoadAssetAtPath<Texture2D>(textureAtlasData.imagePath + ".png");
                        }
#endif
                    }
                    else
                    {
                        textureAtlas = Resources.Load<Texture2D>(textureAtlasData.imagePath);
                    }

                    material = UnityFactoryHelper.GenerateMaterial(defaultShaderName, textureAtlas.name + "_Mat", textureAtlas);
                    if (textureAtlasData.width < 2)
                    {
                        textureAtlasData.width = (uint)textureAtlas.width;
                    }

                    if (textureAtlasData.height < 2)
                    {
                        textureAtlasData.height = (uint)textureAtlas.height;
                    }

                    textureAtlasData._disposeEnabled = true;
#if UNITY_EDITOR
                    if (!Application.isPlaying)
                    {
                        string path = AssetDatabase.GetAssetPath(textureAtlas);
                        path = path.Substring(0, path.Length - 4);
                        AssetDatabase.CreateAsset(material, path + "_Mat.mat");
                        AssetDatabase.SaveAssets();
                    }
#endif
                }
                textureAtlasData.texture = material;
            }
        }

        public override void Clear(bool disposeData = true)
        {
            base.Clear(disposeData);

            _armatureGameObject = null;
            _isUGUI = false;

            _cacheUnityDragonBonesData.Clear();
        }


        public IEventDispatcher<EventObject> soundEventManager
        {
            get
            {
                return _dragonBonesInstance.eventManager;
            }
        }

        public DragonBonesData LoadData(UnityDragonBonesData data, bool isUGUI = false, float armatureScale = 0.01f, float texScale = 1.0f)
        {
            DragonBonesData dragonBonesData = null;

            if (data.dragonBonesJSON != null)
            {
                dragonBonesData = LoadDragonBonesData(data.dragonBonesJSON, data.dataName, armatureScale);

                if (!string.IsNullOrEmpty(data.dataName) && dragonBonesData != null && data.textureAtlas != null)
                {
#if UNITY_EDITOR
                    bool isDirty = false;
                    if (!Application.isPlaying)
                    {
                        for (int i = 0; i < data.textureAtlas.Length; ++i)
                        {
                            if (isUGUI)
                            {
                                if (data.textureAtlas[i].uiMaterial == null)
                                {
                                    isDirty = true;
                                    break;
                                }
                            }
                            else
                            {
                                if (data.textureAtlas[i].material == null)
                                {
                                    isDirty = true;
                                    break;
                                }
                            }
                        }
                    }
#endif

                    var textureAtlasDatas = this.GetTextureAtlasData(data.dataName);
                    if (textureAtlasDatas != null)
                    {
                        for (int i = 0, l = textureAtlasDatas.Count; i < l; ++i)
                        {
                            if (i < data.textureAtlas.Length)
                            {
                                var textureAtlasData = textureAtlasDatas[i] as UnityTextureAtlasData;
                                var textureAtlas = data.textureAtlas[i];

                                textureAtlasData.uiTexture = textureAtlas.uiMaterial;
                                textureAtlasData.texture = textureAtlas.material;
#if UNITY_EDITOR
                                if (!Application.isPlaying)
                                {
                                    textureAtlasData.imagePath = AssetDatabase.GetAssetPath(textureAtlas.texture);
                                    textureAtlasData.imagePath = textureAtlasData.imagePath.Substring(0, textureAtlasData.imagePath.Length - 4);
                                    _RefreshTextureAtlas(textureAtlasData, isUGUI, true);
                                    if (isUGUI)
                                    {
                                        textureAtlas.uiMaterial = textureAtlasData.uiTexture;
                                    }
                                    else
                                    {
                                        textureAtlas.material = textureAtlasData.texture;
                                    }
                                }
#endif
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < data.textureAtlas.Length; ++i)
                        {
                            LoadTextureAtlasData(data.textureAtlas[i], data.dataName, texScale, isUGUI);
                        }
                    }

#if UNITY_EDITOR
                    if (isDirty)
                    {
                        AssetDatabase.Refresh();
                        EditorUtility.SetDirty(data);
                        AssetDatabase.SaveAssets();
                    }
#endif
                }
            }

            return dragonBonesData;
        }


        public DragonBonesData LoadDragonBonesData(string dragonBonesJSONPath, string name = "", float scale = 0.01f)
        {
            dragonBonesJSONPath = UnityFactoryHelper.CheckResourecdPath(dragonBonesJSONPath);

            TextAsset dragonBonesJSON = Resources.Load<TextAsset>(dragonBonesJSONPath);

            DragonBonesData dragonBonesData = LoadDragonBonesData(dragonBonesJSON, name);

            return dragonBonesData;
        }


        public DragonBonesData LoadDragonBonesData(TextAsset dragonBonesJSON, string name = "", float scale = 0.01f)
        {
            if (dragonBonesJSON == null)
            {
                return null;
            }

            if (!string.IsNullOrEmpty(name))
            {
                var existedData = GetDragonBonesData(name);
                if (existedData != null)
                {
                    return existedData;
                }
            }

            DragonBonesData data = null;
            if (dragonBonesJSON.text == "DBDT")
            {
                BinaryDataParser.jsonParseDelegate = MiniJSON.Json.Deserialize;
                data = ParseDragonBonesData(dragonBonesJSON.bytes, name, scale);
            }
            else
            {
                data = ParseDragonBonesData((Dictionary<string, object>)MiniJSON.Json.Deserialize(dragonBonesJSON.text), name, scale);
            }
            name = !string.IsNullOrEmpty(name) ? name : data.name;
            _dragonBonesDataMap[name] = data;
            return data;
        }

        public UnityTextureAtlasData LoadTextureAtlasData(string textureAtlasJSONPath, string name = "", float scale = 1.0f, bool isUGUI = false)
        {
            textureAtlasJSONPath = UnityFactoryHelper.CheckResourecdPath(textureAtlasJSONPath);

            TextAsset textureAtlasJSON = Resources.Load<TextAsset>(textureAtlasJSONPath);

            if (textureAtlasJSON != null)
            {
                Dictionary<string, object> textureJSONData = (Dictionary<string, object>)MiniJSON.Json.Deserialize(textureAtlasJSON.text);
                UnityTextureAtlasData textureAtlasData = ParseTextureAtlasData(textureJSONData, null, name, scale) as UnityTextureAtlasData;

                if (textureAtlasData != null)
                {
                    textureAtlasData.imagePath = UnityFactoryHelper.GetTextureAtlasImagePath(textureAtlasJSONPath, textureAtlasData.imagePath);

                    _RefreshTextureAtlas(textureAtlasData, isUGUI);
                }

                return textureAtlasData;
            }

            return null;
        }

        public UnityTextureAtlasData LoadTextureAtlasData(UnityDragonBonesData.TextureAtlas textureAtlas, string name, float scale = 1.0f, bool isUGUI = false)
        {
            Dictionary<string, object> textureJSONData = (Dictionary<string, object>)MiniJSON.Json.Deserialize(textureAtlas.textureAtlasJSON.text);
            UnityTextureAtlasData textureAtlasData = ParseTextureAtlasData(textureJSONData, null, name, scale) as UnityTextureAtlasData;

            if (textureJSONData.ContainsKey("width"))
            {
                textureAtlasData.width = uint.Parse(textureJSONData["width"].ToString());
            }

            if (textureJSONData.ContainsKey("height"))
            {
                textureAtlasData.height = uint.Parse(textureJSONData["height"].ToString());
            }

            if (textureAtlasData != null)
            {
                textureAtlasData.uiTexture = textureAtlas.uiMaterial;
                textureAtlasData.texture = textureAtlas.material;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    textureAtlasData.imagePath = AssetDatabase.GetAssetPath(textureAtlas.texture);
                    textureAtlasData.imagePath = textureAtlasData.imagePath.Substring(0, textureAtlasData.imagePath.Length - 4);
                    _RefreshTextureAtlas(textureAtlasData, isUGUI, true);
                    if (isUGUI)
                    {
                        textureAtlas.uiMaterial = textureAtlasData.uiTexture;
                    }
                    else
                    {
                        textureAtlas.material = textureAtlasData.texture;
                    }
                }
#endif
            }

            return textureAtlasData;
        }

        public void RefreshAllTextureAtlas(UnityArmatureComponent unityArmature)
        {
            foreach (var textureAtlasDatas in _textureAtlasDataMap.Values)
            {
                foreach (UnityTextureAtlasData textureAtlasData in textureAtlasDatas)
                {
                    _RefreshTextureAtlas(textureAtlasData, unityArmature.isUGUI);
                }
            }
        }
        public override void ReplaceDisplay(Slot slot, DisplayData displayData, int displayIndex = -1)
        {
            if (displayData.type == DisplayType.Image || displayData.type == DisplayType.Mesh)
            {
                var dataName = displayData.parent.parent.parent.name;
                var textureData = this._GetTextureData(dataName, displayData.path);
                if (textureData != null)
                {
                    var textureAtlasData = textureData.parent as UnityTextureAtlasData;

                    var oldIsUGUI = (slot._armature.proxy as UnityArmatureComponent).isUGUI;

                    if ((oldIsUGUI && textureAtlasData.uiTexture == null) || (!oldIsUGUI && textureAtlasData.texture == null))
                    {
                        LogHelper.LogWarning("ugui display object and normal display object cannot be replaced with each other");
                        return;
                    }
                }
            }

            base.ReplaceDisplay(slot, displayData, displayIndex);
        }

        public void ReplaceSlotDisplay(
                                        string dragonBonesName, string armatureName, string slotName, string displayName,
                                        Slot slot, Texture2D texture, Material material = null,
                                        bool isUGUI = false, int displayIndex = -1)
        {
            var armatureData = this.GetArmatureData(armatureName, dragonBonesName);
            if (armatureData == null || armatureData.defaultSkin == null)
            {
                return;
            }

            var displays = armatureData.defaultSkin.GetDisplays(slotName);
            if (displays == null)
            {
                return;
            }

            DisplayData prevDispalyData = null;
            foreach (var displayData in displays)
            {
                if (displayData.name == displayName)
                {
                    prevDispalyData = displayData;
                    break;
                }
            }

            if (prevDispalyData == null || !((prevDispalyData is ImageDisplayData) || (prevDispalyData is MeshDisplayData)))
            {
                return;
            }

            TextureData prevTextureData = null;
            if(prevDispalyData is ImageDisplayData)
            {
                prevTextureData = (prevDispalyData as ImageDisplayData).texture;
            }
            else
            {
                prevTextureData = (prevDispalyData as MeshDisplayData).texture;
            }

            UnityTextureData newTextureData = new UnityTextureData();
            newTextureData.CopyFrom(prevTextureData);
            newTextureData.rotated = false;
            newTextureData.region.x = 0.0f;
            newTextureData.region.y = 0.0f;
            newTextureData.region.width = texture.width;
            newTextureData.region.height = texture.height;
            newTextureData.frame = newTextureData.region;
            newTextureData.name = prevTextureData.name;
            newTextureData.parent = new UnityTextureAtlasData();
            newTextureData.parent.width = (uint)texture.width;
            newTextureData.parent.height = (uint)texture.height;
            newTextureData.parent.scale = prevTextureData.parent.scale;

            if (material == null)
            {
                if (isUGUI)
                {
                    material = UnityFactoryHelper.GenerateMaterial(defaultUIShaderName, texture.name + "_UI_Mat", texture);
                }
                else
                {
                    material = UnityFactoryHelper.GenerateMaterial(defaultShaderName, texture.name + "_Mat", texture);
                }
            }

            if (isUGUI)
            {
                (newTextureData.parent as UnityTextureAtlasData).uiTexture = material;
            }
            else
            {
                (newTextureData.parent as UnityTextureAtlasData).texture = material;
            }

            material.mainTexture = texture;

            DisplayData newDisplayData = null;
            if (prevDispalyData is ImageDisplayData)
            {
                newDisplayData = new ImageDisplayData();
                newDisplayData.type = prevDispalyData.type;
                newDisplayData.name = prevDispalyData.name;
                newDisplayData.path = prevDispalyData.path;
                newDisplayData.transform.CopyFrom(prevDispalyData.transform);
                newDisplayData.parent = prevDispalyData.parent;
                (newDisplayData as ImageDisplayData).pivot.CopyFrom((prevDispalyData as ImageDisplayData).pivot);
                (newDisplayData as ImageDisplayData).texture = newTextureData;
            }
            else if (prevDispalyData is MeshDisplayData)
            {
                newDisplayData = new MeshDisplayData();
                newDisplayData.type = prevDispalyData.type;
                newDisplayData.name = prevDispalyData.name;
                newDisplayData.path = prevDispalyData.path;
                newDisplayData.transform.CopyFrom(prevDispalyData.transform);
                newDisplayData.parent = prevDispalyData.parent;
                (newDisplayData as MeshDisplayData).texture = newTextureData;

                (newDisplayData as MeshDisplayData).vertices.inheritDeform = (prevDispalyData as MeshDisplayData).vertices.inheritDeform;
                (newDisplayData as MeshDisplayData).vertices.offset = (prevDispalyData as MeshDisplayData).vertices.offset;
                (newDisplayData as MeshDisplayData).vertices.data = (prevDispalyData as MeshDisplayData).vertices.data;
                (newDisplayData as MeshDisplayData).vertices.weight = (prevDispalyData as MeshDisplayData).vertices.weight;
            }

            ReplaceDisplay(slot, newDisplayData, displayIndex);
        }

        public UnityDragonBonesData GetCacheUnityDragonBonesData(string draonBonesName)
        {
            if (string.IsNullOrEmpty(draonBonesName))
            {
                return null;
            }

            for (int i = 0; i < this._cacheUnityDragonBonesData.Count; i++)
            {
                if (this._cacheUnityDragonBonesData[i].dataName == draonBonesName)
                {
                    return this._cacheUnityDragonBonesData[i];
                }
            }

            return null;
        }

        public void AddCacheUnityDragonBonesData(UnityDragonBonesData unityData)
        {
            for (int i = 0; i < this._cacheUnityDragonBonesData.Count; i++)
            {
                if (this._cacheUnityDragonBonesData[i].dataName == unityData.dataName)
                {
                    this._cacheUnityDragonBonesData[i] = unityData;
                    return;
                }
            }

            this._cacheUnityDragonBonesData.Add(unityData);
        }
    }

    internal static class UnityFactoryHelper
    {
        internal static Material GenerateMaterial(string shaderName, string materialName, Texture texture)
        {
            Shader shader = Shader.Find(shaderName);
            Material material = new Material(shader);
            material.name = materialName;
            material.mainTexture = texture;

            return material;
        }

        internal static string CheckResourecdPath(string path)
        {
            var index = path.LastIndexOf("Resources");
            if (index > 0)
            {
                path = path.Substring(index + 10);
            }

            index = path.LastIndexOf(".");
            if (index > 0)
            {
                path = path.Substring(0, index);
            }

            return path;
        }

        internal static string GetTextureAtlasImagePath(string textureAtlasJSONPath, string textureAtlasImageName)
        {
            var index = textureAtlasJSONPath.LastIndexOf("Resources");
            if (index > 0)
            {
                textureAtlasJSONPath = textureAtlasJSONPath.Substring(index + 10);
            }

            index = textureAtlasJSONPath.LastIndexOf("/");

            string textureAtlasImagePath = textureAtlasImageName;
            if (index > 0)
            {
                textureAtlasImagePath = textureAtlasJSONPath.Substring(0, index + 1) + textureAtlasImageName;
            }

            index = textureAtlasImagePath.LastIndexOf(".");
            if (index > 0)
            {
                textureAtlasImagePath = textureAtlasImagePath.Substring(0, index);
            }

            return textureAtlasImagePath;
        }

        internal static string GetTextureAtlasNameByPath(string textureAtlasJSONPath)
        {
            string name = string.Empty;
            int index = textureAtlasJSONPath.LastIndexOf("/") + 1;
            int lastIdx = textureAtlasJSONPath.LastIndexOf("_tex");

            if (lastIdx > -1)
            {
                if (lastIdx > index)
                {
                    name = textureAtlasJSONPath.Substring(index, lastIdx - index);
                }
                else
                {
                    name = textureAtlasJSONPath.Substring(index);
                }
            }
            else
            {
                if (index > -1)
                {
                    name = textureAtlasJSONPath.Substring(index);
                }

            }

            return name;
        }

        internal static void DestroyUnityObject(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return;
            }

#if UNITY_EDITOR
            UnityEngine.Object.DestroyImmediate(obj);
#else
            UnityEngine.Object.Destroy(obj);
#endif
        }
    }

    internal static class LogHelper
    {
        internal static void LogWarning(object message)
        {
            UnityEngine.Debug.LogWarning("[DragonBones]" + message);
        }
    }
}