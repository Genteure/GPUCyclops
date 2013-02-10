using System;
using System.Collections.Generic;
using System.IO;

namespace Media.Formats.Generic
{
  public class GenericTrackFormat : ITrackFormat
  {

    // call backs
    public event LazyRead FetchNextBatch;

    // properties that must be implemented in derived classes
    public virtual string PayloadType 
    {
      get { throw new Exception("Need to implement get PayloadType"); }
    }

    public virtual Codec Codec 
    {
      get { throw new Exception("Need to implement get Codec"); }
      set { throw new Exception("Need to implement set Codec"); }
    }

    //public virtual uint TimeScale { get { return 0; } } // handler-specific, should not exist here

    // Duration should be in units of 100 Nanosecs.
    public virtual ulong DurationIn100NanoSecs 
    {
      get { throw new Exception("Need to implement get Duration"); }
      set { throw new Exception("Need to implement set Duration"); } 
    }

    public virtual uint TrackID { get { return 0; } set { ; } }
		public virtual uint TimeScale { get { return 0; } set { ; } }
    // audio
    public virtual int ChannelCount { get { return 0; } }
    public virtual int SampleSize { get { return 0; } }
    public virtual int SampleRate { get { return 0; } }
    // video
    public virtual Size FrameSize { get { return null; } }

    public virtual bool HasIFrameBoxes { get { return false; } protected set { ;} }

	 public GenericTrackFormat()
	 {
		 Common.Logger.Instance.Info("[GenericTrackFormat] Ctor, full type [" + this.GetType().Name + "].");
	 }

    public virtual List<StreamDataBlockInfo> PrepareSampleReading(UInt64 inStartSampleTime, UInt64 inEndSampleTime, ref ulong lastEnd)
    {
      throw new NotImplementedException("Need to override PrepareSampleReading");
    }

    public virtual List<StreamDataBlockInfo> PrepareSampleReading(int inStartSampleIndex, int inEndSampleIndex, ref ulong lastEnd)
    {
      throw new NotImplementedException("Need to override PrepareSampleReading (with index params)");
    }

    protected void GetNextBatch(int requestedBoxCount, int sliceIndex)
    {
      if (FetchNextBatch != null)
      {
        FetchNextBatch(requestedBoxCount, sliceIndex);
      }
    }

    public virtual int ResetTrack(ulong time)
    {
      throw new NotImplementedException("Need to override ResetAndGetIndex");
    }

    public virtual int SampleAvailable(int index)
    {
      throw new NotImplementedException("Need to override SampleAvailable in GenericTrackFormat");
    }

    public virtual void PrepareSampleWriting(List<StreamDataBlockInfo> sampleLocations, ref ulong currMdatOffset)
    {
      throw new NotImplementedException("Need to override GenericTrackFormat.PrepareSampleWriting");
    }

    public virtual void WriteSamples(BinaryWriter bw, IEnumerable<Slice> slices)
    {
      throw new NotImplementedException("Need to override GenericTrackFormat.WriteSamples");
    }
  }
}
