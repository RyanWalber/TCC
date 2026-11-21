using System;
using System.Collections.Generic;

namespace DragonBones
{

    public class AnimationState : BaseObject
    {
        public bool actionEnabled;
        public bool additiveBlending;

        public bool displayControl;

        public bool resetToPose;

        public int playTimes;

        public int layer;

        public float timeScale;

        public float weight;

        public float autoFadeOutTime;
        public float fadeTotalTime;

        public string name;

        public string group;

        private int _timelineDirty;
        internal int _playheadState;
        internal int _fadeState;
        internal int _subFadeState;
        internal float _position;
        internal float _duration;
        private float _fadeTime;
        private float _time;
        internal float _fadeProgress;
        private float _weightResult;
        internal readonly BlendState _blendState = new BlendState();
        private readonly List<string> _boneMask = new List<string>();
        private readonly List<BoneTimelineState> _boneTimelines = new List<BoneTimelineState>();
        private readonly List<SlotTimelineState> _slotTimelines = new List<SlotTimelineState>();
        private readonly List<ConstraintTimelineState> _constraintTimelines = new List<ConstraintTimelineState>();
        private readonly List<TimelineState> _poseTimelines = new List<TimelineState>();
        private readonly Dictionary<string, BonePose> _bonePoses = new Dictionary<string, BonePose>();
        public AnimationData _animationData;
        private Armature _armature;
        internal ActionTimelineState _actionTimeline = null;
        private ZOrderTimelineState _zOrderTimeline = null;
        public AnimationState _parent = null;
        protected override void _OnClear()
        {
            foreach (var timeline in this._boneTimelines)
            {
                timeline.ReturnToPool();
            }

            foreach (var timeline in this._slotTimelines)
            {
                timeline.ReturnToPool();
            }

            foreach (var timeline in this._constraintTimelines)
            {
                timeline.ReturnToPool();
            }

            foreach (var bonePose in this._bonePoses.Values)
            {
                bonePose.ReturnToPool();
            }

            if (this._actionTimeline != null)
            {
                this._actionTimeline.ReturnToPool();
            }

            if (this._zOrderTimeline != null)
            {
                this._zOrderTimeline.ReturnToPool();
            }

            this.actionEnabled = false;
            this.additiveBlending = false;
            this.displayControl = false;
            this.resetToPose = false;
            this.playTimes = 1;
            this.layer = 0;

            this.timeScale = 1.0f;
            this.weight = 1.0f;
            this.autoFadeOutTime = 0.0f;
            this.fadeTotalTime = 0.0f;
            this.name = string.Empty;
            this.group = string.Empty;

            this._timelineDirty = 2;
            this._playheadState = 0;
            this._fadeState = -1;
            this._subFadeState = -1;
            this._position = 0.0f;
            this._duration = 0.0f;
            this._fadeTime = 0.0f;
            this._time = 0.0f;
            this._fadeProgress = 0.0f;
            this._weightResult = 0.0f;
            this._blendState.Clear();
            this._boneMask.Clear();
            this._boneTimelines.Clear();
            this._slotTimelines.Clear();
            this._constraintTimelines.Clear();
            this._bonePoses.Clear();
            this._animationData = null;
            this._armature = null;
            this._actionTimeline = null;
            this._zOrderTimeline = null;
            this._parent = null;
        }

        private void _UpdateTimelines()
        {
            {
                foreach (var constraint in this._armature._constraints)
                {
                    var timelineDatas = this._animationData.GetConstraintTimelines(constraint.name);

                    if (timelineDatas != null)
                    {
                        foreach (var timelineData in timelineDatas)
                        {
                            switch (timelineData.type)
                            {
                                case TimelineType.IKConstraint:
                                    {
                                        var timeline = BaseObject.BorrowObject<IKConstraintTimelineState>();
                                        timeline.constraint = constraint;
                                        timeline.Init(this._armature, this, timelineData);
                                        this._constraintTimelines.Add(timeline);
                                        break;
                                    }

                                default:
                                    break;
                            }
                        }
                    }
                    else if (this.resetToPose)
                    {
                        var timeline = BaseObject.BorrowObject<IKConstraintTimelineState>();
                        timeline.constraint = constraint;
                        timeline.Init(this._armature, this, null);
                        this._constraintTimelines.Add(timeline);
                        this._poseTimelines.Add(timeline);
                    }
                }
            }
        }

        private void _UpdateBoneAndSlotTimelines()
        {
            {
                Dictionary<string, List<BoneTimelineState>> boneTimelines = new Dictionary<string, List<BoneTimelineState>>();

                foreach (var timeline in this._boneTimelines)
                {
                    var timelineName = timeline.bone.name;
                    if (!(boneTimelines.ContainsKey(timelineName)))
                    {
                        boneTimelines[timelineName] = new List<BoneTimelineState>();
                    }

                    boneTimelines[timelineName].Add(timeline);
                }

                foreach (var bone in this._armature.GetBones())
                {
                    var timelineName = bone.name;
                    if (!this.ContainsBoneMask(timelineName))
                    {
                        continue;
                    }

                    var timelineDatas = this._animationData.GetBoneTimelines(timelineName);
                    if (boneTimelines.ContainsKey(timelineName))
                    {
                        boneTimelines.Remove(timelineName);
                    }
                    else
                    {
                        var bonePose = this._bonePoses.ContainsKey(timelineName) ? this._bonePoses[timelineName] : (this._bonePoses[timelineName] = BaseObject.BorrowObject<BonePose>());
                        if (timelineDatas != null)
                        {
                            foreach (var timelineData in timelineDatas)
                            {
                                switch (timelineData.type)
                                {
                                    case TimelineType.BoneAll:
                                        {
                                            var timeline = BaseObject.BorrowObject<BoneAllTimelineState>();
                                            timeline.bone = bone;
                                            timeline.bonePose = bonePose;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._boneTimelines.Add(timeline);
                                            break;
                                        }
                                    case TimelineType.BoneTranslate:
                                        {
                                            var timeline = BaseObject.BorrowObject<BoneTranslateTimelineState>();
                                            timeline.bone = bone;
                                            timeline.bonePose = bonePose;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._boneTimelines.Add(timeline);
                                            break;
                                        }
                                    case TimelineType.BoneRotate:
                                        {
                                            var timeline = BaseObject.BorrowObject<BoneRotateTimelineState>();
                                            timeline.bone = bone;
                                            timeline.bonePose = bonePose;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._boneTimelines.Add(timeline);
                                            break;
                                        }
                                    case TimelineType.BoneScale:
                                        {
                                            var timeline = BaseObject.BorrowObject<BoneScaleTimelineState>();
                                            timeline.bone = bone;
                                            timeline.bonePose = bonePose;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._boneTimelines.Add(timeline);
                                            break;
                                        }

                                    default:
                                        break;
                                }
                            }
                        }
                        else if (this.resetToPose)
                        {
                            var timeline = BaseObject.BorrowObject<BoneAllTimelineState>();
                            timeline.bone = bone;
                            timeline.bonePose = bonePose;
                            timeline.Init(this._armature, this, null);
                            this._boneTimelines.Add(timeline);
                            this._poseTimelines.Add(timeline);
                        }
                    }
                }

                foreach (var timelines in boneTimelines.Values)
                {
                    foreach (var timeline in timelines)
                    {
                        this._boneTimelines.Remove(timeline);
                        timeline.ReturnToPool();
                    }
                }
            }

            {
                Dictionary<string, List<SlotTimelineState>> slotTimelines = new Dictionary<string, List<SlotTimelineState>>();
                List<int> ffdFlags = new List<int>();

                foreach (var timeline in this._slotTimelines)
                {
                    var timelineName = timeline.slot.name;
                    if (!(slotTimelines.ContainsKey(timelineName)))
                    {
                        slotTimelines[timelineName] = new List<SlotTimelineState>();
                    }

                    slotTimelines[timelineName].Add(timeline);
                }

                foreach (var slot in this._armature.GetSlots())
                {
                    var boneName = slot.parent.name;
                    if (!this.ContainsBoneMask(boneName))
                    {
                        continue;
                    }

                    var timelineName = slot.name;
                    var timelineDatas = this._animationData.GetSlotTimelines(timelineName);

                    if (slotTimelines.ContainsKey(timelineName))
                    {
                        slotTimelines.Remove(timelineName);
                    }
                    else
                    {
                        var displayIndexFlag = false;
                        var colorFlag = false;
                        ffdFlags.Clear();

                        if (timelineDatas != null)
                        {
                            foreach (var timelineData in timelineDatas)
                            {
                                switch (timelineData.type)
                                {
                                    case TimelineType.SlotDisplay:
                                        {
                                            var timeline = BaseObject.BorrowObject<SlotDislayTimelineState>();
                                            timeline.slot = slot;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._slotTimelines.Add(timeline);
                                            displayIndexFlag = true;
                                            break;
                                        }
                                    case TimelineType.SlotColor:
                                        {
                                            var timeline = BaseObject.BorrowObject<SlotColorTimelineState>();
                                            timeline.slot = slot;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._slotTimelines.Add(timeline);
                                            colorFlag = true;
                                            break;
                                        }
                                    case TimelineType.SlotDeform:
                                        {
                                            var timeline = BaseObject.BorrowObject<DeformTimelineState>();
                                            timeline.slot = slot;
                                            timeline.Init(this._armature, this, timelineData);
                                            this._slotTimelines.Add(timeline);
                                            ffdFlags.Add((int)timeline.vertexOffset);
                                            break;
                                        }

                                    default:
                                        break;
                                }
                            }
                        }

                        if (this.resetToPose)
                        {
                            if (!displayIndexFlag)
                            {
                                var timeline = BaseObject.BorrowObject<SlotDislayTimelineState>();
                                timeline.slot = slot;
                                timeline.Init(this._armature, this, null);
                                this._slotTimelines.Add(timeline);
                                this._poseTimelines.Add(timeline);
                            }

                            if (!colorFlag)
                            {
                                var timeline = BaseObject.BorrowObject<SlotColorTimelineState>();
                                timeline.slot = slot;
                                timeline.Init(this._armature, this, null);
                                this._slotTimelines.Add(timeline);
                                this._poseTimelines.Add(timeline);
                            }

                            if (slot.rawDisplayDatas != null)
                            {
                                foreach (var displayData in slot.rawDisplayDatas)
                                {
                                    if (displayData != null && displayData.type == DisplayType.Mesh)
                                    {
                                        var meshOffset = (displayData as MeshDisplayData).vertices.offset;
                                        if (!ffdFlags.Contains(meshOffset))
                                        {
                                            var timeline = BaseObject.BorrowObject<DeformTimelineState>();
                                            timeline.vertexOffset = meshOffset;
                                            timeline.slot = slot;
                                            timeline.Init(this._armature, this, null);
                                            this._slotTimelines.Add(timeline);
                                            this._poseTimelines.Add(timeline);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var timelines in slotTimelines.Values)
                {
                    foreach (var timeline in timelines)
                    {
                        this._slotTimelines.Remove(timeline);
                        timeline.ReturnToPool();
                    }
                }
            }






        }

        private void _AdvanceFadeTime(float passedTime)
        {
            var isFadeOut = this._fadeState > 0;

            if (this._subFadeState < 0)
            {
                this._subFadeState = 0;

                var eventType = isFadeOut ? EventObject.FADE_OUT : EventObject.FADE_IN;
                if (this._armature.eventDispatcher.HasDBEventListener(eventType))
                {
                    var eventObject = BaseObject.BorrowObject<EventObject>();
                    eventObject.type = eventType;
                    eventObject.armature = this._armature;
                    eventObject.animationState = this;
                    this._armature._dragonBones.BufferEvent(eventObject);
                }
            }

            if (passedTime < 0.0f)
            {
                passedTime = -passedTime;
            }

            this._fadeTime += passedTime;

            if (this._fadeTime >= this.fadeTotalTime)
            {
                this._subFadeState = 1;
                this._fadeProgress = isFadeOut ? 0.0f : 1.0f;
            }
            else if (this._fadeTime > 0.0f)
            {
                this._fadeProgress = isFadeOut ? (1.0f - this._fadeTime / this.fadeTotalTime) : (this._fadeTime / this.fadeTotalTime);
            }
            else
            {
                this._fadeProgress = isFadeOut ? 1.0f : 0.0f;
            }

            if (this._subFadeState > 0)
            {
                if (!isFadeOut)
                {
                    this._playheadState |= 1;
                    this._fadeState = 0;
                }

                var eventType = isFadeOut ? EventObject.FADE_OUT_COMPLETE : EventObject.FADE_IN_COMPLETE;
                if (this._armature.eventDispatcher.HasDBEventListener(eventType))
                {
                    var eventObject = BaseObject.BorrowObject<EventObject>();
                    eventObject.type = eventType;
                    eventObject.armature = this._armature;
                    eventObject.animationState = this;
                    this._armature._dragonBones.BufferEvent(eventObject);
                }
            }
        }

        internal void Init(Armature armature, AnimationData animationData, AnimationConfig animationConfig)
        {
            if (this._armature != null)
            {
                return;
            }

            this._armature = armature;

            this._animationData = animationData;
            this.resetToPose = animationConfig.resetToPose;
            this.additiveBlending = animationConfig.additiveBlending;
            this.displayControl = animationConfig.displayControl;
            this.actionEnabled = animationConfig.actionEnabled;
            this.layer = animationConfig.layer;
            this.playTimes = animationConfig.playTimes;
            this.timeScale = animationConfig.timeScale;
            this.fadeTotalTime = animationConfig.fadeInTime;
            this.autoFadeOutTime = animationConfig.autoFadeOutTime;
            this.weight = animationConfig.weight;
            this.name = animationConfig.name.Length > 0 ? animationConfig.name : animationConfig.animation;
            this.group = animationConfig.group;

            if (animationConfig.pauseFadeIn)
            {
                this._playheadState = 2;
            }
            else
            {
                this._playheadState = 3;
            }

            if (animationConfig.duration < 0.0f)
            {
                this._position = 0.0f;
                this._duration = this._animationData.duration;
                if (animationConfig.position != 0.0f)
                {
                    if (this.timeScale >= 0.0f)
                    {
                        this._time = animationConfig.position;
                    }
                    else
                    {
                        this._time = animationConfig.position - this._duration;
                    }
                }
                else
                {
                    this._time = 0.0f;
                }
            }
            else
            {
                this._position = animationConfig.position;
                this._duration = animationConfig.duration;
                this._time = 0.0f;
            }

            if (this.timeScale < 0.0f && this._time == 0.0f)
            {
                this._time = -0.000001f;
            }

            if (this.fadeTotalTime <= 0.0f)
            {
                this._fadeProgress = 0.999999f;
            }

            if (animationConfig.boneMask.Count > 0)
            {
                this._boneMask.ResizeList(animationConfig.boneMask.Count);
                for (int i = 0, l = this._boneMask.Count; i < l; ++i)
                {
                    this._boneMask[i] = animationConfig.boneMask[i];
                }
            }

            this._actionTimeline = BaseObject.BorrowObject<ActionTimelineState>();
            this._actionTimeline.Init(this._armature, this, this._animationData.actionTimeline);
            this._actionTimeline.currentTime = this._time;
            if (this._actionTimeline.currentTime < 0.0f)
            {
                this._actionTimeline.currentTime = this._duration - this._actionTimeline.currentTime;
            }

            if (this._animationData.zOrderTimeline != null)
            {
                this._zOrderTimeline = BaseObject.BorrowObject<ZOrderTimelineState>();
                this._zOrderTimeline.Init(this._armature, this, this._animationData.zOrderTimeline);
            }
        }
        internal void AdvanceTime(float passedTime, float cacheFrameRate)
        {
            this._blendState.dirty = true;

            if (this._fadeState != 0 || this._subFadeState != 0)
            {
                this._AdvanceFadeTime(passedTime);
            }

            if (this._playheadState == 3)
            {
                if (this.timeScale != 1.0f)
                {
                    passedTime *= this.timeScale;
                }

                this._time += passedTime;
            }

            if (this._timelineDirty != 0)
            {
                if (this._timelineDirty == 2)
                {
                    this._UpdateTimelines();
                }

                this._timelineDirty = 0;
                this._UpdateBoneAndSlotTimelines();
            }

            if (this.weight == 0.0f)
            {
                return;
            }

            var isCacheEnabled = this._fadeState == 0 && cacheFrameRate > 0.0f;
            var isUpdateTimeline = true;
            var isUpdateBoneTimeline = true;
            var time = this._time;
            this._weightResult = this.weight * this._fadeProgress;

            if (this._parent != null)
            {
                this._weightResult *= this._parent._weightResult / this._parent._fadeProgress;
            }

            if (this._actionTimeline.playState <= 0)
            {
                this._actionTimeline.Update(time);
            }

            if (isCacheEnabled)
            {
                var internval = cacheFrameRate * 2.0f;
                this._actionTimeline.currentTime = (float)Math.Floor(this._actionTimeline.currentTime * internval) / internval;
            }

            if (this._zOrderTimeline != null && this._zOrderTimeline.playState <= 0)
            {
                this._zOrderTimeline.Update(time);
            }

            if (isCacheEnabled)
            {
                var cacheFrameIndex = (int)Math.Floor(this._actionTimeline.currentTime * cacheFrameRate);
                if (this._armature._cacheFrameIndex == cacheFrameIndex)
                {
                    isUpdateTimeline = false;
                    isUpdateBoneTimeline = false;
                }
                else
                {
                    this._armature._cacheFrameIndex = cacheFrameIndex;
                    if (this._animationData.cachedFrames[cacheFrameIndex])
                    {
                        isUpdateBoneTimeline = false;
                    }
                    else
                    {
                        this._animationData.cachedFrames[cacheFrameIndex] = true;
                    }
                }
            }

            if (isUpdateTimeline)
            {
                if (isUpdateBoneTimeline)
                {
                    for (int i = 0, l = this._boneTimelines.Count; i < l; ++i)
                    {
                        var timeline = this._boneTimelines[i];

                        if (timeline.playState <= 0)
                        {
                            timeline.Update(time);
                        }

                        if (i == l - 1 || timeline.bone != this._boneTimelines[i + 1].bone)
                        {
                            var state = timeline.bone._blendState.Update(this._weightResult, this.layer);
                            if (state != 0)
                            {
                                timeline.Blend(state);
                            }
                        }
                    }
                }

                if (this.displayControl)
                {
                    for (int i = 0, l = this._slotTimelines.Count; i < l; ++i)
                    {
                        var timeline = this._slotTimelines[i];
                        var displayController = timeline.slot.displayController;

                        if (
                            displayController == null ||
                            displayController == this.name ||
                            displayController == this.group
                        )
                        {
                            if (timeline.playState <= 0)
                            {
                                timeline.Update(time);
                            }
                        }
                    }
                }

                for (int i = 0, l = this._constraintTimelines.Count; i < l; ++i)
                {
                    var timeline = this._constraintTimelines[i];
                    if (timeline.playState <= 0)
                    {
                        timeline.Update(time);
                    }
                }
            }

            if (this._fadeState == 0)
            {
                if (this._subFadeState > 0)
                {
                    this._subFadeState = 0;

                    if (this._poseTimelines.Count > 0)
                    {
                        foreach (var timeline in this._poseTimelines)
                        {
                            if (timeline is BoneTimelineState)
                            {
                                this._boneTimelines.Remove(timeline as BoneTimelineState);
                            }
                            else if (timeline is SlotTimelineState)
                            {
                                this._slotTimelines.Remove(timeline as SlotTimelineState);
                            }
                            else if (timeline is ConstraintTimelineState)
                            {
                                this._constraintTimelines.Remove(timeline as ConstraintTimelineState);
                            }

                            timeline.ReturnToPool();
                        }

                        this._poseTimelines.Clear();
                    }
                }

                if (this._actionTimeline.playState > 0)
                {
                    if (this.autoFadeOutTime >= 0.0f)
                    {
                        this.FadeOut(this.autoFadeOutTime);
                    }
                }
            }
        }


        public void Play()
        {
            this._playheadState = 3;
        }

        public void Stop()
        {
            this._playheadState &= 1;
        }

        public void FadeOut(float fadeOutTime, bool pausePlayhead = true)
        {
            if (fadeOutTime < 0.0f)
            {
                fadeOutTime = 0.0f;
            }

            if (pausePlayhead)
            {
                this._playheadState &= 2;
            }

            if (this._fadeState > 0)
            {
                if (fadeOutTime > this.fadeTotalTime - this._fadeTime)
                {
                    return;
                }
            }
            else
            {
                this._fadeState = 1;
                this._subFadeState = -1;

                if (fadeOutTime <= 0.0f || this._fadeProgress <= 0.0f)
                {
                    this._fadeProgress = 0.000001f;
                }

                foreach (var timeline in this._boneTimelines)
                {
                    timeline.FadeOut();
                }

                foreach (var timeline in this._slotTimelines)
                {
                    timeline.FadeOut();
                }

                foreach (var timeline in this._constraintTimelines)
                {
                    timeline.FadeOut();
                }
            }

            this.displayControl = false;
            this.fadeTotalTime = this._fadeProgress > 0.000001f ? fadeOutTime / this._fadeProgress : 0.0f;
            this._fadeTime = this.fadeTotalTime * (1.0f - this._fadeProgress);
        }


        public bool ContainsBoneMask(string boneName)
        {
            return this._boneMask.Count == 0 || this._boneMask.IndexOf(boneName) >= 0;
        }

        public void AddBoneMask(string boneName, bool recursive = true)
        {
            var currentBone = this._armature.GetBone(boneName);
            if (currentBone == null)
            {
                return;
            }

            if (this._boneMask.IndexOf(boneName) < 0)
            {
                this._boneMask.Add(boneName);
            }

            if (recursive)
            {
                foreach (var bone in this._armature.GetBones())
                {
                    if (this._boneMask.IndexOf(bone.name) < 0 && currentBone.Contains(bone))
                    {
                        this._boneMask.Add(bone.name);
                    }
                }
            }

            this._timelineDirty = 1;
        }

        public void RemoveBoneMask(string boneName, bool recursive = true)
        {
            if (this._boneMask.Contains(boneName))
            {
                this._boneMask.Remove(boneName);
            }

            if (recursive)
            {
                var currentBone = this._armature.GetBone(boneName);
                if (currentBone != null)
                {
                    var bones = this._armature.GetBones();
                    if (this._boneMask.Count > 0)
                    {
                        foreach (var bone in bones)
                        {
                            if (this._boneMask.Contains(bone.name) && currentBone.Contains(bone))
                            {
                                this._boneMask.Remove(bone.name);
                            }
                        }
                    }
                    else
                    {
                        foreach (var bone in bones)
                        {
                            if (bone == currentBone)
                            {
                                continue;
                            }

                            if (!currentBone.Contains(bone))
                            {
                                this._boneMask.Add(bone.name);
                            }
                        }
                    }
                }
            }

            this._timelineDirty = 1;
        }

        public void RemoveAllBoneMask()
        {
            this._boneMask.Clear();
            this._timelineDirty = 1;
        }

        public bool isFadeIn
        {
            get { return this._fadeState < 0; }
        }

        public bool isFadeOut
        {
            get { return this._fadeState > 0; }
        }

        public bool isFadeComplete
        {
            get { return this._fadeState == 0; }
        }

        public bool isPlaying
        {
            get { return (this._playheadState & 2) != 0 && this._actionTimeline.playState <= 0; }
        }

        public bool isCompleted
        {
            get { return this._actionTimeline.playState > 0; }
        }

        public int currentPlayTimes
        {
            get { return this._actionTimeline.currentPlayTimes; }
        }


        public float totalTime
        {
            get { return this._duration; }
        }

        public float currentTime
        {
            get { return this._actionTimeline.currentTime; }
            set
            {
                var currentPlayTimes = this._actionTimeline.currentPlayTimes - (this._actionTimeline.playState > 0 ? 1 : 0);
                if (value < 0.0f || this._duration < value)
                {
                    value = (value % this._duration) + currentPlayTimes * this._duration;
                    if (value < 0.0f)
                    {
                        value += this._duration;
                    }
                }

                if (this.playTimes > 0 && currentPlayTimes == this.playTimes - 1 && value == this._duration)
                {
                    value = this._duration - 0.000001f;
                }

                if (this._time == value)
                {
                    return;
                }

                this._time = value;
                this._actionTimeline.SetCurrentTime(this._time);

                if (this._zOrderTimeline != null)
                {
                    this._zOrderTimeline.playState = -1;
                }

                foreach (var timeline in this._boneTimelines)
                {
                    timeline.playState = -1;
                }

                foreach (var timeline in this._slotTimelines)
                {
                    timeline.playState = -1;
                }
            }
        }
    }

    internal class BonePose : BaseObject
    {
        public readonly Transform current = new Transform();
        public readonly Transform delta = new Transform();
        public readonly Transform result = new Transform();

        protected override void _OnClear()
        {
            this.current.Identity();
            this.delta.Identity();
            this.result.Identity();
        }
    }

    internal class BlendState
    {
        public bool dirty;
        public int layer;
        public float leftWeight;
        public float layerWeight;
        public float blendWeight;

        public int Update(float weight, int p_layer)
        {
            if (this.dirty)
            {
                if (this.leftWeight > 0.0f)
                {
                    if (this.layer != p_layer)
                    {
                        if (this.layerWeight >= this.leftWeight)
                        {
                            this.leftWeight = 0.0f;

                            return 0;
                        }
                        else
                        {
                            this.layer = p_layer;
                            this.leftWeight -= this.layerWeight;
                            this.layerWeight = 0.0f;
                        }
                    }
                }
                else
                {
                    return 0;
                }

                weight *= this.leftWeight;
                this.layerWeight += weight;
                this.blendWeight = weight;

                return 2;
            }

            this.dirty = true;
            this.layer = p_layer;
            this.layerWeight = weight;
            this.leftWeight = 1.0f;
            this.blendWeight = weight;

            return 1;
        }

        public void Clear()
        {
            this.dirty = false;
            this.layer = 0;
            this.leftWeight = 0.0f;
            this.layerWeight = 0.0f;
            this.blendWeight = 0.0f;
        }
    }
}
