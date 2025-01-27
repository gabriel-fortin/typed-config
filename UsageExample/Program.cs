using org.g14.FeatureFlags;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<FeatureManager>();

var host = builder.Build();

FeatureManager features = host.Services.GetService<FeatureManager>()
    ?? throw new InvalidOperationException("FeatureManager could not be retrieved from DI container");

Console.WriteLine("Hello, World!");

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

// host.Run();