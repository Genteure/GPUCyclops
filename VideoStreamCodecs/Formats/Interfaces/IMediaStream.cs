using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Formats
{
  public delegate void MediaTrackAddedHandler(IMediaTrack inUpdatedTrack);
  public delegate void MediaTrackLogicalBreakHandler(IMediaTrack inTrack);

  public interface IMediaStream
  {
    List<IMediaTrack> MediaTracks
    {
      get;
    }

    bool IsMediaStreamFragmented
    {
      get;
    }

    bool IsForReading
    {
      get;
    }

    Stream Stream
    {
      get;
    }

    Hints Hints
    {
      get;
    }

    string ObjectDescriptor // used only when reading stream from a source, may be null
    {
      get;
    }

    string UserData // used only when reading stream from a source, may be null
    {
      get;
    }


    ulong DurationIn100NanoSecs
    {
      get;
      //set;
    }

    UInt64 FragmentDuration
    {
      get;
      set;
    }

    bool CachingEnabled
    {
      get;
      //set;
    }

    bool EOF
    {
      get;
    }

    IMediaTrack this[CodecTypes type, int id]
    {
      get;
    }

    /// <summary>
    /// Open using path name.
    /// </summary>
    /// <param name="pathName"></param>
    void Open(string pathName);

    void Open(string pathName, bool withCaching);

    /// <summary>
    /// Open
    /// Used for reading an existing file, caching disabled.
    /// </summary>
    /// <param name="stream"></param>
    void Open(Stream stream);

    /// <summary>
    /// Open
    /// Used for reading an existing file, possibly with caching enabled.
    /// NOTE: Caching disables the Read routine (becomes no-op).
    /// </summary>
    /// <param name="stream">IO Stream already opened for reading.</param>
    /// <param name="withCaching">Set to true to enable caching.</param>
    void Open(Stream stream, bool withCaching);

    /// <summary>
    /// Create
    /// Create a stream from a file path.
    /// </summary>
    /// <param name="pathName"></param>
    void Create(string pathName);

    /// <summary>
    /// Create
    /// Used for writing to a new file.
    /// </summary>
    /// <param name="outStream"></param>
    void Create(Stream outStream);

    void Create(Stream outStream, bool withCaching);

    void Read();

    /// <summary>
    /// LazyRead
    /// This is so far implemented only in the QBox handler. The idea is to limit the count of boxes stored in main memory
    /// at any one time. GenericMediaTrack or its implementation is supposed to store only those boxes that have so far been
    /// unprocessed by PrepareSampleReading. If PrepareSampleReading is given a time or index parameter that is eiher before
    /// or after the set of boxes available in main memory, then it should set the base stream position to a best guess
    /// position of the targeted box, and then call LazyRead.
    /// </summary>
    /// <param name="requestedBoxCount">count of boxes to read</param>
    void LazyRead(int requestedBoxCount);

    // UpdateDuration needs to be implemented for a dynamically growing stream
    ulong UpdateDuration();

    void SynchronizeAllTracks(long position, ulong time);

    void Write();

    /// <summary>
    /// InitializeForWriting
    /// What we do here is initialize a couple of properties.
    /// </summary>
    /// <param name="mediaTracks">This is with the item type of whatever is the source media stream.</param>
    void InitializeForWriting(List<IsochronousTrackInfo> mediaTracks);

    ulong CurrMDatOffset
    {
      get;
      set;
    }

    void CheckMDatOffset(long tempFilePos);

    /// <summary>
    /// PrepareSampleWriting
    /// Note: this[CodecTypes.Audio, 1] is of the same type as sourceAudio, but different instances (one is the destination, the other source).
    /// </summary>
    /// <param name="sourceAudio"></param>
    /// <param name="sourceVideo"></param>
    void PrepareSampleWriting(IMediaTrack sourceAudio, IMediaTrack sourceVideo);

    /// <summary>
    /// PrepareSampleWriting
    /// This overloaded method accepts slices and codec type as params.
    /// </summary>
    /// <param name="slicesInfo">The list of slices to be written, in StreamDataBlockInfo format</param>
    /// <param name="codecType">A member of the CodecTypes enum</param>
    void PrepareSampleWriting(List<StreamDataBlockInfo> slicesInfo, CodecTypes codecType);

    /// <summary>
    /// WriteSamples
    /// Writing out a slice of both the audio and video tracks means that the tracks are going to be interleaved in the final mdat.
    /// </summary>
    /// <param name="sourceAudio"></param>
    /// <param name="sourceVideo"></param>
    void WriteSamples(IMediaTrack sourceAudio, IMediaTrack sourceVideo);

    void WriteSamples(IEnumerable<Slice> slices, CodecTypes codecType);

    /// <summary>
    /// FinalizeStream
    /// Write out the header and mdat to this final stream.
    /// </summary>
    void FinalizeStream();

    // this routine needs to prepare each of the track to iterate through samples which 
    // are between the set iterator window start and end times...
    // NOTE: for fragmented ISMV tracks, the time span requested (IteratorWindowEndTime minus IteratorWindowStartTime) must be greater than the shortest fragment.
    bool PrepareSampleReading(UInt64 IteratorWindowStartTime, UInt64 IteratorWindowEndTime);

    void AddTrack(IMediaTrack inTrack);

    void TriggerLogicalBreak(IMediaTrack inTrack);

    string ToString();

    // the event fires when a new media track if found in the incoming media stream...
    event MediaTrackAddedHandler MediaTrackAdded;

    // this event fires when a logical break in the input media stream is found...
    // for a fragmented file, this would be called at the start of each fragment...
    // for an mp4 file, this would be called at the start of the file only...
    event MediaTrackLogicalBreakHandler MediaTrackLogicalBreak;
  }
}
