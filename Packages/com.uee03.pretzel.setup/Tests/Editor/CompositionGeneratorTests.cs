using System;
using NUnit.Framework;
using Pretzel.Setup.Composition;

namespace Pretzel.Setup.Tests
{
    public sealed class CompositionGeneratorTests
    {
        private static CompositionSettings Settings()
        {
            CompositionSettings settings = new() { namespaceName = "MyGame.Composition", scopeName = "Game" };
            settings.Normalize();
            return settings;
        }

        [Test]
        public void Registrations_CoverServicesEntryPointsAndFlow()
        {
            CompositionSettings settings = Settings();
            settings.services.Add(new ServiceDefinition { name = "InventoryService" });
            settings.services.Add(new ServiceDefinition { name = "Spawner", withInterface = false, tickable = true, lifetime = ServiceLifetime.Scoped });
            settings.services.Add(new ServiceDefinition { name = "QuestManager", startable = true });

            string source = CompositionGenerator.RegistrationsSource(settings);

            Assert.That(source, Does.Contain("namespace MyGame.Composition"));
            Assert.That(source, Does.Contain("public sealed partial class GameInstaller"));
            Assert.That(source, Does.Contain("builder.Register<IInventoryService, InventoryService>(Lifetime.Singleton);"));
            Assert.That(source, Does.Contain("builder.RegisterEntryPoint<Spawner>(Lifetime.Scoped).AsSelf();"));
            Assert.That(source, Does.Contain("builder.RegisterEntryPoint<QuestManager>(Lifetime.Singleton);"));
            Assert.That(source, Does.Contain("builder.RegisterEntryPoint<GameFlow>();"));
            Assert.That(source, Does.Not.Contain("\r"));
        }

        [Test]
        public void PretzelServices_AreRegisteredWhenSelected()
        {
            foreach (string id in new[] { "events", PretzelServiceCatalog.Data, "preferences", PretzelServiceCatalog.Api })
            {
                Assume.That(PretzelServiceCatalog.Find(id).IsInstalled, Is.True, id + " module is not installed.");
            }

            CompositionSettings settings = Settings();
            settings.createFlow = false;
            settings.pretzelServices.Add("events");
            settings.pretzelServices.Add(PretzelServiceCatalog.Data);
            settings.pretzelServices.Add("preferences");
            settings.pretzelServices.Add(PretzelServiceCatalog.Api);
            settings.apiBaseUrl = "https://api.example.com/\"v1\"";

            string source = CompositionGenerator.RegistrationsSource(settings);

            Assert.That(source, Does.Contain("using Pretzel.Events;"));
            Assert.That(source, Does.Contain("builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();"));
            Assert.That(source, Does.Contain("builder.Register<IDataStore>(_ => DataIO.Default, Lifetime.Singleton);"));
            Assert.That(source, Does.Contain("new PreferencesService(resolver.Resolve<IDataStore>())"));
            Assert.That(source, Does.Contain("new ApiClient(\"https://api.example.com/\\\"v1\\\"\")"));
            Assert.That(source, Does.Not.Contain("GameFlow"));
            Assert.That(source, Does.Not.Contain("SceneLoader"));
        }

        [TestCase("My Game")]
        [TestCase("1Game")]
        [TestCase("Game..Root")]
        public void InvalidNamespace_IsRejected(string name)
        {
            CompositionSettings settings = Settings();
            settings.namespaceName = name;

            Assert.Throws<ArgumentException>(() => CompositionGenerator.Validate(settings));
        }

        [Test]
        public void DuplicateOrInvalidServices_AndFoldersOutsideAssets_AreRejected()
        {
            CompositionSettings duplicate = Settings();
            duplicate.services.Add(new ServiceDefinition { name = "Audio" });
            duplicate.services.Add(new ServiceDefinition { name = "Audio" });
            Assert.Throws<ArgumentException>(() => CompositionGenerator.Validate(duplicate));

            CompositionSettings clash = Settings();
            clash.services.Add(new ServiceDefinition { name = "GameInstaller" });
            Assert.Throws<ArgumentException>(() => CompositionGenerator.Validate(clash));

            CompositionSettings folder = Settings();
            folder.codeFolder = "Packages/Code";
            Assert.Throws<ArgumentException>(() => CompositionGenerator.Validate(folder));

            Assert.DoesNotThrow(() => CompositionGenerator.Validate(Settings()));
        }
    }
}
