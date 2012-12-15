namespace Media.Formats.MP4
{
    using System;

    [Flags]
    public enum TrackFragmentHeaderBoxFlags
    {
        BaseDataOffsetPresent = 1,
        SampleDescriptionIndexPresent = 2,
        DefaultSampleDurationPresent = 8,
        DefaultSampleSizePresent = 0x10,
        DefaultSampleFlagsPresent = 0x20
    }
}
