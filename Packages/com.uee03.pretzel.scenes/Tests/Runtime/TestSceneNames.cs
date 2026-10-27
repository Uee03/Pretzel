namespace Pretzel.Scenes.Tests
{
    internal static class TestSceneNames
    {
        public const string Prefix = "PretzelLoaderTest";
        public const string A = Prefix + "A";
        public const string B = Prefix + "B";
        public const string C = Prefix + "C";
        public const string Missing = Prefix + "Missing";

        public static readonly string[] All = { A, B, C };

        public static string Marker(string sceneName) => sceneName + "Marker";
    }
}
