// using org.g14.FeatureFlags;
using org.g14.UsageExample;

var builder = Host.CreateApplicationBuilder(args);

// builder.Services.AddSingleton<IFeatureManager, FeatureManager>();

var host = builder.Build();

// IFeatureManager features = host.Services.GetService<IFeatureManager>()
//     ?? throw new InvalidOperationException("FeatureManager could not be retrieved from DI container");

Console.WriteLine("Hello, World!");

/*
if (features["TwoWayMessaging"].IsEnabled)
{
    Console.WriteLine("Two way messaging");
}

if (features["IncomeSupport"].IsEnabled)
{
    Console.WriteLine("Income Support");
    int n = features["IncomeSupport"].Get<int>("MagicNumber");
    Console.WriteLine($"     -- magic number is {n}");

    if (features["IncomeSupport.Page8"].IsEnabled)
    {
        Console.WriteLine("Page 8 of Income Support");
    }
}
*/

// namespaces/classes for when we generate:
// ...FeatureFlags.FlagsRoot
// ...FeatureFlags.FlagsRoot.TwoWayMessagingFlags
// ...FeatureFlags.FlagsRoot.IncomeSupportFlags
// ...FeatureFlags.FlagsRoot.IncomeSupportFlags.Page8Flags

// temporary: this will eventually be generated; located in .AddGeneratedFeatureFlags() service collection extension
FlagsRoot features = host.Services.GetRequiredService<FlagsRoot>();

features.TestBool = false;
bool b = features.TestBool;
features.TestBool = false; // TODO: assigning should not be possible (introducing the interfaces can fix that)
/*
if (features.IncomeSupport.IsEnabled)
{
    Console.WriteLine("Income Support");
    int n = features.IncomeSupport.MagicNumber;
    Console.WriteLine($"     -- magic number is {n}");

    if (features.IncomeSupport.Page8.IsEnabled)
    {
        Console.WriteLine("Page 8 of Income Support");
    }
}
*/

// host.Run();

// TODO: DOC: Limitation: all keys must be present in `appsettings.json`. Scanning other files might be added later.
// TODO: generate the IFeatureManager interface; generate interfaces for all classes we want exposed to the dev
