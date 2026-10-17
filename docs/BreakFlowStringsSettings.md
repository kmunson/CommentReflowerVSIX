# Break Flow Strings Settings

**Break Flow Strings Settings** let you set up patterns to be detected in the
lines of a comment block. A line that contains one of these patterns has one of
these properties:

- It is never reflowed, no matter how long it is. Preceding lines are not
  reflowed into it either.
- It is never reflowed into following lines. If the line is too long, a new line
  is created to take the overflow, before any following lines.

## The break flow string list

The tab shows a list of the **names** of all the currently defined break flow
strings. The name has no special function and is just for identification, but
it must be unique. Click an entry to show its settings in the controls below.
Click it again to rename it.

The buttons to the top right of the list are:

### New

Creates a new break flow string. Its settings must be filled in before it can be
used.

### Delete

Permanently deletes the selected break flow string.

### Up / Down

Moves the selected break flow string up or down in the search order. **Order is
important.** Text is scanned for break flow strings in the order of this list,
and only the first match found is used for further processing.

## Break flow string settings

### Match String

The string to match anywhere in a line for it to count as a break flow line. It
can be a regular expression, set by ticking the **is RegEx** checkbox.

### Never reflow line with this string at all

If this is selected, the line is never reflowed, no matter how long it is.
Preceding lines are never wrapped into it either; new lines are inserted to take
the overflow instead. In short: "if this line matches, never touch it".

### Never reflow line with this string into next

If this is selected, the line can be reflowed and can be reflowed into, but it
is never reflowed into following lines; new lines are inserted to take the
overflow. This setting generally only makes sense if the pattern is the last
thing on a line.

For example, if the match string is the non-regular-expression `$`:

```
// A short line of text.
// A really long line of text, so long that it will have to be reflowed, which at the end of it has the break flow string.$
// A line following the break flow string
```

Becomes:

```
// A short line of text. A really long line of text, so long that it will have to be reflowed, which at 
// the end of it has the break flow string.$
// A line following the break flow string
```
