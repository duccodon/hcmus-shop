# Feature 07 — Code Obfuscator

**Owner**: Dev A
**Status**: Planned
**Bonus**: +0.25 pts
**Phase**: 7

## Summary

Run the compiled .NET assemblies through an obfuscator before packaging. Makes decompilation (e.g. with dnSpy) harder by renaming symbols.

## User-visible behavior

None — purely a build-time step. The installed app behaves identically.

## Tool: Obfuscar

Open-source, free, NuGet package. Simpler than ConfuserEx for our needs.

## Files

| File | Purpose |
|------|---------|
| `obfuscar.xml` | Configuration: which assemblies to obfuscate, what to rename |
| `hcmus-shop.csproj` | AfterBuild target to invoke Obfuscar |

## Implementation

### obfuscar.xml
```xml
<?xml version="1.0"?>
<Obfuscator>
  <Var name="InPath" value="bin\Release\net10.0-windows10.0.19041.0" />
  <Var name="OutPath" value="bin\Release\Obfuscated" />
  <Var name="KeepPublicApi" value="false" />
  <Var name="HidePrivateApi" value="true" />
  <Module file="$(InPath)\hcmus-shop.dll">
    <SkipType name="hcmus_shop.App" />
    <SkipType name="hcmus_shop.MainWindow" />
  </Module>
</Obfuscator>
```

### .csproj target
```xml
<Target Name="Obfuscate" AfterTargets="Build" Condition="'$(Configuration)' == 'Release'">
  <Exec Command="dotnet tool run obfuscar.console obfuscar.xml" />
</Target>
```

## Verification

1. Build in Release: `dotnet build -c Release`
2. Open `bin\Release\Obfuscated\hcmus-shop.dll` in dnSpy
3. Confirm class/method names look like `a()`, `b()`, `_x123()` instead of meaningful names
4. App still runs after obfuscation

## Edge cases

| Case | Behavior |
|------|----------|
| WinUI XAML binding breaks (renames a property the binding needs) | Add to `<SkipType>` or use `<SkipMethod>` |
| Reflection fails (e.g. JSON deserialization) | Mark DTOs to skip property renaming |

## Extension points

- Switch to ConfuserEx for stronger protection (control flow obfuscation)
- Add string encryption
- Add anti-debugging
