using System;
using System.IO;
using NUnit.Framework;
using Pretzel.Data.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Pretzel.Data.Tests
{
    public sealed class SetupGeneratorTests
    {
        private readonly System.Collections.Generic.List<string> _folders = new();

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
        [TestCase(DataFormat.Compressed)]
        [TestCase(DataFormat.Binary)]
        public void PlainSetupNamesItsFormatAndKeepsEveryPlaceholderOut(DataFormat format)
        {
            string code = DataIOSetupGenerator.RenderPlain(format);
            Assert.That(code, Does.Contain("DataFormat." + format + ";"));
            Assert.That(code, Does.Contain("public static class ProjectDataIO"));
            Assert.That(code, Does.Contain("Initialize(Action<DataIOConfig> configure = null)"));
            Assert.That(code, Does.Not.Contain("@@"));
            Assert.That(code, Does.Not.Contain("EncryptionKeys"));
        }

        [Test]
        public void PlainSetupRejectsTheEncryptedFormat()
        {
            Assert.Throws<ArgumentException>(() => DataIOSetupGenerator.RenderPlain(DataFormat.Encrypted));
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
        public void RsaSetupExplainsTheClientCannotReadAndAddsTheServerStore()
        {
            string code = DataIOSetupGenerator.RenderEncrypted(EncryptionMode.Rsa, KeySource.Provider);
            Assert.That(code, Does.Contain("EncryptionMode.Rsa;"));
            Assert.That(code, Does.Contain("CreateServerStore"));
            Assert.That(code, Does.Contain("Never call this in a shipped game"));
            Assert.That(code, Does.Contain("cannot read the files back"));
            Assert.That(code, Does.Not.Contain("@@"));
        }

        [Test]
        public void WriteCreatesTheScriptAndOnlyAsksBeforeReplacingADifferentOne()
        {
            string folder = NewFolder();
            string plain = DataIOSetupGenerator.RenderPlain(DataFormat.Compressed);
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
            Assert.Throws<InvalidOperationException>(() => DataIOSetupGenerator.CreateExamples(NewFolder(), false));
        }

        [Test]
        public void ExamplesAreAddedOnceAndNeverReplaced()
        {
            string folder = NewFolder();
            DataIOSetupGenerator.Write(folder, DataIOSetupGenerator.RenderPlain(DataFormat.PlainText));

            var created = DataIOSetupGenerator.CreateExamples(folder, false);
            Assert.That(created, Has.Count.EqualTo(4), "Three scripts and the guide.");
            foreach (string name in new[] { "ProjectSaveData.cs", "ProjectSaveService.cs", "ProjectSaveExample.cs", "DataIOSetup.md" })
            {
                Assert.That(File.Exists(folder + "/" + name), Is.True, name);
            }

            Assert.That(File.Exists(folder + "/PretzelEncryptionExample.cs"), Is.False, "The text demo is for encrypted setups.");
            Assert.That(File.ReadAllText(folder + "/ProjectSaveExample.cs"), Does.Contain("ProjectDataIO.Initialize()"));
            Assert.That(File.ReadAllText(folder + "/ProjectSaveExample.cs"), Does.Not.Contain("ProjectDataBootstrap"));

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
            Assert.That(inspector, Does.Contain("CustomEditor(typeof(PretzelEncryptionExample))"));
            Assert.That(inspector, Does.Contain("CreateInspectorGUI"));
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
        [TestCase("Embedded", EncryptionMode.Rsa)]
        [TestCase("Token", EncryptionMode.Aes)]
        [TestCase("Environment", EncryptionMode.Aes)]
        [TestCase("Environment", EncryptionMode.Rsa)]
        public void KeyDeliveryWritesOnlyTheSmallSetupScript(string deliveryName, EncryptionMode mode)
        {
            KeyDelivery delivery = (KeyDelivery)Enum.Parse(typeof(KeyDelivery), deliveryName);
            string folder = NewFolder();
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            try
            {
                config.encryptionMode = mode;
                EncryptionKeys keys = mode == EncryptionMode.Aes
                    ? new EncryptionKeys { AesKey = EncryptionKeys.GenerateAesKey() }
                    : EncryptionKeys.GenerateRsa(2048);

                Assert.That(ShippingGenerator.Generate(folder, delivery, config, keys, out string path), Is.Null);
                string source = File.ReadAllText(path);

                Assert.That(Directory.GetFileSystemEntries(folder).Length, Is.EqualTo(2), "The script and its .meta, nothing else.");
                Assert.That(source, Does.Contain("ProjectDataIO.Initialize()"));
                Assert.That(source, Does.Not.Contain("DataIO.Save"), "The long usage comment is gone.");
                Assert.That(source, Does.Not.Contain("DataIO.Default"));
                Assert.That(source.Split('\n').Length, Is.LessThan(mode == EncryptionMode.Rsa ? 40 : 60));
                if (mode == EncryptionMode.Rsa)
                {
                    Assert.That(source, Does.Not.Contain(EncryptionKeys.EncodeXml(keys.RsaPrivateKey)));
                }

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
                config.encryptionMode = EncryptionMode.Rsa;
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
