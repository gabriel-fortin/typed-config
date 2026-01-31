using UsageExample.GeneratedTypedConfig;
using UsageExample.GeneratedTypedConfig.Flags.TwoWayMessaging.RefreshIntervals;

namespace UsageExample;

public class ExampleClass(
    TypedConfig config
)
{
    public void ExampleMethod()
    {
        Console.WriteLine();

        // entry not following naming convention
        bool b = config.Flags.TestBool;

        if (config.Flags.IncomeSupport.IsEnabled)
        {
            Console.WriteLine("Income Support");
            int n = config.Flags.IncomeSupport.MagicNumber;
            Console.WriteLine($"    Magic number is {n}");

            if (config.Flags.IncomeSupport.Page8.IsEnabled)
            {
                Console.WriteLine("    Page 8");
            }
        }

        if (config.Flags.TwoWayMessaging.IsEnabled)
        {
            Console.WriteLine("Two Way Messaging");
            Console.WriteLine("    Refresh intervals:");
            foreach (RefreshIntervalsItemType interval in config.Flags.TwoWayMessaging.RefreshIntervals)
            {
                Console.Out.WriteLine($"        {interval.Interval}s, {interval.Repeats} times");
            }
        }
    }
}