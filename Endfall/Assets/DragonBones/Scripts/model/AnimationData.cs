using System;
using System.Collections.Generic;

namespace DragonBones
{

    public class AnimationData : BaseObject
    {
        public uint frameIntOffset;
        public uint frameFloatOffset;
        public uint frameOffset;

        public uint frameCount;

        public uint playTimes;

        public float duration;
        public float scale;

        public float fadeInTime;
        public float cacheFrameRate;

        public string name;
        public readonly List<bool> cachedFrames = new List<bool>();
        public readonly Dictionary<string, List<TimelineData>> boneTimelines = new Dictionary<string, List<TimelineData>>();
        public readonly Dictionary<string, List<TimelineData>> slotTimelines = new Dictionary<string, List<TimelineData>>();
        public readonly Dictionary<string, List<TimelineData>> constraintTimelines = new Dictionary<string, List<TimelineData>>();
        public readonly Dictionary<string, List<int>> boneCachedFrameIndices = new Dictionary<string, List<int>>();
        public readonly Dictionary<string, List<int>> slotCachedFrameIndices = new Dictionary<string, List<int>>();
        public TimelineData actionTimeline = null;
        public TimelineData zOrderTimeline = null;
        public ArmatureData parent;

        public AnimationData()
        {

        }

        protected override void _OnClear()
        {
            foreach (var pair in boneTimelines)
            {
                for (int i = 0; i < pair.Value.Count; ++i)
                {
                    pair.Value[i].ReturnToPool();
                }
            }

            foreach (var pair in slotTimelines)
            {
                for (int i = 0; i < pair.Value.Count; ++i)
                {
                    pair.Value[i].ReturnToPool();
                }
            }

            foreach (var pair in constraintTimelines)
            {
                for (int i = 0; i < pair.Value.Count; ++i)
                {
                    pair.Value[i].ReturnToPool();
                }
            }

            if (this.actionTimeline != null)
            {
                this.actionTimeline.ReturnToPool();
            }

            if (this.zOrderTimeline != null)
            {
                this.zOrderTimeline.ReturnToPool();
            }

            this.frameIntOffset = 0;
            this.frameFloatOffset = 0;
            this.frameOffset = 0;
            this.frameCount = 0;
            this.playTimes = 0;
            this.duration = 0.0f;
            this.scale = 1.0f;
            this.fadeInTime = 0.0f;
            this.cacheFrameRate = 0.0f;
            this.name = "";
            this.boneTimelines.Clear();
            this.slotTimelines.Clear();
            this.constraintTimelines.Clear();
            this.boneCachedFrameIndices.Clear();
            this.slotCachedFrameIndices.Clear();
            this.cachedFrames.Clear();

            this.actionTimeline = null;
            this.zOrderTimeline = null;
            this.parent = null;
        }

        public void CacheFrames(float frameRate)
        {
            if (this.cacheFrameRate > 0.0f)
            {
                return;
            }

            this.cacheFrameRate = Math.Max((float)Math.Ceiling(frameRate * scale), 1.0f);
            var cacheFrameCount = (int)Math.Ceiling(this.cacheFrameRate * duration) + 1;

            cachedFrames.ResizeList(0, false);
            cachedFrames.ResizeList(cacheFrameCount, false);

            foreach (var bone in this.parent.sortedBones)
            {
                var indices = new List<int>(cacheFrameCount);
                for (int i = 0, l = indices.Capacity; i < l; ++i)
                {
                    indices.Add(-1);
                }

                this.boneCachedFrameIndices[bone.name] = indices;
            }

            foreach (var slot in this.parent.sortedSlots)
            {
                var indices = new List<int>(cacheFrameCount);
                for (int i = 0, l = indices.Capacity; i < l; ++i)
                {
                    indices.Add(-1);
                }

                this.slotCachedFrameIndices[slot.name] = indices;
            }
        }

        public void AddBoneTimeline(BoneData bone, TimelineData tiemline)
        {
            if (bone == null || tiemline == null)
            {
                return;
            }

            if (!this.boneTimelines.ContainsKey(bone.name))
            {
                this.boneTimelines[bone.name] = new List<TimelineData>();
            }

            var timelines = this.boneTimelines[bone.name];
            if (!timelines.Contains(tiemline))
            {
                timelines.Add(tiemline);
            }
        }
        public void AddSlotTimeline(SlotData slot, TimelineData timeline)
        {
            if (slot == null || timeline == null)
            {
                return;
            }

            if (!this.slotTimelines.ContainsKey(slot.name))
            {
                this.slotTimelines[slot.name] = new List<TimelineData>();
            }

            var timelines = this.slotTimelines[slot.name];
            if (!timelines.Contains(timeline))
            {
                timelines.Add(timeline);
            }
        }

        public void AddConstraintTimeline(ConstraintData constraint, TimelineData timeline)
        {
            if (constraint == null || timeline == null)
            {
                return;
            }

            if (!this.constraintTimelines.ContainsKey(constraint.name))
            {
                this.constraintTimelines[constraint.name] = new List<TimelineData>();
            }

            var timelines = this.constraintTimelines[constraint.name];
            if (!timelines.Contains(timeline))
            {
                timelines.Add(timeline);
            }
        }

        public List<TimelineData> GetBoneTimelines(string timelineName)
        {
            return this.boneTimelines.ContainsKey(timelineName) ? this.boneTimelines[timelineName] : null;
        }
        public List<TimelineData> GetSlotTimelines(string timelineName)
        {
            return slotTimelines.ContainsKey(timelineName) ? slotTimelines[timelineName] : null;
        }

        public List<TimelineData> GetConstraintTimelines(string timelineName)
        {
            return constraintTimelines.ContainsKey(timelineName) ? constraintTimelines[timelineName] : null;
        }
        public List<int> GetBoneCachedFrameIndices(string boneName)
        {
            return this.boneCachedFrameIndices.ContainsKey(boneName) ? this.boneCachedFrameIndices[boneName] : null;
        }

        public List<int> GetSlotCachedFrameIndices(string slotName)
        {
            return this.slotCachedFrameIndices.ContainsKey(slotName) ? this.slotCachedFrameIndices[slotName] : null;
        }
    }

    public class TimelineData : BaseObject
    {
        public TimelineType type;
        public uint offset;
        public int frameIndicesOffset;

        protected override void _OnClear()
        {
            this.type = TimelineType.BoneAll;
            this.offset = 0;
            this.frameIndicesOffset = -1;
        }
    }
}
