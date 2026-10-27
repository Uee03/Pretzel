namespace Pretzel.DI
{
    public enum Lifetime
    {
        /// <summary>A new instance on every resolve.</summary>
        Transient = 0,

        /// <summary>One instance per scope. The root container is a scope too.</summary>
        Scoped = 1,

        /// <summary>One instance for the container that registered it and every child scope.</summary>
        Singleton = 2
    }
}
