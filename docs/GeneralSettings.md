# General Settings

## Use tabs to indent blocks

Whether tabs are used to indent. If this is checked, tabs are used to indent
new lines of blocks, and new lines within blocks when they are created. If it is
not checked, only spaces are used. Tabs have the width set for the current file
type in *Tools > Options > Text Editor > Tabs*.

## Wrap Width

The column number that reflowable text is wrapped at, provided the block start
column is at least **Minimum Block Width** columns less than it.

## Minimum Block Width

The minimum wrap width of any block of text in a comment block. **Wrap Width**
will be exceeded if necessary to meet this minimum. The purpose of this setting
is to ensure that blocks of text that start on the far right of the screen do
not get wrapped badly (for example, one word per line).

Note that the starting column is taken from the first column of text, not the
first column of the block. For instance, with a **Wrap Width** of 80 and a
**Minimum Block Width** of 20:

```
          1         2         3         4         5         6         7         8         9
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
/**
 *                                                                             A block starting very near the wrap width, so it will be wrapped to minimum block width from the start instead.
 */
```

Becomes:

```
/**
 *                                                                             A block starting very 
 *                                                                             near the wrap width, 
 *                                                                             so it will be wrapped 
 *                                                                             to minimum block 
 *                                                                             width from the start 
 *                                                                             instead.
 */
```
