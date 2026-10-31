using System;

namespace Pretzel.Samples.Cloud.CodeAndConfig
{
    /// <summary>The JSON result of the <c>SayHello</c> endpoint.</summary>
    [Serializable]
    public sealed class GreetingResponse
    {
        public string message;
    }
}
