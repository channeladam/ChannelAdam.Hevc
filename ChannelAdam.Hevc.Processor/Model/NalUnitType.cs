//-----------------------------------------------------------------------
// <copyright file="NalUnitType.cs">
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

namespace ChannelAdam.Hevc.Processor.Model
{
    public enum NalUnitType
    {
        NAL_TRAIL_N = 0,
        NAL_TRAIL_R = 1,
        NAL_TSA_N = 2,
        NAL_TSA_R = 3,
        NAL_STSA_N = 4,
        NAL_STSA_R = 5,
        NAL_RADL_N = 6,
        NAL_RADL_R = 7,
        NAL_RASL_N = 8,
        NAL_RASL_R = 9,
        NAL_BLA_W_LP = 16,
        NAL_BLA_W_RADL = 17,
        NAL_BLA_N_LP = 18,
        NAL_IDR_W_RADL = 19,
        NAL_IDR_N_LP = 20,
        NAL_CRA_NUT = 21,
        NAL_IRAP_VCL23 = 23,
        NAL_VPS = 32,
        NAL_SPS = 33,
        NAL_PPS = 34,
        NAL_AUD = 35,
        NAL_EOS_NUT = 36,
        NAL_EOB_NUT = 37,
        NAL_FD_NUT = 38,
        NAL_SEI_PREFIX = 39,
        NAL_SEI_SUFFIX = 40,
    };
}