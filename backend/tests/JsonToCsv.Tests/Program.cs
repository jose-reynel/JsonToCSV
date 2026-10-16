using System.Text;
using JsonToCsv.Application.UseCases;
using JsonToCsv.Domain.Exceptions;
using JsonToCsv.Infrastructure.Services;

// Runner de pruebas mínimo y sin dependencias externas: dotnet run --project tests/JsonToCsv.Tests
var useCase = new ConvertJsonToCsvUseCase(new JsonToCsvConverterService());
int passed = 0, failed = 0;

async Task<string> Convert(string json, string delim = ",")
{
    using var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
    var bytes = await useCase.ExecuteAsync(ms, delim);
    Assert(bytes.Length > 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF, "debe empezar con BOM UTF-8");
    return new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);
}

void Assert(bool cond, string msg) { if (!cond) throw new Exception(msg); }
void Eq(string expected, string actual) =>
    Assert(expected == actual, $"esperado:\n{Show(expected)}\nobtenido:\n{Show(actual)}");
string Show(string s) => s.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");

async Task Test(string name, Func<Task> body)
{
    try { await body(); passed++; Console.WriteLine($"  OK   {name}"); }
    catch (Exception ex) { failed++; Console.WriteLine($"  FAIL {name}\n       {ex.Message}"); }
}

async Task Throws<T>(Func<Task> body, string? contains = null) where T : Exception
{
    try { await body(); }
    catch (T ex) { if (contains != null) Assert(ex.Message.Contains(contains), $"mensaje inesperado: {ex.Message}"); return; }
    throw new Exception($"se esperaba {typeof(T).Name}");
}

// Parser RFC 4180 independiente (solo para pruebas) para verificar ida y vuelta
List<List<string>> ParseCsv(string text, char d)
{
    var rows = new List<List<string>>(); var row = new List<string>(); var f = new StringBuilder();
    bool q = false; int i = 0;
    while (i < text.Length)
    {
        char c = text[i];
        if (q) { if (c == '"') { if (i + 1 < text.Length && text[i + 1] == '"') { f.Append('"'); i++; } else q = false; } else f.Append(c); }
        else if (c == '"') q = true;
        else if (c == d) { row.Add(f.ToString()); f.Clear(); }
        else if (c == '\r') { }
        else if (c == '\n') { row.Add(f.ToString()); f.Clear(); rows.Add(row); row = new(); }
        else f.Append(c);
        i++;
    }
    if (f.Length > 0 || row.Count > 0) { row.Add(f.ToString()); rows.Add(row); }
    return rows;
}

Console.WriteLine("Pruebas JsonToCsv");

await Test("arreglo simple de objetos", async () =>
    Eq("id,nombre\r\n1,Ana\r\n2,Luis\r\n", await Convert("""[{"id":1,"nombre":"Ana"},{"id":2,"nombre":"Luis"}]""")));

await Test("objeto raíz único = 1 fila", async () =>
    Eq("a,b\r\n1,2\r\n", await Convert("""{"a":1,"b":2}""")));

await Test("objetos anidados usan punto", async () =>
    Eq("p.nombre,p.dir.ciudad\r\nAna,Bogotá\r\n", await Convert("""{"p":{"nombre":"Ana","dir":{"ciudad":"Bogotá"}}}""")));

await Test("arreglos usan [i]", async () =>
    Eq("tags[0],tags[1],items[0].x\r\nx,y,5\r\n", await Convert("""{"tags":["x","y"],"items":[{"x":5}]}""")));

await Test("registros con llaves distintas -> unión de encabezados, celdas vacías", async () =>
    Eq("a,b,c\r\n1,2,\r\n1,,3\r\n", await Convert("""[{"a":1,"b":2},{"a":1,"c":3}]""")));

await Test("null -> vacío, booleanos en minúscula, números intactos", async () =>
    Eq("n,t,f,num\r\n,true,false,1.50e3\r\n", await Convert("""{"n":null,"t":true,"f":false,"num":1.50e3}""")));

await Test("escapa coma, comillas y saltos de línea (RFC 4180)", async () =>
{
    var csv = await Convert("{\"a\":\"x,y\",\"b\":\"di \\\"hola\\\"\",\"c\":\"l1\\nl2\"}");
    Eq("a,b,c\r\n\"x,y\",\"di \"\"hola\"\"\",\"l1\nl2\"\r\n", csv);
    var rows = ParseCsv(csv, ',');
    Eq("x,y", rows[1][0]); Eq("di \"hola\"", rows[1][1]); Eq("l1\nl2", rows[1][2]);
});

await Test("delimitador punto y coma (la coma ya no se escapa)", async () =>
    Eq("a;b\r\nx,y;\"p;q\"\r\n", await Convert("""{"a":"x,y","b":"p;q"}""", ";")));

await Test("delimitador tabulación ('tab' y '\\t')", async () =>
{
    Eq("a\tb\r\n1\t2\r\n", await Convert("""{"a":1,"b":2}""", "tab"));
    Eq("a\tb\r\n1\t2\r\n", await Convert("""{"a":1,"b":2}""", "\t"));
});

await Test("delimitador inválido cae a coma", async () =>
    Eq("a,b\r\n1,2\r\n", await Convert("""{"a":1,"b":2}""", "|")));

await Test("tildes y ñ se conservan en UTF-8", async () =>
    Eq("n\r\nÁrbol niño ✓\r\n", await Convert("""{"n":"Árbol niño ✓"}""")));

await Test("elementos escalares en arreglo -> columna 'value'", async () =>
    Eq("value\r\n1\r\ndos\r\n", await Convert("""[1,"dos"]""")));

await Test("objeto y arreglo vacíos anidados -> columna vacía", async () =>
    Eq("a,b\r\n,\r\n", await Convert("""{"a":{},"b":[]}""")));

await Test("ida y vuelta de dataset mixto", async () =>
{
    var csv = await Convert("""[{"id":1,"u":{"n":"A;B","t":["p","q,r"]}},{"id":2,"u":{"n":"C \"x\""}}]""");
    var rows = ParseCsv(csv, ',');
    Eq("id,u.n,u.t[0],u.t[1]", string.Join(",", rows[0]));
    Eq("1|A;B|p|q,r", string.Join("|", rows[1]));
    Eq("2|C \"x\"||", string.Join("|", rows[2]));
});

await Test("20 000 registros", async () =>
{
    var sb = new StringBuilder("[");
    for (int i = 0; i < 20000; i++) sb.Append(i > 0 ? "," : "").Append($"{{\"id\":{i},\"v\":\"fila {i}\"}}");
    var csv = await Convert(sb.Append(']').ToString());
    Assert(ParseCsv(csv, ',').Count == 20001, "debe haber 20001 filas");
});

await Test("JSON inválido -> DomainException", () =>
    Throws<DomainException>(() => Convert("{ no es json"), "inválida"));
await Test("raíz escalar -> DomainException", () =>
    Throws<DomainException>(() => Convert("42"), "raíz"));
await Test("arreglo vacío -> DomainException", () =>
    Throws<DomainException>(() => Convert("[]"), "registros"));
await Test("stream vacío -> DomainException", () =>
    Throws<DomainException>(() => useCase.ExecuteAsync(new MemoryStream(), ","), "vacío"));

Console.WriteLine($"\nResultado: {passed} OK, {failed} fallidas");
return failed == 0 ? 0 : 1;
