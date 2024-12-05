using System.Diagnostics;
using CliFx;
using Spoofy.Handlers;

namespace Spoofy;

public class Program
{
    public static async Task<int> Main() =>
        await new CliApplicationBuilder().AddCommandsFromThisAssembly().Build().RunAsync();
}
