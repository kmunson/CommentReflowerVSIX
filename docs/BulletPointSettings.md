# Bullet Point Settings

**Bullet Point Settings** let you set up bullet points to be detected in comment
blocks and handled appropriately. Here, a bullet point is a regular expression
matched at the start of a comment block line (after any whitespace) that has two
properties:

- preceding lines are never reflowed into it
- it can be reflowed into following lines with a different indentation from the
  first character of the matched string

## The bullet point list

The **Bullets** page of *Tools > Options > Comment Reflower AI* shows a list of
all the currently defined bullet points, with a column for each setting. The
**Name** has no special function and is just for identification, but it must
be unique.

The buttons below the list are:

### Add

Creates a new bullet point. Its settings must be filled in before it can be
used.

### Edit

Changes the settings of the selected bullet point.

### Move up / Move down

Moves the selected bullet point up or down in the search order. **Order is
important.** Text is scanned for bullet points in the order of this list, and
only the first match found is used for further processing.

### Remove

Permanently deletes the selected bullet point.

## Bullet point settings

### Match String

The string to match at the start of a comment line for it to be recognised as a
bullet point. It can be a regular expression, set by ticking the **is RegEx**
checkbox. Don't use the start-of-line symbol `^` in the regular expression. It
is added automatically, along with a pattern that matches any amount of
whitespace before the bullet.

### Wrap on Edge

Either *left* or *right*. This sets where the following lines of a bullet are
wrapped to when the bullet runs over more than one line. *Left* is the left side
of the matched string and *right* is the right side.

For instance, if the bullet point string is the non-regular-expression
`bullet - ​`, left and right wrapping look like this:

```C++
    //          bullet - A indented bullet point with wrap on left edge will 
    //          look like this
    //
    //              bullet - A indented bullet point with wrap on right edge 
    //                       will look like this.
```
