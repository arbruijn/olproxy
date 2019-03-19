using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace minijson
{
    class MiniJson
    {
        public static object Parse(string data, ref int pos)
        {
            char c;
            int i = pos, l = data.Length;
            for (; ; )
            {
                if (i == l)
                    throw new Exception("Expected value at " + i);
                if ((c = data[i++]) != ' ' && c != '\n' && c != '\t' && c != '\r')
                    break;
            }
            switch (c)
            {
                case '{':
                    var obj = new Dictionary<string, object>();
                    while (i < l)
                        if ((c = data[i++]) != ' ' && c != '\n' && c != '\t' && c != '\r')
                            break;
                    if (c == '}')
                        return obj;
                    i--;
                    for (; ; )
                    {
                        if (!(Parse(data, ref i) is string key))
                            throw new Exception("Expected string at " + i);
                        while (i < l)
                            if ((c = data[i++]) != ' ' && c != '\n' && c != '\t' && c != '\r')
                                break;
                        if (c != ':')
                            throw new Exception("Expected ':' at " + i);
                        obj.Add(key, Parse(data, ref i));
                        while (i < l)
                            if ((c = data[i++]) != ' ' && c != '\n' && c != '\t' && c != '\r')
                                break;
                        if (c == '}')
                            break;
                        if (c != ',')
                            throw new Exception("Expected ',' at " + (i - 1));
                    }
                    pos = i;
                    return obj;
                case '[':
                    var list = new List<object>();
                    while (i < l)
                        if ((c = data[i++]) != ' ' && c != '\n' && c != '\t' && c != '\r')
                            break;
                    if (c == ']')
                        return list;
                    i--;
                    for (; ; )
                    {
                        list.Add(Parse(data, ref i));
                        while (i < l)
                            if ((c = data[i++]) != ' ' && c != '\n' && c != '\t' && c != '\r')
                                break;
                        if (c == ']')
                            break;
                        if (c != ',')
                            throw new Exception("Expected ',' at " + (i - 1));
                    }
                    pos = i;
                    return list;
                case '"':
                    var iq = data.IndexOf('"', i);
                    if (iq == -1)
                        throw new Exception("Unterminated string at " + i);
                    var ib = data.IndexOf('\\', i, iq - i);
                    if (ib == -1)
                    {
                        pos = iq + 1;
                        return data.Substring(i, iq - i);
                    }
                    string s = "";
                    for (; ; )
                    {
                        s += data.Substring(i, ib - i);
                        i = ib + 1;
                        if (i == iq)
                            if ((iq = data.IndexOf('"', i + 1)) == -1)
                                throw new Exception("Unterminated string at " + i);
                        c = data[i++];
                        if (c == 'u')
                        {
                            if (!ushort.TryParse(data.Substring(i, 4), NumberStyles.HexNumber, null, out ushort n))
                                throw new Exception("Unknown unicode string escape at " + i);
                            s += (char)n;
                            i += 4;
                        }
                        else
                        {
                            var esc = "\"\\/bnrt".IndexOf(c);
                            if (esc == -1)
                                throw new Exception("Unknown string escape " + c + " at " + (i - 1));
                            s += "\"\\/\b\n\r\t"[esc];
                        }
                        if ((ib = data.IndexOf('\\', i, iq - i)) == -1)
                            break;
                    }
                    s += data.Substring(i, iq - i);
                    pos = iq + 1;
                    return s;
                default:
                    if (c == '-' || c >= '0' && c <= '9')
                    {
                        int j = i - 1;
                        while (i++ < l && (c = data[i - 1]) >= '0' && c <= '9')
                            ;
                        if (c == '.' || c == 'e' || c == 'E')
                        {
                            if (c == '.')
                            {
                                if (i == l)
                                    throw new Exception("Expected digit");
                                while (i++ < l && (c = data[i - 1]) >= '0' && c <= '9')
                                    ;
                            }
                            if (c == 'e' || c == 'E')
                            {
                                if (i == l)
                                    throw new Exception("Expected digit");
                                if ((c = data[i]) == '+' || c == '-')
                                {
                                    i++;
                                    if (i == l)
                                        throw new Exception("Expected digit");
                                }
                                while (i++ < l && (c = data[i - 1]) >= '0' && c <= '9')
                                    ;
                            }
                        }
                        else if (int.TryParse(data.Substring(j, i - j - 1), out int n))
                        {
                            i--;
                            pos = i;
                            return n;
                        }
                        i--;
                        pos = i;
                        return double.Parse(data.Substring(j, i - j), CultureInfo.InvariantCulture);
                    }
                    if (c == 't' && i + 3 <= l && data[i] == 'r' && data[i + 1] == 'u' && data[i + 2] == 'e')
                    {
                        pos = i + 3;
                        return true;
                    }
                    if (c == 'f' && i + 4 <= l && data[i] == 'a' && data[i + 1] == 'l' && data[i + 2] == 's' && data[i + 3] == 'e')
                    {
                        pos = i + 4;
                        return false;
                    }
                    if (c == 'n' && i + 3 <= l && data[i] == 'u' && data[i + 1] == 'l' && data[i + 2] == 'l')
                    {
                        pos = i + 3;
                        return null;
                    }
                    throw new Exception("Expected value at " + (i - 1));
            }
        }
        public static object Parse(string data)
        {
            int pos = 0;
            return Parse(data, ref pos);
        }
        public static string ToString(object val)
        {
            if (val is string s)
                return '"' + s + '"';
            if (val is Dictionary<string, object> d)
                return "{" + String.Join(",", d.Keys.Select(x => ToString(x) + ":" + ToString(d[x]))) + "}";
            if (val is List<object> l)
                return "[" + String.Join(",", l.Select(x => ToString(x))) + "]";
            return val == null ? "null" : val is Boolean b ? b ? "true" : "false" : val.ToString();
        }
    }
/*
public class MiniJsonTest
{
    public static void Run()
    {
        foreach (var test in new [] {
            "\"test\"",
            "\"test\\\"test\"",
            "\"test\\\\\"",
            "\"test\\\\\\\"test\"",
            "\"[\\/\\b\\n\\r\\t\\u20ac]\"",
            "123",
            "1.1",
            "1.1e1",
            "1.1e+1",
            "1.1e-1",
            "1e1",
            "1E1",
            "[]",
            "[1]",
            "[1,2]",
            "[1,2,1.0,1e1]",
            "[true,false,null]",
            "{}",
            "{\"a\":\"b\"}",
            "{\"a\":\"b\",\"c\":\"d\"}",
            "{\"a\":1}",
            "{\"a\":1.1}",
            "{\"a\":1.0e1}",
            "{\"a\":1,\"b\":1.1,\"c\":1.0e1}",
            "{\"a\":1,\"b\":-1}"})
            Debug.WriteLine(test + " -> " + MiniJson.Parse(test) + " -> " + MiniJson.ToString(MiniJson.Parse(test)));
    }
}
*/
}
