# Pretzel Extensions

Extension methods for .NET and Unity types. Depends on `pretzel.core`.

| Class | Covers |
|---|---|
| `StringExtensions` | `IsNullOrEmpty(trim)`, `ToBytes`, `ToBase64`, `FromBase64`, `HasSpecialChars`, `RemoveSpecialChars`, `ToAlphanumeric`, `InnerTrim`, `IsValidEmail`, `IsValidUrl`, `IsValidPhoneNumber` |
| `ByteExtensions`, `EncodingTypeExtensions` | `ToBase64`, `FromBytes`, `EncodingType` to `Encoding` |
| `IOExtensions` | `WriteText`, `ReadText`, `WriteBytes`, `ReadLines` and their `Async` versions, `WriteWithStream`, `ReadWithStream`, `IsPathValid` |
| `ObjectExtensions` | `IsNull`, `IsNotNull`, Unity-aware for destroyed objects |
| `GameObjectExtensions` | `Activate`, `Deactivate`, `ActivateRoutine`, `DeactivateRoutine`, `SetParent`, `Destroy`, `Clone` |
| `TransformExtensions` | `DestroyAllChildren`, `ReparentAndAlign`, `PositionToInt`, `SetColliderInteractionLayers`, `Clone` |
| `MonoBehaviourExtensions` | `Activate`, `Deactivate`, `Destroy`, `SetParent`, `PrintWarning`, `PrintError`, `PrintException` |
| `ListExtensions` | `Shuffle`, `Swap`, `GetRandomItem`, `TryGetRandomItem` |
| `LayerMaskExtensions` | `InLayerMask` |
| `FloatExtensions` | `ToTimeMS`, `ToTimeHMS` |
| `Texture2DExtensions` | `ToBase64`, `ToTexture2D` |

Names that changed from LazyJedi: `RemoveSpecialChars_ExclSpaces` is now `ToAlphanumeric`, `RemoveAllSpecialChars` is now `RemoveSpecialChars`, the email and URL checks are one method each, `Activate(float)` and `Deactivate(float)` on `GameObject` are now `ActivateRoutine` and `DeactivateRoutine`, and the stream helpers are `WriteWithStream` and `ReadWithStream`.

## Sample

`Samples~/ExtensionsUsage`.