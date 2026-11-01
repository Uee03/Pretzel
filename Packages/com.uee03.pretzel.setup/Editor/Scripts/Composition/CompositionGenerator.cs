using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor.Compilation;

namespace Pretzel.Setup.Composition
{
    /// <summary>
    /// Writes the composition root code. The installer, flow and service files are created once and then belong to the
    /// project; the registrations file is rewritten on every run.
    /// </summary>
    internal static class CompositionGenerator
    {
        private static readonly Regex _identifier = new("^[A-Za-z_][A-Za-z0-9_]*$");

        /// <summary>Ready-to-use files written with the starter services, in dependency order.</summary>
        public static IReadOnlyList<string> StarterTemplates { get; } = new[]
        {
            "ISaveService", "SaveService", "GameState", "GameProgress", "IGameService", "GameService"
        };

        public const string ExampleTemplate = "GameServicesExample";

        public static string GeneratedFileName(CompositionSettings settings) => settings.InstallerName + ".Generated.cs";

        /// <summary>Returns the paths of the files that were written. Throws <see cref="ArgumentException"/> for invalid settings.</summary>
        public static IReadOnlyList<string> Generate(CompositionSettings settings)
        {
            Validate(settings);
            Directory.CreateDirectory(settings.codeFolder);
            List<string> written = new();

            WriteIfMissing(written, settings, settings.InstallerName + ".cs", InstallerSource(settings));
            WriteIfChanged(written, settings, GeneratedFileName(settings), RegistrationsSource(settings));
            if (settings.createFlow)
            {
                WriteIfMissing(written, settings, settings.FlowName + ".cs", FlowSource(settings));
            }

            if (settings.starterServices)
            {
                bool reuseSaveService = CanReuseProjectSaveService(settings);
                foreach (string template in StarterTemplates)
                {
                    if (!reuseSaveService || !IsSaveTemplate(template))
                    {
                        WriteIfMissing(written, settings, template + ".cs", CompositionTemplates.Render(template, settings));
                    }
                }

                if (settings.starterExample)
                {
                    WriteIfMissing(written, settings, ExampleTemplate + ".cs", CompositionTemplates.Render(ExampleTemplate, settings));
                }
            }

            foreach (ServiceDefinition service in settings.services)
            {
                if (service.withInterface)
                {
                    WriteIfMissing(written, settings, service.InterfaceName + ".cs", InterfaceSource(settings, service));
                }

                WriteIfMissing(written, settings, service.name + ".cs", ServiceSource(settings, service));
            }

            return written;
        }

        public static void Validate(CompositionSettings settings)
        {
            if (!settings.namespaceName.Split('.').All(part => _identifier.IsMatch(part)))
            {
                throw new ArgumentException($"'{settings.namespaceName}' is not a valid namespace.");
            }

            if (!_identifier.IsMatch(settings.scopeName))
            {
                throw new ArgumentException($"'{settings.scopeName}' is not a valid scope name. Use letters, digits and underscores.");
            }

            foreach (string folder in new[] { settings.codeFolder, settings.assetFolder })
            {
                if (!string.Equals(folder, "Assets", StringComparison.Ordinal) && !folder.StartsWith("Assets/", StringComparison.Ordinal))
                {
                    throw new ArgumentException($"'{folder}' must be a folder under Assets/.");
                }
            }

            HashSet<string> names = new(StringComparer.Ordinal) { settings.InstallerName, settings.FlowName };
            if (settings.starterServices)
            {
                if (!PretzelServiceCatalog.Find(PretzelServiceCatalog.Data).IsInstalled)
                {
                    throw new ArgumentException("SaveService stores data with the Pretzel Data module. Install it from the Addons tab, or untick the starter services.");
                }

                names.UnionWith(StarterTemplates);
                names.Add(ExampleTemplate);
            }

            foreach (ServiceDefinition service in settings.services)
            {
                if (!_identifier.IsMatch(service.name ?? string.Empty))
                {
                    throw new ArgumentException($"'{service.name}' is not a valid class name.");
                }

                if (!names.Add(service.name))
                {
                    throw new ArgumentException($"'{service.name}' is used twice, or by a starter service.");
                }
            }
        }

        public static IReadOnlyList<PretzelServiceOption> SelectedPretzelServices(CompositionSettings settings)
        {
            return PretzelServiceCatalog.All
                .Where(option => option.IsInstalled && (settings.pretzelServices.Contains(option.Id, StringComparer.OrdinalIgnoreCase)
                                                        || (settings.starterServices && string.Equals(option.Id, PretzelServiceCatalog.Data, StringComparison.Ordinal))))
                .ToList();
        }

        public static string RegistrationsSource(CompositionSettings settings)
        {
            IReadOnlyList<PretzelServiceOption> pretzel = SelectedPretzelServices(settings);
            SortedSet<string> usings = new(StringComparer.Ordinal) { "VContainer", "VContainer.Unity" };
            foreach (PretzelServiceOption option in pretzel)
            {
                usings.Add(option.UsingNamespace);
            }

            StringBuilder body = new();
            foreach (PretzelServiceOption option in pretzel)
            {
                body.Append("            ").Append(option.Registration(settings)).Append('\n');
            }

            foreach (ServiceDefinition service in settings.services)
            {
                body.Append("            ").Append(Registration(service)).Append('\n');
            }

            if (settings.starterServices)
            {
                body.Append("            builder.Register<ISaveService, SaveService>(Lifetime.Singleton);\n");
                body.Append("            builder.Register<IGameService, GameService>(Lifetime.Singleton);\n");
                if (settings.starterExample)
                {
                    body.Append("            builder.RegisterEntryPoint<").Append(ExampleTemplate).Append(">();\n");
                }
            }

            if (settings.createFlow)
            {
                body.Append("            builder.RegisterEntryPoint<").Append(settings.FlowName).Append(">();\n");
            }

            StringBuilder source = new();
            source.Append("// <auto-generated>Pretzel > Project Setup > VContainer rewrites this file. Edit ")
                .Append(settings.InstallerName).Append(".cs instead.</auto-generated>\n");
            foreach (string name in usings)
            {
                source.Append("using ").Append(name).Append(";\n");
            }

            source.Append('\n')
                .Append("namespace ").Append(settings.namespaceName).Append('\n')
                .Append("{\n")
                .Append("    public sealed partial class ").Append(settings.InstallerName).Append('\n')
                .Append("    {\n")
                .Append("        private static void InstallGenerated(IContainerBuilder builder)\n")
                .Append("        {\n")
                .Append(body)
                .Append("        }\n")
                .Append("    }\n")
                .Append("}\n");
            return source.ToString();
        }

        private static string Registration(ServiceDefinition service)
        {
            string lifetime = "Lifetime." + service.lifetime;
            if (service.IsEntryPoint)
            {
                string entryPoint = $"builder.RegisterEntryPoint<{service.name}>({lifetime})";
                return service.withInterface ? entryPoint + ";" : entryPoint + ".AsSelf();";
            }

            return service.withInterface
                ? $"builder.Register<{service.InterfaceName}, {service.name}>({lifetime});"
                : $"builder.Register<{service.name}>({lifetime});";
        }

        private static string InstallerSource(CompositionSettings settings)
        {
            return "using Pretzel.Integrations.VContainer;\n"
                   + "using VContainer;\n"
                   + "using VContainer.Unity;\n\n"
                   + $"namespace {settings.namespaceName}\n"
                   + "{\n"
                   + $"    public sealed partial class {settings.InstallerName} : ScriptableInstaller\n"
                   + "    {\n"
                   + "        public override void Install(IContainerBuilder builder)\n"
                   + "        {\n"
                   + "            InstallGenerated(builder);\n"
                   + "        }\n"
                   + "    }\n"
                   + "}\n";
        }

        private static string FlowSource(CompositionSettings settings)
        {
            return "using VContainer.Unity;\n\n"
                   + $"namespace {settings.namespaceName}\n"
                   + "{\n"
                   + $"    public sealed class {settings.FlowName} : IStartable\n"
                   + "    {\n"
                   + "        public void Start()\n"
                   + "        {\n"
                   + "        }\n"
                   + "    }\n"
                   + "}\n";
        }

        private static string InterfaceSource(CompositionSettings settings, ServiceDefinition service)
        {
            return $"namespace {settings.namespaceName}\n"
                   + "{\n"
                   + $"    public interface {service.InterfaceName}\n"
                   + "    {\n"
                   + "    }\n"
                   + "}\n";
        }

        private static string ServiceSource(CompositionSettings settings, ServiceDefinition service)
        {
            List<string> bases = new();
            if (service.withInterface)
            {
                bases.Add(service.InterfaceName);
            }

            if (service.startable)
            {
                bases.Add("IStartable");
            }

            if (service.tickable)
            {
                bases.Add("ITickable");
            }

            StringBuilder source = new();
            if (service.IsEntryPoint)
            {
                source.Append("using VContainer.Unity;\n\n");
            }

            source.Append("namespace ").Append(settings.namespaceName).Append('\n')
                .Append("{\n")
                .Append("    public sealed class ").Append(service.name);
            if (bases.Count > 0)
            {
                source.Append(" : ").Append(string.Join(", ", bases));
            }

            source.Append('\n').Append("    {\n");
            List<string> members = new();
            if (service.startable)
            {
                members.Add("        public void Start()\n        {\n        }\n");
            }

            if (service.tickable)
            {
                members.Add("        public void Tick()\n        {\n        }\n");
            }

            source.Append(string.Join("\n", members)).Append("    }\n").Append("}\n");
            return source.ToString();
        }

        /// <summary>
        /// The Pretzel Data examples create ISaveService and SaveService in the global namespace. The generated GameService
        /// uses that pair instead of a second copy only when both are compiled, compatible and visible from the code folder.
        /// Otherwise the starter copies are written in the settings namespace, which never clashes with a global type.
        /// </summary>
        private static bool CanReuseProjectSaveService(CompositionSettings settings)
        {
            Type saveInterface = FindSingleGlobalType("ISaveService");
            Type saveClass = FindSingleGlobalType("SaveService");
            return IsCompatibleSaveService(saveInterface, saveClass)
                   && IsVisibleFrom(settings, saveInterface.Assembly)
                   && IsVisibleFrom(settings, saveClass.Assembly);
        }

        /// <summary>True when the pair offers what GameService and the generated registration need.</summary>
        internal static bool IsCompatibleSaveService(Type saveInterface, Type saveClass)
        {
            if (saveInterface == null || saveClass == null || !saveInterface.IsInterface || !saveClass.IsClass || saveClass.IsAbstract
                || !saveInterface.IsAssignableFrom(saveClass))
            {
                return false;
            }

            MethodInfo[] methods = saveInterface.GetMethods();
            bool hasMembers = methods.Any(method => string.Equals(method.Name, "LoadOrCreateAsync", StringComparison.Ordinal))
                              && methods.Any(method => string.Equals(method.Name, "Save", StringComparison.Ordinal));
            return hasMembers && saveClass.GetConstructors().Any(constructor => constructor.GetParameters()
                .All(parameter => string.Equals(parameter.ParameterType.FullName, "Pretzel.Data.IDataStore", StringComparison.Ordinal)));
        }

        private static bool IsSaveTemplate(string template)
        {
            return string.Equals(template, "ISaveService", StringComparison.Ordinal) || string.Equals(template, "SaveService", StringComparison.Ordinal);
        }

        private static Type FindSingleGlobalType(string name)
        {
            // Asks each compiled assembly by name instead of enumerating the AppDomain, which can include unloaded assemblies.
            List<Type> found = CompilationPipeline.GetAssemblies(AssembliesType.Editor)
                .Select(assembly => Type.GetType(name + ", " + assembly.name, false))
                .Where(type => type != null)
                .ToList();
            return found.Count == 1 ? found[0] : null;
        }

        private static bool IsVisibleFrom(CompositionSettings settings, System.Reflection.Assembly assembly)
        {
            string target = Path.GetFileNameWithoutExtension(
                CompilationPipeline.GetAssemblyNameFromScriptPath(settings.codeFolder + "/" + settings.InstallerName + ".cs") ?? string.Empty);
            string name = assembly.GetName().Name;
            if (string.Equals(target, name, StringComparison.Ordinal))
            {
                return true;
            }

            UnityEditor.Compilation.Assembly compiled = CompilationPipeline.GetAssemblies()
                .FirstOrDefault(candidate => string.Equals(candidate.name, target, StringComparison.Ordinal));
            return compiled != null && compiled.assemblyReferences.Any(reference => string.Equals(reference.name, name, StringComparison.Ordinal));
        }

        private static void WriteIfMissing(List<string> written, CompositionSettings settings, string fileName, string content)
        {
            string path = settings.codeFolder + "/" + fileName;
            if (!File.Exists(path))
            {
                File.WriteAllText(path, content, new UTF8Encoding(false));
                written.Add(path);
            }
        }

        private static void WriteIfChanged(List<string> written, CompositionSettings settings, string fileName, string content)
        {
            string path = settings.codeFolder + "/" + fileName;
            if (!File.Exists(path) || !string.Equals(File.ReadAllText(path), content, StringComparison.Ordinal))
            {
                File.WriteAllText(path, content, new UTF8Encoding(false));
                written.Add(path);
            }
        }
    }
}
