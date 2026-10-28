using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Pretzel.Data.Editor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pretzel.Data.Tests
{
    public sealed class BuildValidatorTests
    {
        private const string EmbeddedAes = "private static readonly byte[] PartA = new byte[] { 0x01 };";
        private const string TokenScript = "public static async Awaitable<bool> LoadKeysAsync()";
        private const string EnvironmentScript = "private const string EnvironmentPrefix = \"X\";";

        private readonly List<Object> _created = new();

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }
        }

        private DataIOConfig Config(DataFormat format, EncryptionMode mode, KeySource source)
        {
            DataIOConfig config = ScriptableObject.CreateInstance<DataIOConfig>();
            _created.Add(config);
            config.format = format;
            config.encryptionMode = mode;
            config.keySource = source;
            return config;
        }

        private static string AesJson() => GameEncryptionKeyConfig.ToJson(new EncryptionKeys { AesKey = EncryptionKeys.GenerateAesKey() }, EncryptionMode.Aes);

        private static IEnumerable<string> Errors(IReadOnlyList<BuildIssue> issues) =>
            issues.Where(i => i.Severity == IssueSeverity.Error).Select(i => i.Message);

        private static IEnumerable<string> Warnings(IReadOnlyList<BuildIssue> issues) =>
            issues.Where(i => i.Severity == IssueSeverity.Warning).Select(i => i.Message);

        [Test]
        public void NothingIsCheckedWhenTheFormatIsNotEncrypted()
        {
            Assert.That(DataIOBuildValidator.Validate(null, null, null), Is.Empty);
            Assert.That(DataIOBuildValidator.Validate(Config(DataFormat.PlainText, EncryptionMode.Aes, KeySource.Automatic), null, null), Is.Empty);
        }

        [Test]
        public void ALeftoverKeyFileIsWarnedAboutWhenNothingIsEncrypted()
        {
            IReadOnlyList<BuildIssue> issues = DataIOBuildValidator.Validate(Config(DataFormat.Binary, EncryptionMode.Aes, KeySource.Automatic), null, AesJson());
            Assert.That(Errors(issues), Is.Empty);
            Assert.That(Warnings(issues).Single(), Does.Contain("not Encrypted"));
        }

        [Test]
        public void AutomaticWithoutAnyShippedKeyFailsTheBuild()
        {
            IReadOnlyList<BuildIssue> issues = DataIOBuildValidator.Validate(Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.Automatic), null, null);
            Assert.That(Errors(issues).Single(), Does.Contain("No key ships"));
        }

        [Test]
        public void AutomaticWithEmbeddedSetupPassesAndWarnsThatTheKeyCanBeExtracted()
        {
            IReadOnlyList<BuildIssue> issues = DataIOBuildValidator.Validate(Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.Automatic), EmbeddedAes, null);
            Assert.That(Errors(issues), Is.Empty);
            Assert.That(Warnings(issues).Single(), Does.Contain("ships the AES key"));
        }

        [Test]
        public void ProviderWithEmbeddedSetupPassesAndAnUnusedJsonIsFlagged()
        {
            DataIOConfig config = Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.Provider);
            Assert.That(Errors(DataIOBuildValidator.Validate(config, EmbeddedAes, null)), Is.Empty);

            IReadOnlyList<BuildIssue> issues = DataIOBuildValidator.Validate(config, EmbeddedAes, AesJson());
            Assert.That(Errors(issues), Is.Empty);
            Assert.That(Warnings(issues).Any(w => w.Contains("not used", StringComparison.Ordinal) && w.Contains("Delete", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void ProviderWithTokenFlowOrCustomCodeIsNotAnError()
        {
            DataIOConfig config = Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.Provider);
            Assert.That(Errors(DataIOBuildValidator.Validate(config, TokenScript, null)), Is.Empty);
            Assert.That(Warnings(DataIOBuildValidator.Validate(config, null, null)).Single(), Does.Contain("no PretzelDataSetup"));
        }

        [Test]
        public void ProviderWithAnEnvironmentOnlySetupFails()
        {
            IReadOnlyList<BuildIssue> issues = DataIOBuildValidator.Validate(Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.Provider), EnvironmentScript, null);
            Assert.That(Errors(issues).Single(), Does.Contain("registers no provider"));
        }

        [Test]
        public void EnvironmentSourceOnlyWarns()
        {
            IReadOnlyList<BuildIssue> issues = DataIOBuildValidator.Validate(Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.Environment), EnvironmentScript, null);
            Assert.That(Errors(issues), Is.Empty);
            Assert.That(Warnings(issues).Single(), Does.Contain("only works for servers"));
        }

        [Test]
        public void BundledJsonNeedsAFileWithTheRightKeyAndValidContent()
        {
            DataIOConfig aes = Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.BundledJson);
            Assert.That(Errors(DataIOBuildValidator.Validate(aes, null, null)).Single(), Does.Contain("missing"));
            Assert.That(Errors(DataIOBuildValidator.Validate(aes, null, "{}")).Single(), Does.Contain("invalid"));
            Assert.That(Errors(DataIOBuildValidator.Validate(aes, null, "{ not json")).Single(), Does.Contain("invalid"));

            IReadOnlyList<BuildIssue> good = DataIOBuildValidator.Validate(aes, null, AesJson());
            Assert.That(Errors(good), Is.Empty);
            Assert.That(Warnings(good).Single(), Does.Contain("ships the AES key"));
        }

        [Test]
        public void UnsupportedSavedModeFailsWithoutCoercion()
        {
            DataIOConfig unsupported = Config(DataFormat.Encrypted, (EncryptionMode)1, KeySource.Provider);
            Assert.That(Errors(DataIOBuildValidator.Validate(unsupported, EmbeddedAes, null)).Single(), Does.Contain("unsupported"));
            Assert.That(unsupported.encryptionMode, Is.EqualTo((EncryptionMode)1));
        }

        [Test]
        public void AProviderAndAJsonTogetherAreFlaggedUnderAutomatic()
        {
            IReadOnlyList<BuildIssue> issues = DataIOBuildValidator.Validate(Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.Automatic), EmbeddedAes, AesJson());
            Assert.That(Errors(issues), Is.Empty);
            Assert.That(Warnings(issues).Any(w => w.Contains("provider wins", StringComparison.Ordinal)), Is.True);
        }

        [Test]
        public void AMalformedJsonFailsOnlyWhenItWouldBeRead()
        {
            DataIOConfig automatic = Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.Automatic);
            Assert.That(Errors(DataIOBuildValidator.Validate(automatic, EmbeddedAes, "{ nope")).Single(), Does.Contain("would throw at startup"));
            DataIOConfig provider = Config(DataFormat.Encrypted, EncryptionMode.Aes, KeySource.Provider);
            Assert.That(Errors(DataIOBuildValidator.Validate(provider, EmbeddedAes, "{ nope")), Is.Empty);
        }

        [Test]
        public void IssuesReadAsOneLine()
        {
            BuildIssue error = new(IssueSeverity.Error, "x");
            BuildIssue warning = new(IssueSeverity.Warning, "y");
            Assert.That(error.ToString(), Is.EqualTo("Error: x"));
            Assert.That(warning.ToString(), Is.EqualTo("Warning: y"));
        }
    }
}
