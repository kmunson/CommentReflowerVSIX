# Bullet Point Settings

**Bullet Point Settings** allows you to set up bullet points to be detected in Comment Blocks and handled appropriately. For the
purposes of this program bullet points are regular expressions matched at the start of a comment block line (following
whitespace) that has two properties:

- preceeding lines will never be reflowed into it
- it can be reflowed into subsequent lines with a different indentation to the first character of the matched string

The first thing you will encounter upon selecting the **Bullet Point Settings** tab is a a list box containing
the **names** of all the currently defined Bullet Points. The **Name** has no special function programatically
and is just for user identification, however it must be unique. Left clicking on any entry in the list shows its
settings in the controls below. Left clicking again allows renaming of the block. To the top right of the listbox
is 4 buttons to manipulate the entries in it:

## New

Creates a new Bullet Point. It will need to have its settings set before use.

## Delete

Permanantly deletes the currently selected Bullet Point.

## Up/ Down

Moves the currently selected Bullet Point up and down in the seacrh order. Note that order **is** important. When a block of code is scanned for Bullet Points, it is scanned in the order of this list, and the first match found is the only one used to do further processing.

The settings for any Bullet Point are:

## Match String

This is the string to match at the start of the line of a comment for it to be recognised as a bullet point. It
is optionally a regular expression, set by ticking the **is RegEx** checkbox. Note that you should not use the
start of line symbol "^" in the regular expression as this is automatically prepended as well as a sting to match any
amount of whitespace before the bullet.

## Wrap on Edge

This can have values *left* and *right*. It indicates where subsequent lines of the bullet should
be wrapped to if the bullet goes over multiple line. *Left* is the leftside of the Matched String, *right*
is the right side. So say for instance the bullet point string is the non-regular expression "bullet - ", then the
following shows th left and right wrapping:

```
    //          bullet - A indented bullet point with wrap on left edge will 
    //          look like this
    //
    //              bullet - A indented bullet point with wrap on right edge 
    //                       will look like this.
```
