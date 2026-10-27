namespace Pretzel.UI
{
    public enum BillboardMode
    {
        /// <summary>Copies the camera rotation, so every billboard is parallel to the screen.</summary>
        MatchCamera = 0,

        /// <summary>Turns toward the camera position.</summary>
        FaceCamera = 1,

        /// <summary>Turns toward the camera around the world Y axis only, staying upright.</summary>
        UprightFaceCamera = 2
    }
}
