//-----------------------------------------------------------------------
// <copyright file="SequenceParameterSet.cs">
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

namespace ChannelAdam.Hevc.Processor.Model
{
    public class SequenceParameterSet
    {
        public byte sps_max_sub_layers_minus1 { get; set; }
        public uint[] sps_max_dec_pic_buffering_minus1 { get; set; }

        public uint num_short_term_ref_pic_sets { get; set; }
        public List<ShortTermRefPicSet> ShortTermRefPicSets { get; set; } = new List<ShortTermRefPicSet>();
    }
}