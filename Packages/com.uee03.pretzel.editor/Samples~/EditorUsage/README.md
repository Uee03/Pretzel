# Editor Usage

Shows **GameObject > Pretzel > Group Selected Objects**.

1. Open any scene in Edit mode.
2. Choose **Tools > Pretzel Samples > Editor > Create Objects To Group**. Four objects are created and selected.
3. Choose **GameObject > Pretzel > Group Selected Objects** (or right-click them in the Hierarchy, or use
   **Tools > Pretzel Samples > Editor > Group Selection**).
4. A `Group` object is created at the centre of the selection, keeps the world positions of its children,
   and enters rename mode.
5. **Edit > Undo** removes the group in one step.

The command works on the top-level editable objects of the selection. Prefab assets in the Project
window are ignored.

Delete this folder to remove the sample menu items.
