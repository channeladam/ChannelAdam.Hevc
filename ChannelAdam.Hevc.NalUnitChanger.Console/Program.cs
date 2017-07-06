using ChannelAdam.Hevc.Processor;
using Microsoft.Extensions.Configuration;
using System;

namespace ChannelAdam.Hevc.NalUnitChanger.Console
{
    public static class Program
    {
        public static void Main(string[] args = null)
        {
            System.Console.WriteLine($"{DateTime.Now.ToString()} - STARTED");

            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            IConfiguration config = builder.Build();

            string inputFile = config.GetValue<string>("in");
            string outputFile = config.GetValue<string>("out");

            var nalUnitProcessorEventHandler = new DefaultNalUnitProcessorEventHandler()
            {
                NewSarWidth = config.GetValue<byte>("sarWidth", 1),
                NewSarHeight = config.GetValue<byte>("sarHeight", 1)
            };
            var h265Processor = new H265BitstreamProcessor(new NalUnitProcessor(nalUnitProcessorEventHandler));
            h265Processor.Process(inputFile, outputFile);

            System.Console.WriteLine($"{DateTime.Now.ToString()} - FINISHED");
        }
    }
}