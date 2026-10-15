# Comment Block Settings

**Comment Block Settings** allows you to set up different comment block patterns to be
detected in different file types with different behaviors. A comment block is characterised
by patterns matched at the same indentation level on consecutive lines of a source file.
The first line may have text before it, but subsequent lines can only have whitespace. There
are three time of patterns that are matched as part of a block: start an end patterns
that are optionally matched on the first and last lines of a block, and a line start pattern
that must occur with the same indentation on all other lines. Once a comment block is matched
these patterns are stripped to enable processing of the text inside them, they are then
reattached when processing is complete.

The first thing you will encounter upon selecting the **Comment Block Settings** tab is a a
list box containing the **names** of all the currently defined comment blocks. The **Name**
has no special function programatically and is just for user identification, however it must be
unique. Left clicking on any entry in the list shows its settings in the controls below. Left
clicking again allows renaming of the block. To the top right of the listbox is 4 buttons to
manipulate the entries in it:

## New

Creates a new Comment Block. It will need to have its settings set before use.

## Delete

Permanantly deletes the currently selected Comment Block.

## Up/ Down

Moves the currently selected Comment Block up and down in the seacrh order. Note that order **is** important.
When a block of code is scanned for Comment Blocks, it is scanned in the order of this list, and the first match found
is the only one used to do further processing.

The settings for any Comment Block are:

## File Types

This is a semi-colon separated list of file names with "\*" wildcards to associate
this comment block with. Only files matching one of these entries will be scanned for
this particular block.

## Block Start is

This can have values *not used*, *always on own line*, *on own line if comment is >1 lines*, or *never on own line*.

- The value *not used* indicates that the block has no particular start pattern - the first line of the block will start
  with the line start pattern, for instance:

  ```
  // this comment block
  // has no start pattern
  ```

- The value *always on own line* indicates that the block start pattern is always on a line by itself -
  it will never have comment text following it, for instance the start of the following block:

  ```
  /****************************************************
   * You never want text after the block start above
   ****************************************************/
  ```

  Note that the pattern can still have text optionally before it: see the **First Line of Block** setting.

- The value *never on own line* indicates that the pattern is always on the same line as comment text, for instance:

  ```
  /* The block start (/*) has
   * a comment after it */
  ```

- The value *on own line if comment is >1 lines* indicates that the pattern is on the same line as comment text
  only when the total length of the comment block is one line:

  ```
  /** The start block is on the same line as comment text as the first line is short. */

  /** 
   * For the exact same matched block, the block start is now on a different line,
   * because the text is too long to fit on a single line
   */
  ```

  Note that if the block start string has a single trailing space then it will be trimmed if the block start is
  placed on a line by itself.

## Block Start

The actual string for the block start. This is optionally a regular expression, set by ticking the **is RegEx** checkbox.
Note that you should not use the start of line symbol "^" in the regular expression as this is automatically prepended.

## Block End is

This can have values *not used*, *always on own line*, *on own line if comment is >1 lines*, or *never on own line*.
These have the exact same meanings as for the **Block Start is** setting. Note that is the block end is not a line by itself
then it will always have the line start pattern before it.

## Block End

The actual string for the block end. This is optionally a regular expression, set by ticking the **is RegEx** checkbox.
Note that you should not use the start of line symbol "^" in the regular expression as this is automatically prepended.

## Line Start

The pattern for a continuation line of the block. This may not be a regular expression. Note that if
the pattern has a trailing space it will also match a line with a carriage return in place of this space.

## First Line of Block

This can have values *must not contain text before start* or *can contain text before start*.
Basically they represent whether the very first line of the block (either the block start line or the first continuation line if the block
has no block start) will be matched if there is anything other than whitespace on the line before it. So for instance with
*must not contain text before start* selected the block below will not be matched:

```
int i = 0; // This block will not be matched as "must not contain text before start"
           // was selected. If "can contain text before start" was selected then
           // the block would be matched.
```

Note that no matter what is slected here, all lines after the first can only contain whitespace before the block.
