using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Pretzel.Setup.Settings;

namespace Pretzel.Setup.Tests
{
    public sealed class SettingsSerializationTests
    {
        [Test]
        public void TheComputedActivePresetIsNotStored()
        {
            string json = JObject.FromObject(new PretzelProjectSettings()).ToString();

            Assert.That(json, Does.Not.Contain("ActivePreset"));
            Assert.That(json, Does.Contain("activeFolderPreset"));
        }

        [Test]
        public void EveryPresetCreatesTheTestsFolderAndTheGamePresetTheUiFolders()
        {
            foreach (FolderPreset preset in FolderPreset.CreateDefaults())
            {
                Assert.That(preset.folders, Does.Contain("_Scripts/Tests"), preset.name);
            }

            FolderPreset game = FolderPreset.CreateDefaults().First(preset => string.Equals(preset.name, FolderPreset.Game, System.StringComparison.Ordinal));
            Assert.That(game.folders, Does.Contain("_Project/UI/Layouts").And.Contain("_Project/UI/Styles"));
        }
    }
}
