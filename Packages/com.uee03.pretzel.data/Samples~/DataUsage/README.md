# Data Usage

All sample files are written under `persistentDataPath/PretzelSamples`, so they never touch your game's saves.

| File | What it shows |
|------|---------------|
| `DataUsageExample.cs` | `DataIO`, slots and folders, a custom `DataStore` with the binary strategy and migrations, async saves, and loading a ScriptableObject (`SampleAudioSettings`) into a runtime copy |
| `DataToolsExample.cs` | `DataKey`, `SerializableGuid`, the JSON extensions, `Exists`/`TryLoad`/`ValueOr`/`List`/`ListSlots`/`Delete` and their async versions, the format strategies and `Base64Transform` |
| `QuestLog.cs` | Saving an `IQuestReward` interface list with Newtonsoft `TypeNameHandling.Auto` and an allow-list binder |
| `EncryptionUsageExample.cs` | AES saves from the DataIOConfig, keys fetched after sign-in, and sign-out |
| `PlayerProfile.cs` | A versioned save type with `[SchemaVersion]` and `[Preserve]` for IL2CPP |

## Try it

1. Add `DataUsageExample` and `DataToolsExample` to a GameObject. Optionally create an audio settings asset
   (**Assets > Create > Pretzel > Samples > Audio Settings**) and assign it.
2. Press Play and read the Console.
3. For encryption, choose **Encrypted** and **AES** in **Pretzel > Data Format**, then use the component context
   menu of `EncryptionUsageExample` in Play mode.

Rules:

- Create each `DataStore` once and keep it.
- Never overwrite a save that loaded as `Corrupt`. Offer recovery instead.
- Interface fields need `TypeNameHandling` and a binder that lists the allowed types. Never use
  `TypeNameHandling` without one on data a player can edit.
