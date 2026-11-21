namespace DragonBones
{

    public interface IAnimatable
    {

        void AdvanceTime(float passedTime);
        WorldClock clock
        {
            get;
            set;
        }
    }
}
