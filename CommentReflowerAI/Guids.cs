// Copyright (C) 2026  Kristofel Munson
//
// This program is free software; you can redistribute it and/or modify it under
// the terms of the GNU General Public License as published by the Free Software
// Foundation; either version 2 of the License, or (at your option) any later
// version.
//
// This program is distributed in the hope that it will be useful, but WITHOUT ANY
// WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A
// PARTICULAR PURPOSE. See the GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License along with
// this program; if not, write to the Free Software Foundation, Inc., 59 Temple
// Place - Suite 330, Boston, MA 02111-1307, USA.

using System;

namespace CommentReflower;

// MUST match the GuidSymbol values in CommentReflower.vsct
static class GuidList
{
	public const string guidCommentReflowerPkgString = "72013fe4-9734-4e6d-9903-7f3213491b37";
	public const string guidCommentReflowerCmdSetString = "b0b60bde-f731-40e3-8a6d-0750ad716ea0";
	public static readonly Guid guidCommentReflowerCmdSet = new(guidCommentReflowerCmdSetString);
};
