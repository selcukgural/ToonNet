#r "nuget: ToonNet.Core, *"

using ToonNet.Core.Encoding;
using ToonNet.Core.Models;

Console.WriteLine("=== Quick Implicit Conversion Test ===\n");

// ✅ NEW! Clean syntax like System.Text.Json
var user = new ToonObject
{
    ["name"] = "Alice",          // string → ToonString (implicit!)
    ["age"] = 30,                // int → ToonNumber (implicit!)
    ["salary"] = 75000.50m,      // decimal → ToonNumber (implicit!)
    ["isActive"] = true,         // bool → ToonBoolean (implicit!)
    ["email"] = "alice@test.com"
};

user["manager"] = ToonNull.Instance;  // Explicit null

var encoder = new ToonEncoder();
var document = new ToonDocument(user);
Console.WriteLine(encoder.Encode(document));

Console.WriteLine("\n✅ Implicit conversion works perfectly!");
