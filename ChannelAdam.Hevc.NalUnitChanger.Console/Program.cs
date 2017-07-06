//-----------------------------------------------------------------------
// <copyright file="Program.cs">
//     Copyright (c) 2017 Adam Craven. All rights reserved.
// </copyright>
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//-----------------------------------------------------------------------

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