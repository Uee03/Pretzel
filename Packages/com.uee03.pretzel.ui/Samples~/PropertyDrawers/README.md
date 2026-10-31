# Property Drawers

`PropertyDrawer`s built with `CreatePropertyGUI`, grouped by complexity. Each level has a demo component; create one from
**Pretzel > Samples > Property Drawers > Create** (or **Add Component > Pretzel Samples**) and look at the Inspector.

| Level | Drawers | What it shows |
|-------|---------|---------------|
| `1-Basic` | `[ReadOnlyField]`, `[ProgressBar]` | The smallest useful drawer: wrap the default `PropertyField` so undo, multi-object editing and prefab overrides keep working, and an attribute drawer with a parameter that adds a `ProgressBar` kept current with `TrackPropertyValue`. |
| `2-Intermediate` | `FloatRange`, `SpawnSettings`, `[ShowIf]` | Type drawers for `[Serializable]` classes: a foldout with child fields in rows, a `MinMaxSlider` that writes two properties and follows them back, and a drawer that hides a field while a sibling bool is false (also inside nested classes and list elements). |
| `3-Advanced` | `LootEntry`, `[SerializeReferenceSelector]`, `StatusBadge` | A drawer for an element type used in lists, a dropdown fed from a data source (`ItemCatalogSource`, reloaded when an `ItemCatalog` asset is imported), HelpBox validation, `RegisterValueChangeCallback`, a reusable `[UxmlElement]` control (`StatusBadge`) and a type dropdown for `[SerializeReference]` fields (`DamageEffect`, `HealEffect`, `SpawnEffect`). |
| `Shared` | - | `SampleAssets` finds the USS by name; `SampleMenu` creates the demo objects. |

## Try it

1. Import the sample (**Pretzel UI > Samples > Property Drawers**).
2. For level 3, create an item catalog first: **Assets > Create > Pretzel Samples > Item Catalog**, add some ids, then
   open the Advanced demo.
3. Edit values, press Undo, select several demo objects at once, and use the list reorder handles.

Rules shown here:

- Return a `VisualElement` from `CreatePropertyGUI` and let `PropertyField` do the binding.
- Put state in a custom element (`LootEntryElement`, `ReferenceSelectorElement`) rather than in the drawer, and subscribe to
  outside events only while the element is attached to a panel.
- `TrackPropertyValue` also fires on Undo, so it is the right hook for derived UI.

Assemblies: `Pretzel.UI.Samples.PropertyDrawers.Runtime` and `Pretzel.UI.Samples.PropertyDrawers.Editor`.
