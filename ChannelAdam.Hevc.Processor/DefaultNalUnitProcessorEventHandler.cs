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