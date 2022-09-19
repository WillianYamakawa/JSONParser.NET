public class JSON
    {
        public static readonly string TrashChars = " \n\r\t";

        public static Query Parse(ref string str)
        {
            string textToParse = SanitizeStringToParse(ref str);
            List<IndexPair> newQuotesList = GetQuotesPairIndexList(ref textToParse);

            int endIndex = 0;
            return new Query(DecodeObject(ref textToParse, 0, ref endIndex, newQuotesList));
        }

        public static bool TryParse(ref string str,out Query query){
            try{
                query = Parse(ref str);
                return true;
            }catch{
                query = null;
                return false;
            }
        }

        public static Query ParseAsArray(ref string str)
        {
            string textToParse = SanitizeStringToParse(ref str);
            List<IndexPair> newQuotesList = GetQuotesPairIndexList(ref textToParse);

            int endIndex = 0;
            return new Query(DecodeArray(ref textToParse, 0, ref endIndex, newQuotesList));
        }

        public static bool TryParseAsArray(ref string str, out Query query)
        {
            try
            {
                query = ParseAsArray(ref str);
                return true;
            }
            catch
            {
                query = null;
                return false;
            }
        }

        public static string SanitizeStringToParse(ref string str)
        {
            StringBuilder sb = new StringBuilder(str.Length);
            List<IndexPair> quotesList = GetQuotesPairIndexList(ref str);

            for (int i = 0; i < str.Length; i++)
            {
                if (!IsWithinQuotes(i, quotesList) && TrashChars.IndexOf(str[i]) != -1)
                {
                    //NOTHING
                }
                else
                {
                    sb.Append(str[i]);
                }
            }

            return sb.ToString();
        }

        public static bool IsWithinQuotes(int index, List<IndexPair> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].isWithin(index))
                {
                    return true;
                }
            }
            return false;
        }

        public static string JSONPrepareToString(string str)
        {
            StringBuilder sb = new StringBuilder(str);
            sb= sb.Replace(@"\b", "\b");
            sb.Replace(@"\f", "\f");
            sb.Replace(@"\n", "\n");
            sb.Replace(@"\r", "\r");
            sb.Replace(@"\t", "\t");
            sb.Replace("\\\"", "\"");
            sb.Replace(@"\\", "\\");
            return sb.ToString();
        }

        public static string StringPrepareToJSON(string str)
        {
            StringBuilder sb = new StringBuilder(str);
            sb.Replace("\b", @"\b");
            sb.Replace("\f", @"\f");
            sb.Replace("\n", @"\n");
            sb.Replace("\r", @"\r");
            sb.Replace("\t", @"\t");
            sb.Replace("\"", "\\\"");
            sb.Replace("\\", @"\\");
            return sb.ToString();
        }

        public static int GetStringEndWithinQuotes(ref string str, int startIndex)
        {
            int endIndex = -1;
            if (str[startIndex] != '"') throw new Exception("String didnot start with \"");
            startIndex++;
            for (int i = startIndex; i < str.Length; i++)
            {
                char ch = str[i];
                if (ch == '\"')
                {
                    if (str[i - 1] != '\\')
                    {
                        endIndex = i;
                        break;
                    }
                }
            }
            if (endIndex == -1) throw new Exception("String didnot end with \"");
            return endIndex - 1;
        }

        public static bool IsDigit(char n)
        {
            byte bc = (byte)n;
            if (bc >= 0x30 && bc <= 0x39)
            {
                return true;
            }
            return false;
        }

        public static StringValue DecodeString(ref string str, int startIndex, ref int endIndex)
        {
            endIndex = GetStringEndWithinQuotes(ref str, startIndex) + 1;
            return JSONPrepareToString(str.Substring(startIndex + 1, endIndex - 1 - startIndex));
        }

        public static NumberValue<float> DecodeNumber(ref string str, int startIndex, ref int endIndex)
        {
            endIndex = -1;
            for (int i = startIndex; i < str.Length; i++)
            {
                char ch = str[i];
                if (!IsDigit(ch) && ch != '.')
                {
                    endIndex = i;
                    break;
                }
            }
            if (endIndex == -1 || endIndex == startIndex) throw new Exception("Invalid Number");
            string numberStr = str.Substring(startIndex, endIndex - startIndex);
            endIndex--;
            return float.Parse(numberStr, CultureInfo.InvariantCulture);
        }

        public static BooleanValue DecodeBoolean(ref string str, int startIndex, ref int endIndex)
        {
            if (str.StartsWith("true"))
            {
                endIndex = startIndex + 3;
                return true;
            }
            else if(str.StartsWith("false"))
            {
                endIndex = startIndex + 4;
                return false;
            }
            throw new Exception("Invalid boolean string");
        }

        public static ArrayValue DecodeArray(ref string str, int startIndex, ref int endIndex, List<IndexPair> listOfQuotesPairIndex)
        {
            if (str[startIndex] != '[') throw new Exception("Array didnot start with [");
            ArrayValue ob = new ArrayValue();
            if (str[startIndex + 1] == ']')
            {
                endIndex = startIndex + 1;
                return ob;
            }
            endIndex = GetIndexOfRightCharOfPairOutsideQuotes('[', ']', ref str, startIndex, listOfQuotesPairIndex);
            if (endIndex == -1) throw new Exception("Missing ]");
            int lastSeenIndex = startIndex;
            while (true)
            {
                int valueStartIndex = lastSeenIndex + 1;
                char nextChar = str[valueStartIndex];

                IValue value;

                int valueEndIndex = 0;
                switch (nextChar)
                {
                    case '\"':
                        value = DecodeString(ref str, valueStartIndex, ref valueEndIndex);
                        ob.Add(value);
                        break;

                    case '{':
                        value = DecodeObject(ref str, valueStartIndex, ref valueEndIndex, listOfQuotesPairIndex);
                        ob.Add(value);
                        break;

                    case '[':
                        value = DecodeArray(ref str, valueStartIndex, ref valueEndIndex, listOfQuotesPairIndex);
                        ob.Add(value);
                        break;

                    case 't':
                        value = DecodeBoolean(ref str, valueStartIndex, ref valueEndIndex);
                        ob.Add(value);
                        break;

                    case 'f':
                        value = DecodeBoolean(ref str, valueStartIndex, ref valueEndIndex);
                        ob.Add(value);
                        break;

                    case 'n':
                        if (str[valueStartIndex + 1] == 'u' && str[valueStartIndex + 2] == 'l' && str[valueStartIndex + 3] == 'l')
                        {
                            ob.Add(null);
                            valueEndIndex = valueStartIndex + 3;
                        }
                        else
                        {
                            throw new Exception("Invalid value");
                        }
                        break;

                    default:
                        value = new NumberValue<float>(DecodeNumber(ref str, valueStartIndex, ref valueEndIndex));
                        ob.Add(value);
                        break;
                }

                int nextCharIndex = valueEndIndex + 1;
                char nextCharAfterValue = str[nextCharIndex];

                if (nextCharAfterValue != ',')
                {
                    if (nextCharAfterValue == ']')
                    {
                        endIndex = nextCharIndex;
                        return ob;
                    }
                    else
                    {
                        throw new Exception("Invalid end of value");
                    }
                }
                else
                {
                    lastSeenIndex = valueEndIndex + 1;
                }

            }
        }

        public static ObjectValue DecodeObject(ref string str, int startIndex, ref int endIndex, List<IndexPair> listOfQuotesPairIndex)
        {
            if (str[startIndex] != '{') throw new Exception("Object didnot start with {");
            ObjectValue ob = new ObjectValue();
            if (str[startIndex + 1] == '}')
            {
                endIndex = startIndex + 1;
                return ob;
            }
            if (endIndex == -1) throw new Exception("Missing }");
            int lastSeenIndex = startIndex;
            while (true)
            {
                int delimiterIndex = GetIndexOfCharOutsideQuotes(':', ref str, lastSeenIndex + 1, listOfQuotesPairIndex);
                int lastKeyIndex = 0;
                string key = DecodeString(ref str, lastSeenIndex + 1, ref lastKeyIndex);

                int valueStartIndex = delimiterIndex + 1;
                char nextChar = str[valueStartIndex];

                IValue value;

                int valueEndIndex = 0;
                switch (nextChar)
                {
                    case '\"':
                        value = DecodeString(ref str, valueStartIndex, ref valueEndIndex);
                        ob.Add(key, value);
                        break;

                    case '{':
                        value = DecodeObject(ref str, valueStartIndex, ref valueEndIndex, listOfQuotesPairIndex);
                        ob.Add(key, value);
                        break;

                    case '[':
                        value = DecodeArray(ref str, valueStartIndex, ref valueEndIndex, listOfQuotesPairIndex);
                        ob.Add(key, value);
                        break;

                    case 't':
                        value = DecodeBoolean(ref str, valueStartIndex, ref valueEndIndex);
                        ob.Add(key, value);
                        break;

                    case 'f':
                        value = DecodeBoolean(ref str, valueStartIndex, ref valueEndIndex);
                        ob.Add(key, value);
                        break;

                    case 'n':
                        if (str[valueStartIndex + 1] == 'u' && str[valueStartIndex + 2] == 'l' && str[valueStartIndex + 3] == 'l')
                        {
                            ob.Add(key, null);
                            valueEndIndex = valueStartIndex + 3;
                        }
                        else
                        {
                            throw new Exception("Invalid value");
                        }
                        break;

                    default:
                        value = new NumberValue<float>(DecodeNumber(ref str, valueStartIndex, ref valueEndIndex));
                        ob.Add(key, value);
                        break;
                }

                int nextCharIndex = valueEndIndex + 1;
                char nextCharAfterValue = str[nextCharIndex];

                if (nextCharAfterValue != ',')
                {
                    if (nextCharAfterValue == '}')
                    {
                        endIndex = nextCharIndex;
                        return ob;
                    }
                    else
                    {
                        throw new Exception("Invalid end of value");
                    }
                }
                else
                {
                    lastSeenIndex = valueEndIndex + 1;
                }

            }
        }

        public static int GetIndexOfCharOutsideQuotes(char ch, ref string str, int startIndex, List<IndexPair> list)
        {
            int lastSeenIndex = startIndex;
            if (str[startIndex] == ch) return startIndex;
            while (true)
            {
                int index = str.IndexOf(ch, lastSeenIndex + 1);
                if (index == -1) break;
                bool hasFoundInsideQuotes = IsWithinQuotes(index, list);
                
                if (!hasFoundInsideQuotes)
                {
                    return index;
                }
                else
                {
                    lastSeenIndex = index;
                }
            }
            return -1;
        }

        public static int GetIndexOfRightCharOfPairOutsideQuotes(char charLeft, char charRight, ref string str, int startIndex, List<IndexPair> list)
        {
            int scopeLevel = 0;
            int lastSeenIndex = startIndex;
            while(true)
            {
                int charLeftIndex = GetIndexOfCharOutsideQuotes(charLeft, ref str, lastSeenIndex + 1, list);
                int charRightIndex = GetIndexOfCharOutsideQuotes(charRight, ref str, lastSeenIndex + 1, list);
                if (charRightIndex == -1) return -1;
                if (charRightIndex < charLeftIndex || charLeftIndex == -1)
                {
                    if (scopeLevel == 0)
                    {
                        return charRightIndex;
                    }
                    else
                    {
                        scopeLevel--;
                        lastSeenIndex = charRightIndex;
                    }

                }
                else
                {
                    scopeLevel++;
                    lastSeenIndex = charLeftIndex;
                }
            }
        }

        public static List<IndexPair> GetQuotesPairIndexList(ref string str)
        {
            List<IndexPair> listOfQuotes = new List<IndexPair>();
            int lastSeenIndex = -1;
            while (true)
            {
                int leftQuoteIndex = str.IndexOf("\"", lastSeenIndex + 1);
                if (leftQuoteIndex == -1) break;
                int rightQuoteIndex = GetStringEndWithinQuotes(ref str, leftQuoteIndex) + 1;
                listOfQuotes.Add(new IndexPair(leftQuoteIndex, rightQuoteIndex));
                lastSeenIndex = rightQuoteIndex;
            }
            return listOfQuotes;
        }

        public static ArrayValue DecodeArray(string str)
        {
            return null;
        }

        public struct IndexPair
        {
            public int start;
            public int end;

            public IndexPair(int start, int end){
                this.start = start;
                this.end = end;
            }

            public bool isWithin(int n)
            {
                return n >= this.start && n <= this.end;
            }

            public override string ToString()
            {
                return start.ToString() + " " + end.ToString();
            }
        }

        public interface IValue
        {
            string ToJSON();
            IValue this[string str] { get; }
            IValue this[int idx] { get; }
        }

        public class StringValue : IValue
        {
            private string _innerString { get; set; }

            public StringValue(string str)
            {
                _innerString = str;
            }

            public string ToJSON()
            {
                if (this._innerString == null) return "null";
                return "\"" + StringPrepareToJSON(_innerString) + "\"";
            }

            public static implicit operator string(StringValue sv)
            {
                return sv._innerString ?? "";
            }

            public static implicit operator StringValue(string str)
            {
                return new StringValue(str);
            }

            public override string ToString()
            {
                return this._innerString;
            }

            public IValue this[string str]
            {
                get
                {
                    return null;
                }

            }

            public IValue this[int idx]
            {
                get
                {
                    return null;
                }
            }
        }

        public class NumberValue<T> : IValue
        {
            private T _innerNumber { get; set; }

            public NumberValue(T number)
            {
                _innerNumber = number;
            }

            public string ToJSON()
            {
                return _innerNumber.ToString().Replace(",", ".");
            }

            public static implicit operator T(NumberValue<T> nv)
            {
                return nv._innerNumber;
            }

            public static implicit operator NumberValue<T>(T val)
            {
                return new NumberValue<T>(val);
            }

            public override string ToString()
            {
                return this._innerNumber.ToString().Replace(",", ".");
            }

            public IValue this[string str]
            {
                get
                {
                    return null;
                }
            }

            public IValue this[int idx]
            {
                get
                {
                    return null;
                }
            }

        }

        public class ObjectValue : IValue
        {
            private Dictionary<string, IValue> _innerItems { get; set; }
            public List<string> Keys { get { 
                List<string> list = new List<string>();

                foreach (var key in this._innerItems.Keys)
                {
                    list.Add(key);
                }
                return list;
            } }

            public ObjectValue()
            {
                this._innerItems = new Dictionary<string, IValue>();
            }

            public ObjectValue(Dictionary<string, IValue> items)
            {
                this._innerItems = items;
            }

            public void Add(string key, IValue value)
            {
                this._innerItems[key] = value;
            }

            public string ToJSON()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("{");

                List<string> keys = new List<string>();

                foreach(var key in this._innerItems.Keys){
                    keys.Add(key);
                }

                for (int i = 0; i < keys.Count; i++)
                {
                    IValue value = this._innerItems[keys[i]];
                    builder.Append("\"");
                    builder.Append(keys[i]);
                    builder.Append("\":");
                    if (value == null)
                    {
                        builder.Append("null");
                    }
                    else
                    {
                        builder.Append(value.ToJSON());
                    }
                    if (i < keys.Count - 1) { builder.Append(","); }
                }
                builder.Append("}");
                return builder.ToString();
            }

            public IValue this[string str]{
                get
                {
                    return this._innerItems[str];
                }

                set
                {
                    this._innerItems[str] = value;
                }
            }

            public IValue this[int idx]
            {
                get
                {
                    return null;
                }
            }

            public static implicit operator Dictionary<string, IValue>(ObjectValue ov)
            {
                return ov._innerItems;
            }

            public static implicit operator ObjectValue(Dictionary<string, IValue> dict)
            {
                return new ObjectValue(dict);
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var key in this._innerItems.Keys)
                {
                    sb.Append(key);
                    sb.Append('\n');
                }
                return sb.ToString();
            }
        }

        public class ArrayValue : IValue
        {
             private List<IValue> _innerItems { get; set; }

             public int Length { get { return this._innerItems.Count; } }

            public ArrayValue()
            {
                this._innerItems = new List<IValue>();
            }

            public ArrayValue(List<IValue> items)
            {
                this._innerItems = items;
            }

            public void Add(IValue value)
            {
                this._innerItems.Add(value);
            }

            public string ToJSON()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("[");
                for (int i = 0; i < this._innerItems.Count; i++)
                {
                    IValue value = this._innerItems[i];
                    if (value == null)
                    {
                        builder.Append("null");
                    }
                    else
                    {
                        builder.Append(value.ToJSON());
                    }
                    if (i < this._innerItems.Count - 1) { builder.Append(","); }
                }
                builder.Append("]");
                return builder.ToString();
                 
            }

            public IValue this[int idx]
            {
                get
                {
                    return this._innerItems[idx];
                }

                set
                {
                    this._innerItems[idx] = value;
                }
            }

            public IValue this[string str]
            {
                get
                {
                    return null;
                }
            }

            public static implicit operator List<IValue>(ArrayValue av)
            {
                return av._innerItems;
            }

            public static implicit operator ArrayValue(List<IValue> list)
            {
                return new ArrayValue(list);
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in this._innerItems)
                {
                    sb.Append(item.ToString());
                    sb.Append('\n');
                }
                return sb.ToString();
            }
        }

        public class BooleanValue : IValue
        {
            private bool _innerBool { get; set; }

            public BooleanValue(bool bl)
            {
                _innerBool = bl;
            }

            public string ToJSON()
            {
                return _innerBool ? "true" : "false";
            }

            public static implicit operator bool(BooleanValue bv)
            {
                return bv._innerBool;
            }

            public static implicit operator BooleanValue(bool bl)
            {
                return new BooleanValue(bl);
            }

            public override string ToString()
            {
                return this._innerBool.ToString();
            }

            public IValue this[string str]
            {
                get
                {
                    return null;
                }
            }

            public IValue this[int idx]
            {
                get
                {
                    return null;
                }
            }
        }

        public class Query
        {
            IValue _selected;

            public bool TryParseString(out string str)
            {
                try
                {
                    str = (StringValue)this._selected;
                    return true;
                }catch{
                    str = null;
                    return false;
                }
            }

            public bool TryParseNumber(out float num)
            {
                try
                {
                    num = (NumberValue<float>)this._selected;
                    return true;
                }
                catch
                {
                    num = 0;
                    return false;
                }
            }

            public bool TryParseList(out List<Query> list)
            {
                try
                {
                    ArrayValue av = (ArrayValue)this._selected;
                    list = new List<Query>();

                    for (int i = 0; i < av.Length; i++)
                    {
                        list.Add(new Query(av[i]));
                    }
                    
                    return true;
                }
                catch
                {
                    list = null;
                    return false;
                }
            }

            public bool TryParseDictionary(out Dictionary<string, Query> dict)
            {
                try
                {
                    ObjectValue ov = (ObjectValue)this._selected;
                    dict = new Dictionary<string, Query>();
                    List<string> keys = ov.Keys;

                    for (int i = 0; i < keys.Count; i++)
                    {
                        dict[keys[i]] = new Query(ov[keys[i]]);
                    }

                    return true;
                }
                catch
                {
                    dict = null;
                    return false;
                }
            }

            public Query(IValue value)
            {
                this._selected = value;
            }

            public Query this[string str]
            {
                get
                {
                    try
                    {
                        this._selected = this._selected[str];
                    }
                    catch
                    {
                        this._selected = null;
                    }
                    return this;
                }
            }

            public Query this[int idx]
            {
                get
                {
                    try
                    {
                        this._selected = this._selected[idx];
                    }
                    catch
                    {
                        this._selected = null;
                    }
                    return this;
                }
            }
        }
    }
