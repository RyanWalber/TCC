using System.Collections.Generic;

namespace DragonBones
{

    public class AnimationConfig : BaseObject
    {
        public bool pauseFadeOut;

        public AnimationFadeOutMode fadeOutMode;
        public TweenType fadeOutTweenType;
        public float fadeOutTime;
        public bool pauseFadeIn;

        public bool actionEnabled;
        public bool additiveBlending;

        public bool displayControl;

        public bool resetToPose;
        public TweenType fadeInTweenType;

        public int playTimes;

        public int layer;

        public float position;

        public float duration;

        public float timeScale;

        public float weight;

        public float fadeInTime;

        public float autoFadeOutTime;

        public string name;

        public string animation;

        public string group;
        public readonly List<string> boneMask = new List<string>();

        protected override void _OnClear()
        {
            this.pauseFadeOut = true;
            this.fadeOutMode = AnimationFadeOutMode.All;
            this.fadeOutTweenType = TweenType.Line;
            this.fadeOutTime = -1.0f;

            this.actionEnabled = true;
            this.additiveBlending = false;
            this.displayControl = true;
            this.pauseFadeIn = true;
            this.resetToPose = true;
            this.fadeInTweenType = TweenType.Line;
            this.playTimes = -1;
            this.layer = 0;
            this.position = 0.0f;
            this.duration = -1.0f;
            this.timeScale = -100.0f;
            this.weight = 1.0f;
            this.fadeInTime = -1.0f;
            this.autoFadeOutTime = -1.0f;
            this.name = "";
            this.animation = "";
            this.group = "";
            this.boneMask.Clear();
        }

        public void Clear()
        {
            this._OnClear();
        }

        public void CopyFrom(AnimationConfig value)
        {
            this.pauseFadeOut = value.pauseFadeOut;
            this.fadeOutMode = value.fadeOutMode;
            this.autoFadeOutTime = value.autoFadeOutTime;
            this.fadeOutTweenType = value.fadeOutTweenType;

            this.actionEnabled = value.actionEnabled;
            this.additiveBlending = value.additiveBlending;
            this.displayControl = value.displayControl;
            this.pauseFadeIn = value.pauseFadeIn;
            this.resetToPose = value.resetToPose;
            this.playTimes = value.playTimes;
            this.layer = value.layer;
            this.position = value.position;
            this.duration = value.duration;
            this.timeScale = value.timeScale;
            this.fadeInTime = value.fadeInTime;
            this.fadeOutTime = value.fadeOutTime;
            this.fadeInTweenType = value.fadeInTweenType;
            this.weight = value.weight;
            this.name = value.name;
            this.animation = value.animation;
            this.group = value.group;

            boneMask.ResizeList(value.boneMask.Count, null);
            for (int i = 0, l = boneMask.Count; i < l; ++i)
            {
                boneMask[i] = value.boneMask[i];
            }
        }

        public bool ContainsBoneMask(string boneName)
        {
            return boneMask.Count == 0 || boneMask.Contains(boneName);
        }

        public void AddBoneMask(Armature armature, string boneName, bool recursive = false)
        {
            var currentBone = armature.GetBone(boneName);
            if (currentBone == null)
            {
                return;
            }

            if (!boneMask.Contains(boneName))
            {
                boneMask.Add(boneName);
            }

            if (recursive)
            {
                var bones = armature.GetBones();
                for (int i = 0, l = bones.Count; i < l; ++i)
                {
                    var bone = bones[i];
                    if (!boneMask.Contains(bone.name) && currentBone.Contains(bone))
                    {
                        boneMask.Add(bone.name);
                    }
                }
            }
        }

        public void RemoveBoneMask(Armature armature, string name, bool recursive = true)
        {
            if (boneMask.Contains(name))
            {
                boneMask.Remove(name);
            }

            if (recursive)
            {
                var currentBone = armature.GetBone(name);
                if (currentBone != null)
                {
                    var bones = armature.GetBones();
                    if (boneMask.Count > 0)
                    {
                        for (int i = 0, l = bones.Count; i < l; ++i)
                        {
                            var bone = bones[i];
                            if (boneMask.Contains(bone.name) && currentBone.Contains(bone))
                            {
                                boneMask.Remove(bone.name);
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0, l = bones.Count; i < l; ++i)
                        {
                            var bone = bones[i];
                            if (bone == currentBone)
                            {
                                continue;
                            }

                            if (!currentBone.Contains(bone))
                            {
                                boneMask.Add(bone.name);
                            }
                        }
                    }
                }
            }
        }
    }
}