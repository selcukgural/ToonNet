# ToonNet.AspNetCore

Bu proje, ToonNet’i ASP.NET Core uygulamalarına **IServiceCollection** üzerinden entegre etmek için DI (Dependency Injection) ve **Options + Configuration Binding + Validation** altyapısını sağlar.

Bu README yalnızca `ToonNet.AspNetCore` projesi içindir.

---

## 1) Kurulum

### Seçenek A: Bu repo içinde ProjectReference

Uygulama projenize aşağıdaki referansı ekleyin:

```xml
<ItemGroup>
  <ProjectReference Include="path/to/src/ToonNet.AspNetCore/ToonNet.AspNetCore.csproj" />
</ItemGroup>
```

### Seçenek B: (İleride) NuGet

Bu repo şu an ProjectReference kullanımına uygun yapıdadır. NuGet yayımlanırsa buraya paket adı/versiyonu eklenebilir.

---

## 2) En Hızlı Başlangıç (default options)

`Program.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Registers ToonParser, ToonEncoder and options (default values)
builder.Services.AddToonNet();

var app = builder.Build();
app.Run();
```

Bu kullanım:
- `ToonParser` ve `ToonEncoder`’ı DI’a ekler.
- `ToonOptions` ve `ToonSerializerOptions` için `IOptions<T>` altyapısını hazırlar.

---

## 3) Configuration Binding ile Kullanım (önerilen)

### appsettings.json

`ToonNet` root section altında iki alt bölüm beklenir:
- `ToonOptions`
- `ToonSerializerOptions`

Örnek:

```json
{
  "ToonNet": {
    "ToonOptions": {
      "IndentSize": 4,
      "MaxDepth": 100,
      "Delimiter": ",",
      "StrictMode": true,
      "AllowExtendedLimits": false
    },
    "ToonSerializerOptions": {
      "IgnoreNullValues": true,
      "PropertyNamingPolicy": "CamelCase",
      "IncludeTypeInformation": false,
      "PublicOnly": true,
      "IncludeReadOnlyProperties": true,
      "MaxDepth": 100,
      "AllowExtendedLimits": false
    }
  }
}
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddToonNet(builder.Configuration);

var app = builder.Build();
app.Run();
```

Notlar:
- `Delimiter` config’te `string` olarak okunur; ilk karakter kullanılır.
- `ValidateDataAnnotations()` ve `ValidateOnStart()` aktif olduğu için hatalı config, uygulama start’ında fail-fast şekilde ortaya çıkar.

---

## 4) Delegate ile Options Konfigürasyonu

Config yerine kodla ayarlamak istersen:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddToonNet(
    configureToonOptions: o =>
    {
        o.IndentSize = 2;
        o.StrictMode = true;
    },
    configureSerializerOptions: o =>
    {
        o.IgnoreNullValues = true;
        o.PropertyNamingPolicy = PropertyNamingPolicy.CamelCase;
    });

var app = builder.Build();
app.Run();
```

---

## 5) Controller/Minimal API içinde Kullanım

### DI ile ToonParser/ToonEncoder kullanımı

```csharp
using Microsoft.AspNetCore.Mvc;
using ToonNet.Core.Encoding;
using ToonNet.Core.Parsing;

[ApiController]
[Route("toon")]
public sealed class ToonController : ControllerBase
{
    private readonly ToonParser _parser;
    private readonly ToonEncoder _encoder;

    public ToonController(ToonParser parser, ToonEncoder encoder)
    {
        _parser = parser;
        _encoder = encoder;
    }

    [HttpPost("roundtrip")]
    public ActionResult<string> RoundTrip([FromBody] string toon)
    {
        var doc = _parser.Parse(toon);
        var normalized = _encoder.Encode(doc);
        return Ok(normalized);
    }
}
```

### Options okuma

```csharp
using Microsoft.Extensions.Options;
using ToonNet.Core;

public sealed class SomeService
{
    private readonly ToonOptions _toonOptions;

    public SomeService(IOptions<ToonOptions> toonOptions)
    {
        _toonOptions = toonOptions.Value;
    }
}
```

---

## 6) ToonSerializer ile Kullanım (statik API)

`ToonSerializer` statik olduğundan DI kaydı gerekmez; fakat **options** DI üzerinden yönetilebilir.

```csharp
using Microsoft.Extensions.Options;
using ToonNet.Core.Serialization;

public sealed class UserToonService
{
    private readonly ToonSerializerOptions _serializerOptions;

    public UserToonService(IOptions<ToonSerializerOptions> serializerOptions)
    {
        _serializerOptions = serializerOptions.Value;
    }

    public string SerializeUser(User user)
    {
        return ToonSerializer.Serialize(user, _serializerOptions);
    }
}
```

Async kullanım:

```csharp
public async Task<string> SerializeUserAsync(User user, CancellationToken ct)
{
    return await ToonSerializer.SerializeAsync(user, _serializerOptions, ct);
}
```

---

## 7) Validation Davranışı ve Hata Senaryoları

Bu paket:
- `ToonOptions` ve `ToonSerializerOptions` üzerinde DataAnnotations doğrulaması uygular.
- Ayrıca ToonNet.Core tarafındaki runtime guard’lar (ör. `IndentSize` çift sayı olmalı) korunur.

Örnek hata:
- `IndentSize = 3` gibi bir değer, `ArgumentOutOfRangeException` ile fail eder.

Üretimde öneri:
- Configuration binding kullanıyorsanız, invalid config’i CI/CD’de yakalamak için uygulamayı startup aşamasında çalıştıran smoke test ekleyin.

---

## 8) Konfigürasyon Şeması Özeti

Root: `ToonNet`
- `ToonOptions`
  - `IndentSize` (int)
  - `MaxDepth` (int)
  - `Delimiter` (string, ilk karakter)
  - `StrictMode` (bool)
  - `AllowExtendedLimits` (bool)
- `ToonSerializerOptions`
  - `IgnoreNullValues` (bool)
  - `PropertyNamingPolicy` (string: `Default`, `CamelCase`, `SnakeCase`, `LowerCase`)
  - `IncludeTypeInformation` (bool)
  - `PublicOnly` (bool)
  - `IncludeReadOnlyProperties` (bool)
  - `MaxDepth` (int)
  - `AllowExtendedLimits` (bool)

---

## 9) Geliştirme / Test

Repo kökünde:

```bash
dotnet test ToonNet.sln
```

AspNetCore DI entegrasyonu için testler:
- `tests/ToonNet.Tests/AspNetCore/ToonNetServiceCollectionExtensionsTests.cs`
