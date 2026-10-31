using UnityEngine.UIElements;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    /// <summary>Plain classes receive MonoBehaviours like any other dependency.</summary>
    public sealed class ComponentsDemo : IStartable
    {
        private const string Topic = "08 Components";

        private readonly ISampleLog _log;
        private readonly UIDocument _document;
        private readonly SceneMarker _marker;
        private readonly SpawnedComponent _spawned;

        public ComponentsDemo(ISampleLog log, UIDocument document, SceneMarker marker, SpawnedComponent spawned)
        {
            _log = log;
            _document = document;
            _marker = marker;
            _spawned = spawned;
        }

        public void Start()
        {
            _log.Write(Topic, $"RegisterComponent: {_document.GetType().Name} on '{_document.name}'");
            _log.Write(Topic, $"InHierarchy: {_marker.GetType().Name} found on '{_marker.name}'");
            _log.Write(Topic, $"OnNewGameObject: '{_spawned.name}' created on first resolve, injected {_spawned.Injected}");
        }
    }
}
