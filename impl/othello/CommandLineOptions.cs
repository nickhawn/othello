using CommandLine;
using CommandLine.Text;
using System.Collections.Generic;

namespace othello
{
    public class CommandLineOptions
    {
        private const int DefaultPort = 1338;

        [Option('h', "host", Required = false, HelpText = "Host to connect to", Default = "localhost")]
        public string Host { get; set; }

        [Option('p', "port", Required = false, HelpText = "Port to connect on", Default = DefaultPort)]
        public int Port { get; set; }

        [Usage(ApplicationAlias = "dotnet run --")]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example($"Connect to localhost:{DefaultPort}", new CommandLineOptions());
                yield return new Example("Connect to remote.com:12345", new CommandLineOptions { Host = "remote.com", Port = 54321 });
            }
        }
    }
}
