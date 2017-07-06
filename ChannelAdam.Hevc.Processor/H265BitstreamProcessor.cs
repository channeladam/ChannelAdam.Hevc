//-----------------------------------------------------------------------
// <copyright file="H265BitstreamProcessor.cs">
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

using ChannelAdam.Hevc.Processor.Abstractions;
using System.Collections.Generic;
using System.IO;

namespace ChannelAdam.Hevc.Processor
{
    /// <summary>
    /// Processes an H.265 / HEVC Bitstream.
    /// </summary>
    /// <remarks>
    /// Reference:
    /// H.265(12/16) Approved in 2016-12-22 (http://www.itu.int/rec/T-REC-H.265-201612-I/en)  Article:E 41298  Posted:2017-03-16
    /// Rec. ITU-T H.265 v4 (12/2016)
    /// </remarks>
    public class H265BitstreamProcessor
    {
        #region Private Fields

        private readonly INalUnitProcessor _processor;

        #endregion Private Fields

        #region Public Constructors

        public H265BitstreamProcessor(INalUnitProcessor processor)
        {
            _processor = processor;
        }

        #endregion Public Constructors

        #region Public Methods

        public void Process(string inputFile, string outputFile)
        {
            using (var reader = new BufferedStream(File.OpenRead(inputFile)))
            {
                using (var writer = new BufferedStream(File.OpenWrite(outputFile)))
                {
                    writer.SetLength(0);

                    ProcessRawByteStream(reader, writer);
                }
            }
        }

        public void ProcessRawByteStream(Stream reader, Stream writer)
        {
            const int EndOfStream = -1;

            var nalUnitBytesList = new List<byte>(102400);
            var parser = new NalUnitBitstreamParser(reader);

            parser.InitialiseInputBuffer();

            // Process until the end of the stream
            while (parser.ReadByteIntoInputBuffer() != EndOfStream)
            {
                if (parser.IsNalUnitFound)
                {
                    parser.WriteInputBufferTo(writer);

                    // Extract the NAL Unit
                    nalUnitBytesList.Clear();
                    bool hasMoreDataInInputStream = parser.ExtractNalUnitBytesInto(nalUnitBytesList);

                    // Process the NAL Unit
                    byte[] nalUnitBytes = _processor.ProcessNalUnit(nalUnitBytesList.ToArray());
                    writer.Write(nalUnitBytes, 0, nalUnitBytes.Length);

                    if (!hasMoreDataInInputStream) break;
                }
                else
                {
                    if (parser.IsBufferFull)
                    {
                        parser.WriteOldestInputBufferByteTo(writer);
                    }
                }
            }

            parser.WriteInputBufferTo(writer);
        }

        #endregion Public Methods
    }
}