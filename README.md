
# JSON parser for .NET 2.0 or Higher

# Parse

```cs
JSon.Parse(str);
```

#### Returns:
JSon.Query Object

### Example

```cs
    string jsonAsText = "{\"Hello\": \"World!\"}"
    var query = JSon.Parse(jsonAsText);
```

## JSON.Query

```cs
public class Query : IEnumerator<Query>, IEnumerable<Query>
```

#### Access JSON values

```cs
//...
string str = "{"id": 1, "info": {"name": "Willian", "age": 18} }"
string name;

JSon.Query query = JSon.Parse(str)
query.Fetch("info/name").TryGetString(out name);
```

#### Parsers
```cs
public bool TryGetString(out string str)
```
```cs
public bool TryGetNumber(out float num)
```
```cs
public bool TryGetBoolean(out float num)
```
```cs
JSon json = new Json.JSon(File.Open(@"C:\SomeFile", FileMode.Open, FileAccess.Read));
            
var parsed = json.Parse();
            
var objData = parsed.Fetch("0/objData");

foreach (var data in objData)
{
    float x;
    if (data.Fetch("x").TryGetNumber(out x))
    {
        Console.WriteLine(x);
    }
} 
```

# Object to JSON String

Inherits from JSON.IValue

### StringValue

```cs
var sv = new StringValue("github");
StringValue sv2 = "Implicit";
```

###A NumberValue

```cs
var nv = new NumberValue<int>(1);
NumberValue<float> nv2 = 1.2f;
```

### BooleanValue

```cs
var bv = new BooleanValue(true);
BooleanValue bv2 = false;
```

### ObjectValue

```cs
ObjectValue ov = new ObjectValue();

ov["name"] = new StringValue("Will");
```

### ArrayValue

```cs
ArrayValue av = new ArrayValue();

av.Add(new ObjectValue());
```

## ToJSON

```cs
IValue.ToJSON();
```
#### Returns: 
string

### Object

```cs
ObjectValue ob = new ObjectValue();
ob["name"] = new StringValue("Will");

ob.ToJSON();
// returns {"name": "Will"}
```
