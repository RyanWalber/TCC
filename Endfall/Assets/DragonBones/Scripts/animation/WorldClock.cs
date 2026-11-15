using System;
using System.Collections.Generic;

namespace DragonBones
{

    public class WorldClock : IAnimatable
    {

        public float time = 0.0f;


        public float timeScale = 1.0f;
        private float _systemTime = 0.0f;
        private readonly List<IAnimatable> _animatebles = new List<IAnimatable>();
        private WorldClock _clock = null;

        public WorldClock(float time = -1.0f)
        {
            this.time = time;
            this._systemTime = DateTime.Now.Ticks * 0.01f * 0.001f;
        }


        public void AdvanceTime(float passedTime)
        {
            if (float.IsNaN(passedTime))
            {
                passedTime = 0.0f;
            }

            var currentTime = DateTime.Now.Ticks * 0.01f * 0.001f;
            if (passedTime < 0.0f)
            {
                passedTime = currentTime - this._systemTime;
            }

            this._systemTime = currentTime;

            if (this.timeScale != 1.0f)
            {
                passedTime *= this.timeScale;
            }

            if (passedTime == 0.0f)
            {
                return;
            }

            if (passedTime < 0.0f)
            {
                this.time -= passedTime;
            }
            else
            {
                this.time += passedTime;
            }

            int i = 0, r = 0, l = _animatebles.Count;
            for (; i < l; ++i)
            {
                var animateble = _animatebles[i];
                if (animateble != null)
                {
                    if (r > 0)
                    {
                        _animatebles[i - r] = animateble;
                        _animatebles[i] = null;
                    }

                    animateble.AdvanceTime(passedTime);
                }
                else
                {
                    r++;
                }
            }

            if (r > 0)
            {
                l = _animatebles.Count;
                for (; i < l; ++i)
                {
                    var animateble = _animatebles[i];
                    if (animateble != null)
                    {
                        _animatebles[i - r] = animateble;
                    }
                    else
                    {
                        r++;
                    }
                }

                _animatebles.ResizeList(l - r, null);
            }
        }

        public bool Contains(IAnimatable value)
        {
            if (value == this)
            {
                return false;
            }

            IAnimatable ancestor = value;
            while (ancestor != this && ancestor != null)
            {
                ancestor = ancestor.clock;
            }

            return ancestor == this;
        }

        public void Add(IAnimatable value)
        {
            if (value != null && !_animatebles.Contains(value))
            {
                _animatebles.Add(value);
                value.clock = this;
            }
        }

        public void Remove(IAnimatable value)
        {
            var index = _animatebles.IndexOf(value);
            if (index >= 0)
            {
                _animatebles[index] = null;
                value.clock = null;
            }
        }

        public void Clear()
        {
            for (int i = 0, l = _animatebles.Count; i < l; ++i)
            {
                var animateble = _animatebles[i];
                _animatebles[i] = null;
                if (animateble != null)
                {
                    animateble.clock = null;
                }
            }
        }

        [System.Obsolete("")]
        public WorldClock clock
        {
            get { return _clock; }
            set
            {
                if (_clock == value)
                {
                    return;
                }

                if (_clock != null)
                {
                    _clock.Remove(this);
                }

                _clock = value;

                if (_clock != null)
                {
                    _clock.Add(this);
                }
            }
        }
    }
}