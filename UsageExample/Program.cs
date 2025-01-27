using org.g14.FeatureFlags;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<FeatureManager>();

var host = builder.Build();

FeatureManager feature = host.Services.GetService<FeatureManager>()
    ?? throw new InvalidOperationException("FeatureManager could not be retrieved from DI container");

Console.WriteLine("Hello, World!");

if (feature["TwoWayMessaging"].IsEnabled)
{
    Console.WriteLine("Two way messaging");
}

if (feature["IncomeSupport"].IsEnabled)
{
    Console.WriteLine("Income Support");

    if (feature["IncomeSupport.Page8"].IsEnabled)
    {
        Console.WriteLine("Page 8 of Income Support");
    }
}

// host.Run();