using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Pretzel.Data.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Pretzel.Data.Tests
{
    public sealed class SetupGeneratorTests
    {
        private readonly List<string> _folders = new();

        [TearDown]
        public void TearDown()
        {
            foreach (string folder in _folders)
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }

        private string NewFolder()
        {
            RequireNoExistingSetupScripts();
            string folder = "Assets/PretzelSetupTest_" + Guid.NewGuid().ToString("N");
            _folders.Add(folder);
            return folder;
        }

        // These tests create real scripts. A project that already has them would get duplicate classes, so skip there.
        private static void RequireNoExistingSetupScripts()
        {
            foreach (string name in new[]
                     {
                         "ProjectDataIO", "PretzelDataSetup", "ProjectSaveData", "ProjectSaveService", "ProjectSaveExample",
                         "PretzelEncryptionExample", "PretzelEncryptionExampleEditor"
                     })
            {
                foreach (string guid in AssetDatabase.FindAssets(name + " t:MonoScript"))
                {
                    if (Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) == name)
                    {
                        Assert.Ignore($"This project already has {name}.cs; the test will not create a second copy.");
                    }
                }
            }
        }

        [TestCase(DataFormat.PlainText)]
        [TestCase(DataFormat.Binary)]
        public void PlainSetupNamesItsFormatAndKeepsEveryPlaceholderOut(DataFormat format)
        {
            string code = DataIOSetupGenerator.RenderPlain(format);
            Assert.That(code, Does.Contain("DataFormat." + format + ";"));
            Assert.That(code, Does.Contain("public static class ProjectDataIO"));
            Assert.That(code, Does.Contain("Initialize(Action<DataIOConfig> configure = null, Action<DataStoreOptions> configureStore = null)"));
            Assert.That(code, Does.Not.Contain("@@"));
            Assert.That(code, Does.Not.Contain("EncryptionKeys"));
        }

        [Test]
        public void PlainSetupRejectsTheEncryptedFormat()
        {
            Assert.Throws<ArgumentException>(() => DataIOSetupGenerator.RenderPlain(DataFormat.Encrypted));
        }

        [TestCase(DataFormat.PlainText, EncryptionMode.Aes, "PretzelPlainTextExample", "Settings", "PlainTextStrategy")]
        [TestCase(DataFormat.Binary, EncryptionMode.Aes, "PretzelBinaryExample", "Inventory", "BinaryDataStrategy")]
        [TestCase(DataFormat.Encrypted, EncryptionMode.Aes, "PretzelAesExample", "Profile", "EncryptionMode.Aes")]
        public void UseCaseTemplatesAreDistinctAndIndependent(DataFormat format, EncryptionMode mode, string name, string payload, string strategy)
        {
            string code = DataIOSetupGenerator.RenderUseCaseExample(format, mode);
            Assert.That(code, Does.Contain("public sealed class " + name));
            Assert.That(code, Does.Contain("class " + payload));
            Assert.That(code, Does.Contain(strategy));
            Assert.That(code, Does.Not.Contain("ProjectDataIO"));
            Assert.That(code, Does.Not.Contain("@@"));
            Assert.That(code, Does.Not.Contain("Rsa"));
            Assert.That(code, Does.Not.Contain("RSA"));
        }

        [Test]
        public void UseCaseGenerationNeedsNoSetupAndNeverReplacesAnExistingFile()
        {
            foreach (string guid in AssetDatabase.FindAssets("PretzelBinaryExample t:MonoScript"))
            {
                if (Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)) == "PretzelBinaryExample")
                {
                    Assert.Ignore("The host already has this example; do not generate a duplicate class.");
                }
            }

            string folder = NewFolder();
            string path = DataIOSetupGenerator.CreateUseCaseExample(folder, DataFormat.Binary);
            Assert.That(File.Exists(DataIOSetupGenerator.ScriptPath(folder)), Is.False);
            string edited = File.ReadAllText(path) + "\n// user edit\n";
            File.WriteAllText(path, edited);
            Assert.Throws<IOException>(() => DataIOSetupGenerator.CreateUseCaseExample(folder, DataFormat.Binary));
            Assert.That(File.ReadAllText(path), Is.EqualTo(edited));
        }

        [Test]
        public void SetupLayoutHasAButtonForEveryUseCase()
        {
            VisualElement view = DataEditorAssets.Layout("SetupTab").Instantiate();
            foreach (string useCase in new[] { "plain", "binary", "aes" })
            {
                Assert.That(view.Q<Button>("setup-panel__" + useCase + "-example"), Is.Not.Null, useCase);
            }
        }

        [TestCase(KeySource.Provider)]
        [TestCase(KeySource.BundledJson)]
        public void AesSetupHasNoServerStoreAndNamesItsKeySource(KeySource source)
        {
            string code = DataIOSetupGenerator.RenderEncrypted(EncryptionMode.Aes, source);
            Assert.That(code, Does.Contain("EncryptionMode.Aes;"));
            Assert.That(code, Does.Contain("KeySource." + source + ";"));
            Assert.That(code, Does.Contain("InitializeWithKeys"));
            Assert.That(code, Does.Contain("ForClient"));
            Assert.That(code, Does.Contain("Describe()"));
            Assert.That(code, Does.Not.Contain("CreateServerStore"));
            Assert.That(code, Does.Not.Contain("@@"));
            Assert.That(code, Does.Not.Contain("//@@"));
        }

        [Test]
        public void UnsupportedEncryptionModesAreRejectedInsteadOfCoerced()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DataIOSetupGenerator.RenderEncrypted((EncryptionMode)1, KeySource.Provider));
            Assert.Throws<ArgumentOutOfRangeException>(() => DataIOSetupGenerator.RenderUseCaseExample(DataFormat.Encrypted, (EncryptionMode)1));
        }

        [Test]
        public void SetupLayoutOffersIsolatedDiagnosticsAndNoEncryptionModeChoice()
        {
            VisualElement view = DataEditorAssets.Layout("SetupTab").Instantiate();
            Assert.That(view.Q<Button>("setup-panel__test").text, Is.EqualTo("Test save and load"));
            Assert.That(view.Q<Button>("setup-panel__open-store").text, Is.EqualTo("Open this store's folder"));
            Assert.That(view.Q<Label>("setup-panel__summary"), Is.Not.Null);
            Assert.That(view.Q<DropdownField>("setup-panel__mode"), Is.Null);
            Assert.That(view.Q<Button>("setup-panel__rsa-example"), Is.Null);

            VisualElement encryption = DataEditorAssets.Layout("EncryptionTab").Instantiate();
            Foldout advanced = encryption.Q<Foldout>("encryption-panel__advanced");
            Assert.That(advanced.value, Is.False);
            Assert.That(advanced.Q<Button>("encryption-panel__rotate"), Is.Not.Null);
            Assert.That(encryption.Q<DropdownField>(), Is.Null);

            Foldout previous = DataEditorAssets.Layout("KeysTab").Instantiate().Q<Foldout>("keys-panel__advanced");
            Assert.That(previous.value, Is.False);
            Assert.That(previous.Q<Button>("keys-panel__previous-clear"), Is.Not.Null);
        }

        [TestCase(DataFormat.PlainText)]
        [TestCase(DataFormat.Binary)]
        [TestCase(DataFormat.Encrypted)]
        public void GeneratedSetupPassesStoreOptionsBeforeConstructingEveryStore(DataFormat format)
        {
            string code = format == DataFormat.Encrypted
                ? DataIOSetupGenerator.RenderEncrypted(EncryptionMode.Aes, KeySource.Provider)
                : DataIOSetupGenerator.RenderPlain(format);
            Assert.That(code, Does.Contain("Initialize(Action<DataIOConfig> configure = null, Action<DataStoreOptions> configureStore = null)"));
            Assert.That(code, Does.Contain("CreateStore(string subFolder, Action<DataIOConfig> configure = null, Action<DataStoreOptions> configureStore = null)"));
            Assert.That(code, Does.Not.Contain("RSA"));
            Assert.That(code, Does.Not.Contain("CreateServerStore"));
            if (format == DataFormat.Encrypted)
            {
                Assert.That(code, Does.Contain("InitializeWithKeys(EncryptionKeys keys, Action<DataIOConfig> configure = null, Action<DataStoreOptions> configureStore = null)"));
                Assert.That(code, Does.Contain("store = config.CreateStore(null, clientKeys, configureStore);"));
                Assert.That(code, Does.Contain("copy.format == DataFormat.Encrypted ? _clientKeys : null, configureStore)"));
                Assert.That(code, Does.Contain("Create(keys, configure, configureStore);"));
            }
            else
            {
                Assert.That(code, Does.Contain("store = config.CreateStore(configure: configureStore);"));
                Assert.That(code, Does.Contain("copy.CreateStore(DataRoots.PersistentData(subFolder), configure: configureStore)"));
            }
        }

        [TestCase(DataFormat.PlainText, SerializerChoice.Newtonsoft)]
        [TestCase(DataFormat.PlainText, SerializerChoice.JsonUtility)]
        [TestCase(DataFormat.Binary, SerializerChoice.Newtonsoft)]
        [TestCase(DataFormat.Encrypted, SerializerChoice.Newtonsoft)]
        public void DiagnosticRoundTripUsesConfiguredKeysAndDoesNotTouchPlayerRootOrDefault(DataFormat format, SerializerChoice serializer)
        {
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            Func<EncryptionKeys> previousProvider = EncryptionKeys.Provider;
            IDataStore previousDefault = DataIO.Default;
            EncryptionKeys keys = new() { AesKey = EncryptionKeys.GenerateAesKey() };
            byte[] originalKey = (byte[])keys.AesKey.Clone();
            string diagnostics = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Library", "Pretzel", "DataIODiagnostics");
            string[] existingDiagnostics = Directory.Exists(diagnostics) ? Directory.GetDirectories(diagnostics) : Array.Empty<string>();
            int calls = 0;
            EncryptionKeys.Provider = () => { calls++; return keys; };
            try
            {
                config.format = format;
                config.serializer = serializer;
                config.fileExtension = DataFormats.DefaultExtension(format);
                config.keySource = KeySource.Provider;
                // An invalid player root makes an accidental access fail without touching real profiles.
                config.saveFolder = "../PlayerProfilesMustNotBeUsed";
                Assert.That(SetupPanel.TestSaveAndLoad(config), Does.Contain("passed"));
                Assert.That(calls, Is.EqualTo(format == DataFormat.Encrypted ? 1 : 0));
                Assert.That(keys.AesKey, Is.EqualTo(originalKey));
                Assert.That(config.saveFolder, Is.EqualTo("../PlayerProfilesMustNotBeUsed"));
                Assert.That(DataIO.Default, Is.SameAs(previousDefault));
                Assert.That(Directory.GetDirectories(diagnostics), Is.EquivalentTo(existingDiagnostics), "Only the diagnostic run's own folder is removed.");
            }
            finally
            {
                EncryptionKeys.Provider = previousProvider;
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void DiagnosticDoesNotInventAKeyWhenConfiguredSourceIsMissing()
        {
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            Func<EncryptionKeys> previousProvider = EncryptionKeys.Provider;
            EncryptionKeys.Provider = null;
            try
            {
                config.format = DataFormat.Encrypted;
                config.keySource = KeySource.Provider;
                Assert.Throws<InvalidOperationException>(() => SetupPanel.TestSaveAndLoad(config));
                Assert.That(EncryptionKeys.Provider, Is.Null);
                config.encryptionMode = (EncryptionMode)1;
                Assert.Throws<NotSupportedException>(() => SetupPanel.TestSaveAndLoad(config));
                Assert.That(config.encryptionMode, Is.EqualTo((EncryptionMode)1));
            }
            finally
            {
                EncryptionKeys.Provider = previousProvider;
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void PendingStateRestoresAesHistoryAndRejectsMissingKeys()
        {
            string current = EncryptionKeys.EncodeAesKey(EncryptionKeys.GenerateAesKey());
            string previous = EncryptionKeys.EncodeAesKey(EncryptionKeys.GenerateAesKey());
            EncryptionKeys restored = DataFormatState.RestorePending($"{{\"aes\":\"{current}\",\"previous\":[\"{previous}\"]}}");
            Assert.That(EncryptionKeys.EncodeAesKey(restored.AesKey), Is.EqualTo(current));
            Assert.That(restored.PreviousAesKeys, Has.Count.EqualTo(1));
            Assert.Throws<FormatException>(() => DataFormatState.RestorePending("{}"));
        }

        [Test]
        public void WriteCreatesTheScriptAndOnlyAsksBeforeReplacingADifferentOne()
        {
            string folder = NewFolder();
            string plain = DataIOSetupGenerator.RenderPlain(DataFormat.PlainText);
            Assert.That(DataIOSetupGenerator.WouldReplace(folder, plain), Is.False, "Nothing exists yet.");

            string path = DataIOSetupGenerator.Write(folder, plain);
            Assert.That(path, Is.EqualTo(folder + "/ProjectDataIO.cs"));
            Assert.That(File.ReadAllText(path), Is.EqualTo(plain));
            Assert.That(DataIOSetupGenerator.WouldReplace(folder, plain), Is.False, "The same content is not a replacement.");
            Assert.That(DataIOSetupGenerator.WouldReplace(folder, DataIOSetupGenerator.RenderPlain(DataFormat.Binary)), Is.True);
        }

        [Test]
        public void WriteRefusesAFolderOutsideAssets()
        {
            Assert.Throws<ArgumentException>(() => DataIOSetupGenerator.Write("Packages/nope", "x"));
            Assert.Throws<ArgumentException>(() => DataIOSetupGenerator.Write("Assets/../Outside", "x"));
        }

        [Test]
        public void WriteRefusesASecondCopyOfTheClass()
        {
            string first = NewFolder();
            string second = NewFolder();
            DataIOSetupGenerator.Write(first, DataIOSetupGenerator.RenderPlain(DataFormat.PlainText));
            Assert.Throws<IOException>(() => DataIOSetupGenerator.Write(second, DataIOSetupGenerator.RenderPlain(DataFormat.PlainText)));
            Assert.That(File.Exists(second + "/ProjectDataIO.cs"), Is.False);
        }

        [Test]
        public void ExamplesNeedTheSetupFirst()
        {
            string folder = NewFolder();
            Assert.Throws<InvalidOperationException>(() => DataIOSetupGenerator.CreateExamples(folder, false));
        }

        [Test]
        public void ExamplesAreAddedOnceAndNeverReplaced()
        {
            string folder = NewFolder();
            DataIOSetupGenerator.Write(folder, DataIOSetupGenerator.RenderPlain(DataFormat.PlainText));

            List<string> created = DataIOSetupGenerator.CreateExamples(folder, false);
            Assert.That(created, Has.Count.EqualTo(4), "Three scripts and the guide.");
            foreach (string name in new[] { "ProjectSaveData.cs", "ProjectSaveService.cs", "ProjectSaveExample.cs", "DataIOSetup.md" })
            {
                Assert.That(File.Exists(folder + "/" + name), Is.True, name);
            }

            Assert.That(File.Exists(folder + "/PretzelEncryptionExample.cs"), Is.False, "The text demo is for encrypted setups.");
            Assert.That(File.ReadAllText(folder + "/ProjectSaveExample.cs"), Does.Contain("ProjectDataIO.Initialize()"));
            Assert.That(File.ReadAllText(folder + "/ProjectSaveExample.cs"), Does.Not.Contain("ProjectDataBootstrap"));
            Assert.That(File.ReadAllText(folder + "/ProjectSaveExample.cs"),
                Does.Contain("ex is IOException or InvalidDataException or UnauthorizedAccessException"));

            string service = folder + "/ProjectSaveService.cs";
            string edited = File.ReadAllText(service) + "\n// my change\n";
            File.WriteAllText(service, edited);
            Assert.That(DataIOSetupGenerator.CreateExamples(folder, false), Is.Empty);
            Assert.That(File.ReadAllText(service), Is.EqualTo(edited));
        }

        [Test]
        public void EncryptedSetupsGetTheTextDemoAndItsInspector()
        {
            string folder = NewFolder();
            DataIOSetupGenerator.Write(folder, DataIOSetupGenerator.RenderEncrypted(EncryptionMode.Aes, KeySource.Provider));

            DataIOSetupGenerator.CreateExamples(folder, true);
            string demo = File.ReadAllText(folder + "/PretzelEncryptionExample.cs");
            string inspector = File.ReadAllText(folder + "/Editor/PretzelEncryptionExampleEditor.cs");
            Assert.That(demo, Does.Contain("public void Encrypt()"));
            Assert.That(demo, Does.Contain("public void Decrypt()"));
            Assert.That(demo, Does.Contain("[HideInInspector] public EncryptionMode mode = EncryptionMode.Aes;"));
            Assert.That(demo, Does.Contain("new(mode, GetKeys())"), "Retained serialized modes must still pass runtime validation.");
            Assert.That(inspector, Does.Contain("CustomEditor(typeof(PretzelEncryptionExample))"));
            Assert.That(inspector, Does.Contain("CreateInspectorGUI"));
            Assert.That(inspector, Does.Not.Contain("FindProperty(\"mode\")"));
        }

        [Test]
        public void ALegacyTextDemoIsKeptAndGetsNoIncompatibleInspector()
        {
            string folder = NewFolder();
            DataIOSetupGenerator.Write(folder, DataIOSetupGenerator.RenderEncrypted(EncryptionMode.Aes, KeySource.Provider));
            const string legacy = "public sealed class PretzelEncryptionExample : UnityEngine.MonoBehaviour { }";
            File.WriteAllText(folder + "/PretzelEncryptionExample.cs", legacy);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"existing encryption example was preserved"));
            DataIOSetupGenerator.CreateExamples(folder, true);

            Assert.That(File.ReadAllText(folder + "/PretzelEncryptionExample.cs"), Is.EqualTo(legacy));
            Assert.That(File.Exists(folder + "/Editor/PretzelEncryptionExampleEditor.cs"), Is.False);
        }

        [Test]
        public void DefaultFolderIsTheDataIOFolder()
        {
            Assert.That(DataIOSetupGenerator.DefaultFolder, Is.EqualTo("Assets/Pretzel/DataIO"));
            Assert.That(ShippingGenerator.DefaultFolder, Is.EqualTo("Assets/Pretzel/DataIO"));
        }

        [TestCase("Embedded", EncryptionMode.Aes)]
        [TestCase("Token", EncryptionMode.Aes)]
        [TestCase("Environment", EncryptionMode.Aes)]
        public void KeyDeliveryWritesOnlyTheSmallSetupScript(string deliveryName, EncryptionMode mode)
        {
            KeyDelivery delivery = (KeyDelivery)Enum.Parse(typeof(KeyDelivery), deliveryName);
            string folder = NewFolder();
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            try
            {
                config.encryptionMode = mode;
                EncryptionKeys keys = new() { AesKey = EncryptionKeys.GenerateAesKey() };

                Assert.That(ShippingGenerator.Generate(folder, delivery, config, keys, out string path), Is.Null);
                string source = File.ReadAllText(path);

                Assert.That(Directory.GetFileSystemEntries(folder).Length, Is.EqualTo(2), "The script and its .meta, nothing else.");
                Assert.That(source, Does.Contain("ProjectDataIO.Initialize()"));
                Assert.That(source, Does.Not.Contain("DataIO.Save"), "The long usage comment is gone.");
                Assert.That(source, Does.Not.Contain("DataIO.Default"));
                Assert.That(source.Split('\n').Length, Is.LessThan(60));
                Assert.That(source, Does.Not.Contain("RSA"));
                Assert.That(source, Does.Not.Contain("Rsa"));

                if (delivery == KeyDelivery.Environment)
                {
                    Assert.That(source, Does.Contain("KeySource.Environment"));
                }

                if (delivery == KeyDelivery.Token)
                {
                    Assert.That(source, Does.Contain("LoadKeysAsync"));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TokenDeliveryNeedsAnAesKeyMode()
        {
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            try
            {
                config.encryptionMode = (EncryptionMode)1;
                string error = ShippingGenerator.Generate(NewFolder(), KeyDelivery.Token, config, new EncryptionKeys(), out string path);
                Assert.That(error, Does.Contain("AES"));
                Assert.That(path, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void EmbeddedSetupCarriesOlderKeysSoOldSavesStillOpen()
        {
            string folder = NewFolder();
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            try
            {
                EncryptionKeys keys = new() { AesKey = EncryptionKeys.GenerateAesKey() };
                keys.AddPreviousAesKey(EncryptionKeys.GenerateAesKey());
                keys.AddPreviousAesKey(EncryptionKeys.GenerateAesKey());

                Assert.That(ShippingGenerator.Generate(folder, KeyDelivery.Embedded, config, keys, out string path), Is.Null);
                string source = File.ReadAllText(path);
                Assert.That(source, Does.Contain("OldPartsA"));
                Assert.That(source, Does.Contain("keys.PreviousAesKeys.Add"));

                keys.PreviousAesKeys.Clear();
                Assert.That(ShippingGenerator.Generate(folder, KeyDelivery.Embedded, config, keys, out path), Is.Null);
                Assert.That(File.ReadAllText(path), Does.Not.Contain("OldPartsA"), "No history, no extra arrays.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void KeyDeliveryAsksForAMissingKeyInsteadOfWritingAnEmptyScript()
        {
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            try
            {
                string error = ShippingGenerator.Generate(NewFolder(), KeyDelivery.Embedded, config, new EncryptionKeys(), out string path);
                Assert.That(error, Does.Contain("AES key"));
                Assert.That(path, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }
    }
}
