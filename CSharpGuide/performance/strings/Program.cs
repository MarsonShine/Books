using strings;

TestStrings();

static void TestStrings()
{
    Strings.RemoveInstanceIdFromQueryString2("?instanceId=2&name=marsonshine&age=30");
    Strings.EnumerateQuery("?instanceId=2&name=marsonshine&age=30");
}

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.Run();
