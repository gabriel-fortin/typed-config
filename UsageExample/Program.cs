// using org.g14.FeatureFlags;

using UsageExample.GeneratedTypedConfig;
using UsageExample.GeneratedTypedConfig.FeatureFlags.TwoWayMessaging.RefreshIntervals;

Console.WriteLine();

/*
// initialise host, DI container etc.
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<IFeatureManager, FeatureManager>();
var host = builder.Build();

// retrieve dependency from DI container
IFeatureManager features = host.Services.GetService<IFeatureManager>()
    ?? throw new InvalidOperationException("FeatureManager could not be retrieved from DI container");

// do work
if (features["IncomeSupport"].IsEnabled)
{
    Console.WriteLine("Income Support");
    int n = features["IncomeSupport"].Get<int>("MagicNumber");
    Console.WriteLine($"    Magic number is {n}");

    if (features["IncomeSupport.Page8"].IsEnabled)
    {
        Console.WriteLine("    Page");
    }
}

if (features["TwoWayMessaging"].IsEnabled)
{
    Console.WriteLine("Two way messaging");
}
*/


// initialise host, DI container etc.
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddGeneratedFeatureFlags();
var host = builder.Build();

// retrieve dependency from DI container
TypedConfig config = host.Services.GetRequiredService<TypedConfig>();

// do work
bool b = config.FeatureFlags.TestBool;
if (config.FeatureFlags.IncomeSupport.IsEnabled)
{
    Console.WriteLine("Income Support");
    int n = config.FeatureFlags.IncomeSupport.MagicNumber;
    Console.WriteLine($"    Magic number is {n}");

    if (config.FeatureFlags.IncomeSupport.Page8.IsEnabled)
    {
        Console.WriteLine("    Page 8");
    }
}

if (config.FeatureFlags.TwoWayMessaging.IsEnabled)
{
    Console.WriteLine("Two Way Messaging");
    Console.WriteLine("    Refresh intervals:");
    foreach (RefreshIntervalsItemType interval in config.FeatureFlags.TwoWayMessaging.RefreshIntervals)
    {
        Console.Out.WriteLine($"        {interval.Interval}s, {interval.Repeats} times");
    }
}

// host.Run();


