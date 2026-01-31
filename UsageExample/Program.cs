using Microsoft.Extensions.DependencyInjection.Extensions;
using UsageExample;
using UsageExample.GeneratedTypedConfig;

// initialise host, prepare DI container
var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddTypedConfig();
builder.Services.TryAddTransient<ExampleClass>();

IHost host = builder.Build();

// execute the example
ExampleClass clazz = host.Services.GetRequiredService<ExampleClass>();
clazz.ExampleMethod();




