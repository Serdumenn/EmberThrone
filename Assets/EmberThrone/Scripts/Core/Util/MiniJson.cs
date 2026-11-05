using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EmberThrone.Core
{
    /// <summary>
    /// Small dependency-free JSON reader/writer used by Core (levels, configs).
    /// Objects map to Dictionary&lt;string, object&gt;, arrays to List&lt;object&gt;,
    /// integers to long, other numbers to double.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var parser = new Parser(json);
            object value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) throw parser.Error("Unexpected trailing characters");
            return value;
        }

        public static string Serialize(object value, bool pretty = false)
        {
            var sb = new StringBuilder();
            Write(sb, value, pretty, 0);
            return sb.ToString();
        }

        // ---------------------------------------------------------------- reading helpers

        public static Dictionary<string, object> AsObject(object value, string context)
        {
            if (value is Dictionary<string, object> d) return d;
            throw new FormatException(context + ": expected a JSON object");
        }

        public static List<object> AsArray(object value, string context)
        {
            if (value is List<object> l) return l;
            throw new FormatException(context + ": expected a JSON array");
        }

        public static bool TryGet(Dictionary<string, object> obj, string key, out object value)
        {
            return obj.TryGetValue(key, out value) && value != null;
        }

        public static int GetInt(Dictionary<string, object> obj, string key, int fallback)
        {
            if (!TryGet(obj, key, out var v)) return fallback;
            double value;
            if (v is long l) value = l;
            else if (v is double d && Math.Abs(d - Math.Round(d)) < 1e-9) value = Math.Round(d);
            else throw new FormatException("'" + key + "' must be an integer");
            if (value < int.MinValue || value > int.MaxValue) throw new FormatException("'" + key + "' is out of range");
            return (int)value;
        }

        public static double GetDouble(Dictionary<string, object> obj, string key, double fallback)
        {
            if (!TryGet(obj, key, out var v)) return fallback;
            if (v is long l) return l;
            if (v is double d) return d;
            throw new FormatException("'" + key + "' must be a number");
        }

        public static string GetString(Dictionary<string, object> obj, string key, string fallback)
        {
            if (!TryGet(obj, key, out var v)) return fallback;
            if (v is string s) return s;
            throw new FormatException("'" + key + "' must be a string");
        }

        public static bool GetBool(Dictionary<string, object> obj, string key, bool fallback)
        {
            if (!TryGet(obj, key, out var v)) return fallback;
            if (v is bool b) return b;
            throw new FormatException("'" + key + "' must be true/false");
        }

        public static string[] GetStringArray(Dictionary<string, object> obj, string key)
        {
            if (!TryGet(obj, key, out var v)) return null;
            var list = AsArray(v, key);
            var result = new string[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                if (!(list[i] is string s)) throw new FormatException("'" + key + "' must contain only strings");
                result[i] = s;
            }
            return result;
        }

        // ---------------------------------------------------------------- parser

        sealed class Parser
        {
            readonly string _s;
            int _i;

            public Parser(string s) { _s = s; }

            public bool AtEnd => _i >= _s.Length;

            public FormatException Error(string message)
            {
                return new FormatException("JSON: " + message + " at position " + _i);
            }

            public void SkipWhitespace()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (AtEnd) throw Error("Unexpected end of input");
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
                        throw Error("Unexpected character '" + c + "'");
                }
            }

            void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw Error("Expected '" + word + "'");
                _i += word.Length;
            }

            Dictionary<string, object> ParseObject()
            {
                var result = new Dictionary<string, object>();
                _i++; // {
                SkipWhitespace();
                if (!AtEnd && _s[_i] == '}') { _i++; return result; }
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != '"') throw Error("Expected property name");
                    string key = ParseString();
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != ':') throw Error("Expected ':'");
                    _i++;
                    result[key] = ParseValue();
                    SkipWhitespace();
                    if (AtEnd) throw Error("Unterminated object");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; return result; }
                    throw Error("Expected ',' or '}'");
                }
            }

            List<object> ParseArray()
            {
                var result = new List<object>();
                _i++; // [
                SkipWhitespace();
                if (!AtEnd && _s[_i] == ']') { _i++; return result; }
                while (true)
                {
                    result.Add(ParseValue());
                    SkipWhitespace();
                    if (AtEnd) throw Error("Unterminated array");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; return result; }
                    throw Error("Expected ',' or ']'");
                }
            }

            string ParseString()
            {
                var sb = new StringBuilder();
                _i++; // opening quote
                while (true)
                {
                    if (AtEnd) throw Error("Unterminated string");
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw Error("Unterminated escape");
                    char e = _s[_i++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_i + 4 > _s.Length) throw Error("Bad unicode escape");
                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4;
                            break;
                        default: throw Error("Bad escape '\\" + e + "'");
                    }
                }
            }

            object ParseNumber()
            {
                int start = _i;
                bool isInteger = true;
                if (_s[_i] == '-') _i++;
                while (_i < _s.Length)
                {
                    char c = _s[_i];
                    if (c >= '0' && c <= '9') { _i++; continue; }
                    if (c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') { isInteger = false; _i++; continue; }
                    break;
                }
                string token = _s.Substring(start, _i - start);
                if (isInteger && long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long l)) return l;
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                throw Error("Bad number '" + token + "'");
            }
        }

        // ---------------------------------------------------------------- writer

        static void Write(StringBuilder sb, object value, bool pretty, int indent)
        {
            switch (value)
            {
                case null: sb.Append("null"); return;
                case string s: WriteString(sb, s); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case float f: sb.Append(((double)f).ToString("R", CultureInfo.InvariantCulture)); return;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); return;
                case IDictionary dict: WriteObject(sb, dict, pretty, indent); return;
                case IList list: WriteArray(sb, list, pretty, indent); return;
                default: WriteString(sb, Convert.ToString(value, CultureInfo.InvariantCulture)); return;
            }
        }

        static void WriteObject(StringBuilder sb, IDictionary dict, bool pretty, int indent)
        {
            if (dict.Count == 0) { sb.Append("{}"); return; }
            sb.Append('{');
            bool first = true;
            foreach (DictionaryEntry entry in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                if (pretty) NewLine(sb, indent + 1);
                WriteString(sb, Convert.ToString(entry.Key, CultureInfo.InvariantCulture));
                sb.Append(pretty ? ": " : ":");
                Write(sb, entry.Value, pretty, indent + 1);
            }
            if (pretty) NewLine(sb, indent);
            sb.Append('}');
        }

        static void WriteArray(StringBuilder sb, IList list, bool pretty, int indent)
        {
            if (list.Count == 0) { sb.Append("[]"); return; }
            // Arrays of scalars stay on one line unless they are strings (board rows read better one per line).
            bool multiline = pretty && ContainsContainersOrStrings(list);
            sb.Append('[');
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(multiline ? "," : (pretty ? ", " : ","));
                if (multiline) NewLine(sb, indent + 1);
                Write(sb, list[i], pretty, indent + 1);
            }
            if (multiline) NewLine(sb, indent);
            sb.Append(']');
        }

        static bool ContainsContainersOrStrings(IList list)
        {
            foreach (var item in list)
                if (item is IDictionary || item is IList || item is string) return true;
            return false;
        }

        static void NewLine(StringBuilder sb, int indent)
        {
            sb.Append('\n');
            sb.Append(' ', indent * 2);
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }
    }
}
