using System;

namespace Pretzel.Samples.Cloud.CodeAndConfig
{
    /// <summary>The request of the <c>SayHello</c> endpoint. Field names are the endpoint's parameter names.</summary>
    [Serializable]
    public sealed class GreetingRequest
    {
        public string name = "Pretzel";
    }
}
