# Typed Config

A C# source generator and analyzer for type-safe access to appsettings.

## Quick start
1) Add dependencies to the `.csproj`:
```xml
<ItemGroup>
    <ProjectReference Include="org.g14.TypedConfig" Version="1.0.0" />
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

Add references to the analyzer and generator components in your `.csproj` file.  
The analyzer can be safely omitted.
```xml
<ItemGroup>
    <ProjectReference Include="org.g14.TypedConfig" Version="1.0.0" />
</ItemGroup>
```

### 2. Add configuration root to DI

Register the root of the generated classes. In `Program.cs`:
```csharp
builder.Services.AddTypedConfig();
```
That will register the generated `TypedConfig` class in the DI container.
That's the root of the configuration object.

### 3. Add configuration

Define your configuration in `appsettings.json`. For example:

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

### 3. Use Your Configuration

Access your configuration with full type safety by injecting the root of your appsettings:

```csharp
public class ExampleClass(
    TypedConfig config
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

### Configuration Objects vs Boolean Values

When defining a feature flag or toggleable setting in your configuration, follow this convention:

- ❌ **Don't** create a boolean value directly for a feature:
  ```json
  {
    "Flags": {
      "Bagels": true  // Wrong!
    }
  }
  ```

- ✅ **Do** create an object with a nested `IsEnabled` boolean:
  ```json
  {
    "Flags": {
      "Bagels": {
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
