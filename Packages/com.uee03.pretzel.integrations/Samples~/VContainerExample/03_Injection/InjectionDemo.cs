using System;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Pretzel.Samples.VContainerExample
{
    public sealed class InjectionDemo : IStartable, IDisposable
    {
        private const string Topic = "03 Injection";

        private readonly ISampleLog _log;
        private readonly MethodInjected _method;
        private readonly MemberInjected _members;
        private readonly IObjectResolver _resolver;
        private GameObject _existing;

        // Taking the resolver is only right here because injecting existing objects is the point of the demo.
        public InjectionDemo(ISampleLog log, MethodInjected method, MemberInjected members, IObjectResolver resolver)
        {
            _log = log;
            _method = method;
            _members = members;
            _resolver = resolver;
        }

        public void Start()
        {
            _log.Write(Topic, $"Constructor: this demo received its log. Method: {_method.Log != null}. Field: {_members.FieldInjected}. Property: {_members.PropertyLog != null}");

            MethodInjected created = new();
            _resolver.Inject(created);
            _log.Write(Topic, $"Inject on an object created with new: {created.Log != null}");

            _existing = new GameObject("Injection Example", typeof(InjectedBehaviour));
            _resolver.InjectGameObject(_existing);
            _log.Write(Topic, $"InjectGameObject on an existing GameObject: {_existing.GetComponent<InjectedBehaviour>().Log != null}");
        }

        public void Dispose()
        {
            if (_existing != null)
            {
                UnityEngine.Object.Destroy(_existing);
            }
        }
    }
}
