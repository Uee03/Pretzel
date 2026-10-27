# Extensions Usage

Add `ExtensionsUsageExample` to a GameObject in an empty scene and press Play. The Console shows each result.
Everything the sample creates lives under an `Extensions Sandbox` object that is destroyed with the sample, and
files go to `temporaryCachePath/PretzelSamples`.

| Area | Extensions |
|------|------------|
| Strings | validation (email, URL, phone, alphanumeric, pattern), cleaning (`RemoveSpecialChars`, `ToAlphanumeric`, `InnerTrim`, ...), Base64 and bytes |
| Bytes and encodings | `ToBase64`, `FromBytes`, `EncodingType.ToEncoding()` |
| Collections | `Shuffle`, `Swap`, `GetRandomItem`, `TryGetRandomItem` for lists and arrays |
| Numbers | `ToTimeMS`, `ToTimeHMS` |
| Objects | `IsNull` / `IsNotNull`, which also detect destroyed Unity objects |
| LayerMask | `InLayerMask` |
| Transform | `PositionToInt`, `ReparentAndAlign`, `SetColliderInteractionLayers`, `DestroyAllChildren`, `Clone`, `Activate`, `Deactivate`, `Destroy` |
| GameObject | `GetParent`, `GetParentGo`, `SetParent`, `Clone`, `Activate`/`Deactivate`, timed routines, `Destroy` |
| MonoBehaviour | `PrintWarning`/`PrintError`/`PrintException`, enable/disable, GameObject on/off, timed variants, `Destroy`, `DestroyGO` |
| Texture2D | `ToBase64` and `ToTexture2D` (the caller destroys the texture) |
| Files | sync, async and stream read/write for text, bytes and lines, plus `IsPathValid` |

The sample logs one error on purpose, to show `PrintError`.
