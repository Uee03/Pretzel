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
    }
}
