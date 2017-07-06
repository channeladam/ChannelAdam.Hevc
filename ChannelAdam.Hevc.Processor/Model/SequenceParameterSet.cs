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