//-----------------------------------------------------------------------
// <copyright file="NalUnitBitstreamParser.cs">
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

using System.Collections.Generic;
using System.IO;

namespace ChannelAdam.Hevc.Processor
{
    /// <summary>
    /// Parses the bitstream of an H.265 / HEVC Bitstream.
    /// </summary>
    /// <remarks>
    /// Reference:
    /// H.265(12/16) Approved in 2016-12-22 (http://www.itu.int/rec/T-REC-H.265-201612-I/en)  Article:E 41298  Posted:2017-03-16
    /// Rec. ITU-T H.265 v4 (12/2016)
    /// </remarks>
    public class NalUnitBitstreamParser
    {
        #region Private Fields

        private const int NalUnitStartSequenceMaxLength = 4;

        private Stream _reader;

        #endregion Private Fields

        #region Public Constructors

        public NalUnitBitstreamParser(Stream reader)
        {
            _reader = reader;
        }

        #endregion Public Constructors

        #region Public Properties

        public int BufferIndex { get; set; }
        public int CountOfBufferedBytes { get; set; } = 0;
        public byte[] InputBuffer { get; set; } = new byte[NalUnitStartSequenceMaxLength];
        public bool IsBufferFull => CountOfBufferedBytes >= NalUnitStartSequenceMaxLength;

        /// <summary>
        /// Determines if the start of a NAL Unit has been found.
        /// </summary>
        /// <remarks>
        /// Reference:
        /// B.2.1 Byte stream NAL unit syntax - and - B.3 Byte stream NAL unit decoding process
        /// while( next_bits( 24 ) != 0x000001 &amp;&amp; next_bits( 32 ) != 0x00000001 )
        /// </remarks>
        public bool IsNalUnitFound => (InputBuffer[0] == 0x0 && InputBuffer[1] == 0x0 && InputBuffer[2] == 0x1)
                   || (IsBufferFull && InputBuffer[0] == 0x0 && InputBuffer[1] == 0x0 && InputBuffer[2] == 0x0 && InputBuffer[3] == 0x1);

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        /// Extract the bytes of the NAL Unit into the given collection
        /// </summary>
        /// <param name="nalUnitBytes"></param>
        /// <remarks>
        /// Reference: B.3 Byte stream NAL unit decoding process
        ///
        /// NumBytesInNalUnit is set equal to the number of bytes starting with the byte at the current position in the byte
        ///   stream up to and including the last byte that precedes the location of one or more of the following conditions:
        ///     – A subsequent byte-aligned three-byte sequence equal to 0x000000,
        ///     – A subsequent byte-aligned three-byte sequence equal to 0x000001,
        ///     – The end of the byte stream, as determined by unspecified means.
        /// </remarks>
        /// <returns>True if there is more data remaining in the input stream.</returns>
        public bool ExtractNalUnitBytesInto(IList<byte> nalUnitBytes)
        {
            int nextByte;
            byte one = 0,
                 two = 0,
                 three = 0;

            nextByte = _reader.ReadByte();
            if (nextByte == -1) return false;
            one = (byte)nextByte;
            nalUnitBytes.Add(one);

            nextByte = _reader.ReadByte();
            if (nextByte == -1) return false;
            two = (byte)nextByte;
            nalUnitBytes.Add(two);

            while ((nextByte = _reader.ReadByte()) != -1)
            {
                three = (byte)nextByte;

                if (one == 0x0 && two == 0x0 && (three == 0x0 || three == 0x1))
                {
                    // Remove the previous two bytes, as the are the end sequence and are not part of the Nal Unit payload
                    nalUnitBytes.RemoveAt(nalUnitBytes.Count - 1);
                    nalUnitBytes.RemoveAt(nalUnitBytes.Count - 1);

                    InputBuffer[0] = one;
                    InputBuffer[1] = two;
                    InputBuffer[2] = three;
                    InputBuffer[3] = 0x0;
                    BufferIndex = 3;
                    CountOfBufferedBytes = 3;

                    return true;    // we did not reach the end of the input stream
                }

                nalUnitBytes.Add(three);

                // Prepare for next iteration
                one = two;
                two = three;
            }

            // No more data - no final 3-byte sequence
            return false;
        }

        public void InitialiseInputBuffer()
        {
            // Initialise the input buffer with the first 2 bytes
            CountOfBufferedBytes = _reader.Read(InputBuffer, 0, 2);
            BufferIndex = 2;
        }

        public int ReadByteIntoInputBuffer()
        {
            int result = _reader.ReadByte();
            if (result != -1)
            {
                InputBuffer[BufferIndex] = (byte)result;
                CountOfBufferedBytes++;

                if (BufferIndex == 2)
                {
                    BufferIndex = 3;
                }
            }

            return result;
        }

        public void WriteInputBufferTo(Stream writer)
        {
            if (CountOfBufferedBytes > 0)
            {
                writer.Write(InputBuffer, 0, CountOfBufferedBytes);
                CountOfBufferedBytes = 0;

                InputBuffer[0] = 0;
                InputBuffer[1] = 0;
                InputBuffer[2] = 0;
                InputBuffer[3] = 0;
            }
        }

        public void WriteOldestInputBufferByteTo(Stream writer)
        {
            writer.WriteByte(InputBuffer[0]);
            CountOfBufferedBytes--;

            InputBuffer[0] = InputBuffer[1];
            InputBuffer[1] = InputBuffer[2];
            InputBuffer[2] = InputBuffer[3];
            InputBuffer[3] = 0;
        }

        #endregion Public Methods
    }

    /*
     Annex E - Video usability information  (p370)
     ==============================================

     E.3.1 VUI parameters semantics
     ------------------------------
     aspect_ratio_info_present_flag equal to 1 specifies that aspect_ratio_idc is present.
     aspect_ratio_info_present_flag equal to 0 specifies that aspect_ratio_idc is not present.
     aspect_ratio_idc specifies the value of the sample aspect ratio of the luma samples.
     Table E.1 shows the meaning of the code.
     When aspect_ratio_idc indicates EXTENDED_SAR, the sample aspect ratio is represented by sar_width: sar_height.
     When the aspect_ratio_idc syntax element is not present, the value of aspect_ratio_idc is inferred to be equal to 0.
     Values of aspect_ratio_idc in the range of 17 to 254, inclusive, are reserved for future use by ITU-T | ISO/IEC and shall not be
     present in bitstreams conforming to this version of this Specification.
     Decoders shall interpret values of aspect_ratio_idc in the range of 17 to 254, inclusive, as equivalent to the value 0.

     Table E.1 – Interpretation of sample aspect ratio indicator
     aspect_ratio_idc    Sample aspect ratio   Examples of use (informative)
         0 Unspecified
         1 1:1 ("square")    7680x4320 16:9 frame without horizontal overscan
                             3840x2160 16:9 frame without horizontal overscan
                             1280x720 16:9 frame without horizontal overscan
                             1920x1080 16:9 frame without horizontal overscan (cropped from 1920x1088)
                             640x480 4:3 frame without horizontal overscan
         2 12:11 720x576 4:3 frame with horizontal overscan
                 352x288 4:3 frame without horizontal overscan
         3 10:11 720x480 4:3 frame with horizontal overscan
                 352x240 4:3 frame without horizontal overscan
         4 16:11 720x576 16:9 frame with horizontal overscan
                 528x576 4:3 frame without horizontal overscan
         5 40:33 720x480 16:9 frame with horizontal overscan
                 528x480 4:3 frame without horizontal overscan
         6 24:11 352x576 4:3 frame without horizontal overscan
                 480x576 16:9 frame with horizontal overscan
         7 20:11 352x480 4:3 frame without horizontal overscan
                 480x480 16:9 frame with horizontal overscan
         8 32:11 352x576 16:9 frame without horizontal overscan
         9 80:33 352x480 16:9 frame without horizontal overscan
         10 18:11 480x576 4:3 frame with horizontal overscan
         11 15:11 480x480 4:3 frame with horizontal overscan
         12 64:33 528x576 16:9 frame without horizontal overscan
         13 160:99 528x480 16:9 frame without horizontal overscan
         14 4:3 1440x1080 16:9 frame without horizontal overscan
         15 3:2 1280x1080 16:9 frame without horizontal overscan
         16 2:1 960x1080 16:9 frame without horizontal overscan
         17..254 Reserved
         255 EXTENDED_SAR

     NOTE 1 – For the examples in Table E.1, the term "without horizontal overscan" refers to display processes in which the display
     area matches the area of the cropped decoded pictures and the term "with horizontal overscan" refers to display processes in which
     some parts near the left or right border of the cropped decoded pictures are not visible in the display area.
     As an example, the entry "720x576 4:3 frame with horizontal overscan" for aspect_ratio_idc equal to 2 refers to having an area of 704x576 luma samples
     (which has an aspect ratio of 4:3) of the cropped decoded frame (720x576 luma samples) that is visible in the display area.

     NOTE 2 – For the examples in Table E.1, the frame spatial resolutions shown as examples of use would be the dimensions of the
     conformance cropping window when field_seq_flag is equal to 0 and would have twice the height of the dimensions of the
     conformance cropping window when field_seq_flag is equal to 1.

     sar_width indicates the horizontal size of the sample aspect ratio (in arbitrary units).
     sar_height indicates the vertical size of the sample aspect ratio (in the same arbitrary units as sar_width).
     sar_width and sar_height shall be relatively prime or equal to 0. When aspect_ratio_idc is equal to 0 or sar_width is equal
         to 0 or sar_height is equal to 0, the sample aspect ratio is unspecified in this Specification.
     overscan_info_present_flag equal to 1 specifies that the overscan_appropriate_flag is present.
         When overscan_info_present_flag is equal to 0 or is not present, the preferred display method for the video signal is unspecified.
     overscan_appropriate_flag equal to 1 indicates that the cropped decoded pictures output are suitable for display using overscan.
     overscan_appropriate_flag equal to 0 indicates that the cropped decoded pictures output contain visually
         important information in the entire region out to the edges of the conformance cropping window of the picture, such that
         the cropped decoded pictures output should not be displayed using overscan. Instead, they should be displayed using either
         an exact match between the display area and the conformance cropping window, or using underscan.
     As used in this paragraph, the term "overscan" refers to display processes in which some parts near the borders of the cropped decoded
     pictures are not visible in the display area.
     The term "underscan" describes display processes in which the entire cropped decoded pictures are visible in the display area,
         but they do not cover the entire display area. For display processes that
         neither use overscan nor underscan, the display area exactly matches the area of the cropped decoded pictures.

     NOTE 3 – For example, overscan_appropriate_flag equal to 1 might be used for entertainment television programming, or for a live
     view of people in a videoconference and overscan_appropriate_flag equal to 0 might be used for computer screen capture or security
     camera content.

     E.2.2 HRD parameters syntax
     ----------------------------
     hrd_parameters( commonInfPresentFlag, maxNumSubLayersMinus1 ) { Descriptor
         if( commonInfPresentFlag ) {
             nal_hrd_parameters_present_flag u(1)
             vcl_hrd_parameters_present_flag u(1)
             if( nal_hrd_parameters_present_flag | | vcl_hrd_parameters_present_flag ) {
                 sub_pic_hrd_params_present_flag u(1)
                 if( sub_pic_hrd_params_present_flag ) {
                     tick_divisor_minus2 u(8)
                     du_cpb_removal_delay_increment_length_minus1 u(5)
                     sub_pic_cpb_params_in_pic_timing_sei_flag u(1)
                     dpb_output_delay_du_length_minus1 u(5)
                 }
                 bit_rate_scale u(4)
                 cpb_size_scale u(4)
                 if( sub_pic_hrd_params_present_flag )
                     cpb_size_du_scale u(4)
                 initial_cpb_removal_delay_length_minus1 u(5)
                 au_cpb_removal_delay_length_minus1 u(5)
                 dpb_output_delay_length_minus1 u(5)
             }
         }
         for( i = 0; i <= maxNumSubLayersMinus1; i++ ) {
             fixed_pic_rate_general_flag[ i ] u(1)
             if( !fixed_pic_rate_general_flag[ i ] )
                 fixed_pic_rate_within_cvs_flag[ i ] u(1)
             if( fixed_pic_rate_within_cvs_flag[ i ] )
                 elemental_duration_in_tc_minus1[ i ] ue(v)
             else
                 low_delay_hrd_flag[ i ] u(1)
             if( !low_delay_hrd_flag[ i ] )
                 cpb_cnt_minus1[ i ] ue(v)
             if( nal_hrd_parameters_present_flag )
                 sub_layer_hrd_parameters( i )
             if( vcl_hrd_parameters_present_flag )
                 sub_layer_hrd_parameters( i )
         }
     }

     E.2.3 Sub-layer HRD parameters syntax
     --------------------------------------
     sub_layer_hrd_parameters( subLayerId ) { Descriptor
         for( i = 0; i <= CpbCnt; i++ ) {
             bit_rate_value_minus1[ i ] ue(v)
             cpb_size_value_minus1[ i ] ue(v)
             if( sub_pic_hrd_params_present_flag ) {
                 cpb_size_du_value_minus1[ i ] ue(v)
                 bit_rate_du_value_minus1[ i ] ue(v)
             }
             cbr_flag[ i ] u(1)
         }
     }
 */
}