namespace ChannelAdam.Hevc.Processor.Abstractions
{
    public interface INalUnitProcessorEventHandler
    {
        void ProcessAspectRatioVideoUsabilityInformation(NalUnitBitstreamNavigator nav, bool aspect_ratio_info_present_flag, byte aspect_ratio_idc, uint sar_width, uint sar_height);
    }
}