using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Pretzel.Data.Tests
{
    public sealed class ShippingUsageExampleTests
    {
        [TestCase("Environment", EncryptionMode.Aes)]
        [TestCase("Embedded", EncryptionMode.Aes)]
        [TestCase("Token", EncryptionMode.Aes)]
        [TestCase("Environment", EncryptionMode.Rsa)]
        [TestCase("Embedded", EncryptionMode.Rsa)]
        public void GeneratedSetupIncludesUsageForEverySupportedDelivery(string deliveryName, EncryptionMode mode)
        {
            Assembly editor = typeof(Pretzel.Data.Editor.DataFormatWindow).Assembly;
            Type generator = editor.GetType("Pretzel.Data.Editor.ShippingGenerator", true);
            Type delivery = editor.GetType("Pretzel.Data.Editor.KeyDelivery", true);
            MethodInfo generate = generator.GetMethod("Generate", BindingFlags.Public | BindingFlags.Static);
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            string folder = "Assets/PretzelShippingTest_" + Guid.NewGuid().ToString("N");
            try
            {
                config.encryptionMode = mode;
                EncryptionKeys keys = mode == EncryptionMode.Aes
                    ? new EncryptionKeys { AesKey = EncryptionKeys.GenerateAesKey() }
                    : EncryptionKeys.GenerateRsa(2048);
                object[] args = { folder, Enum.Parse(delivery, deliveryName), config, keys, null };
                Assert.That(generate.Invoke(null, args), Is.Null);
                string source = File.ReadAllText((string)args[4]);
                Assert.That(source, Does.Contain("DataIO.Save(\"profile\""));
                Assert.That(source, Does.Contain("DataIO.Load<Profile>"));
                Assert.That(source, Does.Contain("DataStatus.NotFound"));
                Assert.That(source, Does.Contain("Never ship that key"));
                Assert.That(source, Does.Contain("DataStoreFactory.Create"));
                Assert.That(source, Does.Contain("EncryptionUsageExample"));
                string example = File.ReadAllText(folder + "/PretzelEncryptionExample.cs");
                Assert.That(source, Does.Contain("PretzelEncryptionExample.cs"));
                Assert.That(example, Does.Contain("class PretzelEncryptionExample : MonoBehaviour"));
                Assert.That(example, Does.Contain("SaveAesProfile"));
                Assert.That(example, Does.Contain("LoadAesProfile"));
                Assert.That(example, Does.Contain("ReadRsaReportOnServer"));
                Assert.That(example, Does.Contain("SaveAndLoadAfterSignInAsync"));
                Assert.That(example, Does.Contain("SaveAesWithJsonKeys"));
                Assert.That(example, Does.Contain("LoadAesWithJsonKeys"));
                Assert.That(example, Does.Contain("WriteRsaWithJsonKeys"));
                Assert.That(example, Does.Contain("SaveAesWithEmbeddedKeys"));
                Assert.That(example, Does.Contain("LoadAesWithEmbeddedKeys"));
                Assert.That(example, Does.Contain("WriteRsaWithEmbeddedKeys"));
                Assert.That(example, Does.Contain("EncryptionKeys keys = GameEncryptionKeyConfig.Load();"));
                Assert.That(example, Does.Contain("EncryptionKeys keys = EncryptionKeys.Provider.Invoke();"));
                if (deliveryName == "Token")
                {
                    Assert.That(source, Does.Contain("await PretzelDataSetup.LoadKeysAsync"));
                    Assert.That(source, Does.Contain("await DataIO.SaveAsync"));
                }

                if (mode == EncryptionMode.Rsa)
                {
                    Assert.That(source, Does.Not.Contain(EncryptionKeys.EncodeXml(keys.RsaPrivateKey)));
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void StandaloneCompanionGenerationPreservesUserEdits()
        {
            Type generator = typeof(Pretzel.Data.Editor.DataFormatWindow).Assembly
                .GetType("Pretzel.Data.Editor.ShippingGenerator", true);
            MethodInfo generate = generator.GetMethod("GenerateUsageExample", BindingFlags.Public | BindingFlags.Static);
            string folder = "Assets/PretzelExampleTest_" + Guid.NewGuid().ToString("N");
            try
            {
                string path = (string)generate.Invoke(null, new object[] { folder });
                string edited = File.ReadAllText(path) + "\n// User customization\n";
                File.WriteAllText(path, edited);
                Assert.That(generate.Invoke(null, new object[] { folder }), Is.EqualTo(path));
                Assert.That(File.ReadAllText(path), Is.EqualTo(edited));
                Assert.That(File.Exists(folder + "/PretzelDataSetup.cs"), Is.False);
            }
            finally
            {
                AssetDatabase.DeleteAsset(folder);
            }
        }
    }
}
