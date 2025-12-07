# Feature Flags Tools

A C# source generator and analyzer toolkit for type-safe feature flags management.

## Overview

This project provides a source generator that creates strongly-typed feature flag classes from your 
`appsettings.json` configuration, along with Roslyn analyzers to enforce naming conventions and best practices.

## Features

- **Type-Safe Feature Flags**: Access your feature flags with compile-time type safety
- **Source Generator**: Automatically generates C# classes from your `appsettings.json`
- **Roslyn Analyzers**: Enforces naming conventions for boolean values
- **Dependency Injection Support**: Easy integration with .NET's DI container
- **Nested Configuration**: Support for hierarchical feature flag structures

## Getting Started

### 1. Configure Your Feature Flags

Define your feature flags in `appsettings.json`:

```json
{
  "FeatureFlags": {
    "HasTestBool": true,
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

### 2. Register Feature Flags with DI

In your `Program.cs`:

```csharp
builder.Services.AddGeneratedFeatureFlags();
```

### 3. Use Your Feature Flags

Access your feature flags with full type safety:

```csharp
FlagsRootType features = InjectedInConstructor();

// Simple boolean flags
bool isTest = features.HasTestBool;

// Nested feature flags with properties
if (features.IncomeSupport.IsEnabled)
{
    int magicNumber = features.IncomeSupport.MagicNumber;
    
    if (features.IncomeSupport.Page8.IsEnabled)
    {
        // Do something
    }
}

// Array configurations
if (features.TwoWayMessaging.IsEnabled)
{
    foreach (RefreshIntervalsItemType interval in features.TwoWayMessaging.RefreshIntervals)
    {
        Console.WriteLine($"{interval.Interval}s, {interval.Repeats} times");
    }
}
```

## Conventions

### For Consumers

**Feature Flag Objects vs Boolean Values**

When defining a feature flag in your project, follow this convention:

- ❌ **Don't** create a boolean value directly for a feature:
  ```json
  {
    "FeatureFlags": {
      "Bagels": true  // Wrong!
    }
  }
  ```

- ✅ **Do** create an object with a nested `IsEnabled` boolean:
  ```json
  {
    "FeatureFlags": {
      "Bagels": {
        "IsEnabled": true  // Correct!
      }
    }
  }
  ```

This convention allows you to easily extend features with additional configuration properties later.

### For Generator Code

Internal code conventions used in the generator:

- **`create` or `get`**: Methods that perform local operations without side effects
- **`generate`**: Methods that add source code to the compilation pipeline

### Boolean Naming Convention

The included Roslyn analyzer automatically checks your `appsettings.json` file and enforces that boolean values within the `FeatureFlags` section follow naming conventions, starting with prefixes like:

- `Is...` (e.g., `IsEnabled`)
- `Was...`
- `Has...`
- `Can...`
- `Should...`
- `Allow...`
- `Enable...`
- `Use...`

This helps maintain consistency and readability across your configuration.

## Limitations

### Current Limitations

- **Single Configuration File**: Currently, all keys must be present in `appsettings.json`. Support for scanning other configuration files (e.g., `appsettings.Development.json`) may be added in the future.

## Project Structure

- **FeatureFlags**: (obsolete) Previous string-based, untyped implementation of feature flags
- **FeatureFlags.Analyzer**: Roslyn analyzer for enforcing naming conventions
- **FeatureFlags.Generation**: Source generator for creating type-safe feature flag classes
- **FeatureFlags.Generation.Tests**: Test suite for the generator
- **UsageExample**: Example project demonstrating usage

