
namespace Pretzel.Data.Editor
{
    internal sealed class FormatInfo
    {
        public string Title;
        public string Summary;
        public string[] Extensions;
        public string[] Pros;
        public string[] Cons;
        public string BestFor;
    }

    /// <summary>The text the Data Format window shows. Kept in one place so the window code stays about behavior.</summary>
    internal static class DataFormatInfo
    {
        public static FormatInfo Get(DataFormat format)
        {
            return format switch
            {
                DataFormat.Binary => _binary,
                DataFormat.Encrypted => _encrypted,
                _ => _plainText
            };
        }

        private static readonly FormatInfo _plainText = new()
        {
            Title = "Plain text",
            Summary = "Readable JSON. The default.",
            Extensions = new[] { "json" },
            Pros = new[]
            {
                "Easy to read, debug and edit by hand.",
                "Diffs and merges well in version control.",
                "Works with every serializer and every tool.",
                "Players and support staff can repair a broken file."
            },
            Cons = new[]
            {
                "Players can read and change every value, so it is no protection against cheating.",
                "No integrity check: a damaged or edited file is only noticed when it fails to parse.",
                "Largest on disk."
            },
            BestFor = "Settings, development builds, single-player games where cheating does not matter, and data you want to diff."
        };

        private static readonly FormatInfo _binary = new()
        {
            Title = "Binary",
            Summary = "Versioned binary values using .NET BinaryWriter and BinaryReader. Never BinaryFormatter.",
            Extensions = new[] { "bin", "dat" },
            Pros = new[]
            {
                "Not readable or editable in a text editor.",
                "Keeps value types (int, long, double, bool) exactly.",
                "No per-type read/write implementation needed for ordinary serializable game data.",
                "Works with schema versions and migrations."
            },
            Cons = new[]
            {
                "Only discourages casual editing: field names and strings are still visible in a hex editor.",
                "Not guaranteed smaller than JSON: type tags, lengths and field names are kept.",
                "Cannot be diffed or edited by hand.",
                "Not a security feature. Use Encrypted if the data must stay private."
            },
            BestFor = "Saves you do not want players to casually open and edit."
        };

        private static readonly FormatInfo _encrypted = new()
        {
            Title = "Encrypted",
            Summary = "JSON encrypted with AES-256, tamper-checked with HMAC-SHA256.",
            Extensions = new[] { "pretzel", "enc" },
            Pros = new[]
            {
                "Contents are unreadable without the key.",
                "Edited or damaged files are rejected instead of loaded as garbage (the HMAC check).",
                "A random IV for every file, so equal data never gives equal files."
            },
            Cons = new[]
            {
                "You must manage and protect keys. A lost key means lost data.",
                "A key that ships inside the game can be extracted. It slows cheaters down, it does not stop them.",
                "Cannot be diffed or merged."
            },
            BestFor = "Local saves where casual tampering matters. Not for secrets or anything a server must trust: a key in the game can be extracted."
        };

        public static readonly string[] AesModeNotes =
        {
            "One secret 32 byte key encrypts and decrypts.",
            "The game needs the key to read its own saves, so the key has to reach the player's machine. Treat it as obfuscation against casual editing."
        };

        public static readonly string[] EncryptedChecklist =
        {
            "Use plain JSON (the zero-setup default) or binary when you do not need encryption.",
            "For AES, generate the setup on the Setup tab or generate a key on the Encryption tab.",
            "Store them on the Keys tab, as environment variables or in this project's key file. Both stay out of version control. Also back them up in a password manager.",
            "Never commit the AES key. Check that UserSettings is in your .gitignore.",
            "Use Test save and load on the Setup tab to check the configured store in an isolated diagnostic folder.",
            "Decide how the game gets the key. Release: embedded C# or bundled JSON on the Setup tab, or a token flow on the Shipping tab.",
            "Optional rotation is under Advanced. Keep previous keys until every old save has been re-saved.",
            "Remember that a key in the game can be extracted. For data that really must stay secret, keep the key and the decryption on a server."
        };

        public static readonly string[] TokenSteps =
        {
            "The player signs in with your auth provider (Steam, Epic, PlayFab, Firebase, Unity Authentication) and the game gets a short-lived access token.",
            "The game sends the token to your key service over HTTPS.",
            "The service validates the token on the server and returns a key made for that player, so one leaked key opens one player's saves.",
            "The game keeps the key in memory only. It never writes it to disk or PlayerPrefs, and creates its store after the key arrives.",
            "Rotate keys with a version number on the server, rate limit requests, and keep old keys until saves are re-saved.",
            "The generated script has a stub, RequestKeyAsync, for your own request. The build check fails until it is implemented. Keys stay fixed for the run, so signing in as another player needs a restart."
        };

        public static readonly string[] Base64Pros =
        {
            "Pure text: safe to paste, e-mail, put in a JSON or XML field, PlayerPrefs or an environment variable.",
            "Survives systems that mangle binary data.",
            "Newtonsoft already writes byte[] fields as Base64 inside JSON."
        };

        public static readonly string[] Base64Cons =
        {
            "Adds about 33 percent (4 characters for every 3 bytes), plus padding.",
            "It is an encoding, not encryption: anyone can decode it.",
            "JsonUtility writes byte[] as a list of numbers, which is much larger. Use Newtonsoft for byte arrays.",
            "Use the URL-safe form (- and _ instead of + and /) in URLs and file names. The decoder here accepts both."
        };
    }
}
