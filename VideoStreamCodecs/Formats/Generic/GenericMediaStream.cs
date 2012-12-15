using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Linq;
using Media;


namespace Media.Formats.Generic {

  public class GenericMediaStream : IMediaStream {
    private List<IMediaTrack> _MediaTracks = new List<IMediaTrack>();

    public List<IMediaTrack> MediaTracks 
    { 
      get { return (_MediaTracks); } 
      protected set { _MediaTracks = value; } 
    }

    public bool IsMediaStreamFragmented
    {
      get;
      protected set;
    }

    public bool IsForReading
    {
      get;
      protected set;
    }

    public Stream Stream
    {
      get;
      protected set;
    }

    // for passing from stream reader to stream writer
    public Hints Hints
    {
      get;
      private set;
    }

    public GenericMediaStream()
    {
		 Common.Logger.Instance.Info("[GenericMediaStream] Ctor, full type [" + this.GetType().Name + "].");
      Hints = new Hints();
      CurrMDatOffset = 8L; 
    }

    protected BinaryReader _binaryReader;

    // for writing:
    protected BinaryWriter _binaryWriter;
    public BinaryWriter BinaryWriter { get { return _binaryWriter; } }

    //public uint TimeScale = 1; // pass this only as a hint, it doesn't belong here

    public List<IsochronousTrackInfo> SourceTrackInfo;  // used only when constructing an IGenericMediaStream

    public string ObjectDescriptor // used only when reading stream from a source, may be null
    {
      get;
      protected set;
    }

    public string UserData // used only when reading stream from a source, may be null
    {
      get;
      protected set;
    }

    public virtual ulong DurationIn100NanoSecs
    {
      get;
      protected set;
    }


    private UInt64 _FragmentDuration = 0;
    public UInt64 FragmentDuration
    {
        get
        {
            if (IsMediaStreamFragmented == false) return (DurationIn100NanoSecs);
            return (_FragmentDuration);
        }
        set { _FragmentDuration = value; }
    }

    CacheManager _cacheManager = null;

    public bool CachingEnabled
    {
      get { return (_cacheManager != null); }
      protected set 
      {
        if (value)
        {
          if (_cacheManager == null)
            _cacheManager = new CacheManager(this);
        }
        else
          _cacheManager = null;
      }
    }

    // Duration will always be in units of 100 nanosec in this class.
    //public ulong ScaledDuration
    //{
    //  get { return (FragmentDuration * (ulong)TimeSpan.FromSeconds(1.0).Ticks) / TimeScale; }
    //}

    /// <summary>
    /// EOF is only useful when reading a file sequentially.
    /// </summary>
    public bool EOF
    {
      get { return Stream.Position == Stream.Length; }
    }



    public IMediaTrack this[CodecTypes type, int id] {
      get 
      {
        if (MediaTracks.Count == 0)
          return null;

        GenericMediaTrack track = null;
        if (id == 0) // get the first track with the given type
        {
          if (MediaTracks.Any(trk => trk.Codec.CodecType == type))
            track = (GenericMediaTrack)MediaTracks.First(trk => trk.Codec.CodecType == type);
        }
        else // get specific track with the given type and track ID
        {
          if (MediaTracks.Any(trk => trk.Codec.CodecType == type && trk.TrackID == id))
            track = (GenericMediaTrack)MediaTracks.First(trk => trk.Codec.CodecType == type && trk.TrackID == id);
        }
        return track;
      }
    }

    /// <summary>
    /// Open using path name.
    /// </summary>
    /// <param name="pathName"></param>
    public virtual void Open(string pathName)
    {
      FileStream stream = File.Open(pathName, FileMode.Open);
      Open(stream);
    }

    public virtual void Open(string pathName, bool withCaching)
    {
      Open(pathName);
      CachingEnabled = withCaching;
    }

    /// <summary>
    /// Open
    /// Used for reading an existing file, caching disabled.
    /// </summary>
    /// <param name="stream"></param>
    public virtual void Open(Stream stream)
    {
		 Common.Logger.Instance.Info("[GenericMediaStream::Open] opened " + stream + ", " + (stream != null ? stream.GetType().Name : string.Empty));
		 IsForReading = true;
      this.Stream = stream;
    }

    /// <summary>
    /// Open
    /// Used for reading an existing file, possibly with caching enabled.
    /// NOTE: Caching disables the Read routine (becomes no-op).
    /// </summary>
    /// <param name="stream">IO Stream already opened for reading.</param>
    /// <param name="withCaching">Set to true to enable caching.</param>
    public void Open(Stream stream, bool withCaching)
    {
      Open(stream);
      CachingEnabled = withCaching; // NOTE: this assignment triggers a call to first Read()
    }

    /// <summary>
    /// Create
    /// Create a stream from a file path.
    /// </summary>
    /// <param name="pathName"></param>
    public virtual void Create(string pathName)
    {
      FileStream stream = File.Open(pathName, FileMode.Create);
      Create(stream);
    }

    /// <summary>
    /// Create
    /// Used for writing to a new file.
    /// </summary>
    /// <param name="outStream"></param>
    public virtual void Create(Stream outStream)
    {
		 Common.Logger.Instance.Info("[GenericMediaStream::Create] created " + outStream + ", " + (outStream != null ? outStream.GetType().Name : string.Empty));
      IsForReading = false;
      this.Stream = outStream;
    }

    public void Create(Stream outStream, bool withCaching)
    {
      Create(outStream);
      CachingEnabled = withCaching;
    }

    public virtual void Read()
    {
        throw new Exception("Generic Media Stream Read(); You must override this method");
    }

    public static readonly int MAX_BOXES_TO_READ = 2400;

    /// <summary>
    /// LazyRead
    /// This is so far implemented only in the QBox handler. The idea is to limit the count of boxes stored in main memory
    /// at any one time. GenericMediaTrack or its implementation is supposed to store only those boxes that have so far been
    /// unprocessed by PrepareSampleReading. If PrepareSampleReading is given a time or index parameter that is eiher before
    /// or after the set of boxes available in main memory, then it should set the base stream position to a best guess
    /// position of the targeted box, and then call LazyRead.
    /// </summary>
    /// <param name="requestedBoxCount">count of boxes to read</param>
    public virtual void LazyRead(int requestedBoxCount)
    {
      throw new Exception("Generic Media Stream LazyRead(); You must override this method");
    }

    // UpdateDuration needs to be implemented for a dynamically growing stream
    public virtual ulong UpdateDuration()
    {
      throw new Exception("Generic Media Stream UpdateDuration(); You must override this method");
    }

    public virtual void SynchronizeAllTracks(long position, ulong time)
    {
      throw new Exception("Generic Media Stream SynchronizeAllTracks(); You must override this method");
    }

    public virtual void Write()
    {
      throw new Exception("Generic Media Stream Write(); You must override this method");
    }

    /// <summary>
    /// InitializeForWriting
    /// What we do here is initialize a couple of properties.
    /// </summary>
    /// <param name="mediaTracks">This is with the item type of whatever is the source media stream.</param>
    public virtual void InitializeForWriting(List<IsochronousTrackInfo> mediaTracks)
    {
      this.SourceTrackInfo = mediaTracks;
      if (this.SourceTrackInfo.Any(rbt => rbt.HandlerType == "Video"))
      {
        RawVideoTrackInfo rvt = (RawVideoTrackInfo)mediaTracks.First(rbt => rbt.HandlerType == "Video");
        this.DurationIn100NanoSecs = rvt.MovieDurationIn100NanoSecs;
        this.Hints.StreamTimeScale = rvt.MovieTimeScale;
      }
      else if (this.SourceTrackInfo.Any(rbt => rbt.HandlerType == "Audio"))
      {
        RawAudioTrackInfo rati = (RawAudioTrackInfo)mediaTracks.First(rbt => rbt.HandlerType == "Audio");
        this.DurationIn100NanoSecs = rati.MovieDurationIn100NanoSecs;
        this.Hints.StreamTimeScale = rati.MovieTimeScale;
      }
      if (CachingEnabled) _cacheManager.SetupForWrite(mediaTracks);
    }

    // current offset into mdat, used when building a track from scratch
    ulong _currMDatOffset;
    public ulong CurrMDatOffset
    {
      get { return _currMDatOffset; }
      set { _currMDatOffset = value; }
    }

    public void CheckMDatOffset(long tempFilePos)
    {
      // use current offset into mdat to verify file position AFTER writing all samples in this batch
      if (tempFilePos != (long)CurrMDatOffset - 8)
        throw new Exception("GenericMediaStream: current file position does not match stbl data");
    }

    /// <summary>
    /// PrepareSampleWriting
    /// Note: this[CodecTypes.Audio, 1] is of the same type as sourceAudio, but different instances (one is the destination, the other source).
    /// </summary>
    /// <param name="sourceAudio"></param>
    /// <param name="sourceVideo"></param>
    public void PrepareSampleWriting(IMediaTrack sourceAudio, IMediaTrack sourceVideo)
    {
      // NOTE: the sequence order of tracks is important because mdat offsets have to match.
      if (this[CodecTypes.Audio, 0] != null)
        this[CodecTypes.Audio, 0].PrepareSampleWriting(sourceAudio, ref _currMDatOffset);
      if (this[CodecTypes.Video, 0] != null)
        this[CodecTypes.Video, 0].PrepareSampleWriting(sourceVideo, ref _currMDatOffset);
    }

    /// <summary>
    /// PrepareSampleWriting
    /// This overloaded method accepts slices and codec type as params.
    /// </summary>
    /// <param name="slicesInfo">The list of slices to be written, in StreamDataBlockInfo format</param>
    /// <param name="codecType">A member of the CodecTypes enum</param>
    public void PrepareSampleWriting(List<StreamDataBlockInfo> slicesInfo, CodecTypes codecType)
    {
      IMediaTrack track = this[codecType, 0];
      if ((track != null) && (track.TrackFormat != null))
        track.TrackFormat.PrepareSampleWriting(slicesInfo, ref _currMDatOffset);
    }

    /// <summary>
    /// WriteSamples
    /// Writing out a slice of both the audio and video tracks means that the tracks are going to be interleaved in the final mdat.
    /// </summary>
    /// <param name="sourceAudio"></param>
    /// <param name="sourceVideo"></param>
    public virtual void WriteSamples(IMediaTrack sourceAudio, IMediaTrack sourceVideo)
    {
      throw new NotImplementedException("Have to implement WriteSamples(GenericAudioTrack sourceAudio, GenericVideoTrack sourceVideo) in derived class");
    }

    public virtual void WriteSamples(IEnumerable<Slice> slices, CodecTypes codecType)
    {
      throw new NotImplementedException("Have to implement WriteSamples(List<StreamDataBlockInfo> slices, CodecTypes codecType) in derived class");
    }

    /// <summary>
    /// FinalizeStream
    /// Write out the header and mdat to this final stream.
    /// </summary>
    public virtual void FinalizeStream()
    {
      throw new NotImplementedException("Need to implement FinalizeStream in derived class");
    }

    // this routine needs to prepare each of the track to iterate through samples which 
    // are between the set iterator window start and end times...
    // NOTE: for fragmented ISMV tracks, the time span requested (IteratorWindowEndTime minus IteratorWindowStartTime) must be greater than the shortest fragment.
    public bool PrepareSampleReading(UInt64 IteratorWindowStartTime, UInt64 IteratorWindowEndTime)
    {
        bool retVal = true;
        foreach (GenericMediaTrack track in this.MediaTracks)
        {
            retVal = retVal && track.PrepareSampleReading(IteratorWindowStartTime, IteratorWindowEndTime);
        }
        return retVal;
    }
    
    public void AddTrack(IMediaTrack inTrack) {
		 Common.Logger.Instance.Info("[GenericMediaStream::AddTrack] added " + inTrack + ", " + (inTrack != null ? inTrack.GetType().Name : string.Empty));
      MediaTracks.Add(inTrack);
      if (MediaTrackAdded != null) MediaTrackAdded(inTrack);
    }

    public void TriggerLogicalBreak(IMediaTrack inTrack)
    {
        if (MediaTrackLogicalBreak != null) MediaTrackLogicalBreak(inTrack);
    }

    public override string ToString() {
      return ("Generic Media Stream ToString(); You must override this method"); 
    }

    // the event fires when a new media track if found in the incoming media stream...
    public event MediaTrackAddedHandler MediaTrackAdded;

    // this event fires when a logical break in the input media stream is found...
    // for a fragmented file, this would be called at the start of each fragment...
    // for an mp4 file, this would be called at the start of the file only...
    public event MediaTrackLogicalBreakHandler MediaTrackLogicalBreak;
  }

}
