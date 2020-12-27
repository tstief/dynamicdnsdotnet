using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace dynamicdnsdotnet
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                try
                {
                    var workingDirectory = args[0];
                    var jsonString = File.ReadAllText(Path.Combine(workingDirectory, "config.json"));
                    var config = JsonSerializer.Deserialize<Configuration>(jsonString);

                    var manager = new DynamicDnsManagerDotNet(workingDirectory, config);
                    manager.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine("No configuration file path argument.");
            }
        }
    }
}

