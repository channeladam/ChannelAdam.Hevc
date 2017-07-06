//-----------------------------------------------------------------------
// <copyright file="DefaultNalUnitProcessorEventHandler.cs">
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

namespace ChannelAdam.Hevc.Processor
{
    public class DefaultNalUnitProcessorEventHandler : INalUnitProcessorEventHandler
    {
        #region Private Fields

        public byte NewSarHeight { get; set; }
        public byte NewSarWidth { get; set; }

        #endregion Private Fields

        #region Public Methods

        public void ProcessAspectRatioVideoUsabilityInformation(NalUnitBitstreamNavigator nav, bool aspect_ratio_info_present_flag, byte aspect_ratio_idc, uint sar_width, uint sar_height)
        {
            const byte SAR_EXTENDED = 255;

            // NOTE: this implementation currently will only CHANGE an existing aspect ratio - not add one if it doesn't exist

            if (aspect_ratio_info_present_flag)
            {
                if (aspect_ratio_idc == SAR_EXTENDED)
                {
                    nav.RewindBits(32);

                    nav.SetBits(NewSarWidth, 16);   //sar_width u(16)
                    nav.SetBits(NewSarHeight, 16);  //sar_height u(16)
                }
            }
        }

        #endregion Public Methods
    }
}