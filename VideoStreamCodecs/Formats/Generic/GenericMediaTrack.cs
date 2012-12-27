using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Media;

namespace Media.Formats.Generic {


  public class GenericMediaTrack : IMediaTrack  {
    private List<StreamDataBlockInfo> _sampleStreamLocations = new List<StreamDataBlockInfo>();
    private AutoResetEvent IndexingExclusion = new AutoResetEvent(true);
    private object _syncObj = new object();

    public PerTrackCacheManager CacheMgr
    {
      get;
      set;
    }

    public virtual IMediaStream ParentStream
    {
      get;
      set;
    }

    public List<StreamDataBlockInfo> SampleStreamLocations
    {
      get 
      {
        if (CacheMgr == null)
          return _sampleStreamLocations;
        else
          return CacheMgr.CurrentSliceList;
      }
      private set 
      {
        if (CacheMgr == null)
          _sampleStreamLocations = value;
        else
          CacheMgr.CurrentSliceList = value;
      }
    }

    public ITrackFormat Format
    {
      get;
      protected set;
    }

    public virtual ITrackFormat TrackFormat
    {
      get { return Format; }
      set 
      { 
        Format = value;
      }
    }

    // This allows the user to index into a virtual series of fragments when actually there is
    // only one CurrentFragment at a time (caching or not, but without caching random access of 
    // fragments is nt possible). CurrentFragment is declared in GenericTrackFormat.
    // NOTE: not all media are fragmented.
    public virtual IEnumerable<IFragment> Fragments
    {
      get
      {
        return null;
      }
    }

    public int TrackID
    {
      get { return (int)TrackFormat.TrackID; }
      private set { TrackFormat.TrackID = (uint)value; }
    }

    public Codec Codec
    {
      get { return TrackFormat.Codec; }
      private set { TrackFormat.Codec = value; }
    }

    // how long in time this track is, 100 nanosec units
    public ulong TrackDurationIn100NanoSecs
    {
      get { return TrackFormat.DurationIn100NanoSecs; }
      set { TrackFormat.DurationIn100NanoSecs = value; }
    }

    // CurrentStartIndex is the start index of the currently active SampleStreamLocations
    public int CurrentStartIndex
    {
      get
      {
        if (SampleStreamLocations.Count == 0)
          return -1;
        if (CacheMgr == null)
          return 0;
        return SampleStreamLocations[0].index;
      }
    }

    public virtual int BlockSize
    {
      get { return CacheMgr.BlockSize; }
    }

    public bool HasIFrameBoxes { get { return TrackFormat.HasIFrameBoxes; } }

    //protected uint sampleCount; // count of samples in this track
    protected ulong lastStart = 0L;
    private ulong lastEnd = 0L; // unused

    public GenericMediaTrack()
    {
		 Common.Logger.Instance.Info("[GenericMediaTrack] Ctor, full type [" + this.GetType().Name + "].");
	 }

    public GenericMediaTrack(GenericTrackFormat format) : this()
    {
      TrackFormat = format;
      ParentStream = null;
    }

    /// <summary>
    /// Copy Constructor
    /// </summary>
    /// <param name="mt"></param>
    public GenericMediaTrack(GenericMediaTrack mt) : this()
    {
      this.TrackID = mt.TrackID;
      this.Codec = mt.Codec;
      this.SampleStreamLocations = mt.SampleStreamLocations;
      this.ParentStream = mt.ParentStream;
      //this.TrackDuration = mt.TrackDuration;
    }

    /// <summary>
    /// IEnumerable
    /// </summary>
    /// <param name="SampleIndex">Starts from 0, can increase indefinitely</param>
    /// <returns></returns>
    public virtual Slice this[int SampleIndex] {
      get 
      {
        IndexingExclusion.WaitOne();
        Slice sample = null;
        int index = SampleIndex - CurrentStartIndex;
       
        if (ParentStream.CachingEnabled)
        {
          if (BlockWithSlice != null)
          {
            if ((BlockWithSlice(SampleIndex) == 0) && (CurrentStartIndex >= 0))
            {
              index = SampleIndex - CurrentStartIndex;
              // FIXME: index should now be a valid index into SampleStreamLocations, but IndexingExclusion does not seem to work
              if (index < SampleStreamLocations.Count)
                sample = GetSample(SampleStreamLocations[index]);
            }
          }
          // this must be a destination track being written to
        }
        else if (index >= 0 && index < SampleStreamLocations.Count)
        {
          sample = GetSample(SampleStreamLocations[index]);
        }
        IndexingExclusion.Set();

        if (SampleAvailable != null)
          SampleAvailable(sample);

//nbl        if ((index == 0) && (sample != null) && (sample.SliceType == SliceType.DFrame))
//nbl          throw new Exception("A DFrame cannot be the first slice in a block");

        return (sample);
      }
      set
      {
        lock (_syncObj)
        {
          if (value == null)
            throw new Exception("Cannot add null slice to track");
          PutSample(value);
          if (SampleIndex < 0) // may have overflowed to negative, do nothing
            return;
          int index = SampleIndex - CurrentStartIndex;
          if (ParentStream.CachingEnabled)
          {
            // media headers prior to CurrentStartIndex already exist and don't need to be prepared
            if ((PrepareMediaHeaders != null) && (SampleIndex >= CurrentStartIndex))
            {
              int val = PrepareMediaHeaders(SampleIndex);
              if (val > 0)
              {
                index = 0;
              }
            }
            else if (index < 0 || (index > SampleStreamLocations.Count))
            {
              // if caching is OFF, this can't happen: the sample should be in the current batch
              throw new Exception("Invalid Sample Index Being Set");
            }
          }
        }
      }
    }

    public virtual Slice GetSample(StreamDataBlockInfo SampleInfo) {
        Slice ans = new Slice();
        ans.SliceBytes = new byte[SampleInfo.SliceSize];

        //ParentStream.EnterMutex();
        ParentStream.Stream.Position = (long)SampleInfo.StreamOffset; // if this GetSample call follows another one, file should be in position
        ParentStream.Stream.Read(ans.SliceBytes, 0, SampleInfo.SliceSize);
        //ParentStream.LeaveMutex();

        ans.Copy(SampleInfo);
        return (ans);
    }

    /// <summary>
    /// PutSample
    /// This is only used when caching is enabled.
    /// Slices are placed in the destination track in the order they are put.
    /// FIXME: In case the writes are non-sequential, gaps must be filled with zero size but non-zero duration slices.
    /// </summary>
    /// <param name="sample"></param>
    public virtual void PutSample(Slice sample)
    {
      //CacheMgr.SlicesForWriting.Add(sample); // FIXME: we don't need SlicesForWriting anymore because a Slice is now a StreamDataBlockInfo
      Slice outSlice = new Slice();
      outSlice.Copy(sample as StreamDataBlockInfo);
      outSlice.SliceBytes = new byte[sample.SliceBytes.Length];
      sample.SliceBytes.CopyTo(outSlice.SliceBytes, 0);
      SampleStreamLocations.Add(outSlice);
    }

    public virtual IEnumerator GetEnumerator() {
      return ((IEnumerator) new IGenericMediaTrackEnumerator(this));
    }

    IEnumerator<Slice> IEnumerable<Slice>.GetEnumerator()
    {
        return ((IEnumerator<Slice>)GetEnumerator());
    }

    /// <summary>
    /// PrepareSampleWriting
    /// If this is a destination track, this needs to be called to initialize moov box structure.
    /// Derived classes implement this method, but still needs to call this base method.
    /// </summary>
    /// <param name="sourceTrack"></param>
    public virtual void PrepareSampleWriting(IMediaTrack sourceTrack, ref ulong currMdatOffset)
    {
      if (sourceTrack.SampleStreamLocations.Count > 0)
        this.TrackFormat.PrepareSampleWriting(sourceTrack.SampleStreamLocations, ref currMdatOffset);
    }

    /// <summary>
    /// PrepareSampleReading
    /// If what the time span the user is asking for is beyond the contents of the track, this method returns false.
    /// </summary>
    /// <param name="inStartSampleTime">in milliseconds</param>
    /// <param name="inEndSampleTime">in milliseconds</param>
    /// <returns>Returns false when inStartSampleTime is beyond duration of fragment/stream.</returns>
    public bool PrepareSampleReading(UInt64 inStartSampleTime, UInt64 inEndSampleTime) {
      SampleStreamLocations = TrackFormat.PrepareSampleReading(inStartSampleTime, inEndSampleTime, ref lastEnd);
      return ((SampleStreamLocations != null) && (SampleStreamLocations.Count > 0));
    }

    public bool PrepareSampleReading(int inStartSampleIndex, int inEndSampleIndex)
    {
      SampleStreamLocations = TrackFormat.PrepareSampleReading(inStartSampleIndex, inEndSampleIndex, ref lastEnd);
      return ((SampleStreamLocations != null) && (SampleStreamLocations.Count > 0));
    }

    // Sample Event
    public event SampleHandler SampleAvailable;

    // a block maybe a fragment (if track is fragmented)
    // NextBlock looks for the block in which the requested slice index resides.
    // The index is converted to a time stamp by cache manager using track heuristics.
    public event NextBlock BlockWithSlice;
    // SlicePutRequest determines whether a slice is the last in a block to be written
    // out to the destination track. If so, PerTrackCacheManager calls PrepareSampleWriting.
    public event SlicePutRequest PrepareMediaHeaders;
  }

  public class IGenericMediaTrackEnumerator : IMediaTrackSliceEnumerator
  {
    protected IMediaTrack MediaTrack;
    protected int SampleIndex = -1;
    private Slice slice;

    public IGenericMediaTrackEnumerator()
    {
      slice = null;
    }

    public IGenericMediaTrackEnumerator(IMediaTrack inTrack) {
      MediaTrack = inTrack;
      slice = null;
    }

    public CodecTypes CodecType
    {
      get { return MediaTrack.Codec.CodecType; }
    }

    public virtual ulong? CurrentTimeStampNew
    {
      get {
        if (SampleIndex < 0) return null;
        Slice slice = MediaTrack[SampleIndex];
        if (slice == null) return MediaTrack.TrackDurationIn100NanoSecs;
        return slice.TimeStampNew;
      }
    }

    public void SetCurrent(Slice slice)
    {
      // slice index must match current index
      slice.index = SampleIndex;
      MediaTrack[SampleIndex] = slice;
    }

    public void Reset()
    { 
      SampleIndex = -1;
      slice = null;
    }

    Slice IEnumerator<Slice>.Current { get { return slice; } }
    object IEnumerator.Current { get { return ((object)slice); } }

    public bool MoveNext() {
      SampleIndex++;
      if ((MediaTrack.SampleStreamLocations == null) || (MediaTrack.SampleStreamLocations.Count == 0))
        return false;
      slice = MediaTrack[SampleIndex];
      if (slice == null) 
        return false;
      return (true);
    }

    public void Dispose()
    {
      MediaTrack = null;
    }
  }

}
