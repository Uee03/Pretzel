using System;

namespace Pretzel.Setup.Composition
{
    /// <summary>A Pretzel service the generated installer can register, available when its module is installed.</summary>
    internal sealed class PretzelServiceOption
    {
        private readonly string _probeType;
        private readonly Func<CompositionSettings, string> _registration;

        public PretzelServiceOption(string id, string label, string probeType, string usingNamespace, Func<CompositionSettings, string> registration)
        {
            Id = id;
            Label = label;
            _probeType = probeType;
            UsingNamespace = usingNamespace;
            _registration = registration;
        }

        public string Id { get; }

        public string Label { get; }

        public string UsingNamespace { get; }

        public bool IsInstalled => Type.GetType(_probeType) != null;

        public string Registration(CompositionSettings settings) => _registration(settings);
    }
}
