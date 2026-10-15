# General Settings

The general settings are:

## Use tabs to indent blocks

Whether tabs are used to indent. If this is checked then tabs are used to indent new lines of blocks,
and new lines within blocks when they are crated. If not checked then only spaces are used. If tabs are used
then they will have the width set for the current file type from the *Tools->Options->TextEditor->Tabs*
dialog.

## Wrap Width

This is the column number that reflowable text is wrapped at assuming the block start column is at least
**Minimum Block Width** columns less than it.

## Minimum Block Width

This is the minimum wrap width of any block of text in a comment block. Basically for any block **Wrap Width**
will be exceeded if neccesary to meet this minimum. The purpose of the setting  is to ensure that blocks of text that start
on the far right of screen do not get wrapped stupidly (eg one word per line). Note that the starting column is taken
from the first column of text not the first column of the block. So for instance with a **Wrap Width** of 80
and a **Minimum Block Width** of 20:

```
          1         2         3         4         5         6         7         8         9
0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
/**
 *                                                                             A block starting very near the wrap width, so it will be wrapped to minimum block width from the start instead.
 */

Becomes:

/**
 *                                                                             A block starting very 
 *                                                                             near the wrap width, 
 *                                                                             so it will be wrapped 
 *                                                                             to minimum block 
 *                                                                             width from the start 
 *                                                                             instead.
 */
```
