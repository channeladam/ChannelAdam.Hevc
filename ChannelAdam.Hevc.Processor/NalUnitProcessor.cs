using ChannelAdam.Hevc.Processor.Abstractions;
using ChannelAdam.Hevc.Processor.Model;
using System;
using System.Collections.Generic;

namespace ChannelAdam.Hevc.Processor
{
    public class NalUnitProcessor : INalUnitProcessor
    {
        #region Private Fields

        private INalUnitProcessorEventHandler _eventHandler;

        #endregion Private Fields

        #region Public Constructors

        public NalUnitProcessor(INalUnitProcessorEventHandler eventHandler)
        {
            _eventHandler = eventHandler;
        }

        #endregion Public Constructors

        #region Public Methods

        /// <summary>
        /// Processes the given NAL Unit bytes.
        /// </summary>
        /// <param name="nalUnitBytes"></param>
        /// <returns>A potentially modified set of bytes for the NAL Unit.</returns>
        public byte[] ProcessNalUnit(byte[] nalUnitBytes)
        {
            NalUnitType nalUnitType = DetermineNalUnitType(nalUnitBytes);

            if (nalUnitType == NalUnitType.NAL_SPS)
            {
                nalUnitBytes = ProcessSequenceParameterSetNalUnit(nalUnitBytes);
            }

            return nalUnitBytes;
        }

        #endregion Public Methods

        #region Private Methods

        private uint CalculateNumDeltaPictureOrderCounts(IList<ShortTermRefPicSet> refPicSets, uint stRpsIdx, ShortTermRefPicSet result)
        {
            uint numDeltaPocs = 0;
            int refRpsIdx = (int)(stRpsIdx - (result.delta_idx_minus1 + 1));
            if (refPicSets[refRpsIdx].inter_ref_pic_set_prediction_flag)
            {
                for (int i = 0; i < refPicSets[refRpsIdx].used_by_curr_pic_flag.Count; i++)
                {
                    if (refPicSets[refRpsIdx].used_by_curr_pic_flag[i] || refPicSets[refRpsIdx].use_delta_flag[i])
                    {
                        numDeltaPocs++;
                    }
                }
            }
            else
            {
                numDeltaPocs = refPicSets[refRpsIdx].num_negative_pics + refPicSets[refRpsIdx].num_positive_pics;
            }

            return numDeltaPocs;
        }

        private NalUnitType DetermineNalUnitType(byte[] nalUnitBytes)
        {
            /*
                https://tools.ietf.org/html/rfc7798

                1.1.4 - NAL Unit Header
                ========================
                HEVC uses a two-byte NAL unit header, as shown in Figure 1.
                The payload of a NAL unit refers to the NAL unit excluding the NAL unit header.

                +---------------+---------------+
                |0|1|2|3|4|5|6|7|0|1|2|3|4|5|6|7|
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |F|   Type    |  LayerId  | TID |
                +-------------+-----------------+

                F: 1 bit
                  forbidden_zero_bit.  Required to be zero in [HEVC].  Note that the inclusion of this bit in the NAL unit header was to enable
                  transport of HEVC video over MPEG-2 transport systems (avoidance of start code emulations) [MPEG2S].
                  In the context of this memo, the value 1 may be used to indicate a syntax violation.

                Type: 6 bits
                  nal_unit_type.  This field specifies the NAL unit type as defined in Table 7-1 of [HEVC].
                  If the most significant bit of this field of a NAL unit is equal to 0 (i.e., the value of this field is less than 32), the NAL unit is a VCL NAL unit.
                  Otherwise, the NAL unit is a non-VCL NAL unit.
                  For a reference of all currently defined NAL unit types and their semantics, please refer to Section 7.4.2 in [HEVC].

                LayerId: 6 bits
                  nuh_layer_id.  Required to be equal to zero in [HEVC].
                  It is anticipated that in future scalable or 3D video coding extensions of this specification, this syntax element will be used to
                  identify additional layers that may be present in the CVS, wherein a layer may be, e.g., a spatial scalable layer, a quality scalable
                  layer, a texture view, or a depth view.

                TID: 3 bits
                  nuh_temporal_id_plus1.  This field specifies the temporal identifier of the NAL unit plus 1.
                  The value of TemporalId is equal to TID minus 1.  A TID value of 0 is illegal to ensure that there is at least one bit in the NAL unit header equal to 1, so to
                  enable independent considerations of start code emulations in the NAL unit header and in the NAL unit payload data.
            */
            // Bit 0 = Forbidden 0 bit
            // Bits 1-6 are the NAL Unit type
            // Bit 7 is the start of the Layer Id
            int nalUnitType = (nalUnitBytes[0] & 0b0_111111_0) >> 1;
            return (NalUnitType)nalUnitType;
        }

        private void ProcessProfileTierLevel(NalUnitBitstreamNavigator nav, bool isProfilePresent, byte maxNumSubLayersMinus1)
        {
            /////////////////
            // Profile
            /////////////////
            if (isProfilePresent)
            {
                nav.SkipBits(2); //general_profile_space u(2)
                nav.SkipBit();    //general_tier_flag u(1)

                byte general_profile_idc = nav.ReadBitsAsByte(5); //general_profile_idc u(5)

                bool[] general_profile_compatibility_flag = new bool[32];
                for (int j = 0; j < 32; j++)
                {
                    general_profile_compatibility_flag[j] = nav.ReadBit(); //general_profile_compatibility_flag[ j ] u(1)
                }

                nav.SkipBit();    //general_progressive_source_flag u(1)
                nav.SkipBit();    //general_interlaced_source_flag u(1)
                nav.SkipBit();    //general_non_packed_constraint_flag u(1)
                nav.SkipBit();    //general_frame_only_constraint_flag u(1)

                if (general_profile_idc == 4 || general_profile_compatibility_flag[4] ||
                    general_profile_idc == 5 || general_profile_compatibility_flag[5] ||
                    general_profile_idc == 6 || general_profile_compatibility_flag[6] ||
                    general_profile_idc == 7 || general_profile_compatibility_flag[7] ||
                    general_profile_idc == 8 || general_profile_compatibility_flag[8] ||
                    general_profile_idc == 9 || general_profile_compatibility_flag[9] ||
                    general_profile_idc == 10 || general_profile_compatibility_flag[10])
                {
                    /* The number of bits in this syntax structure is not affected by this condition */
                    nav.SkipBit();    //general_max_12bit_constraint_flag u(1)
                    nav.SkipBit();    //general_max_10bit_constraint_flag u(1)
                    nav.SkipBit();    //general_max_8bit_constraint_flag u(1)
                    nav.SkipBit();    //general_max_422chroma_constraint_flag u(1)
                    nav.SkipBit();    //general_max_420chroma_constraint_flag u(1)
                    nav.SkipBit();    //general_max_monochrome_constraint_flag u(1)
                    nav.SkipBit();    //general_intra_constraint_flag u(1)
                    nav.SkipBit();    //general_one_picture_only_constraint_flag u(1)
                    nav.SkipBit();    //general_lower_bit_rate_constraint_flag u(1)

                    if (general_profile_idc == 5 || general_profile_compatibility_flag[5] ||
                        general_profile_idc == 9 || general_profile_compatibility_flag[9] ||
                        general_profile_idc == 10 || general_profile_compatibility_flag[10])
                    {
                        nav.SkipBit();       //general_max_14bit_constraint_flag u(1)
                        nav.SkipBits(33);    //general_reserved_zero_33bits u(33)
                    }
                    else
                    {
                        nav.SkipBits(34);    //general_reserved_zero_34bits u(34)
                    }
                }
                else
                {
                    nav.SkipBits(43);        //general_reserved_zero_43bits u(43)
                }

                if ((general_profile_idc >= 1 && general_profile_idc <= 5) ||
                    general_profile_idc == 9 ||
                    general_profile_compatibility_flag[1] || general_profile_compatibility_flag[2] ||
                    general_profile_compatibility_flag[3] || general_profile_compatibility_flag[4] ||
                    general_profile_compatibility_flag[5] || general_profile_compatibility_flag[9])
                {
                    /* The number of bits in this syntax structure is not affected by this condition */
                    nav.SkipBit();    //general_inbld_flag u(1)
                }
                else
                {
                    nav.SkipBit();    //general_reserved_zero_bit u(1)
                }
            }

            ///////////////////////
            // Level
            ///////////////////////
            nav.SkipBits(8);    //general_level_idc u(8)

            bool[] sub_layer_profile_present_flag = new bool[maxNumSubLayersMinus1];
            bool[] sub_layer_level_present_flag = new bool[maxNumSubLayersMinus1];
            for (int i = 0; i < maxNumSubLayersMinus1; i++)
            {
                sub_layer_profile_present_flag[i] = nav.ReadBit();    //sub_layer_profile_present_flag[i] u(1)
                sub_layer_level_present_flag[i] = nav.ReadBit();      //sub_layer_level_present_flag[i] u(1)
            }

            if (maxNumSubLayersMinus1 > 0)
            {
                for (int i = maxNumSubLayersMinus1; i < 8; i++)
                {
                    nav.SkipBits(2);  // reserved_zero_2bits[i] u(2)
                }
            }

            bool[,] sub_layer_profile_compatibility_flag = new bool[maxNumSubLayersMinus1, 32];
            byte[] sub_layer_profile_idc = new byte[maxNumSubLayersMinus1];

            for (int i = 0; i < maxNumSubLayersMinus1; i++)
            {
                if (sub_layer_profile_present_flag[i])
                {
                    nav.SkipBits(2);  //sub_layer_profile_space[i] u(2)
                    nav.SkipBit();     //sub_layer_tier_flag[i] u(1)

                    sub_layer_profile_idc[i] = nav.ReadBitsAsByte(5);     //sub_layer_profile_idc[i] u(5)

                    for (int j = 0; j < 32; j++)
                    {
                        sub_layer_profile_compatibility_flag[i, j] = nav.ReadBit();   //sub_layer_profile_compatibility_flag[i][j] u(1)
                    }

                    nav.SkipBit(); //sub_layer_progressive_source_flag[i] u(1)
                    nav.SkipBit(); //sub_layer_interlaced_source_flag[i] u(1)
                    nav.SkipBit(); //sub_layer_non_packed_constraint_flag[i] u(1)
                    nav.SkipBit(); //sub_layer_frame_only_constraint_flag[i] u(1)

                    if (sub_layer_profile_idc[i] == 4 || sub_layer_profile_compatibility_flag[i, 4] ||
                        sub_layer_profile_idc[i] == 5 || sub_layer_profile_compatibility_flag[i, 5] ||
                        sub_layer_profile_idc[i] == 6 || sub_layer_profile_compatibility_flag[i, 6] ||
                        sub_layer_profile_idc[i] == 7 || sub_layer_profile_compatibility_flag[i, 7] ||
                        sub_layer_profile_idc[i] == 8 || sub_layer_profile_compatibility_flag[i, 8] ||
                        sub_layer_profile_idc[i] == 9 || sub_layer_profile_compatibility_flag[i, 9] ||
                        sub_layer_profile_idc[i] == 10 || sub_layer_profile_compatibility_flag[i, 10])
                    {
                        /* The number of bits in this syntax structure is not affected by this condition */
                        nav.SkipBit(); //sub_layer_max_12bit_constraint_flag[i] u(1)
                        nav.SkipBit(); //sub_layer_max_10bit_constraint_flag[i] u(1)
                        nav.SkipBit(); //sub_layer_max_8bit_constraint_flag[i] u(1)
                        nav.SkipBit(); //sub_layer_max_422chroma_constraint_flag[i] u(1)
                        nav.SkipBit(); //sub_layer_max_420chroma_constraint_flag[i] u(1)
                        nav.SkipBit(); //sub_layer_max_monochrome_constraint_flag[i] u(1)
                        nav.SkipBit(); //sub_layer_intra_constraint_flag[i] u(1)
                        nav.SkipBit(); //sub_layer_one_picture_only_constraint_flag[i] u(1)
                        nav.SkipBit(); //sub_layer_lower_bit_rate_constraint_flag[i] u(1)

                        if (sub_layer_profile_idc[i] == 5 || sub_layer_profile_compatibility_flag[i, 5])
                        {
                            nav.SkipBit();      //sub_layer_max_14bit_constraint_flag u(1)
                            nav.SkipBits(33);  //sub_layer_reserved_zero_33bits[i] u(33)
                        }
                        else
                        {
                            nav.SkipBits(34);  //sub_layer_reserved_zero_34bits[i] u(34)
                        }
                    }
                    else
                    {
                        nav.SkipBits(43);  //sub_layer_reserved_zero_43bits[i] u(43)
                    }

                    if ((sub_layer_profile_idc[i] >= 1 && sub_layer_profile_idc[i] <= 5) ||
                        sub_layer_profile_idc[i] == 9 ||
                        sub_layer_profile_compatibility_flag[i, 1] ||
                        sub_layer_profile_compatibility_flag[i, 2] ||
                        sub_layer_profile_compatibility_flag[i, 3] ||
                        sub_layer_profile_compatibility_flag[i, 4] ||
                        sub_layer_profile_compatibility_flag[i, 5] ||
                        sub_layer_profile_compatibility_flag[i, 9])
                    {
                        /* The number of bits in this syntax structure is not affected by this condition */
                        nav.SkipBit();  // sub_layer_inbld_flag[i] u(1)
                    }
                    else
                    {
                        nav.SkipBit();  //sub_layer_reserved_zero_bit[i] u(1)
                    }
                }

                if (sub_layer_level_present_flag[i])
                {
                    nav.SkipBits(8);  // sub_layer_level_idc[i] u(8)
                }
            }
        }

        /// <summary>
        /// Process the scaling list data.
        /// </summary>
        /// <remarks>
        /// Reference: 7.3.4 Scaling list data syntax
        /// </remarks>
        private void ProcessScalingListData(NalUnitBitstreamNavigator nav)
        {
            bool[,] scaling_list_pred_mode_flag = new bool[4, 6];
            int[,] scaling_list_dc_coef_minus8 = new int[4, 6];

            for (int sizeId = 0; sizeId < 4; sizeId++)
            {
                for (int matrixId = 0; matrixId < 6; matrixId += (sizeId == 3) ? 3 : 1)
                {
                    scaling_list_pred_mode_flag[sizeId, matrixId] = nav.ReadBit(); //scaling_list_pred_mode_flag[sizeId][matrixId] u(1)
                    if (!scaling_list_pred_mode_flag[sizeId, matrixId])
                    {
                        nav.ReadNonNegativeExponentialGolombAsUInt32(); //scaling_list_pred_matrix_id_delta[sizeId][matrixId] ue(v)
                    }
                    else
                    {
                        int nextCoef = 8;
                        int coefNum = Math.Min(64, (1 << (4 + (sizeId << 1))));

                        if (sizeId > 1)
                        {
                            int sizeIdMinux2 = sizeId - 2;
                            scaling_list_dc_coef_minus8[sizeIdMinux2, matrixId] = nav.ReadSignedExponentialGolombAsInt32(); //scaling_list_dc_coef_minus8[sizeId − 2][matrixId] se(v)
                            nextCoef = scaling_list_dc_coef_minus8[sizeIdMinux2, matrixId] + 8;
                        }

                        for (int i = 0; i < coefNum; i++)
                        {
                            int scaling_list_delta_coef = nav.ReadSignedExponentialGolombAsInt32(); //scaling_list_delta_coef se(v)
                            nextCoef = (nextCoef + scaling_list_delta_coef + 256) % 256;
                            // ScalingList[sizeId][matrixId][i] = nextCoef;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Process a Sequence Parameter Set NAL Unit.
        /// </summary>
        /// <remarks>
        /// Reference 7.3.2.2 Sequence parameter set RBSP syntax - seq_parameter_set_rbsp()
        /// </remarks>
        private byte[] ProcessSequenceParameterSetNalUnit(byte[] nalUnitBytes)
        {
            var sps = new SequenceParameterSet();

            var nav = new NalUnitBitstreamNavigator(nalUnitBytes);

            nav.SkipBits(16); // Skip the 2 byte Nal Unit header

            nav.SkipBits(4); // sps_video_parameter_set_id u(4)

            sps.sps_max_sub_layers_minus1 = nav.ReadBitsAsByte(3); // sps_max_sub_layers_minus1 u(3)

            nav.SkipBit();    // sps_temporal_id_nesting_flag u(1)

            ProcessProfileTierLevel(nav, true, sps.sps_max_sub_layers_minus1); //profile_tier_level( 1, sps_max_sub_layers_minus1 )

            nav.ReadNonNegativeExponentialGolombAsUInt32(); //sps_seq_parameter_set_id ue(v)

            uint chroma_format_idc = nav.ReadNonNegativeExponentialGolombAsUInt32();  //chroma_format_idc ue(v)
            if (chroma_format_idc == 3)
            {
                nav.SkipBit();     // separate_colour_plane_flag u(1)
            }

            nav.ReadNonNegativeExponentialGolombAsUInt32(); //pic_width_in_luma_samples ue(v)
            nav.ReadNonNegativeExponentialGolombAsUInt32(); //pic_height_in_luma_samples ue(v)

            bool conformance_window_flag = nav.ReadBit(); //conformance_window_flag u(1)
            if (conformance_window_flag)
            {
                nav.ReadNonNegativeExponentialGolombAsUInt32(); //conf_win_left_offset ue(v)
                nav.ReadNonNegativeExponentialGolombAsUInt32(); //conf_win_right_offset ue(v)
                nav.ReadNonNegativeExponentialGolombAsUInt32(); //conf_win_top_offset ue(v)
                nav.ReadNonNegativeExponentialGolombAsUInt32(); //conf_win_bottom_offset ue(v)
            }

            nav.ReadNonNegativeExponentialGolombAsUInt32(); //bit_depth_luma_minus8 ue(v)
            nav.ReadNonNegativeExponentialGolombAsUInt32(); //bit_depth_chroma_minus8 ue(v)
            nav.ReadNonNegativeExponentialGolombAsUInt32(); //log2_max_pic_order_cnt_lsb_minus4 ue(v)

            sps.sps_max_dec_pic_buffering_minus1 = new uint[sps.sps_max_sub_layers_minus1 + 1];
            bool sps_sub_layer_ordering_info_present_flag = nav.ReadBit(); //sps_sub_layer_ordering_info_present_flag u(1)
            for (int i = (sps_sub_layer_ordering_info_present_flag ? 0 : sps.sps_max_sub_layers_minus1); i <= sps.sps_max_sub_layers_minus1; i++)
            {
                sps.sps_max_dec_pic_buffering_minus1[i] = nav.ReadNonNegativeExponentialGolombAsUInt32(); //sps_max_dec_pic_buffering_minus1[i] ue(v)
                nav.ReadNonNegativeExponentialGolombAsUInt32(); //sps_max_num_reorder_pics[i] ue(v)
                nav.ReadNonNegativeExponentialGolombAsUInt32(); //sps_max_latency_increase_plus1[i] ue(v)
            }

            nav.ReadNonNegativeExponentialGolombAsUInt32(); //log2_min_luma_coding_block_size_minus3 ue(v)
            nav.ReadNonNegativeExponentialGolombAsUInt32(); //log2_diff_max_min_luma_coding_block_size ue(v)
            nav.ReadNonNegativeExponentialGolombAsUInt32(); //log2_min_luma_transform_block_size_minus2 ue(v)
            nav.ReadNonNegativeExponentialGolombAsUInt32(); //log2_diff_max_min_luma_transform_block_size ue(v)
            nav.ReadNonNegativeExponentialGolombAsUInt32(); //max_transform_hierarchy_depth_inter ue(v)
            nav.ReadNonNegativeExponentialGolombAsUInt32(); //max_transform_hierarchy_depth_intra ue(v)

            bool scaling_list_enabled_flag = nav.ReadBit(); //scaling_list_enabled_flag u(1)
            if (scaling_list_enabled_flag)
            {
                bool sps_scaling_list_data_present_flag = nav.ReadBit(); //sps_scaling_list_data_present_flag u(1)
                if (sps_scaling_list_data_present_flag)
                {
                    ProcessScalingListData(nav); //scaling_list_data()
                }
            }

            nav.SkipBit(); //amp_enabled_flag u(1)
            nav.SkipBit(); //sample_adaptive_offset_enabled_flag u(1)

            bool pcm_enabled_flag = nav.ReadBit(); //pcm_enabled_flag u(1)
            if (pcm_enabled_flag)
            {
                nav.SkipBits(4); //pcm_sample_bit_depth_luma_minus1 u(4)
                nav.SkipBits(4); //pcm_sample_bit_depth_chroma_minus1 u(4)
                nav.ReadNonNegativeExponentialGolombAsUInt32(); //log2_min_pcm_luma_coding_block_size_minus3 ue(v)
                nav.ReadNonNegativeExponentialGolombAsUInt32(); //log2_diff_max_min_pcm_luma_coding_block_size ue(v)
                nav.SkipBit(); //pcm_loop_filter_disabled_flag u(1)
            }

            sps.num_short_term_ref_pic_sets = nav.ReadNonNegativeExponentialGolombAsUInt32(); //num_short_term_ref_pic_sets ue(v)
            for (uint i = 0; i < sps.num_short_term_ref_pic_sets; i++)
            {
                sps.ShortTermRefPicSets.Add(ProcessShortTermRefPicSets(nav, sps, i)); //st_ref_pic_set( i )
            }

            bool long_term_ref_pics_present_flag = nav.ReadBit(); //long_term_ref_pics_present_flag u(1)
            if (long_term_ref_pics_present_flag)
            {
                uint num_long_term_ref_pics_sps = nav.ReadNonNegativeExponentialGolombAsUInt32(); //num_long_term_ref_pics_sps ue(v)
                for (int i = 0; i < num_long_term_ref_pics_sps; i++)
                {
                    nav.ReadNonNegativeExponentialGolombAsUInt32(); //lt_ref_pic_poc_lsb_sps[i] u(v)
                    nav.SkipBit(); //used_by_curr_pic_lt_sps_flag[i] u(1)
                }
            }

            nav.SkipBit(); //sps_temporal_mvp_enabled_flag u(1)
            nav.SkipBit(); //strong_intra_smoothing_enabled_flag u(1)

            bool vui_parameters_present_flag = nav.ReadBit(); //vui_parameters_present_flag u(1)
            if (vui_parameters_present_flag)
            {
                ProcessVideoUsabilityInformationParameters(nav); //vui_parameters()
            }

            // **********************************************
            // NOTE: Parsing has only implemented up to here... There are still more extensions...
            // **********************************************

            //sps_extension_present_flag u(1)
            //if( sps_extension_present_flag ) {
            //    sps_range_extension_flag u(1)
            //    sps_multilayer_extension_flag u(1)
            //    sps_3d_extension_flag u(1)
            //    sps_scc_extension_flag u(1)
            //    sps_extension_4bits u(4)
            //}

            //if ( sps_range_extension_flag )
            //    sps_range_extension( )

            //if( sps_multilayer_extension_flag )
            //    sps_multilayer_extension( ) /* specified in Annex F *

            //if (sps_3d_extension_flag)
            //    sps_3d_extension() /* specified in Annex I *

            //if (sps_scc_extension_flag)
            //    sps_scc_extension()

            //if (sps_extension_4bits)
            //    while (more_rbsp_data())
            //        sps_extension_data_flag u(1)

            return nalUnitBytes;
        }

        /// <summary>
        /// Process the short term reference picture sets.
        /// </summary>
        /// <remarks>
        /// Reference:
        ///  7.3.7 Short-term reference picture set syntax
        ///  7.4.8 Short-term reference picture set semantics
        /// </remarks>
        private ShortTermRefPicSet ProcessShortTermRefPicSets(NalUnitBitstreamNavigator nav, SequenceParameterSet sps, uint stRpsIdx)
        {
            var result = new ShortTermRefPicSet();

            if (stRpsIdx != 0)
            {
                result.inter_ref_pic_set_prediction_flag = nav.ReadBit(); //inter_ref_pic_set_prediction_flag u(1)
            }

            if (result.inter_ref_pic_set_prediction_flag)
            {
                if (stRpsIdx == sps.num_short_term_ref_pic_sets)
                {
                    result.delta_idx_minus1 = nav.ReadNonNegativeExponentialGolombAsUInt32(); //delta_idx_minus1 ue(v)
                }

                result.delta_rps_sign = nav.ReadBit() ? (byte)1 : (byte)0;   //delta_rps_sign u(1)
                result.abs_delta_rps_minus1 = nav.ReadNonNegativeExponentialGolombAsUInt32(); //abs_delta_rps_minus1 ue(v)
                // long deltaRps = (1 - 2 * delta_rps_sign) * (abs_delta_rps_minus1 + 1);

                uint numDeltaPocs = CalculateNumDeltaPictureOrderCounts(sps.ShortTermRefPicSets, stRpsIdx, result);
                for (int j = 0; j <= numDeltaPocs; j++)
                {
                    result.used_by_curr_pic_flag.Add(nav.ReadBit()); //used_by_curr_pic_flag[ j ] u(1)
                    if (!result.used_by_curr_pic_flag[j])
                    {
                        result.use_delta_flag.Add(nav.ReadBit());    //use_delta_flag[ j ] u(1)
                    }
                }
            }
            else
            {
                result.num_negative_pics = nav.ReadNonNegativeExponentialGolombAsUInt32(); //num_negative_pics ue(v)
                result.num_positive_pics = nav.ReadNonNegativeExponentialGolombAsUInt32(); //num_positive_pics ue(v)

                if (result.num_negative_pics > sps.sps_max_dec_pic_buffering_minus1[sps.sps_max_sub_layers_minus1])
                {
                    System.Console.WriteLine("ShortTermRefPicSet.num_negative_pics > sps_max_dec_pic_buffering_minus1");
                    return result;
                }

                if (result.num_positive_pics > sps.sps_max_dec_pic_buffering_minus1[sps.sps_max_sub_layers_minus1])
                {
                    System.Console.WriteLine("ShortTermRefPicSet.num_positive_pics > sps_max_dec_pic_buffering_minus1");
                    return result;
                }

                for (int i = 0; i < result.num_negative_pics; i++)
                {
                    result.delta_poc_s0_minus1.Add(nav.ReadNonNegativeExponentialGolombAsUInt32()); //delta_poc_s0_minus1[ i ] ue(v)
                    result.used_by_curr_pic_s0_flag.Add(nav.ReadBit()); //used_by_curr_pic_s0_flag[ i ] u(1)
                }

                for (int i = 0; i < result.num_positive_pics; i++)
                {
                    result.delta_poc_s1_minus1.Add(nav.ReadNonNegativeExponentialGolombAsUInt32()); //delta_poc_s1_minus1[ i ] ue(v)
                    result.used_by_curr_pic_s1_flag.Add(nav.ReadBit()); //used_by_curr_pic_s1_flag[ i ] u(1)
                }
            }

            return result;
        }

        /// <summary>
        /// Process the Video Usability Information.
        /// </summary>
        /// <param name="nav">The NAL Unit bitstream navigator.</param>
        /// <remarks>
        /// Reference: Annex E - Video usability information (p370)
        /// </remarks>
        private void ProcessVideoUsabilityInformationParameters(NalUnitBitstreamNavigator nav)
        {
            ReadAspectRatioVideoUsabilityInformation(nav);

            bool overscan_info_present_flag = nav.ReadBit(); //overscan_info_present_flag u(1)
            if (overscan_info_present_flag)
            {
                nav.SkipBit(); //overscan_appropriate_flag u(1)
            }

            // **********************************************
            // NOTE: Parsing has only implemented up to here...
            // **********************************************

            //bool video_signal_type_present_flag = nav.ReadBit(); //video_signal_type_present_flag u(1)
            //if (video_signal_type_present_flag)
            //{
            //    //video_format u(3)
            //    //video_full_range_flag u(1)
            //    bool colour_description_present_flag = nav.ReadBit(); //colour_description_present_flag u(1)
            //    if (colour_description_present_flag)
            //    {
            //        //colour_primaries u(8)
            //        //transfer_characteristics u(8)
            //        //matrix_coeffs u(8)
            //    }
            //}

            //bool chroma_loc_info_present_flag = nav.ReadBit(); //chroma_loc_info_present_flag u(1)
            //if (chroma_loc_info_present_flag)
            //{
            //    //chroma_sample_loc_type_top_field ue(v)
            //    //chroma_sample_loc_type_bottom_field ue(v)
            //}

            ////neutral_chroma_indication_flag u(1)
            ////field_seq_flag u(1)
            ////frame_field_info_present_flag u(1)

            //bool default_display_window_flag = nav.ReadBit(); //default_display_window_flag u(1)
            //if (default_display_window_flag)
            //{
            //    //def_disp_win_left_offset ue(v)
            //    //def_disp_win_right_offset ue(v)
            //    //def_disp_win_top_offset ue(v)
            //    //def_disp_win_bottom_offset ue(v)
            //}

            //bool vui_timing_info_present_flag = nav.ReadBit(); //vui_timing_info_present_flag u(1)
            //if (vui_timing_info_present_flag)
            //{
            //    //vui_num_units_in_tick u(32)
            //    //vui_time_scale u(32)
            //    bool vui_poc_proportional_to_timing_flag = nav.ReadBit(); //vui_poc_proportional_to_timing_flag u(1)
            //    if (vui_poc_proportional_to_timing_flag)
            //    {
            //        //vui_num_ticks_poc_diff_one_minus1 ue(v)
            //    }

            //    bool vui_hrd_parameters_present_flag = nav.ReadBit(); //vui_hrd_parameters_present_flag u(1)
            //    if (vui_hrd_parameters_present_flag)
            //    {
            //        //hrd_parameters(1, sps_max_sub_layers_minus1)
            //    }
            //}

            //bool bitstream_restriction_flag = nav.ReadBit(); //bitstream_restriction_flag u(1)
            //if (bitstream_restriction_flag)
            //{
            //    //tiles_fixed_structure_flag u(1)
            //    //motion_vectors_over_pic_boundaries_flag u(1)
            //    //restricted_ref_pic_lists_flag u(1)
            //    //min_spatial_segmentation_idc ue(v)
            //    //max_bytes_per_pic_denom ue(v)
            //    //max_bits_per_min_cu_denom ue(v)
            //    //log2_max_mv_length_horizontal ue(v)
            //    //log2_max_mv_length_vertical ue(v)
            //}
        }

        private void ReadAspectRatioVideoUsabilityInformation(NalUnitBitstreamNavigator nav)
        {
            const byte EXTENDED_SAR = 255;

            byte aspect_ratio_idc = 0;
            uint sar_width = 0;
            uint sar_height = 0;

            bool aspect_ratio_info_present_flag = nav.ReadBit(); //aspect_ratio_info_present_flag u(1)
            if (aspect_ratio_info_present_flag)
            {
                aspect_ratio_idc = nav.ReadBitsAsByte(8); //aspect_ratio_idc u(8)
                if (aspect_ratio_idc == EXTENDED_SAR)
                {
                    sar_width = nav.ReadBitsAsUInt32(16); //sar_width u(16)
                    sar_height = nav.ReadBitsAsUInt32(16); //sar_height u(16)
                }
            }

            _eventHandler?.ProcessAspectRatioVideoUsabilityInformation(nav, aspect_ratio_info_present_flag, aspect_ratio_idc, sar_width, sar_height);
        }

        #endregion Private Methods
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