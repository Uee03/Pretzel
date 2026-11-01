namespace Pretzel.Cameras.Editor
{
    public enum CameraRigKind
    {
        /// <summary>Orthographic camera, follow with a dead zone and damping, playfield fit.</summary>
        TwoD,

        /// <summary>Perspective camera held at a fixed distance from a gameplay plane, follow with a dead zone and damping.</summary>
        TwoPointFiveD,

        /// <summary>Perspective camera orbiting a target, rotation composer.</summary>
        ThreeD
    }
}
