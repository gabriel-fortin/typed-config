# Typed Config

A C# source generator and analyzer for type-safe access to appsettings.

## Quick start
1) Add dependencies to the `.csproj`:
```xml
<ItemGroup>
    <PackageReference Include="org.g14.TypedConfig" Version="1.0.0" />
</ItemGroup>
```

2) Register the root of the generated classes in `Program.cs`:  
```csharp
builder.Services.AddTypedConfig();
```

3) Inject `TypedConfig` where needed.

## Overview

This project provides a source generator that creates strongly-typed classes representing entries from your 
`appsettings.json` file, along with Roslyn analyzers to enforce naming conventions and best practices.

## Features

- **Type-Safe Configuration**: Access your configuration with compile-time type-safety
- **Source Generator**: Automatically generates C# classes from your `appsettings.json`
- **Roslyn Analyzers**: Enforce naming conventions for boolean values
- **Dependency Injection Support**: Extension method for .NET's DI container
- **Nested Configuration**: Support for hierarchical configuration structures

## Getting Started

### 1. Add dependencies to the project file

If you are using the nuget package, there is just one dependency to add to your `.csproj`:
```xml
<ItemGroup>
    <PackageReference Include="org.g14.TypedConfig" Version="1.0.0" />
</ItemGroup>
```
That will contain both the generator and the analyzer.

If you want to use project references, a slightly longer form is needed in your `.csproj` file:  
```xml
<ItemGroup>
    <ProjectReference Include="..\TypedConfig.Analyzer\TypedConfig.Analyzer.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    <ProjectReference Include="..\TypedConfig.Generator\TypedConfig.Generator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
    <!--  using ReferenceOutputAssembly="false" prevents generator code from being added to runtime DLLs -->
</ItemGroup>
```
(The analyzer reference can be safely omitted)

### 2. Make appsettings available in the DI container

Register the root of the generated classes. In `Program.cs`:
```csharp
builder.Services.AddTypedConfig();
```
That will register the generated `TypedConfig` class in the DI container. 
That's the root of the configuration object.

### 3. Add configuration

Define your configuration in `appsettings.json` as usual. For example:

```json
{
  "Flags": {
    "TestBool": true,
    "IncomeSupport": {
      "IsEnabled": true,
      "MagicNumber": 42,
      "Page8": {
        "IsEnabled": true
      }
    },
    "TwoWayMessaging": {
      "IsEnabled": true,
      "RefreshIntervals": [
        {
          "Interval": 3,
          "Repeats": 8
        }
      ]
    }
  }
}
```

### 4. Use Your Configuration

Access your configuration with full type safety by injecting the root type for your appsettings:

```csharp
public class ExampleClass(
    TypedConfig config  // injected appsettings
)
{
    public void ExampleMethod()
    {
        // Simple boolean values
        bool isTest = config.Flags.TestBool; // analyzer reports naming convention violation
        
        // Nested configuration
        if (config.Flags.IncomeSupport.IsEnabled)
        {
            int magicNumber = config.Flags.IncomeSupport.MagicNumber;
            
            if (config.Flags.IncomeSupport.Page8.IsEnabled)
            {
                // Do something
            }
        }
        
        // Arrays
        if (config.Flags.TwoWayMessaging.IsEnabled)
        {
            foreach (RefreshIntervalsItemType interval in config.Flags.TwoWayMessaging.RefreshIntervals)
            {
                Console.WriteLine($"{interval.Interval}s, {interval.Repeats} times");
            }
        }
    }
}
```

## Conventions

You do you babe but I recommend the following conventions.

### Keep boolean flags separate

Keep binary switches (flags) in a section named `Flags`, in your appsettings.  
For example:
```json
{
  "SomeOtherSettings": { ... },
  "Flags": {
    "RemoteCalls": {
      "AllowCaching": true,
      "UsePooledConnections": false
    }
  }
}
```

### Configuration Objects vs Boolean Values

When defining a feature flag or toggle-able setting in your configuration, follow this convention:

- ❌ **Don't** create a boolean value directly for a feature:
  ```json
  {
    "Flags": {
      "NewFeature": true  // Wrong!
    }
  }
  ```

- ✅ **Do** create an object with a nested `IsEnabled` boolean:
  ```json
  {
    "Flags": {
      "NewFeature": {
        "IsEnabled": true  // Correct!
      }
    }
  }
  ```

This convention allows you to easily extend features with additional configuration properties later.

### Boolean Naming Convention

The included Roslyn analyzer automatically checks your `appsettings.json` file and enforces that boolean values within the `Flags` section follow naming conventions, starting with prefixes like:

- `Is...` (e.g., `IsEnabled`)
- `Was...`
- `Has...`
- `Can...`
- `Should...`
- `Allow...`
- `Enable...`
- `Use...`

This helps maintain consistency and readability across your configuration.

## Internal code conventions in this repository

Internal code conventions used in the generator:

- **`create` or `get`**: Methods that perform local operations without side effects
- **`generate`**: Methods that add source code to the compilation pipeline

## Limitations

### Current Limitations

- **Single Configuration File**: Currently, all keys must be present in `appsettings.json`. Support for scanning other configuration files (e.g., `appsettings.Development.json`) may be added in the future.

## Project Structure

- **FeatureFlags**: (obsolete) Previous string-based, untyped implementation of feature flags
- **TypedConfig.Analyzer**: Roslyn analyzer for enforcing naming conventions
- **TypedConfig.Generator**: Source generator for creating type-safe configuration classes
- **TypedConfig.Generator.Tests**: Test suite for the generator
- **UsageExample**: Example project demonstrating usage

