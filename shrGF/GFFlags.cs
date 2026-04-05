using System;

namespace shrGF
{
    [Flags]
    public enum GFFlags
    {
        None,

        Comment = 1 << 0,
        Path = 1 << 1,
        Key = 1 << 2,
        Value = 1 << 3,
        Quotes = 1 << 4,

        IsComment = Comment,
        IsPath = Comment | Path,
        IsValue = Comment | Path | Key | Value,
        IsValueQuoted = Comment | Path | Key | Value | Quotes
    }
}
