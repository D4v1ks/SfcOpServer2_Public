using System;
using System.Diagnostics.Contracts;

namespace shrGF
{
    public sealed class GFEntry
    {
        private string _comment;
        private string _path;
        private string _key;
        private string _value;
        private bool _quotes;

        public string Comment
        {
            get
            {
                return _comment;
            }
            set
            {
                _comment = value;
            }
        }
        public string Path
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
            }
        }
        public string Key
        {
            get
            {
                return _key;
            }
            set
            {
                _key = value;
            }
        }
        public string Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
        }
        public bool Quotes
        {
            get
            {
                return _quotes;
            }
            set
            {
                _quotes = value;
            }
        }

        public GFFlags Flags
        {
            get
            {
                GFFlags flags = GFFlags.None;

                if (_comment != null) flags |= GFFlags.Comment;
                if (_path != null) flags |= GFFlags.Path;
                if (_key != null) flags |= GFFlags.Key;
                if (_value != null) flags |= GFFlags.Value;
                if (_quotes) flags |= GFFlags.Quotes;

                return flags;
            }
        }

        public GFEntry(GFEntry entry)
        {
            _comment = entry.Comment;
            _path = entry.Path;
            _key = entry.Key;
            _value = entry.Value;
            _quotes = entry.Quotes;

            Contract.Assert(Enum.IsDefined(Flags));
        }

        public GFEntry(string comment)
        {
            _comment = comment;

            Contract.Assert(Enum.IsDefined(Flags));
        }

        public GFEntry(string comment, string path)
        {
            _comment = comment;
            _path = path;

            Contract.Assert(Enum.IsDefined(Flags));
        }

        public GFEntry(string comment, string path, string key, string value, bool quotes)
        {
            _comment = comment;
            _path = path;
            _key = key;
            _value = value;
            _quotes = quotes;

            Contract.Assert(Enum.IsDefined(Flags));
        }
    }
}
