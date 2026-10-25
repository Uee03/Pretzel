# AES save guidance

## The normal offline game

Use one randomly generated, backed-up AES key for the game's lifetime. Export it once using
embedded C# or bundled JSON, initialize DataIO, and handle load/save failures.
**Routine rotation is optional.** A token service or web-application key-management system is not
required for ordinary single-player saves.

AES is the only encryption option. Plain JSON remains the unencrypted default.
See [setup](DataIOSetup.md) and [key delivery](EncryptionKeyDelivery.md).

## What protection means

| Situation | AES saves |
|---|---|
| Casual text editing | Contents are unreadable; edits without the key fail authentication |
| Corruption or a wrong key | Load fails authentication; the cause cannot be distinguished |
| Player with a debugger/reverse-engineering tools | Can extract any key the client uses |
| Restoring an older valid save | Not prevented |
| Sharing a save with someone who has the same key | Not prevented |
| Multiplayer cheating or sensitive server data | Use server authority; do not trust client encryption |

Embedded XOR masking and IL2CPP are not secret storage. Even a key fetched over HTTPS becomes
accessible once the client uses it. Keep real secrets and authoritative decisions on a trusted
server; use HTTPS for transport.

## Implementation and compatibility

The existing AES wire format is preserved: `PRTZ`, version byte `1`, AES mode byte `1`, a random
16-byte IV, AES-256-CBC ciphertext with PKCS7 padding, then a 32-byte HMAC-SHA256 tag.
Encryption and authentication keys are derived separately with HMAC-SHA256 from the master key
using the ASCII labels `pretzel-encrypt` and `pretzel-mac`.
The header, IV and ciphertext are authenticated before decryption. Tags use fixed-time comparison.
Keys, IVs and salts use a cryptographic random generator.

Do not replace this with bare CBC, reuse a fixed IV, pad a password into a key, or log key bytes.
The IV is deliberately stored with ciphertext; it is not a secret.
This description documents the format, not an independent cryptographic certification.

## Keep the key, preserve the save

- Back up the key outside the project and outside the save folder.
- Never create a fresh key in a load-failure handler or every startup.
- Keep generated key exports out of public source control, including their `.meta` files.
- `UserSettings/Pretzel/pretzel-keys.json` is plaintext and Editor-only. Protect its backups too.
- Handle `Corrupt` explicitly. A new profile must not silently replace unreadable progress.
- Retain save backups; the recoverable write fallback is not a guarantee against every filesystem
  or power-loss failure.

## Optional advanced features

### Changing a key

If you deliberately need to change the key, advanced controls retain the old key in
`PreviousAesKeys`. The store writes with the current key and tries current/history keys for reads.
Re-export delivery files; old saves move to the new key when saved again.
Do not clear history until all supported old saves have migrated. Offline players may skip releases,
so retaining historical read keys is often simpler. Environment variables hold only the current key.
Keeping history preserves compatibility; it does not revoke access from someone with an old key.

### Passphrase-protected exports

Use `EncryptionKeys.DeriveAesKey(password, salt)` only when a person must remember the secret.
It uses PBKDF2-HMAC-SHA256 with a default 600,000 iterations. Generate a salt with
`EncryptionKeys.GenerateSalt()` and retain it with the export; the salt is not secret.
Derive off the main thread, measure on target devices, and keep the derived key only as long as
needed. A weak/forgotten password remains a problem. Do not use reversible save encryption to store
account passwords.

### Server delivery

Only add this when the game needs sign-in/per-player keys. Validate authorization on the server,
use HTTPS, avoid logging keys, and plan offline/error handling. The generated token-delivery code
is a scaffold: implement its request rather than shipping the placeholder.

## Further reading

- [Microsoft: encrypting data](https://learn.microsoft.com/dotnet/standard/security/encrypting-data)
- [OWASP cryptographic storage guidance](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)

These are background guidance; installed source and the documented Pretzel file version determine
compatibility. No performance or platform guarantees follow from these references.
