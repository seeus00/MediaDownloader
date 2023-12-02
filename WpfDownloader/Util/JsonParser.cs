using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Input;

public class JType : IEnumerable<JType>
{ 
    public string Name { get; set; }
    public object Value { get; set; }

    public JToken Next { get; set; } 

    public override string ToString() => (Value != null) ? Value.ToString() : string.Empty;
    public int ToInt => (Value is int) ? (int) Value : 0;
    public bool ToBool => (Value is bool) ? (bool)Value : false;
    public double ToDouble => (Value is double) ? (double)Value : 0.0;

    public IEnumerator<JType> GetEnumerator()
    {
        return Next.Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public JType this[int index]
    {
        get
        {
            if (Next == null) Next = (JToken)this;

            var result = Next.Children[index];
            return result;
        }
        set
        {
            if (index >= 0 && index < Next.Children.Count) Next.Children[index] = value;
        }
    }


    public JType this[string key]
    {
        get
        {
            if (Next == null) Next = (JToken)this;

            var result = Next.Children.Where(i => i.Name == key).FirstOrDefault();
            return result;
        }
        set
        {
            if (Next == null)
                Next = (JToken) this;

            var existInd = Next.Children.FindIndex(child => child.Name == key);
            if (value.GetType() == typeof(JArray) || value.GetType() == typeof(JDict))
            {
                if (existInd == -1)
                {
                    Next.Children.Add(new JType(key, null, (JToken)value));
                }
                else
                {
                    Next.Children.ElementAt(existInd).Next = (JToken)value;
                }
            }
            else if (value.GetType() == typeof(JType))
            {
                if (existInd == -1)
                {
                    Next.Children.Add(new JType(key, value.Value));
                }
                else
                {
                    Next.Children.ElementAt(existInd).Value = value.Value;
                }
            }
        }
    }

    public JType()
    {
        Next = (JToken)this;
    }

    public JType(string name, object value, JToken next = null)
    {
        Name = name;
        Value = value;
        Next = next;
    }


    public JType(string name, object value)
    {
        Name = name;
        Value = value;
        Next = null;
    }

    public JType(string value, JToken next)
    {
        Value = value;
        Next = next;
    }

    public JType(object value)
    {
        Value = value;
        Next = null;
    }

    public JType(JToken next)
    {
        Next = next;
    }

    public bool IsEmpty()
    {
        return (string.IsNullOrEmpty(Next.Name) &&
               Next.Value == null &&
               Next.Children.Count == 0);
    }
}


public class JToken : JType, IEnumerable<JType>
{
    public JToken(IEnumerable<string> lst)
    {
        Children = lst.Select(item => new JType("", item, null)).ToList();
    }

    public List<JType> Children { get; set; }
    public int Count => Children.Count;
    public bool IsReadOnly => false;


    public JType this[int index] { get => Children[index]; set => Children[index] = value; }

    public JToken()
    {
        Children = new List<JType>();
    }

    public bool Contains(string val) => 
        Children.Any(child => child.Value == val);

    IEnumerator IEnumerable.GetEnumerator()
    {
        if (string.IsNullOrEmpty(Name) && Value == null)
            return Next.Children.GetEnumerator();
        return Children.GetEnumerator();
    }

    public void Add(JToken addToken)
    {
        Children.Add(addToken);
    }
}

public class JArray : JToken
{
    public JArray() : base() { }

    public JArray(IEnumerable<string> lst)
    {
        Children = lst.Select(item => new JType(null, item, null)).ToList();
    }
}
public class JDict : JToken
{
    public JDict() : base() { }
    public JDict(string name, object value)
    {
        Next.Children.Add(new JType(name, value, null));
    }
}

public static class JsonParser
{
    public static JToken ParseObject(object obj)
    {
        JToken data = (obj is IEnumerable) ? new JArray() : new JDict();
        RecurseObject(data, obj);

        return data;
    }

    private static string LowercaseFirstLetter(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";

        var builder = new StringBuilder();
        builder.Append(str[0].ToString().ToLower());
        for (int i = 1; i < str.Length; i++)
        {
            builder.Append(str[i]);
        }

        return builder.ToString();
    }

    private static void RecurseObject(JToken token, object currObj)
    {
        if (currObj is string || currObj is int || currObj is float || currObj is double || currObj is bool || currObj is null)
        {
            token.Children.Add(new JType(name: null, value: currObj, next: null));
        }
        else
        {
            //If the first object is just a single list
            if (currObj is IEnumerable)
            {
                foreach (var obj in (IEnumerable) currObj)
                {
                    RecurseObject(token, obj);
                }
                return;
            }

            //If the json is like this: {'str': [{}]}
            if (token is JArray)
            {
                token.Children.Add(new JType(null, null, new JDict()));
                RecurseObject(token.Children.Last().Next, currObj);

                return;
            }

            var props = currObj.GetType().GetProperties();
            if (props.Any())
            {
                foreach (var property in props)
                {
                    string propName = LowercaseFirstLetter(property.Name);
                    if (!string.IsNullOrEmpty(property.Name))
                    {
                        var propVal = property.GetValue(currObj, null);
                        if (propVal is string || propVal is int || propVal is float || propVal is double || propVal is bool ||
                            propVal is null)
                        {

                            if (token is JArray)
                            {
                                token.Children.Add(new JType(name: null, value: propVal, null));
                            }
                            else
                            {
                                token.Children.Add(new JType(name: propName, propVal, null));
                            }

                        }
                        else if (propVal is IEnumerable)
                        {
                            token.Children.Add(new JType(name: propName, null, new JArray()));
                            foreach (var obj in (IEnumerable)propVal)
                            {
                                RecurseObject(token.Children.Last().Next, obj);
                            }
                        }
                        //Is object
                        else
                        {
                            token.Children.Add(new JType(name: propName, null, new JDict()));
                            RecurseObject(token.Children.Last().Next, propVal);
                        }
                    }
                }
            }
        }
    }

    public static JToken Parse(string jsonStr) => new JObject(jsonStr).Parse();

    //Converts object into json string
    public static StringBuilder Serialize(JToken obj, StringBuilder jsonStr = null)
    {
        if (jsonStr == null)
            jsonStr = new StringBuilder();

        if (obj.GetType() == typeof(JDict))
        {
            jsonStr.Append('{');
            foreach (var child in obj.Children)
            {
                if (!string.IsNullOrEmpty(child.Name) && child.Value != null)
                {
                    if (child.Value is bool)
                    {
                        jsonStr.Append($"\"{child.Name}\":{child.Value.ToString().ToLower()}");
                    }
                    else if (child.Value == null)
                    {
                        jsonStr.Append($"\"{child.Name}\":null");
                    }
                    else if (child.Value is string)
                    {
                        jsonStr.Append($"\"{child.Name}\":\"{(string)child.Value}\"");
                    }
                    //Numeric types
                    else
                    {
                        jsonStr.Append($"\"{child.Name}\":{child.Value}");
                    }
                }
                else if (!string.IsNullOrEmpty(child.Name) && child.Value == null
                   && child.Next == null)
                {
                    // Emmpty string ""
                    jsonStr.Append($"\"{child.Name}\":\"\"");
                }
                else
                {
                    jsonStr.Append($"\"{child.Name}\":");
                    jsonStr = Serialize(child.Next, jsonStr);
                }
                jsonStr.Append(',');
            }
            if (jsonStr[jsonStr.Length - 1] == ',')
                jsonStr = jsonStr.Remove(jsonStr.Length - 1, 1);
            jsonStr.Append('}');
        }
        else if (obj.GetType() == typeof(JArray))
        {
            jsonStr.Append('[');
            foreach (var child in obj.Children)
            {
                if (string.IsNullOrEmpty(child.Name) && child.Next == null)
                {
                    if (child.Value is bool)
                    {
                        jsonStr.Append($"{child.Value.ToString().ToLower()}");
                    }
                    else if (child.Value == null)
                    {
                        jsonStr.Append("null");
                    }
                    else if (child.Value is string)
                    {
                        jsonStr.Append($"\"{(string)child.Value}\"");
                    }else
                    {
                        jsonStr.Append($"{child.Value}");
                    }
                }
                else if (string.IsNullOrEmpty(child.Name) && child.Value == null)
                {
                    jsonStr = Serialize(child.Next, jsonStr);
                }

                jsonStr.Append(',');
            }

            if (jsonStr[jsonStr.Length - 1] == ',')
                jsonStr = jsonStr.Remove(jsonStr.Length - 1, 1);
            jsonStr.Append(']');
        }

        return jsonStr;
    }
}

public class JObject
{
    private int _ind = 0;
    private string _jsonStr;

    public static readonly string SYMBOLS = "{}[]:,";

    public JObject(string jsonStr)
    {
        _jsonStr = jsonStr;
    }

    private string getStr()
    {
        var strBuilder = new StringBuilder();
        if (_jsonStr[_ind] == '"')
        {
            while (_jsonStr[++_ind] != '"')
            {
                if (_jsonStr[_ind] == '\\')
                {
                    strBuilder.Append(_jsonStr[_ind]);
                    strBuilder.Append(_jsonStr[++_ind]);
                    continue;
                }

                strBuilder.Append(_jsonStr[_ind]);
            }
        }
        else
        {
            while (!SYMBOLS.Contains(_jsonStr[_ind]))
            {
                strBuilder.Append(_jsonStr[_ind++]);
            }

            _ind--;
        }

        return strBuilder.ToString();
    }

    public JToken Parse()
    {
        if (string.IsNullOrEmpty(_jsonStr) || _jsonStr.Length == 0) 
            throw new Exception("JSON String is empty or null!!");

        var stack = new Stack<JToken>();
        JToken root = (_jsonStr[_ind++] == '[') ? (JToken) new JArray() : new JDict();
        stack.Push(root);

        string currStr = string.Empty;

        int len = _jsonStr.Length;
        for (_ind = 1; _ind < len; _ind++)
        {
            char currChar = _jsonStr[_ind];
            if (stack.Count == 0) break;

            var last = stack.Peek();

            switch (currChar)
            {
                case '\t':
                case '\r':
                case '\n':
                case ' ':
                case ':':
                case ',':
                    break;
                case '[':
                    if (string.IsNullOrEmpty(currStr))
                    {
                        last.Children.Add(new JType(null, null, new JArray()));
                    }else
                    {
                        last.Children.Add(new JType(currStr, null, new JArray()));
                        currStr = string.Empty;
                    }
                    stack.Push(last.Children.Last().Next);
                    break;
                case '{':
                    if (string.IsNullOrEmpty(currStr))
                    {
                        last.Children.Add(new JType(null, null, new JDict()));
                    }
                    else
                    {
                        last.Children.Add(new JType(currStr, null, new JDict()));
                        currStr = string.Empty;
                    }
                    stack.Push(last.Children.Last().Next);
                    break;
                case ']':
                    stack.Pop();
                    break;
                case '}':
                    stack.Pop();
                    break;
                case '"':
                    string str = getStr();
                    if (string.IsNullOrEmpty(currStr))
                    {
                        if (last.GetType() == typeof(JArray))
                        {
                            last.Children.Add(new JType(null, str, null));
                        }else
                        {
                            currStr = str;
                        }
                    }else
                    {
                        last.Children.Add(new JType(currStr, str, null));
                        currStr = string.Empty;
                    }
                    break;
                default:
                    str = getStr();
                    object newObj = null;

                    if (int.TryParse(str, out int intVal))
                    {
                        newObj = intVal;
                    }
                    else if (double.TryParse(str, out double doubleVal))
                    {
                        newObj = doubleVal;
                    }
                    else
                    {
                        newObj = str;
                    }
                   

                    if (string.IsNullOrEmpty(currStr))
                    {
                        if (last.GetType() == typeof(JArray))
                        {
                            last.Children.Add(new JType(null, newObj, null));
                        }
                        else
                        {
                            currStr = str;
                        }
                    }
                    else
                    {
                        last.Children.Add(new JType(currStr, newObj, null));
                        currStr = string.Empty;
                    }
                    break;
            }
        }

        return root;
    }
}


