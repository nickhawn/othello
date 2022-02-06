using System;
using System.Collections.Generic;
using CommandLine;
using System.IO;
using System.Net.Sockets;
using ai;
using Newtonsoft.Json;

namespace othello
{
    class Program
    {
        static void Main(string[] args)
            => Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed(MainWithOptions)
                .WithNotParsed(HandleParseError);

        private static void MainWithOptions(CommandLineOptions opts)
        {
            Console.WriteLine($"connecting to {opts.Host}:{opts.Port} ...");
            
            MainLoop(opts.Host, opts.Port);
            
            Console.WriteLine("");

            Console.Out.Flush();
        }

        private static void MainLoop(string host, int port)
        {

            TcpClient client = null;
            try
            {
                client = new TcpClient(host, port);
                
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("\n" + e.Message);
                client?.Dispose();
                return;
            }

            using (var stream = client.GetStream())
            using (var sr = new StreamReader(stream))
            using (var sw = new StreamWriter(stream))
            {
                var keepGoing = true;
                while (keepGoing)
                {
                    var line = sr.ReadLine();

                    if (line == null)
                    {
                        Console.WriteLine("Game over.");
                        keepGoing = false;
                    }
                    else
                    {
                        var gameMessage = JsonConvert.DeserializeObject<GameMessage>(line);

                        var ai = new RemoteAI(gameMessage);
                        var nextMove = ai.GetNextMove();
                        
                        var serialized = JsonConvert.SerializeObject(nextMove);
                        
                        sw.WriteLine(serialized);
                        sw.Flush();
                    }

                } 
            }
            
            client.Dispose();
        }

        private static void HandleParseError(IEnumerable<Error> errors)
        {
            foreach (var error in errors)
            {
                if (error is HelpRequestedError)
                    continue;

                Console.WriteLine("Parse error: " + error);
            }
        }
    }

}
