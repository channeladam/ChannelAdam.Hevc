using System.Collections.Generic;

namespace ChannelAdam.Hevc.Processor.Model
{
    public class ShortTermRefPicSet
    {
        public bool inter_ref_pic_set_prediction_flag { get; set; }
        public uint delta_idx_minus1 { get; set; }
        public byte delta_rps_sign { get; set; }
        public uint abs_delta_rps_minus1 { get; set; }
        public List<bool> used_by_curr_pic_flag { get; set; } = new List<bool>();
        public List<bool> use_delta_flag { get; set; } = new List<bool>();
        public uint num_negative_pics { get; set; }
        public uint num_positive_pics { get; set; }
        public List<uint> delta_poc_s0_minus1 { get; set; } = new List<uint>();
        public List<bool> used_by_curr_pic_s0_flag { get; set; } = new List<bool>();
        public List<uint> delta_poc_s1_minus1 { get; set; } = new List<uint>();
        public List<bool> used_by_curr_pic_s1_flag { get; set; } = new List<bool>();
    }
}