using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Media.Formats
{
  public delegate void LazyRead(int requestedBoxCount, int sliceIndex);

  public class Size
  {
    public int Height;
    public int Width;
  }

  public interface ITrackFormat
  {
    // call backs
    event LazyRead FetchNextBatch;

    // properties that must be implemented in derived classes
    string PayloadType { get; }
    Codec Codec { get; set; }
    //public virtual uint TimeScale { get { return 0; } } // handler-specific, should not exist here

    // Duration should be in units of 100 Nanosecs.
    ulong DurationIn100NanoSecs
    {
      get;
      set;
    }

    uint TrackID { get; set; }
    uint TimeScale { get; set; }

    // audio
    int ChannelCount { get; }
    int SampleSize { get; }
    int SampleRate { get; }
    // video
    Size FrameSize { get; }

    bool HasIFrameBoxes { get; }

    List<StreamDataBlockInfo> PrepareSampleReading(UInt64 inStartSampleTime, UInt64 inEndSampleTime, ref ulong lastEnd);

    List<StreamDataBlockInfo> PrepareSampleReading(int inStartSampleIndex, int inEndSampleIndex, ref ulong lastEnd);

    int ResetTrack(ulong time);

    int SampleAvailable(int index);

    void PrepareSampleWriting(List<StreamDataBlockInfo> sampleLocations, ref ulong currMdatOffset);

    void WriteSamples(BinaryWriter bw, IEnumerable<Slice> slices);
  }
}
