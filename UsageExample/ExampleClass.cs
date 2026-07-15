using UsageExample.GeneratedTypedConfig;
using UsageExample.GeneratedTypedConfig.TwoWayMessaging.RefreshIntervals;

namespace UsageExample;

public class ExampleClass(
    TypedConfig config
)
{
    public void ExampleMethod()
    {
        Console.WriteLine();

        // entry not following naming convention
        bool b = config.TestBool;

        if (config.IncomeSupport.IsEnabled)
        {
            Console.WriteLine("Income Support");
            int n = config.IncomeSupport.MagicNumber;
            Console.WriteLine($"    Magic number is {n}");

            if (config.IncomeSupport.Page8.IsEnabled)
            {
                Console.WriteLine("    Page 8");
            }
        }

        if (config.TwoWayMessaging.IsEnabled)
        {
            Console.WriteLine("Two Way Messaging");
            Console.WriteLine("    Refresh intervals:");
            foreach (RefreshIntervalsItemType interval in config.TwoWayMessaging.RefreshIntervals)
            {
                Console.Out.WriteLine($"        {interval.Interval}s, {interval.Repeats} times");
            }
        }
    }
}