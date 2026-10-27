namespace Pretzel.Scenes
{
    /// <summary>Implement on an <see cref="ISceneTransition"/> that can show a status line, such as "Spawning enemies".</summary>
    public interface ISceneStatusDisplay
    {
        void ReportStatus(string status);
    }
}
