using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class JType : IEnumerable<JType>
{ 
    public string Name { get; set; }
    public string Value { get; set; }

    public JToken Next { get; set; } 

    public override string ToString() => Value;

    public IEnumerator<JType> GetEnumerator()
    {
        return Next.Children.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
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
                    Next.Children.Add(new JType(key, value.Value, null));
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

    public JType(string name, string value, JToken next)
    {
        Name = name;
        Value = value;
        Next = next;
    }

    public JType(string value, JToken next)
    {
        Value = value;
        Next = next;
    }

    public JType(string value)
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
               string.IsNullOrEmpty(Next.Value) &&
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
        if (string.IsNullOrEmpty(Name) && string.IsNullOrEmpty(Value))
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
}

public static class JsonParser
{
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
                if (!string.IsNullOrEmpty(child.Name) && !string.IsNullOrEmpty(child.Value))
                {
                    if (child.Value == "true" || child.Value == "false" || child.Value == "null" || int.TryParse(child.Value, out _))
                    {
                        jsonStr.Append($"\"{child.Name}\":{child.Value}");
                    }else
                    {
                        jsonStr.Append($"\"{child.Name}\":\"{child.Value}\"");
                    }
                }
                else if (!string.IsNullOrEmpty(child.Name) && string.IsNullOrEmpty(child.Value)
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
                if (string.IsNullOrEmpty(child.Name) && !string.IsNullOrEmpty(child.Value))
                {
                    if (child.Value == "true" || child.Value == "false" || child.Value == "null" || int.TryParse(child.Value, out _))
                    {
                        jsonStr.Append($"{child.Value}");
                    }
                    else
                    {
                        jsonStr.Append($"\"{child.Value}\"");
                    }
                }
                else if (string.IsNullOrEmpty(child.Name) && string.IsNullOrEmpty(child.Value))
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
                    strBuilder.Append(_jsonStr[++_ind]);
                    continue;
                }else if (_jsonStr[_ind] == '/')
                {

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
                        last.Children.Add(new JType(string.Empty, string.Empty, new JArray()));
                    }else
                    {
                        last.Children.Add(new JType(currStr, string.Empty, new JArray()));
                        currStr = string.Empty;
                    }
                    stack.Push(last.Children.Last().Next);
                    break;
                case '{':
                    if (string.IsNullOrEmpty(currStr))
                    {
                        last.Children.Add(new JType(string.Empty, string.Empty, new JDict()));
                    }
                    else
                    {
                        last.Children.Add(new JType(currStr, string.Empty, new JDict()));
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
                            last.Children.Add(new JType(string.Empty, str, null));
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
                    if (string.IsNullOrEmpty(currStr))
                    {
                        if (last.GetType() == typeof(JArray))
                        {
                            last.Children.Add(new JType(string.Empty, str, null));
                        }
                        else
                        {
                            currStr = str;
                        }
                    }
                    else
                    {
                        last.Children.Add(new JType(currStr, str, null));
                        currStr = string.Empty;
                    }
                    break;
            }
        }

        return root;
    }
}


