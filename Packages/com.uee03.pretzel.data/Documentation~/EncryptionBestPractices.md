# Encryption best practices

How to use Pretzel's `Encrypted` format well, and what the research behind it says. Read [EncryptionKeyDelivery.md](EncryptionKeyDelivery.md) next for getting keys into a shipped game.

## Sources

Reviewed on 2026-10-19: Microsoft Learn (Encrypting data), Safeguard (C# cryptography best practices), Milan Jovanovic (AES in C#), Frontegg, Kiteworks, SCDS/McMaster (best practices for encryption), C# Corner (encrypting data in ASP.NET Core), Osama Haider (encryption for API security), dev.to (secure C# applications), Hardik Ojha (secure authentication in Unity), NewSoftwares (persistent data in Unity).

Not usable: the CISA page redirects elsewhere, the Dell Unity page is behind a bot check, and the LinkedIn article needs a login. The Dell page is about storage-array hardware encryption, so it does not apply to game saves anyway.

## 1. Know what encryption can do for a game

| Threat | Does encrypting the save help? |
|---|---|
| A player opens the file and edits gold or level | Yes. Edits are rejected (HMAC), and values are not readable |
| A player copies a save to another PC or shares it | No. The file opens wherever the key is |
| A player with the game and a debugger wants the key | No. Anything the game can use, the machine can reach |
| Data sent to your server is read in transit | Use HTTPS. Add RSA mode if the server alone should read it |
| A leaked backup or cloud copy of saves | Yes, if the key is not stored with it |
| Cheating in multiplayer | No. Keep the rules and the truth on the server |

Encryption raises the effort. It does not make client-side data trustworthy. Decide what you are protecting and from whom before choosing a format.

## 2. Practice by practice

| Practice (what the sources say) | What Pretzel does | What you do |
|---|---|---|
| Use a strong, standard algorithm: AES-256, RSA 2048 or more. Never DES, 3DES, RC2 or RC4 (Frontegg, Safeguard, Kiteworks) | AES-256-CBC. RSA 2048, 3072 or 4096, with 4096 recommended | Do not add custom ciphers |
| Authenticated encryption, not bare AES-CBC (Safeguard) | Encrypt-then-MAC with HMAC-SHA256 over the whole file, verified before decrypting. `AesGcm` is not in Unity's Mono profile, so this is the equivalent | Treat a `Corrupt` result as tampering or a wrong key |
| A new random IV for every encryption, stored with the data (Microsoft, Jovanovic, Safeguard) | Random 16 byte IV per file, written into the file | Nothing. There is no IV to store |
| Keys and IVs from a cryptographic random source, never `System.Random` (Safeguard) | `RandomNumberGenerator` for keys, IVs and salts | Never type a key by hand or pad a string into one |
| Constant-time comparison of tags (Safeguard) | `CryptographicOperations.FixedTimeEquals` | Do the same in any code that reads these files |
| Never hardcode keys. Use environment variables or a vault (Frontegg, C# Corner, dev.to, Jovanovic, Osama Haider) | Keys come from `EncryptionKeys.Resolve`: your code, environment variables, the Editor-only key file, or an optional bundled game JSON config | Pick a delivery option in [EncryptionKeyDelivery.md](EncryptionKeyDelivery.md). Bundled JSON is plain text; embedded C# is obfuscation, not protection, even with IL2CPP |
| Keep keys apart from the data (Jovanovic, Kiteworks) | Editor key file is in `UserSettings`, never next to saves; game JSON is explicitly opt-in | Never put secret keys in the repository or save folder. Only bundle AES for offline saves if you accept that players can extract it; never bundle an RSA private key |
| Rotate keys, and after any suspected leak (Kiteworks, Frontegg, SCDS, C# Corner, Jovanovic, Osama Haider) | A new AES key keeps the old one as a previous key, so old saves still open and move to the new key when saved again. See section 6 | Plan rotation before you ship, and clear previous keys when no old save remains |
| Back up keys. A lost key is lost data (SCDS) | Not automatic | Keep every key in a password manager or secret manager, and a copy offline |
| Use PBKDF2 or similar with a salt when the secret is a password (Safeguard) | `EncryptionKeys.DeriveAesKey` (PBKDF2-HMAC-SHA256, 600,000 iterations. Safeguard's example uses 210,000, and 600,000 is the figure OWASP gives for this function, which was not among the reviewed sources) | See section 5 |
| Hash passwords, do not encrypt them (C# Corner, Hardik Ojha) | Not for passwords | Hash on a server with bcrypt, Argon2 or PBKDF2 |
| HTTPS everywhere, never disable certificate checks (Safeguard, Hardik Ojha, dev.to) | Not an HTTP library | Do not write a `CertificateHandler` that returns `true` |
| Detect tampering and corruption, keep backups of data (SCDS, NewSoftwares) | HMAC, atomic writes and a `.bak` that is recovered from automatically | Keep `KeepBackup` on |

## 3. Choosing a mode

- **AES** for single-player saves. One secret key. Simple, fast. The key has to reach the player's machine, so it is only as safe as that delivery.
- **RSA** for data going from the game to you (scores, bug reports, telemetry). The game holds only the public key and can write but not read. The private key stays on your server or with you.
- **Encrypted + a server** when the data must really stay secret. Keep the key and the decryption on the server and send the result.

RSA only wraps a random 32 byte key per file. The size changes key generation time and adds 256 to 512 bytes to each file, so choose 4096.

## 4. Patterns in the articles you should not copy

Several of the reviewed articles show code that works but is unsafe:

| Seen in | Problem | Do this instead |
|---|---|---|
| dev.to | A fixed key and a fixed IV as constants in the class | A random key from `EncryptionKeys.GenerateAesKey`, a new IV per file (done for you) |
| Osama Haider | `key.PadRight(32)` turns a short string into a key | Random 32 bytes, or `DeriveAesKey` for a passphrase |
| Microsoft Learn sample | RSA with PKCS#1 v1.5 padding, and a hand-typed 16 byte key | OAEP padding (Pretzel uses it), random keys |
| Several | AES-CBC with no MAC | HMAC over the ciphertext (Pretzel does it) |
| Jovanovic, C# Corner | In the demos the key is returned next to the ciphertext, or the IV is stored with the record and the key sits in a variable. Both articles say to use a vault in production | Never keep them together |
| NewSoftwares | Says Unity has built-in `PlayerPrefs` AES encryption | It does not. `PlayerPrefs` is plain text. Encrypt first, for example with Pretzel and `Base64Transform` |

## 5. Passphrase keys

Use a passphrase only when a person must remember the secret, for example a player password that protects an exported save.

```csharp
byte[] salt = EncryptionKeys.GenerateSalt();                     // store this next to the data, it is not secret
byte[] key = EncryptionKeys.DeriveAesKey(playerPassword, salt);  // 600,000 PBKDF2-HMAC-SHA256 iterations
EncryptionKeys keys = new() { AesKey = key };
DataStore store = DataStoreFactory.Create(DataRoots.PersistentData("Export"), DataFormat.Encrypted, keys);
```

- The same password and salt always give the same key. A new salt gives a different key, so each user or file should have its own salt.
- Deriving a key is slow on purpose. In the Unity Editor it took about 2 seconds on a desktop. Run it on a background thread (`Awaitable.BackgroundThreadAsync`), derive once, and keep the key in memory.
- It cannot make a weak password strong. Ask for long passphrases (12 characters or more, ideally random words).
- A forgotten password means the data is gone. Say so in the UI.
- Do not use this to store passwords. Hash them on a server.

## 6. Rotating a key

For AES, generate a new key on the Encryption tab. The old key is kept as a previous key (`EncryptionKeys.PreviousAesKeys`), and the key file, the bundled JSON and the embedded setup all carry it. A store writes with the current key and reads with the current key first, then each previous key. The HMAC is checked before anything is decrypted, so only the key that wrote a file can open it. Old files therefore keep opening, and move to the new key as they are saved again. Clear the previous keys on the Keys tab once every old save was loaded and saved again.

```csharp
EncryptionKeys keys = new() { AesKey = newKey };
keys.AddPreviousAesKey(oldKey);   // add it after setting the new key: a key equal to the current one is ignored
DataStore store = DataStoreFactory.Create(root, DataFormat.Encrypted, keys);
```

Environment variables and a token flow carry the current key only, so supply the history yourself when players may hold older saves. RSA has no history: a new key pair cannot open files wrapped for the old public key, so keep the old private key on your server until those files are processed.

To change a key without keeping the old one in the game, or to change formats, copy by reading with one store and writing with another:

```csharp
DataStore oldStore = DataStoreFactory.Create(root, DataFormat.Encrypted, oldKeys);
DataStore newStore = DataStoreFactory.Create(root, DataFormat.Encrypted, newKeys, extension: "pretzel2");

foreach (int slot in oldStore.ListSlots("Profiles"))
{
    foreach (DataKey key in oldStore.List("Profiles", slot))
    {
        LoadResult<JToken> result = oldStore.Load<JToken>(key);   // any type works, JToken keeps the data as it is
        if (result.Ok)
        {
            newStore.Save(key, result.Value);
        }
    }
}
// Delete the old files and retire the old key only after every file was copied and a copy opens.
```

Rotate when someone who knew the key leaves, when a key may have leaked, and on a schedule that fits your risk. After a leak, a previous key in a build is also extractable, so re-encrypt old saves, then clear it.

## 7. Reading Pretzel files outside Unity

RSA mode exists so a server can read what a game wrote. The format is:

| Bytes | Content |
|---|---|
| 4 | `PRTZ` |
| 1 | Version, currently 1 |
| 1 | Mode: 1 = AES, 2 = RSA |
| 2 + n | RSA mode only: wrapped key length (big endian) and the wrapped key (RSA-OAEP with SHA-1) |
| 16 | IV |
| m | AES-256-CBC ciphertext, PKCS7 padding |
| 32 | HMAC-SHA256 over every byte before it |

The AES and MAC keys are `HMACSHA256(master, "pretzel-encrypt")` and `HMACSHA256(master, "pretzel-mac")`, where master is the AES key (AES mode) or the unwrapped key (RSA mode). If Base64 is on, decode the file text first. The result is UTF-8 JSON.

```csharp
public static string Decrypt(byte[] file, byte[] aesKey, RSA rsaPrivateKey)
{
    if (file.Length < 6 + 16 + 16 + 32 || file[0] != 'P' || file[1] != 'R' || file[2] != 'T' || file[3] != 'Z' || file[4] != 1)
    {
        throw new InvalidDataException("Not a Pretzel encrypted file.");
    }

    int offset = 6;
    byte[] master;
    if (file[5] == 1)
    {
        master = aesKey ?? throw new InvalidOperationException("This file needs the AES key.");
    }
    else if (file[5] == 2)
    {
        int length = (file[offset] << 8) | file[offset + 1];
        offset += 2;
        byte[] wrapped = new byte[length];
        Array.Copy(file, offset, wrapped, 0, length);
        offset += length;
        master = (rsaPrivateKey ?? throw new InvalidOperationException("This file needs the RSA private key."))
            .Decrypt(wrapped, RSAEncryptionPadding.OaepSHA1);
    }
    else
    {
        throw new InvalidDataException("Unknown mode.");
    }

    using HMACSHA256 derive = new(master);
    byte[] encryptionKey = derive.ComputeHash(Encoding.ASCII.GetBytes("pretzel-encrypt"));
    byte[] macKey = derive.ComputeHash(Encoding.ASCII.GetBytes("pretzel-mac"));

    int bodyLength = file.Length - 32;
    using (HMACSHA256 hmac = new(macKey))
    {
        byte[] expected = hmac.ComputeHash(file, 0, bodyLength);
        byte[] actual = new byte[32];
        Array.Copy(file, bodyLength, actual, 0, 32);
        if (!CryptographicOperations.FixedTimeEquals(expected, actual))
        {
            throw new CryptographicException("Wrong key, or the file was modified.");
        }
    }

    byte[] iv = new byte[16];
    Array.Copy(file, offset, iv, 0, 16);
    offset += 16;

    using Aes aes = Aes.Create();
    aes.Key = encryptionKey;
    aes.IV = iv;
    aes.Mode = CipherMode.CBC;
    aes.Padding = PaddingMode.PKCS7;
    using ICryptoTransform decryptor = aes.CreateDecryptor();
    return Encoding.UTF8.GetString(decryptor.TransformFinalBlock(file, offset, bodyLength - offset));
}
```

Load the private key on the server with `rsa.FromXmlString(xml)`, taking the XML from a secret store. This code was run against files written by Pretzel in AES and RSA mode, and rejects an edited file.

## 8. Unity specifics

- `PlayerPrefs` is plain text in the registry or a plist. Do not put secrets or unencrypted save data there. If you need text, write an `Encrypted` store with Base64 on.
- IL2CPP and obfuscators make reading a key harder, not impossible. Do not rely on them.
- Keep session tokens in memory. If one must be stored, encrypt it, and expire it on the server.
- Log nothing secret. Not keys, not tokens, not decrypted data. Pretzel's error messages never include key material.
- Use `UnityWebRequest` over HTTPS only. Never assign a `CertificateHandler` that accepts every certificate, not even for testing.
- Tell players that deleting `persistentDataPath` or reinstalling loses encrypted saves if the key was per install.
- Cloud saves: upload the encrypted file. If the key lives on the device only, a new device cannot read it. A per-player key from a server (see the delivery guide) fixes that.

## 9. Step by step with Pretzel

1. Open `Pretzel > Data Format` and pick **Encrypted** on the Format tab. Leave the extension as `.pretzel`.
2. On the Encryption tab choose the mode, and for RSA choose 4096. Generate the keys.
3. On the Keys tab, store them as environment variables or in the project key file. Check that `UserSettings` is git-ignored. Back the keys up in a password manager.
4. Save and load as usual. `DataIO` uses the config:

   ```csharp
   DataIO.Save("profile", profile);
   LoadResult<Profile> result = DataIO.Load<Profile>("profile");
   if (result.Status == DataStatus.Corrupt)
   {
       // Wrong key or a changed file. Offer the player a restore from backup, do not overwrite it silently.
   }
   ```
5. Test the round trip on the Decrypt tab, and try an edited file to see it rejected.
6. Pick the delivery option for release on the Shipping tab (or the Setup tab), set the key source to match, and read the delivery guide.
7. Run **Check build setup**, then go through the checklist below.

Without the window, in code only:

```csharp
EncryptionKeys keys = EncryptionKeys.Resolve("PRETZEL_MYGAME", KeySource.Provider, out string origin);
if (keys == null)
{
    // Fail loudly in development. In release, show an error, do not fall back to plain text.
}

DataIO.Default = DataStoreFactory.Create(DataRoots.PersistentData("SaveData"), DataFormat.Encrypted, keys.ForClient(EncryptionMode.Aes));
```

## 10. Release checklist

- [ ] The AES key and RSA private key are not in the repository, the build, the logs or a ticket.
- [ ] Keys are backed up in a password manager or secret manager, with an offline copy.
- [ ] `UserSettings` is in `.gitignore`, and the generated `PretzelDataSetup.cs` is not committed if it holds an AES key.
- [ ] A fresh install on a clean machine can read and write saves.
- [ ] A save from the previous build still opens.
- [ ] A wrong key, an edited file and a missing key each end in a handled result, not a crash and not a silent overwrite.
- [ ] HTTPS only, with certificate validation on.
- [ ] You decided how a key is rotated or revoked, and wrote it down.
- [ ] **Check build setup** is clean, and a build without the Editor key file or your environment variables can read and write saves.
- [ ] Anything that must stay secret from the player is on a server, not in the game.

## 11. Limits of this implementation

- RSA key wrapping uses OAEP with SHA-1, because Mono's RSA does not support OAEP with SHA-256. It is safe for wrapping a random key.
- Encryption is AES-CBC plus HMAC, not AES-GCM, because `AesGcm` is not available in Unity's .NET profile. The result gives the same guarantees when used as described.
- One current key per store. AES files hold no key id, so reading tries the current key and then each previous key; the HMAC check makes that safe. RSA has no key history.
- Keys live in normal managed memory and are not wiped after use. This is the same for every Unity encryption library.
- The key file in `UserSettings` is plain text, protected only by your user account and your `.gitignore`.
- Whole files are processed in memory. Split very large data into several smaller files.
- The Binary format uses the obsolete BSON types of the bundled Newtonsoft.
- This is not a security audit. For regulated data (payment, health, children's data), use a reviewed library or a server-side service, and get advice.
