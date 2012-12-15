using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Media;
using Media.Formats;

#if SILVERLIGHT
using System.Windows.Media;
#endif

namespace Driver
{

  public class BaseRecode
  {
    public BaseRecode()
    {
      _maxBlockDuration = 30000000;
    }

    public BaseRecode(IMediaStream srcStream, IMediaStream destStream) : this()
    {
      // streams must be open already
      if (srcStream.Stream == null)
        throw new ArgumentException("Source Stream must be open already");
      if (destStream.Stream == null)
      {
        Thread.Sleep(100);
        if (destStream.Stream == null)
          throw new ArgumentException("Destination Stream must have been created already");
      }
      this.SourceStream = srcStream;
      this.DestStream = destStream;
      this.SourceStream.MediaTrackAdded += new MediaTrackAddedHandler(SourceStream_MediaTrackAdded);
    }

    // Properties

    public IMediaStream SourceStream { get; private set; } // can only be set at start, so might as well make it settable only in private (during construction)
    public IMediaStream DestStream { get; private set; } // same: can only set at start
    public List<IsochronousTrackInfo> TrackInfo { get; set; }
    bool _cttsOut = false;
    public bool CTTSOut
    {
      get { return _cttsOut; }
      set
      {
        _cttsOut = value;
      }
    }

    protected TracksIncluded audioOrVideoOrBoth;

    UInt64 _maxBlockDuration;
    public UInt64 MaxIterateDuration 
    {
      get
      {
        //if (SourceStream.CachingEnabled)
        if (SourceStream.MediaTracks[0].CacheMgr != null)
            return SourceStream.MediaTracks[0].CacheMgr.ReadAheadExtent;
        else return _maxBlockDuration;
      }
      set 
      {
        if (SourceStream.CachingEnabled)
        {
          foreach (IMediaTrack track in SourceStream.MediaTracks)
          {
            if (track.CacheMgr != null) track.CacheMgr.ReadAheadExtent = value;
          }
          foreach (IMediaTrack track in DestStream.MediaTracks)
          {
            if (track.CacheMgr != null) track.CacheMgr.ReadAheadExtent = value;
          }
        }
        else _maxBlockDuration = value;
      } // user must be able to set this in case it needs to be increased
    }

	 private object _tag = null;
	  /// <summary>
	  /// Allows us to attach a small piece of extra information with this recoder instance.
	  /// Usefull with progress report operations.
	  /// </summary>
	 public object Tag
	 {
		 get { return _tag; }
		 set { _tag = value; }
	 }

    // Event(s)

    // this event fires when a new media track is found in the incoming media stream...
    public event MediaTrackAddedHandler RecodeTrackAvailable;
    public event SampleHandler RecodeSampleAvailable;

	  public delegate void ProgressDelegate(BaseRecode recoder, float progress, bool status, object state);
	  public event ProgressDelegate RecodeProgressUpdate;

	  protected void RaiseRecodeProgressUpdate(float progress, bool status, object state)
	  {
		  var del = RecodeProgressUpdate;
		  if (del != null)
		  {
			  del(this, progress, status, state);
		  }
	  }

    /// <summary>
    /// AdjustTrackSpecsToDestination
    /// The purpose of this is to control the recoding by modifying two instances of the class 
    /// BaseTrackInfo under the two subclasses RawAudioTrackInfo and RawVideoTrackInfo.
    /// By default, these subclass instances take on the characteristics of the input media.
    /// NOTE: This method should be XML driven or should pick up parameters from a configuration file.
    /// FIXME: What about other track types?
    /// </summary>
    protected void AdjustTrackSpecsToDestination()
    {

      foreach (IsochronousTrackInfo trackDef in TrackInfo)
      {
        trackDef.TrackID = 0; // reset track ID (destination stream should determine track ID)
        if (trackDef is RawVideoTrackInfo)
          trackDef.CTTSOut = CTTSOut;
      }

      uint oneSecondTicks = (uint)TimeSpan.TicksPerSecond;

      if (DestStream.GetType().FullName.Equals("Media.Formats.MP4.MP4StreamWriter"))
      {
        TrackInfo.ForEach(delegate(IsochronousTrackInfo trk)
        {
          // set the movie time scale to 1,000
          trk.MovieTimeScale = 1000;

          // Set the track time scale to 10,000.
          // QuickTime cannot handle ulong durations, and our mp4 writer automatically switches to ulong if
          // a movie is more than 2,166,748,000 units long (a value which goes beyond int.MaxValue).
          // The track duration can get this high if the time scale is 10,000,000 which is what Expression uses.
          trk.TimeScale = 10000;

          if (trk is RawVideoTrackInfo)
          {
            trk.CTTSOut = CTTSOut;
          }
          else if (trk is RawAudioTrackInfo)
          {
            // if we are recoding to MP4 from HyperAsset, private codec data should be set as follows
            if (SourceStream.GetType().FullName.Contains(".AssetMediaStream")) // This needs to encompass the new AssetMediaStream2 class.
              trk.CodecPrivateData = "038080220000000480801640150020000001F4000001F4000580800511900000000680800102";
          }
        });
      }
      else if (DestStream.IsMediaStreamFragmented)
      {
        TrackInfo.ForEach(delegate(IsochronousTrackInfo trk)
        {
          if (trk is RawVideoTrackInfo)
          {
            // modify RawVideoTrackInfo: for fragmented tracks, timescale should be = oneSecondTicks
            // rvti.MovieDurationIn100NanoSecs = rvti.MovieDurationIn100NanoSecs * (oneSecondTicks / rvti.MovieTimeScale); // FIXME: what if rvti.MovieTimeScale > oneSecondTicks ticks?
            trk.MovieTimeScale = oneSecondTicks;
            //rvti.DurationIn100NanoSecs = rvti.DurationIn100NanoSecs * (oneSecondTicks / rvti.TimeScale);
            trk.TimeScale = oneSecondTicks;

            trk.IsFragment = true;
          }
        });
      }

      RawAudioTrackInfo rati = (RawAudioTrackInfo)TrackInfo.First(t => t is RawAudioTrackInfo);
      if ((rati != null) && (audioOrVideoOrBoth == TracksIncluded.Video))
      {
        TrackInfo.Remove(rati);
        rati = null;
      }

      if (audioOrVideoOrBoth == TracksIncluded.Audio)
      {
        IsochronousTrackInfo rvti;
        do
        {
          rvti = TrackInfo.FirstOrDefault(t => t is RawVideoTrackInfo);
          if (rvti != null)
          {
            TrackInfo.Remove(rvti);
          }
        } while (rvti != null);
      }
    }


    public virtual void Recode()
    {
    }

    public virtual void Recode(ulong startTime, ulong endTime)
    {
    }

    public virtual void Recode(ulong startTime, ulong endTime, ushort videoTrackID)
    {
    }

    public void Recode(ulong startTime, ulong endTime, ushort videoTrackID, bool cttsOut)
    {
      CTTSOut = cttsOut;
      Recode(startTime, endTime, videoTrackID);
    }

    protected void SourceStream_MediaTrackAdded(IMediaTrack inUpdatedTrack)
    {
      if (RecodeTrackAvailable != null)
        RecodeTrackAvailable(inUpdatedTrack);
      inUpdatedTrack.SampleAvailable += new SampleHandler(inUpdatedTrack_SampleAvailable);
    }

    void inUpdatedTrack_SampleAvailable(Slice sample)
    {
      if (RecodeSampleAvailable != null)
        RecodeSampleAvailable(sample);
    }
  }
}
