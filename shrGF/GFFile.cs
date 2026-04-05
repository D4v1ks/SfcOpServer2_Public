using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.IO;
using System.Text;

namespace shrGF
{
    public sealed class GFFile
    {
        private const string SPACING = "\t ";
        private const string NUMBERS = "0123456789";
        private const string LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        //private static readonly SearchValues<char> _whiteValues = SearchValues.Create(SPACING);
        private static readonly SearchValues<char> _pathValues = SearchValues.Create(NUMBERS + LETTERS + "/_");
        private static readonly SearchValues<char> _keyValues = SearchValues.Create(NUMBERS + LETTERS + "#_");

        private static readonly char[] _whiteChars = SPACING.ToCharArray();
        private static readonly char[] _valueSeparators = ",".ToCharArray();

        private readonly Dictionary<string, GFEntry> _entries;
        private readonly Dictionary<string, GFEntry> _temp;

        private string _filename;
        private string _root;
        private int _fileType;
        private string _commentPrefix;

        public bool IsEmpty => _entries.Count + _temp.Count == 0;

        public string Filename
        {
            get
            {
                return _filename;
            }
            set
            {
                _filename = value;
            }
        }

        public string Root
        {
            get
            {
                return _root;
            }
            set
            {
                _root = value;
            }
        }

        public GFFile()
        {
            _entries = [];
            _temp = [];

            _filename = string.Empty;
            _root = string.Empty;

#if DEBUG
            Test();
            Clear();
#endif

        }

        public void Clear()
        {
            _entries.Clear();
            _temp.Clear();

            _filename = string.Empty;
            _root = string.Empty;

            _fileType = 0;
            _commentPrefix = null;
        }

#if DEBUG
        private void Test()
        {
            _filename = "Test.gf";
            _root = Path.GetFileNameWithoutExtension(_filename);

            Initialize();

            string original = @"
 
 // 
 // 1 
 [ 1 ] 
 1 = 1234 
 2 = 0x1234 // 
 3 = -1234 //  2 
 [ 2 ] // 
 1 = ""t"" 
 2 = ""t"" // 
 3 = ""t"" //   3 
 [ 3 ] // 1 
 1 = 0.1, 0.2 
 2 = -1, -2 // 
 3 = -1.0, -2.0 //    4 
 [ 3/0 ] 
 1 = ""a"", ""b"" 
 2 = ""c"", ""d"" // 
 3 = ""e"", ""f"" // 1 
";

            string expected = @"// 1

[1]
1=1234
2=0x1234
3=-1234 //  2

[2]
1=""t""
2=""t""
3=""t"" //   3

[3] // 1
1=0.1, 0.2
2=-1, -2
3=-1.0, -2.0 //    4

[3/0]
1=""a"", ""b""
2=""c"", ""d""
3=""e"", ""f"" // 1
";

            byte[] b = Encoding.ASCII.GetBytes(original);
            MemoryStream m = new(b);
            StreamReader r = new(m, Encoding.ASCII);

            Load(r);

            r.Dispose();
            m.Dispose();

            Array.Clear(b);

            m = new(b);

            StreamWriter w = new(m);

            Save(w, 0, 0);

            int c = (int)w.BaseStream.Position;

            w.Dispose();
            m.Dispose();

            string t = Encoding.ASCII.GetString(b, 0, c);

            if (!t.Equals(expected, StringComparison.Ordinal))
            {
                Debug.Write(t);

                Debugger.Break(); // !? 
            }
        }
#endif

        public bool Load(StreamReader r)
        {
            if (!IsEmpty)
                return false;

            Initialize();

            int line = 0;

            string comment;
            string path = string.Empty;
            string key;
            string value;
            bool quotes;

            string k;
            GFEntry e;

            int i;

            while (true)
            {
                string t = r.ReadLine();

                // checks the line

                if (t == null)
                    break;

                line++;

                t = t.Trim(_whiteChars);

                if (t.Length == 0)
                    continue;

                // checks if it is a comment

                if (_commentPrefix != null && t.StartsWith(_commentPrefix, StringComparison.Ordinal))
                {
                    // ;

                    if (t.Length == _commentPrefix.Length)
                        continue;

                    // ; comment

                    comment = t[_commentPrefix.Length..];

                    goto addComment;
                }

                // checks if it is a path

                if (t.StartsWith('['))
                {
                    if (t.EndsWith(']'))
                    {
                        // [ path ]

                        comment = string.Empty;
                        path = t[1..^1].Trim(_whiteChars);

                        goto tryAddPath;
                    }

                    if (_commentPrefix == null)
                        goto badFormat;

                    i = t.IndexOf(']', 2);

                    if (i < 2)
                        goto badFormat;

                    k = t[(i + 1)..].TrimStart(_whiteChars);

                    if (!k.StartsWith(_commentPrefix, StringComparison.Ordinal))
                        goto badFormat;

                    // [ path ] ; comment

                    comment = k[_commentPrefix.Length..];
                    path = t[1..i].Trim(_whiteChars);

                    goto tryAddPath;
                }

                // checks if we have a value

                i = t.IndexOf('=');

                if (i < 1)
                    goto badFormat;

                key = t[..i].TrimEnd(_whiteChars);

                if (key.AsSpan().ContainsAnyExcept(_keyValues))
                    goto badFormat;

                value = t[(i + 1)..].TrimStart(_whiteChars);

                // checks if we have a comment attached

                comment = string.Empty;

                if (_commentPrefix != null)
                {
                    i = value.IndexOf(_commentPrefix, StringComparison.Ordinal);

                    if (i >= 0)
                    {
                        comment = value[(i + _commentPrefix.Length)..];
                        value = value[..i].TrimEnd(_whiteChars);
                    }
                }

                if ((_fileType & 1) == 1)
                {
                    quotes = false;
                }
                else
                {
                    quotes = value.StartsWith('\"') && value.EndsWith('\"');

                    if (quotes)
                    {
                        // checks if we have a even number of quotes

                        int c = 1;

                        i = 1;

                        do
                        {
                            i = value.IndexOf('\"', i);

                            if (i == -1)
                                break;

                            c++;
                            i++;
                        }
                        while (i < value.Length);

                        if ((c & 1) != 0)
                            goto badFormat;

                        value = value[1..^1];
                    }
                    else
                    {
                        i = value.IndexOfAny(_valueSeparators);

                        if (i >= 0)
                        {
                            string[] arg = value.Split(_valueSeparators, StringSplitOptions.None);

                            for (i = 0; i < arg.Length; i++)
                            {
                                if (!IsNumber(arg[i], NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite))
                                    goto badFormat;
                            }
                        }
                        else if (!value.StartsWith('%') || !value.EndsWith('%'))
                        {
                            if (!IsNumber(value, NumberStyles.None))
                                goto badFormat;
                        }
                    }
                }

                goto tryAddValue;

            addComment:

                k = $"<{line}>";
                e = new GFEntry(comment);

                _entries.Add(k, e);

                continue;

            tryAddPath:

                if (path.Length == 0 || path.Contains("//", StringComparison.Ordinal) || path.AsSpan().ContainsAnyExcept(_pathValues))
                    goto badFormat;

                k = $"{_root}/{path}/".ToUpperInvariant();

                if (_entries.ContainsKey(k))
                    goto badFormat;

                e = new GFEntry(comment, path);

                _entries.Add(k, e);

                if ((_fileType & 5) != 0 && path.Equals("Objects", StringComparison.Ordinal))
                    break;

                continue;

            tryAddValue:

                if (_root.Length == 0)
                {
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase) || key.Equals("Version", StringComparison.OrdinalIgnoreCase))
                    {
                        _root = value;
                    }
                    else if (!key.Equals("ProfilesPath", StringComparison.OrdinalIgnoreCase))
                    {
                        goto badFormat;
                    }
                }

                k = $"{_root}/{path}/{key}".ToUpperInvariant();

                if (_entries.ContainsKey(k))
                    goto badFormat;

                _entries.Add(k, new GFEntry(comment, path, key, value, quotes));

                continue;

            badFormat:

                k = $"Entry not supported in file \"{_filename}\", line {line}\n{t}";

#if DEBUG
                Debug.Write(k);

                Debugger.Break(); // !?
#else
                throw new NotSupportedException(k);
#endif

            }

            return true;
        }

        public bool Load(string fileName)
        {
            if (File.Exists(fileName))
            {
                StreamReader r = null;

                try
                {
                    r = new StreamReader(fileName, Encoding.ASCII);

                    _filename = fileName;
                    _root = Path.GetFileNameWithoutExtension(_filename);

                    return Load(r);
                }
                catch (Exception)
                { }
                finally
                {
                    r?.Dispose();
                }
            }

            return false;
        }

        public bool Save(StreamWriter w, int leftPadding, int rightPadding)
        {
            if (w == null || _entries.Count == 0)
                return false;

            Initialize();

            int c = Math.Max(Math.Abs(leftPadding), Math.Abs(rightPadding));

            char[] padding;

            if (c > 0)
            {
                padding = new char[c];

                Array.Fill(padding, ' ');
            }
            else
            {
                padding = null;
            }

            GFFlags last = GFFlags.None;

            c = 0;

            foreach (KeyValuePair<string, GFEntry> p in _entries)
            {
                GFEntry entry = p.Value;
                GFFlags flags = entry.Flags;

                string t;

                if ((flags & GFFlags.IsValue) == GFFlags.IsValue)
                {
                    t = entry.Key;

                    w.Write(t);

                    if (leftPadding < 0)
                        w.Write(padding, 0, -leftPadding);
                    else if (leftPadding > t.Length)
                        w.Write(padding, 0, leftPadding - t.Length);

                    w.Write('=');

                    if (rightPadding < 0)
                        w.Write(padding, 0, -rightPadding);

                    t = entry.Value;

                    if (entry.Quotes)
                        w.Write('\"');

                    w.Write(t);

                    if (entry.Quotes)
                        w.Write('\"');

                    t = entry.Comment;

                    if (t.Length > 0)
                    {
                        w.Write(' ');
                        w.Write(_commentPrefix);
                        w.Write(t);
                    }

                    w.WriteLine();

                    last = GFFlags.IsValue;
                }
                else if ((flags & GFFlags.IsPath) == GFFlags.IsPath)
                {
                    if (c > 0)
                        w.WriteLine();

                    bool wroteSomething = false;

                    // tries to write the path

                    t = entry.Path;

                    if (t.Length > 0)
                    {
                        w.Write('[');
                        w.Write(t);
                        w.Write(']');

                        wroteSomething = true;
                    }

                    // tries to write the comment

                    t = entry.Comment;

                    if (t.Length > 0)
                    {
                        w.Write(' ');
                        w.Write(_commentPrefix);
                        w.Write(t);

                        wroteSomething = true;
                    }

                    // tries to finish the line

                    if (wroteSomething)
                        w.WriteLine();

                    last = GFFlags.IsPath;
                }
                else if ((flags & GFFlags.IsComment) == GFFlags.IsComment)
                {
                    if (c > 0 && last != GFFlags.IsComment)
                        w.WriteLine();

                    w.Write(_commentPrefix);
                    w.WriteLine(entry.Comment);

                    last = GFFlags.IsComment;
                }
                else
                {
                    Contract.Assert(flags == GFFlags.None);

                    w.WriteLine();

                    last = GFFlags.None;
                }

                c++;
            }

            w.Flush();

            return true;
        }

        public bool Save(string filename, int leftPadding, int rightPadding)
        {
            if (filename != null)
            {
                _filename = filename;

                StreamWriter w = null;

                try
                {
                    w = new StreamWriter(filename, false, Encoding.ASCII);

                    return Save(w, leftPadding, rightPadding);
                }
                catch (Exception)
                { }
                finally
                {
                    w?.Dispose();
                }
            }

            return false;
        }

        public bool Save(int leftPadding, int rightPadding)
        {
            return Save(_filename, leftPadding, rightPadding);
        }

        public bool Save()
        {
            return Save(_filename, 0, 0);
        }

        public void AddOrUpdate(string path, string key, string value, string comment, bool quotes)
        {
            string k1 = $"{_root}/{path}/".ToUpperInvariant();

            GFEntry entry;

            if (key.Length > 0)
            {
                string k2 = k1 + key.ToUpperInvariant();

                if (_entries.TryGetValue(k2, out entry))
                {
                    entry.Value = value;
                    entry.Comment = comment;

                    entry.Quotes = quotes;
                }
                else if (_entries.ContainsKey(k1))
                {
                    entry = new(comment, path, key, value, quotes);

                    using var e = _entries.GetEnumerator();

                    Contract.Assert(_temp.Count == 0);

                    while (e.MoveNext())
                    {
                        var c = e.Current;

                        _temp.Add(c.Key, c.Value);

                        if (c.Key.Equals(k1, StringComparison.Ordinal))
                            break;
                    }

                    while (true)
                    {
                        if (!e.MoveNext())
                            goto appendEntry;

                        var c = e.Current;

                        if (!c.Key.StartsWith(k1, StringComparison.Ordinal))
                        {
                            // inserts the entry

                            _temp.Add(k2, entry);
                            _temp.Add(c.Key, c.Value);

                            break;
                        }

                        _temp.Add(c.Key, c.Value);
                    }

                    while (e.MoveNext())
                    {
                        var c = e.Current;

                        _temp.Add(c.Key, c.Value);
                    }

                    _entries.Clear();

                    foreach (var p in _temp)
                        _entries.Add(p.Key, p.Value);

                    goto clearTemp;

                appendEntry:

                    _entries.Add(k2, entry);

                clearTemp:

                    _temp.Clear();
                }
                else
                {
                    GFEntry entry1 = new(string.Empty, path);
                    GFEntry entry2 = new(comment, path, key, value, quotes);

                    _entries.Add(k1, entry1);
                    _entries.Add(k2, entry2);
                }
            }
            else if (_entries.TryGetValue(k1, out entry))
            {
                entry.Comment = comment;
            }
            else
            {
                entry = new(comment, path);

                _entries.Add(k1, entry);
            }
        }

        public void AddOrUpdate(string path, string key, string value, string comment)
        {
            AddOrUpdate(path, key, value, comment, false);
        }

        public void AddOrUpdate(string path, string key, string value, bool quotes)
        {
            AddOrUpdate(path, key, value, string.Empty, quotes);
        }

        public void AddOrUpdate(string path, string key, string value)
        {
            AddOrUpdate(path, key, value, string.Empty, false);
        }

        public void AddOrUpdate(string path, string key, int value)
        {
            AddOrUpdate(path, key, value.ToString(CultureInfo.InvariantCulture), string.Empty, false);
        }

        public void AddOrUpdate(string path, string key, float value)
        {
            AddOrUpdate(path, key, GetString(value), string.Empty, false);
        }

        public void AddOrUpdate(string path, string key, int[] values)
        {
            StringBuilder s = new();

            s.Append(values[0]);

            for (int i = 1; i < values.Length; i++)
            {
                s.Append(", ");
                s.Append(values[i]);
            }

            AddOrUpdate(path, key, s.ToString(), string.Empty, false);
        }

        public void AddOrUpdate(string path, string key, float[] values)
        {
            StringBuilder s = new();

            s.Append(GetString(values[0]));

            for (int i = 1; i < values.Length; i++)
            {
                s.Append(", ");
                s.Append(GetString(values[i]));
            }

            AddOrUpdate(path, key, s.ToString(), string.Empty, false);
        }

        public void AddOrUpdate(string path, string comment)
        {
            AddOrUpdate(path, string.Empty, string.Empty, comment, false);
        }

        public bool ContainsPath(string path)
        {
            string k = $"{_root}/{path}/".ToUpperInvariant();

            return _entries.ContainsKey(k);
        }

        public bool ContainsKey(string path, string key)
        {
            string k = $"{_root}/{path}/{key}".ToUpperInvariant();

            return _entries.ContainsKey(k);
        }

        public bool TryGetValue(string path, string key, out string value, out bool quotes)
        {
            string k = $"{_root}/{path}/{key}".ToUpperInvariant();

            if (_entries.TryGetValue(k, out GFEntry entry))
            {
                value = entry.Value;
                quotes = entry.Quotes;

                return true;
            }

            value = string.Empty;
            quotes = false;

            return false;
        }

        public bool TryGetValue(string path, string key, out int value)
        {
            if (TryGetValue(path, key, out string v, out _) && int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return true;

            value = 0;

            return false;
        }

        public bool TryGetValue(string path, string key, out float value)
        {
            if (TryGetValue(path, key, out string v, out _) && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return true;

            value = 0f;

            return false;
        }

        public bool TryGetValue(string path, string key, out float[] values)
        {
            if (TryGetValue(path, key, out string v, out _))
            {
                const NumberStyles style = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

                ReadOnlySpan<char> span = [.. v];

                ReadOnlySpan<char> seg = span;
                ReadOnlySpan<char> sep = new(_valueSeparators);

                int c = 0;
                int i;

                while (true)
                {
                    i = seg.IndexOfAny(sep);

                    c++;

                    if (i == -1)
                        break;

                    seg = seg[(i + 1)..];
                }

                values = new float[c];

                seg = span;
                c = -1;

                while (true)
                {
                    i = seg.IndexOfAny(sep);

                    c++;

                    if (i == -1)
                    {
                        if (float.TryParse(seg, style, CultureInfo.InvariantCulture, out values[c]))
                            break;

                        goto invalidValue;
                    }

                    if (!float.TryParse(seg[..i], style, CultureInfo.InvariantCulture, out values[c]))
                        goto invalidValue;

                    seg = seg[(i + 1)..];
                }

                return true;
            }

        invalidValue:

            values = [];

            return false;
        }

        public string GetValue(string path, string key, string defaultValue)
        {
            bool quotesExpected = (_fileType & 1) == 0;

            if (TryGetValue(path, key, out string value, out bool quotes) && quotes == quotesExpected)
                return value;

            AddOrUpdate(path, key, defaultValue, quotesExpected);

            return defaultValue;
        }

        public int GetValue(string path, string key, int defaultValue)
        {
            if (TryGetValue(path, key, out int value))
                return value;

            AddOrUpdate(path, key, defaultValue);

            return defaultValue;
        }

        public float GetValue(string path, string key, float defaultValue)
        {
            if (TryGetValue(path, key, out float value))
                return value;

            AddOrUpdate(path, key, defaultValue);

            return defaultValue;
        }

        public bool TryRemoveKey(string path, string key)
        {
            string k = $"{_root}/{path}/{key}".ToUpperInvariant();

            return _entries.Remove(k);
        }

        public bool TryRemovePath(string path)
        {
            if (_entries.Count == 0)
                return false;

            string k = $"{_root}/{path}/".ToUpperInvariant();
            int c = 0;

            Contract.Assert(_temp.Count == 0);

            foreach (var p in _entries)
            {
                if (p.Key.StartsWith(k, StringComparison.Ordinal))
                    c++;
                else
                    _temp.Add(p.Key, p.Value);
            }

            if (c == 0)
            {
                _temp.Clear();

                return false;
            }

            _entries.Clear();

            foreach (var p in _temp)
                _entries.Add(p.Key, p.Value);

            _temp.Clear();

            return true;
        }

        private void Initialize()
        {
            if (_filename.EndsWith(".ini", StringComparison.OrdinalIgnoreCase) || _filename.EndsWith(".conf", StringComparison.OrdinalIgnoreCase))
            {
                _fileType = 1;
                _commentPrefix = ";";

                return;
            }

            if (_filename.EndsWith(".gf", StringComparison.OrdinalIgnoreCase))
            {
                _fileType = 2;
                _commentPrefix = "//";

                return;
            }

            if (_filename.EndsWith(".mvm", StringComparison.OrdinalIgnoreCase))
            {
                _fileType = 4;
                _commentPrefix = null;

                return;
            }

            throw new NotSupportedException();
        }

        private static bool IsNumber(string t, NumberStyles allowedWhite)
        {
            Contract.Assert((allowedWhite & ~(NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite)) == NumberStyles.None);

            if (t.StartsWith("0x", StringComparison.Ordinal))
                return uint.TryParse(t[2..], allowedWhite | NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out _);

            return
                (long.TryParse(t, allowedWhite | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long v) && v >= int.MinValue && v <= uint.MaxValue) ||
                float.TryParse(t, allowedWhite | NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out _);
        }

        private static string GetString(float value)
        {
            if (Math.Truncate(value) == value)
                return value.ToString("f1", CultureInfo.InvariantCulture);

            return value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
