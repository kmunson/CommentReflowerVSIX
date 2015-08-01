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
//  - a hypenated bullet pint. Again very long so that is has to wrap. Yes this is vey long.
//  - bullet point by itself.
//  - bullet point
//    to be wrapped
// @tag - Again very long so that is has to wrap. Yes this is very long. Yes this is very long. In fact so long that it has to wrap down two lines. In fact so long that it has to wrap down two lines.
// @tag Just checking that the other type
//      of tag works correctly. 
//      0 - now the single character followed
//          by hyphen test indented at the same level as last

/**
 *  - Bullet on a single line that should not be pulled up
 */

/**
 * Now we are checking that different indenations work.
 *   This should not be joined with the previous line and should wrap with the correct indentation.
 *   This should wrap happily with the previous line.
 *     A different 
 *     indentation 
 *     again.
 * And back a the first indentation.
 */

/// Doxygen style.
/// with spaces.
///     1) test - hello
///               there

///		Doxygen style
///		with tabs.
///		1) test - hello
///				  there

/*!
 * Doxygen comment allows blank comments to be skipped on 
 * first line and for the block still to be recognised and processed.
 */

/*! Also allows for stuff on first line, 
 *  which should be wrapped correctly, as long as the indents match.
 */

/*
 * Same with C-style 
 * comment 
 */
