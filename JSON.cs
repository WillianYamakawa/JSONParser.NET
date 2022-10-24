using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Json
{
    class Query : IEnumerator<Query>, IEnumerable<Query>{
        private JSon.IValue current;
        private int enumIndex;

        public Query(JSon.IValue value)
        {
            current = value;
        }

        public Query Fetch(string query){
                var value = current;
                try
                {
                    if (query == "")
                    {
                        return new Query(value);
                    }
                    string[] queries = query.Split('/');
                    foreach (string route in queries)
                    {
                        if (value == null)
                        {
                            break;
                        }
                        int index;
                        if (int.TryParse(route, out index))
                        {
                            value = value[index];
                        }
                        else
                        {
                            value = value[route];
                        }
                    }
                    return new Query(value);
                }
                catch
                {
                    return new Query(null);
                }
                
            }

        public bool TryGetString(out string result)
        {
            try
            {
                result = (JSon.StringValue)current;
                return true;
            }catch{
                result = null;
                return false;
            }
        }

        public bool TryGetNumber(out float result)
        {
            try
            {
                result = (JSon.NumberValue)current;
                return true;
            }
            catch
            {
                result = -1;
                return false;
            }
        }

        public bool TryGetBool(out bool result)
        {
            try
            {
                result = (JSon.BooleanValue)current;
                return true;
            }
            catch
            {
                result = false;
                return false;
            }
        }

        public Query Current
        {
            get {return new Query(current[enumIndex++]); }
        }

        public void Dispose()
        {
            enumIndex = 0;
        }

        object System.Collections.IEnumerator.Current
        {
            get { return new Query(current[enumIndex++]); }
        }

        public bool MoveNext()
        {
            if(current != null && current is JSon.ArrayValue){
                if (enumIndex < ((JSon.ArrayValue)current).Length)
                {
                    return true;
                }
            }
            return false;
        }

        public void Reset()
        {
            enumIndex = 0;
        }

        public IEnumerator<Query> GetEnumerator()
        {
            return this;
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this;
        }
    }

    class JSon
    {
        private Stream ms;
        private StreamReader buffer;

        public JSon(string json)
        {
            ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            buffer = new StreamReader(ms);
        }

        public JSon(Stream stream)
        {
            ms = stream;
            buffer = new StreamReader(ms);
        }

        public Query Parse()
        {
            if (buffer.Peek() == '{')
            {
                return new Query(ParseObject());
            }
            else if (buffer.Peek() == '[')
            {
                return new Query(ParseArray());
            }
            else
            {
                throw new Exception("Unexpected symbol at 1: "+buffer.Peek());
            }
        }

        public StringValue ParseString()
        {
            StringBuilder sb = new StringBuilder();
            buffer.Read();
            bool isLastScape = false;
            char c;
            while(buffer.Peek() >= 0){
                c = (char)buffer.Read();
                if (isLastScape)
                {
                    if (c == 'n')
                    {
                        sb.Append('\n');
                    }
                    else if (c == 'r')
                    {
                        sb.Append('\r');
                    }
                    else if (c == 't')
                    {
                        sb.Append('\t');
                    }
                    else if (c == '"')
                    {
                        sb.Append('\"');
                    }
                    else if(c == '\\')
                    {
                        sb.Append('\\');
                    }
                    isLastScape = false;
                }
                else
                {
                    if (c == '"')
                    {
                        break;
                    }else if(c == '\\'){
                        isLastScape = true;
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }
            return new StringValue(sb.ToString());
        }

        public NumberValue ParseNumber()
        {
            StringBuilder sb = new StringBuilder();
            char c;
            while (buffer.Peek() >= 0)
            {
                c = (char)buffer.Peek();
                if (Char.IsDigit(c) || c == '.' || c == '-')
                {
                    buffer.Read();
                    sb.Append(c);
                }
                else
                {
                    break;
                }
            }
            return new NumberValue(float.Parse(sb.ToString(), CultureInfo.InvariantCulture));
        }

        public BooleanValue ParseBoolean()
        {
            char first = (char)buffer.Read();
            if (first == 't' && (char)buffer.Read() == 'r' && (char)buffer.Read() == 'u' && (char)buffer.Read() == 'e')
            {
                return new BooleanValue(true);
            }
            else if (first == 'f' && (char)buffer.Read() == 'a' && (char)buffer.Read() == 'l' && (char)buffer.Read() == 's' && (char)buffer.Read() == 'e')
            {
                return new BooleanValue(false);
            }
            throw new Exception("Coundn't parse bool");
        }

        public ObjectValue ParseObject()
        {
            ObjectValue values = new ObjectValue();
            buffer.Read();
            if ((char)buffer.Peek() == '}')
            {
                buffer.Read();
            }
            else
            {
                while (buffer.Peek() >= 0)
                {
                    this.ReadUntilCharIsNotTrash();
                    string key = this.ParseString();
                    this.ReadUntilCharIsNotTrash();
                    if ((char)buffer.Read() != ':') { throw new Exception("Did not found :"); }
                    this.ReadUntilCharIsNotTrash();
                    char nextChar = (char)buffer.Peek();
                    IValue value = null;
                    switch (nextChar)
                    {
                        case '\"':
                            value = ParseString();
                            break;
                        case 'n':
                            if ((char)buffer.Read() == 'n' && (char)buffer.Read() == 'u' && (char)buffer.Read() == 'l' && (char)buffer.Read() == 'l')
                            {
                                value = null;
                            }
                            break;
                        case 't':
                            value = ParseBoolean();
                            break;
                        case 'f':
                            value = ParseBoolean();
                            break;
                        case '{':
                            value = ParseObject();
                            break;
                        case '[':
                            value = ParseArray();
                            break;
                        default:
                            value = ParseNumber();
                            break;
                    }
                    this.ReadUntilCharIsNotTrash();
                    values[key] = value;
                    char end = (char)buffer.Read();
                    if (end == '}')
                    {
                        break;
                    }
                    else if (end == ',')
                    {
                        continue;
                    }
                    else
                    {
                        throw new Exception("Unexpected end: " + end);
                    }
                }
            }
            return values;
        }

        public ArrayValue ParseArray()
        {
            ArrayValue values = new ArrayValue();
            buffer.Read();
            if ((char)buffer.Peek() == ']')
            {
                buffer.Read();
            }
            else
            {
                while (buffer.Peek() >= 0)
                {
                    this.ReadUntilCharIsNotTrash();
                    char nextChar = (char)buffer.Peek();
                    IValue value = null;
                    switch (nextChar)
                    {
                        case '\"':
                            value = ParseString();
                            break;
                        case 'n':
                            if ((char)buffer.Read() == 'n' && (char)buffer.Read() == 'u' && (char)buffer.Read() == 'l' && (char)buffer.Read() == 'l')
                            {
                                value = null;
                            }
                            break;
                        case 't':
                            value = ParseBoolean();
                            break;
                        case 'f':
                            value = ParseBoolean();
                            break;
                        case '{':
                            value = ParseObject();
                            break;
                        case '[':
                            value = ParseArray();
                            break;
                        default:
                            value = ParseNumber();
                            break;
                    }
                    this.ReadUntilCharIsNotTrash();
                    values.Add(value);
                    char end = (char)buffer.Read();
                    if (end == ']')
                    {
                        break;
                    }
                    else if (end == ',')
                    {
                        continue;
                    }
                    else
                    {
                        throw new Exception("Unexpected end: " + end);
                    }
                }
            }
            return values;
        }

        public void ReadUntilCharIsNotTrash(){
            while ((char)buffer.Peek() == ' ' || (char)buffer.Peek() == '\n' || (char)buffer.Peek() == '\r' || (char)buffer.Peek() == '\t'){
                buffer.Read();
            }
        }

        public interface IValue
        {
            string ToJSON();
            JSon.IValue this[string str] { get; }
            JSon.IValue this[int idx] { get; }
        }

        public class StringValue : JSon.IValue
        {
            private string _innerString;
            public string Data { get { return this._innerString; } set { this._innerString = value; } }
            public string ToJSON() 
            {
                if (this._innerString == null) return "null"; 
                else{
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < this._innerString.Length; i++)
                    {
                        if (this._innerString[i] == '\n')
                        {
                            sb.Append("\\n");
                        }
                        else if (this._innerString[i] == '\r')
                        {
                            sb.Append("\\r");
                        }
                        else if (this._innerString[i] == '\t')
                        {
                            sb.Append("\\t");
                        }
                        else if (this._innerString[i] == '"')
                        {
                            sb.Append("\\\"");
                        }
                        else if (this._innerString[i] == '\\')
                        {
                            sb.Append("\\\\");
                        }
                        else
                        {
                            sb.Append(this._innerString[i]);
                        }
                    }
                    return "\"" + sb.ToString() +"\"";
                }; 
            }
            public static implicit operator string(JSon.StringValue sv) { return sv._innerString ?? string.Empty; }
            public static implicit operator JSon.StringValue(string str) { return new JSon.StringValue(str); }
            public override string ToString() { return this._innerString; }
            public JSon.IValue this[string str] { get { return null; } }
            public JSon.IValue this[int idx] { get { return null; } }
            public StringValue(string str) { this._innerString = str; }
        }

        public class NumberValue : JSon.IValue
        {
            private float _innerNumber;
            public float Data { get { return this._innerNumber; } set { this._innerNumber = value; } }
            public string ToJSON() { return _innerNumber.ToString().Replace(",", "."); }
            public static implicit operator float(JSon.NumberValue nv) { return nv._innerNumber; }
            public static implicit operator JSon.NumberValue(float val) { return new JSon.NumberValue(val); }
            public override string ToString() { return this._innerNumber.ToString().Replace(",", "."); }
            public JSon.IValue this[string str] { get { return null; } }
            public JSon.IValue this[int idx] { get { return null; } }
            public NumberValue(float number) { this._innerNumber = number; }
        }

        public class ObjectValue : Dictionary<string, JSon.IValue>, JSon.IValue
        {
            public new void Add(string key, JSon.IValue value) { this[key] = value; }
            public JSon.IValue this[int idx] { get { return null; } }

            public string ToJSON()
            {
                StringBuilder builder = new StringBuilder().Append("{");
                List<string> keys = new List<string>(this.Keys);
                for (int i = 0; i < keys.Count; i++)
                {
                    JSon.IValue value = this[keys[i]];
                    builder.Append("\"").Append(keys[i]).Append("\":");
                    if (value == null) { builder.Append("null"); } else { builder.Append(value.ToJSON()); }
                    if (i < keys.Count - 1) { builder.Append(","); }
                }
                builder.Append("}");
                return builder.ToString();
            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var key in this.Keys) { sb.Append(key).Append('\n'); }
                return sb.ToString();
            }
        }

        public class ArrayValue : JSon.IValue
        {
            private List<JSon.IValue> _innerItems;
            public int Length { get { return this._innerItems.Count; } }
            public void Add(JSon.IValue value) { this._innerItems.Add(value); }
            public JSon.IValue this[int idx] { get { return this._innerItems[idx]; } set { this._innerItems[idx] = value; } }
            public JSon.IValue this[string str] { get { return null; } }
            public static implicit operator List<JSon.IValue>(JSon.ArrayValue av) { return av._innerItems; }
            public static implicit operator JSon.ArrayValue(List<JSon.IValue> list) { return new JSon.ArrayValue(list); }

            public string ToJSON()
            {
                StringBuilder builder = new StringBuilder().Append("[");
                for (int i = 0; i < this._innerItems.Count; i++)
                {
                    JSon.IValue value = this._innerItems[i];
                    if (value == null) { builder.Append("null"); } else { builder.Append(value.ToJSON()); }
                    if (i < this._innerItems.Count - 1) { builder.Append(","); }
                }
                builder.Append("]");
                return builder.ToString();

            }

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();
                foreach (var item in this._innerItems) { sb.Append(item.ToString()).Append('\n'); }
                return sb.ToString();
            }

            public ArrayValue() { this._innerItems = new List<JSon.IValue>(); }
            public ArrayValue(List<JSon.IValue> items) { this._innerItems = items; }
        }

        public class BooleanValue : JSon.IValue
        {
            private bool _innerBool;
            public bool Data { get { return this._innerBool; } set { this._innerBool = value; } }
            public string ToJSON() { return this._innerBool ? "true" : "false"; }
            public static implicit operator bool(JSon.BooleanValue bv) { return bv._innerBool; }
            public static implicit operator JSon.BooleanValue(bool bl) { return new JSon.BooleanValue(bl); }
            public override string ToString() { return this._innerBool.ToString(); }
            public JSon.IValue this[string str] { get { return null; } }
            public JSon.IValue this[int idx] { get { return null; } }
            public BooleanValue(bool bl) { this._innerBool = bl; }
        }
    }
}
