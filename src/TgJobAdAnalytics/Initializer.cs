using System.Runtime.CompilerServices;
using System.Text;

namespace TgJobAdAnalytics;

internal static class Initializer
{
    [ModuleInitializer]
    internal static void Init()
    {
        Console.OutputEncoding = Encoding.UTF8;
    }
}
