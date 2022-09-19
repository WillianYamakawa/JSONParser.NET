
# JSON parser for .NET 2.0 or Higher

# Parse

```cs
JSON.Parse(ref string str);
```

#### Returns:
JSON.Query Object

### TryParse

```cs
JSON.TryParse(ref string str, out Query query);
```

#### Returns: 
bool <has successfuly parsed>

### ParseAsArray

```cs
JSON.ParseAsArray(ref string str);
```

#### Returns:
JSON.Query Object

### TryParseAsArray

```cs
JSON.TryParseAsArray(ref string str, out Query query);
```

#### Returns: 
bool <has successfuly parsed>

### Example

```cs
    string jsonAsText = "{"Hello": "World!"}"
    var query = JSON.Parse(ref jsonAsText);
```

## JSON.Query

```cs
public class Query
```

#### Access JSON values

```cs
//...
string str = "{"id": 1, "info": {"name": "Willian", "age": 18} }"
string name;

JSON.Query query = JSON.Parse(ref str)
query["info"]["name"].TryParseString(out name);
```

#### Parsers
```cs
public bool TryParseString(out string str)
```
```cs
public bool TryParseNumber(out float num)
```
```cs
public bool TryParseList(out List<Query> list)
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
