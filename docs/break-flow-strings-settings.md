# Break Flow Strings Settings

**Break Flow Strings Settings** allows you to set up different Break Flow String patterns to be detected in
lines of a Commebt Block. A line which contains one of these patterns can have one of the following properties:

- Either it will never be reflowed, no matter how long it is. This also means that preceeding lines will not be reflowed into it.
- or it will never be reflowed into subsequent lines. This means that if the line is too long a new line
  will be created to take the overflow before any proceeding lines

The first thing you will encounter upon selecting the **Break Flow Strings Settings** tab is a a list box
containing the **names** of all the currently defined Break Flow Strings. The **Name** has no special
function programatically and is just for user identification, however it must be unique. Left clicking on any
entry in the list shows its settings in the controls below. Left clicking again allows renaming of the block.
To the top right of the listbox is 4 buttons to manipulate the entries in it:

## New

Creates a new Break Flow String. It will need to have its settings set before use.

## Delete

Permanantly deletes the currently selected Break Flow String.

## Up/ Down

Moves the currently selected Break Flow String up and down in the seacrh order. Note that order **is**
important. When a block of code is scanned for Break Flow Strings, it is scanned in the order of this list,
and the first match found is the only one used to do further processing.

The settings for any Break Flow String are:

## Match String

This is the string to match anywhere in the line for it to be matches as a Break Flow line. It
is optionally a regular expression, set by ticking the **is RegEx** checkbox.

## Never reflow line with this string at all

If this is selected the line will never be reflowed, no matter how long it is. Also preceeding lines
will never be wrapped into it (new lines will be inserted to take the overflow instead).Bascially this
setting means "if this line is a match never touch this line!"

## Never reflow line with this string into next

If this is selected the line will be reflowed, and can be reflowed into, but the line will never
be reflowed into proceeding lines (new lines will be inserted to take the overflow). Generally this
setting only make sense if the pattern is the last thing on a line. An example of this setting is
say if the match string is the non-regular expression "$", then:

```
// A short line of text.
// A really long line of text, so long that it will have to be reflowed, which at the end of it has the break flow string.$
// A line following the break flow string

Becomes:

// A short line of text. A really long line of text, so long that it will have to be reflowed, which at 
// the end of it has the break flow string.$
// A line following the break flow string
```
