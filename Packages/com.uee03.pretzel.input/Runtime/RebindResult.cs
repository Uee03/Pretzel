namespace Pretzel.Inputs
{
    public enum RebindResult
    {
        /// <summary>The player pressed a control and the binding changed.</summary>
        Completed,

        /// <summary>The player cancelled, or the session was disposed. The binding is unchanged.</summary>
        Cancelled
    }
}
