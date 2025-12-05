// using org.g14.FeatureFlags;

using UsageExample.GeneratedFeatureFlags;
using UsageExample.GeneratedFeatureFlags.TwoWayMessaging.RefreshIntervals;

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
FlagsRootType features = host.Services.GetRequiredService<FlagsRootType>();

// do work
bool b = features.TestBool;
if (features.IncomeSupport.IsEnabled)
{
    Console.WriteLine("Income Support");
    int n = features.IncomeSupport.MagicNumber;
    Console.WriteLine($"    Magic number is {n}");

    if (features.IncomeSupport.Page8.IsEnabled)
    {
        Console.WriteLine("    Page 8");
    }
}

if (features.TwoWayMessaging.IsEnabled)
{
    Console.WriteLine("Two Way Messaging");
    Console.WriteLine("    Refresh intervals:");
    foreach (RefreshIntervalsItemType interval in features.TwoWayMessaging.RefreshIntervals)
    {
        Console.Out.WriteLine($"        {interval.Interval}s, {interval.Repeats} times");
    }
}

// host.Run();

// TODO: DOC: Limitation: all keys must be present in `appsettings.json`. Scanning other files might be added later.
// TODO: DOC: conventions for consumers: if I have a Bagels feature in the project, I should not create a Bagels bool value in appsettings; I should create a Bagels object with a nested IsEnabled bool value
// TODO: DOC: conventions in generator code: 'create' or 'get' means a local operation that doesn't have side effects; 'generate' means adding source code to the compilation
// TODO: analyzer checking that bool values in appsettings have names starting with is/was/has/can/should/allow/enable/use... This is complementary to the naming convention

