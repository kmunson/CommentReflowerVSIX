// First test an underline
// -----------------------
// The above text should be underlined. The above text should be underlined --
// The above text should be underlined.
//
// Now test an xml comment.
// <xmlcomment>
// That should not have wrapped. But this should. That should not have wrapped.
// But this should.
// </xmlcomment>
//
// Now test a doublespace line.
// a  ==  b
// Now test a rcs id tag line.
// $Id xxx $
// Now test a rcs source tag line.
// $Source  xxx $
//

/**
 * Text
 * <alwayslinebreakonlastline>
 */

/**
 * <alwayslinebreakonfirstline>
 * Text
 */

/**
 * <alwayslinebreakononlyline>
 */

/** <alwayslinebreakonfirstline> */

// This block should wrap
// <pre>
// But this should not as
// it is in a preformatted block
// </pre>
// But again this should wrap okay.

/**
 * Thisisareallylongwordthatistoolongtoeverbeanythingotherthanonalinebyitself..a
 */

/**
 * Hello
 * Thisisareallylongwordthatistoolongtoeverbeanythingotherthanonalinebyitself..a
 */

/**
 * Hello
 * Thisisareallylongwordthatistoolongtoeverbeanythingotherthanonalinebyitself..a
 */

// 																			This block is very indented
// 																			and so should wrap at 30 chars
// 																			after the start.

/**
 * The next line just contains a trimmed line sdtart but that should be fine
 * this block should be rewrappend anyway.
 *
 * The next line just contains a trimmed line sdtart but that should be fine
 * this block should be rewrappend anyway.
 *
 */

/**
 * Now a test that the HTML BR tag works correctly. This line should wrap okay
 * but it should not wrap into the next.<BR>
 * The line that should not be wrapped into.
 */

/// Doxygen block with spaces with blank line with no line start trailer.
///
/// This block should still wrap okay.

///	Doxygen block with tabs with blank line with no line start trailer.
///
///		This block should still wrap okay.
