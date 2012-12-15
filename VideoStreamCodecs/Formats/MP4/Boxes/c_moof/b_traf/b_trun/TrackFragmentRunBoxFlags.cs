namespace Media.Formats.MP4
{
    using System;

    [Flags]
    public enum TrackFragmentRunBoxFlags
    {
        DataOffsetPresent = 1,
        FirstSampleFlagsPresent = 4,
        SampleCompositionTimeOffsetsPresent = 0x800,
        SampleDurationPresent = 0x100,
        SampleFlagsPresent = 0x400,
        SampleSizePresent = 0x200
    }
}
