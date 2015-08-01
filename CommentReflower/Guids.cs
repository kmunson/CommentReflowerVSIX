// Guids.cs
// MUST match guids.h
using System;

namespace CommentReflower
{
    static class GuidList
    {
        public const string guidCommentReflowerPkgString = "df158482-14ea-4165-8977-6a57ececffe7";
        public const string guidCommentReflowerCmdSetString = "edcfdb5f-228b-42d4-9609-1b2b7fa21e50";
        public static readonly Guid guidCommentReflowerCmdSet = new Guid(guidCommentReflowerCmdSetString);
    };
}