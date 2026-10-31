using VContainer;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Field and property injection. Prefer constructors; these hide what a class needs.</summary>
    public sealed class MemberInjected
    {
        [Inject] private ISampleLog _fieldLog = null;

        [Inject]
        public ISampleLog PropertyLog { get; private set; }

        public bool FieldInjected => _fieldLog != null;
    }
}
