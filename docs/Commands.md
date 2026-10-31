# Commands

Comment Reflower has two modes of operation. It can work either on a single
block, or on all blocks that intersect a selection.

## Reflow a single block

Place the cursor on any line within the block, then from the **Tools** menu
select **Reflow Comment at Cursor**. Alternatively, right-click to bring up the
context menu and select **Reflow Comment at Cursor**.

## Reflow all blocks in a selection

First select the text. Note that all comments that intersect the selection will
be reflowed, not just the parts of them that are within it. Then from the
**Tools** menu select **Reflow All Comments in Selection**. Alternatively,
right-click to bring up the context menu and select **Reflow All Comments in
Selection**.

## Commands not available

If these menu items are not shown, the current file does not have any comment
block file associations. See [Comment Block Settings](CommentBlockSettings.md#file-types).

**Reflow All Comments in Selection** is shown but not enabled when no text is
selected.

If the settings have a problem, such as a regular expression that isn't valid,
running either command shows a message describing it instead of reflowing. See
[Invalid settings](Settings.md#invalid-settings).
