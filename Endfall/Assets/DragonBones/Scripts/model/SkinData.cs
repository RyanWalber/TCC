using System.Collections.Generic;

namespace DragonBones
{

    public class SkinData : BaseObject
    {

        public string name;
        public readonly Dictionary<string, List<DisplayData>> displays = new Dictionary<string, List<DisplayData>>();
        public ArmatureData parent;

        protected override void _OnClear()
        {
            foreach (var list in this.displays.Values)
            {
                foreach (var display in list)
                {
                    display.ReturnToPool();
                }
            }

            this.name = "";
            this.displays.Clear();
            this.parent = null;
        }

        public void AddDisplay(string slotName, DisplayData value)
        {
            if (!string.IsNullOrEmpty(slotName) && value != null && !string.IsNullOrEmpty(value.name))
            {
                if (!this.displays.ContainsKey(slotName))
                {
                    this.displays[slotName] = new List<DisplayData>();
                }

                if (value != null)
                {
                    value.parent = this;
                }

                var slotDisplays = this.displays[slotName];
                slotDisplays.Add(value);
            }
        }
        public DisplayData GetDisplay(string slotName, string displayName)
        {
            var slotDisplays = this.GetDisplays(slotName);
            if (slotDisplays != null)
            {
                foreach (var display in slotDisplays)
                {
                    if (display != null && display.name == displayName)
                    {
                        return display;
                    }
                }
            }

            return null;
        }
        public List<DisplayData> GetDisplays(string slotName)
        {
            if (string.IsNullOrEmpty(slotName) || !this.displays.ContainsKey(slotName))
            {
                return null;
            }

            return this.displays[slotName];
        }

    }
}
