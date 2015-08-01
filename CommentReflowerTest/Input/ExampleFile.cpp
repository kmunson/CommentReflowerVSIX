/**
 * @file
 *
 *   Example file for Comment Reflower
 *
 * $Source $
 * $Id $
 * Copyright (C) 2004  Ian Nowland
 *
 * The purpose of Comment Reflower is to evenly wrap the 
 * wrappable text (as opposed to all text) in comments. As you will notice this
 * comment is not evenly wrapped, much the same way as most comments in code begin to look after 
 * a couple of edits. This hurts their readability, which
 * in turn hurts their maintainability. Comment Reflower attempts to fix this.
 *
 * With the cursor in this block select 
 * "Tools->Reflow Comment Containg Cursor" to make this block 
 * easier to read.
 *
 * The first thing to note about Comment Reflower is that it requires blank lines to 
 * separate paragraphs. Thus because of the blank lines above the previous paragraphs
 * will not be reflowed into each other. 
 *   The only time blank lines are not neccesary is when the new line is at a
 *   different indentation level to the previous. This is useful but having to 
 *   do so continually for plain English text would make it difficult to read.
 *
 * However, comment reflower has two more sophisticated features:-
 * - Bullet Points
 * - Break Reflow Strings
 * 
 * Bullet Points have two main implementation points:-
 * 1) Firstly when recognised on the next line they prevent the 
 *    currently processedline from being flowed into them
 * 2) Secondly they can set the indentation level to be the right 
 *    side of the bullet point, so that when text is reflowed it is done slightly 
 *    indented, in the way Word processors do.
 *
 * Bullet points are just regular expressions matched against the start
 * of the line, and thus don't just have to be normal bullet points.
 * For instance you could do:
 * @doxygentag the comment for a doxygen tag, which will be wrapped at the right edge of the tag
 * @doxygentag2 the comment for a doxygen tag, which will be 
 *              wrapped at the right edge of the tag             
 *
 * Break Reflow Strings are just regular expressions that if matched on a line cause
 * it never to be reflowed. So for instance:
 *
 * Underlines are matched not to reflow
 * ------------------------------------
 *
 * <xmlcomment>
 *   xml comments on a line by themselves are never reflower, which is useful for c# style documentation
 * </xmlcomment>
 *
 * The HTML BR is recognised and respected, meaning a line with it at the end like this.<BR>
 * Will never flow into the line immediately following it like this one.<BR>
 *
 * Also the xml pre block is recognised, so tables and things are never reflowed:
 * <pre>
 * -----------------
 * |       |       |
 * -----------------
 * </pre>
 *
 * The other main features of Comment Reflower is that all blocks, bullets and break flow strings are completely customisable,
 * so you're not trapped with my views as to what blocks should look like. It
 * also leaves you free to add support to any file types you may wish.
 *
 * Now to see on action on the othe blocks in the file you can either select them and
 * then go "Tools->Reflow Comment Containg Cursor", or you can select the whole file
 * and go "Tools->Reflow All Comments In Selected"
 */


/******************************************************************************
 * A C-style block comment, of the type some people use 
 * for functions and the like. Comment Reflowe ensures the start and end blocks
 * Never get put on the same line 
 ******************************************************************************/

///////////////////////////////////////////////////////////////////////////////
// The same type of thing except for being C++ style.
///////////////////////////////////////////////////////////////////////////////

/* Now just a normal C-block comment. This behaves differently to the big function
 * type above in that the start of block is always on the same line as 
 * actual comment. The end of line never is, unless the comment is 1 line,
 * as the next blocks demonstrate.
 */

/* Very short 1 line block with the ending on the same line */

/* Very short 1 line block which will
 * reflow to be on 1 line.
 */

/* Too long 1 line block which will be reflowed to be over multiple lines, thus respecting the wrap limit */


int j; /* Blocks are fine to be started ater code, so this will be reflowed correctly. */
int /* However if code follows, the block will not be reflowed, no matter how long it is */ i;

/*

Note the continuation  " *" at the start of line is very important, and if it is
lacking the block will not be matched to be reflowed. This is useful as it means you can use this idiom
to comment out blocks of code like the below and not worry about them being reflowed.

int i;
for (i=0; i < 10; i++)
{
	cout << i << endl;
}

*/

/**
 * Doxygen comments are much like normal C comments, except the start
 * of block is on a line by itself if the comment is one line.
 */


/**
 * The bullet point rules are very useful to match to doxygen tags,
 * to indent the text that concerns them.
 *
 * @tag Very long text following tag1 that will be wrapped correctly
 * \tag this type of tag is happily recognised too
 *      and will be reflowed correctly.
 */

/*! This type of doxygen block is recognised too, with the same behavious as the last */

// C style comments are recognised too, and are faithfully reflowed to have the correct wrap.
       // As long as they hav different indentation,
int i; // or as long at they have text before them, then they will not be flowed
       // into each other

       // Blank lines work as well, of course.

///
/// <summary>
///   C# is a fan of doing comments like this, which I personally 
///   don't like as it is a waste of whitespace.
/// </summary>
/// <param>
///   However because the break reflow strings recognise XML tags on a line
///   by themselves,
/// </param>
/// <param>
///   Then they are faithfully reflowed without screwing up the XML tag formatting.
/// </param>

//
// Enough on block types for now, so looking at bullets:
// 1) test - just a single line bullet
// 2) test - This line is far too long and so should wrap to the right hand side of the word test
// 3) test - this bullet is 
//           wrapped too short and so should
//           be merged
// Text at a normal indentation after a bullet. This
// should wrap normally.
// 1) A different type of bullet with the same type of start as the previous one. Very long so that it has to wrap.
// 2) Just a short
//    one that should be pulled up
//     - a hyphenated bullet pint. Again very long so that is has to wrap. Yes this is vey long.
//     - bullet point by itself.
//     - bullet point
//       to be wrapped
// @tag - Again very long so that is has to wrap. Yes this is very long. Yes this is very long. In fact so long that it has to wrap down two lines. In fact so long that it has to wrap down two lines.
// @tag Just checking that the other type
//      of tag works correctly. 
//      0 - now the single character followed
//          by hyphen bullet indented at the same level as last
//      1 - a following bullet
//