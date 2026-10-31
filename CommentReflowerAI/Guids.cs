// Guids.cs
// MUST match the GuidSymbol values in CommentReflower.vsct
using System;

namespace CommentReflower
{
    static class GuidList
    {
        public const string guidCommentReflowerPkgString = "72013fe4-9734-4e6d-9903-7f3213491b37";
        public const string guidCommentReflowerCmdSetString = "b0b60bde-f731-40e3-8a6d-0750ad716ea0";
        public static readonly Guid guidCommentReflowerCmdSet = new Guid(guidCommentReflowerCmdSetString);
    };
}