using System;
using System.Collections.Generic;
using System.Linq;

namespace Pretzel.Setup.Composition
{
    internal static class PretzelServiceCatalog
    {
        public const string Data = "data";
        public const string Audio = "audio";
        public const string Api = "api";

        public static IReadOnlyList<PretzelServiceOption> All { get; } = new[]
        {
            new PretzelServiceOption("events", "Event bus (IEventBus)", "Pretzel.Events.EventBus, Pretzel.Events", "Pretzel.Events",
                _ => "builder.Register<EventBus>(Lifetime.Singleton).As<IEventBus>();"),
            new PretzelServiceOption("scenes", "Scene loader", "Pretzel.Scenes.SceneLoader, Pretzel.Scenes", "Pretzel.Scenes",
                _ => "builder.Register<SceneLoader>(Lifetime.Singleton);"),
            new PretzelServiceOption(Data, "Data store (IDataStore, DataIO.Default)", "Pretzel.Data.DataIO, Pretzel.Data", "Pretzel.Data",
                _ => "builder.Register<IDataStore>(_ => DataIO.Default, Lifetime.Singleton);"),
            new PretzelServiceOption("preferences", "Preferences service", "Pretzel.Preferences.PreferencesService, Pretzel.Preferences", "Pretzel.Preferences",
                settings => settings.pretzelServices.Contains(Data, StringComparer.OrdinalIgnoreCase)
                    ? "builder.Register(resolver => new PreferencesService(resolver.Resolve<IDataStore>()), Lifetime.Singleton);"
                    : "builder.Register(_ => new PreferencesService(), Lifetime.Singleton);"),
            new PretzelServiceOption(Api, "API client", "Pretzel.Api.ApiClient, Pretzel.Api", "Pretzel.Api",
                settings => string.IsNullOrWhiteSpace(settings.apiBaseUrl)
                    ? "builder.Register(_ => new ApiClient(), Lifetime.Singleton);"
                    : $"builder.Register(_ => new ApiClient({Literal(settings.apiBaseUrl.Trim())}), Lifetime.Singleton);"),
            new PretzelServiceOption("pooling", "Pool registry", "Pretzel.Pooling.PoolRegistry, Pretzel.Pooling", "Pretzel.Pooling",
                _ => "builder.Register(_ => new PoolRegistry(), Lifetime.Singleton);"),
            new PretzelServiceOption(Audio, "Audio manager (child of the root scope)", "Pretzel.Audio.AudioManager, Pretzel.Audio", "Pretzel.Audio",
                _ => "builder.RegisterComponentInHierarchy<AudioManager>();"),
        };

        public static PretzelServiceOption Find(string id)
        {
            foreach (PretzelServiceOption option in All)
            {
                if (string.Equals(option.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return option;
                }
            }

            return null;
        }

        private static string Literal(string value) => "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
