namespace ChannelAdam.Hevc.Processor.Abstractions
{
    public interface INalUnitProcessor
    {
        byte[] ProcessNalUnit(byte[] nalUnitBytes);
    }
}