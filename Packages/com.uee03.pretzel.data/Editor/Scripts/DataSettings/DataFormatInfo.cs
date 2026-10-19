
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
                DataFormat.Compressed => Compressed,
                DataFormat.Binary => Binary,
                DataFormat.Encrypted => Encrypted,
                _ => PlainText
            };
        }

        private static readonly FormatInfo PlainText = new()
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

        private static readonly FormatInfo Compressed = new()
        {
            Title = "Compressed",
            Summary = "JSON compressed with GZip.",
            Extensions = new[] { "json.gz", "gz" },
            Pros = new[]
            {
                "Large or repetitive saves often shrink several times over.",
                "Smaller cloud uploads and faster sync.",
                "Still plain JSON inside, so any gzip tool can open it.",
                "Built into .NET, no extra dependency."
            },
            Cons = new[]
            {
                "Not secure: unzipping the file shows everything.",
                "Not readable or diffable as it is.",
                "Little or no gain for tiny files.",
                "One damaged byte can make the whole file unreadable (the .bak recovery helps)."
            },
            BestFor = "Big saves, world or level data, cloud saves."
        };

        private static readonly FormatInfo Binary = new()
        {
            Title = "Binary",
            Summary = "BSON, a binary encoding of the JSON data model.",
            Extensions = new[] { "bin", "dat" },
            Pros = new[]
            {
                "Not readable or editable in a text editor.",
                "Keeps value types (int, long, double, bool) exactly.",
                "BSON is a standard, so other tools can read it.",
                "Works with schema versions and migrations."
            },
            Cons = new[]
            {
                "Only discourages casual editing: field names and strings are still visible in a hex editor.",
                "Often not smaller than JSON, because type tags, lengths and field names are kept. Use Compressed for size.",
                "Cannot be diffed or edited by hand.",
                "Not a security feature. Use Encrypted if the data must stay private."
            },
            BestFor = "Saves you do not want players to casually open and edit."
        };

        private static readonly FormatInfo Encrypted = new()
        {
            Title = "Encrypted",
            Summary = "JSON encrypted with AES-256, tamper-checked with HMAC-SHA256. Optionally with an RSA public key.",
            Extensions = new[] { "pretzel", "enc" },
            Pros = new[]
            {
                "Contents are unreadable without the key.",
                "Edited or damaged files are rejected instead of loaded as garbage (the HMAC check).",
                "A random IV for every file, so equal data never gives equal files.",
                "RSA mode lets your server or tools decrypt what the game wrote, without the game holding the secret."
            },
            Cons = new[]
            {
                "You must manage and protect keys. A lost key means lost data.",
                "A key that ships inside the game can be extracted. It slows cheaters down, it does not stop them.",
                "Cannot be diffed or merged.",
                "In RSA mode the game cannot read its own files back."
            },
            BestFor = "Saves where tampering matters, private user data, and data sent to a server."
        };

        public static readonly string[] AesModeNotes =
        {
            "One secret 32 byte key encrypts and decrypts.",
            "The game needs the key to read its own saves, so the key has to reach the player's machine. Treat it as obfuscation against casual editing."
        };

        public static readonly string[] RsaModeNotes =
        {
            "Each file gets a random AES key, wrapped with your RSA public key.",
            "The game only needs the public key to write. The public key can be public.",
            "Reading needs the private key. Keep it on your server, in a secret manager or on your own machine, never in the game.",
            "Use it for data going from the game to you, such as leaderboards, bug reports and telemetry."
        };

        public static string RsaSizeNote(int bits)
        {
            return bits switch
            {
                4096 => "4096 bits (recommended). The largest margin. Generating takes longest, but happens once. Adds 512 bytes to every file.",
                3072 => "3072 bits. Matches the 128-bit security level NIST gives AES-128. A good balance.",
                _ => "2048 bits. The minimum that is still acceptable. Fast, but with the smallest safety margin."
            };
        }

        public static readonly string[] EncryptedChecklist =
        {
            "Pick a mode: AES for single-player saves, RSA for data you send to a server.",
            "Generate the keys on the Encryption tab.",
            "Store them on the Keys tab, as environment variables or in this project's key file. Both stay out of version control. Also back them up in a password manager.",
            "Never commit the AES key or the RSA private key. Check that UserSettings is in your .gitignore.",
            "Test a round trip on the Decrypt tab.",
            "Decide how the game gets the key. Development: environment variables. Release: the Shipping tab.",
            "Keep old keys until every old save has been re-saved, or those saves cannot be opened.",
            "Remember that a key in the game can be extracted. For data that really must stay secret, keep the key and the decryption on a server."
        };

        public static readonly string[] PrivateKeyAdvice =
        {
            "Never ship it, commit it or paste it into chat or tickets.",
            "Keep it in a secret manager (Azure Key Vault, AWS Secrets Manager, HashiCorp Vault), a password manager, or an encrypted backup.",
            "Give your server access through its secret store or environment, not through a file in the repository.",
            "On a build server, use the CI system's secret variables.",
            "If it leaks, generate a new pair, ship the new public key, and re-encrypt what you still need."
        };

        public static readonly string[] TokenSteps =
        {
            "The player signs in with your auth provider (Steam, Epic, PlayFab, Firebase, Unity Authentication) and the game gets a short-lived access token.",
            "The game sends the token to your key service over HTTPS.",
            "The service validates the token on the server and returns a key made for that player, so one leaked key opens one player's saves.",
            "The game keeps the key in memory only. It never writes it to disk or PlayerPrefs, and creates its store after the key arrives.",
            "Rotate keys with a version number on the server, rate limit requests, and keep old keys until saves are re-saved.",
            "The generated script has a stub, RequestKeyAsync, for your own request. The guide has the full flow and a server example."
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
