# Comment Block Settings

**Comment Block Settings** let you set up different comment block patterns to be
detected in different file types, with different behaviors.

A comment block is characterised by patterns matched at the same indentation
level on consecutive lines of a source file. The first line may have text before
it, but subsequent lines can only have whitespace. Three kinds of pattern are
matched as part of a block:

- a **start** pattern, optionally matched on the first line of the block
- an **end** pattern, optionally matched on the last line of the block
- a **line start** pattern, which must occur with the same indentation on all
  other lines

Once a comment block is matched, these patterns are stripped so the text inside
them can be processed. They are reattached when processing is complete.

## The comment block list

The tab shows a list of the **names** of all the currently defined comment
blocks. The name has no special function and is just for identification, but it
must be unique. Click an entry to show its settings in the controls below. Click
it again to rename it.

The buttons to the top right of the list are:

### New

Creates a new comment block. Its settings must be filled in before it can be
used.

### Delete

Permanently deletes the selected comment block.

### Up / Down

Moves the selected comment block up or down in the search order. **Order is
important.** Text is scanned for comment blocks in the order of this list, and
only the first match found is used for further processing.

## Comment block settings

### File Types

A semicolon-separated list of file names, with `*` wildcards, to associate with
this comment block. Only files matching one of these entries are scanned for
this block.

### Block Start is

One of *not used*, *always on own line*, *on own line if comment is >1 lines*,
or *never on own line*.

- **not used**: the block has no start pattern. The first line of the block
  starts with the line start pattern, for instance:

  ```
  // this comment block
  // has no start pattern
  ```

- **always on own line**: the block start pattern is always on a line by itself
  and never has comment text after it, for instance the start of this block:

  ```
  /****************************************************
   * You never want text after the block start above
   ****************************************************/
  ```

  The pattern can still optionally have text before it. See
  [First Line of Block](#first-line-of-block).

- **never on own line**: the pattern is always on the same line as comment
  text, for instance:

  ```
  /* The block start (/*) has
   * a comment after it */
  ```

- **on own line if comment is >1 lines**: the pattern is on the same line as
  comment text only when the whole comment block is one line long:

  ```
  /** The start block is on the same line as comment text as the first line is short. */

  /** 
   * For the exact same matched block, the block start is now on a different line,
   * because the text is too long to fit on a single line
   */
  ```

  If the block start string has a single trailing space, it is trimmed when the
  block start is placed on a line by itself.

### Block Start

The string for the block start. It can be a regular expression, set by ticking
the **is RegEx** checkbox. Don't use the start-of-line symbol `^` in the regular
expression, because it is added automatically.

### Block End is

One of *not used*, *always on own line*, *on own line if comment is >1 lines*,
or *never on own line*. These have exactly the same meanings as for
[Block Start is](#block-start-is). If the block end is not on a line by itself,
it always has the line start pattern before it.

### Block End

The string for the block end. It can be a regular expression, set by ticking the
**is RegEx** checkbox. Don't use the start-of-line symbol `^` in the regular
expression, because it is added automatically.

### Line Start

The pattern for a continuation line of the block. This cannot be a regular
expression. If the pattern has a trailing space, it also matches a line that
ends where that space would be.

### First Line of Block

Either *must not contain text before start* or *can contain text before start*.
This controls whether the first line of the block (the block start line, or the
first continuation line if the block has no block start) is matched when there
is anything other than whitespace before it on the line. For instance, with
*must not contain text before start* selected, this block is not matched:

```
int i = 0; // This block will not be matched as "must not contain text before start"
           // was selected. If "can contain text before start" was selected then
           // the block would be matched.
```

Whatever is selected here, all lines after the first can only contain
whitespace before the block.
