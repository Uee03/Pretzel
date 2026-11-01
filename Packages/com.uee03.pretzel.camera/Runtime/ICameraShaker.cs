namespace Pretzel.Cameras
{
    /// <summary>Anything that can shake the screen. Effects depend on this, not on a particular camera.</summary>
    public interface ICameraShaker
    {
        /// <summary>Current trauma, 0 to 1.</summary>
        float Trauma { get; }

        /// <summary>Adds trauma between 0 and 1. A light hit is 0.2, a big explosion 0.6 or more.</summary>
        void AddTrauma(float amount);
    }
}
