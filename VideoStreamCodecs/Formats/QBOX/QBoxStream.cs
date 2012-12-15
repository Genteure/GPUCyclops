using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Media.Formats.Generic;

namespace Media.Formats.QBOX {
  public class QBoxStream : GenericMediaStream {
    // members used when reading qbox file (not used when writing)
    private List<QBox> _audioBoxes;
    private List<QBox> _videoBoxes;
    private List<ushort> _videoTrackIDs;
    private Dictionary<ushort, int> tempIndices = new Dictionary<ushort, int>();
    private MediaTimeUtils[] MediaTimes = new MediaTimeUtils[40]; // sparse array indexed by track ID


    public QBoxStream()
      : base() {
      _audioBoxes = new List<QBox>();
      _videoBoxes = new List<QBox>();
      _videoTrackIDs = new List<ushort>();

      _audioBoxes.Clear();
      _videoBoxes.Clear();

      MediaTracks = new List<IMediaTrack>();
    }

    public override ulong DurationIn100NanoSecs {
      get {
        //double msDuration = OldGetDurationFromAudioQBox(base.Stream); // GetDurationFromLastAudioQBox();
        //base.Duration = (ulong)msDuration;
        return base.DurationIn100NanoSecs;
      }
      protected set { base.DurationIn100NanoSecs = value; }
    }

    /// <summary>
    /// GetDurationFromLastAudioQBox
    /// This is necessary for a dynamically growing stream.
    /// It needs to be threadsafe because we set and then reset the stream Position.
    /// Remove mutex and call this from LazyRead to avoid deadlocks.
    /// </summary>
    /// <returns></returns>
    public ulong GetDurationFromLastQBox() {
      if (Stream.CanSeek == false) return (0);

      lock (base.Stream) {
        long currentPos = Stream.Position; // store where we are at currently...

        // try to find an audio qbox...
        BinaryReader br = new BinaryReader(base.Stream);
        br.BaseStream.Position = br.BaseStream.Length - 8;
        while (true) {
          if (QBox.SeekPrevQBox(br)) {
            QBox box = new QBox();
            long boxPos = br.BaseStream.Position;
            box.Read(br);

            //            if (box.mSampleStreamType == QBox.QBOX_SAMPLE_TYPE_AAC)
            if (box.mStreamDuration != 0) {
              br.BaseStream.Position = currentPos;
              ulong ans = MediaTimes[box.mSampleStreamId].TicksToTime(box.mStreamDuration, MediaTimeUtils.TimeUnitType.OneHundredNanoSeconds);
              return (ans);
            } else {
              if (br.BaseStream.Position - 8 <= 0) {
                // no more data to read...
                br.BaseStream.Position = currentPos;
              }

              br.BaseStream.Position = boxPos - 8; // rewind a bit...
            }

          } else {
            //DurationIn100NanoSecs = 0; // return value assigned to DurationIn100NanoSecs
            break; // can't find a previous qbox!!
          }
        }
        br.BaseStream.Position = currentPos;
      }
      return (0U);
    }


    public override void Open(Stream stream) {
      base.Open(stream);
      _binaryReader = new BinaryReader(base.Stream);
    }

    public override void Create(Stream outStream) {
      base.Create(outStream);
      _binaryWriter = new BinaryWriter(base.Stream);
    }


    /// <summary>
    /// Read
    /// This gets called before PrepareSampleRead, so we just pre-Read so many QBoxes.
    /// If the input stream is "live", the base stream length is undetermined and will be set at current value.
    /// The base stream position cannot be assumed to be zero: it may already have been prepositioned to a certain point
    /// in the file near the end. Therefore, since the base stream position is just some random position initially, we have
    /// to search to the first qbox we can find.
    /// </summary>
    public override void Read() { LazyRead(int.MaxValue); }

    private long currStreamLength = 0L;

//nbl; removed as we shouldn't 'fix' bframe time stamps
		//private Dictionary<ushort, ulong> PrevTimeStamps = new Dictionary<ushort, ulong>();
		//private Dictionary<ushort, int> PrevIndices = new Dictionary<ushort, int>();

    public override void LazyRead(int requestedBoxCount) {
      QBox qbox = null;
      int i = 0;
      int boxCount = 0;

      lock (_binaryReader.BaseStream) {
        // clear out all qbox lists
        // we expect the payload buffers to stay intact because these are now referenced in Slices
        _audioBoxes.Clear();
        _videoBoxes.Clear();

        while ((boxCount < requestedBoxCount) && (_binaryReader.BaseStream.Position < _binaryReader.BaseStream.Length)) {
          try {
            qbox = new QBox();
            qbox.Read(_binaryReader);
            if (MediaTimes[qbox.mSampleStreamId] == null)
              MediaTimes[qbox.mSampleStreamId] = new MediaTimeUtils();
            MediaTimes[qbox.mSampleStreamId].SetClockRate(((qbox.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_120HZ_CLOCK) != 0U));

//nbl; we can't fill in duration for bframes as this doesn't make sense... the CTTS info is presentation time used for mp4 stuff
//qbox.FixTimeStamp(PrevTimeStamps, PrevIndices);  // <---- Kludge! Some qboxes may have mStreamDuration reset, fix it here

            boxCount++;
          }
            // for the moment we catch two different exceptions, yet all we do is break our while loop
          catch (EndOfStreamException eos) {
            string msg = eos.Message;
            break;
          } catch (Exception ex) {
            throw ex;
          }

          switch (qbox.SampleStreamTypeString()) {
            case "AAC":
            case "PCM":
            case "MP2A":
            case "Q711":
            case "Q722":
            case "Q726":
            case "Q728":
              _audioBoxes.Add(qbox);
              break;
            case "H264":
            case "H264_SLICE":
            case "JPEG":
            case "MPEG2_ELEMENTARY":
              if (!_videoTrackIDs.Contains(qbox.mSampleStreamId)) {
                _videoTrackIDs.Add(qbox.mSampleStreamId);
              }

              _videoBoxes.Add(qbox);
              break;
            case "VIN_STATS_GLOBAL":
            case "VIN_STATS_MB":
            case "USER_METADATA":
            case "DEBUG":
            default:
              System.Diagnostics.Debug.WriteLine("Unknown QBox: {0}", qbox.SampleStreamTypeString());
              break;
          }

          i++;
        } // end of while

      }

      // define the tracks, if we haven't already
      // note that for qboxes, we really only care about formats (QBoxTrackFormat), and tracks are just generic.
      if (MediaTracks.Count == 0 && qbox != null) {
        if (_audioBoxes.Count > 0) {
          ushort audioTrackID = _audioBoxes[0].mSampleStreamId;
          QBoxTrackFormat audioTrackFormat = new QBoxTrackFormat(_audioBoxes, audioTrackID, MediaTimes[audioTrackID]);
          QBoxAudioTrack audioTrack = new QBoxAudioTrack(audioTrackFormat, this);
          //          audioTrack.NextIndexToRead = tempIndices[audioTrackID];
          //GenericAudioTrack audioTrack = new GenericAudioTrack(audioTrackFormat, this);
          //this.Duration = audioTrack.TrackDuration;
          //this.TimeScale = (uint)audioTrack.SampleRate;
          base.AddTrack(audioTrack);
        }

        foreach (ushort trackID in _videoTrackIDs) {
          QBoxTrackFormat videoTrackFormat = new QBoxTrackFormat(_videoBoxes, trackID, MediaTimes[trackID]);
          QBoxVideoTrack videoTrack = new QBoxVideoTrack(videoTrackFormat, this);
          videoTrack.NextIndexToRead = (int) (qbox.mBoxContinuityCounter + 1);
          if (DurationIn100NanoSecs < videoTrack.TrackDurationIn100NanoSecs)
            this.DurationIn100NanoSecs = videoTrack.TrackDurationIn100NanoSecs;
          //this.TimeScale = videoTrack.TrackFormat.TimeScale;
          base.AddTrack(videoTrack);
        }
      } else if (_audioBoxes.Count > 0 && _videoBoxes.Count > 0) {
        // add qboxes to existing track formats
        foreach (GenericMediaTrack track in this.MediaTracks) {
          QBoxTrackFormat format = track.TrackFormat as QBoxTrackFormat;
          if (track is GenericAudioTrack) {
            format.AddMore(_audioBoxes);
          } else {
            format.AddMore(_videoBoxes);
          }
        }
      }

      if (currStreamLength < Stream.Length) {
        currStreamLength = Stream.Length;
        // if the duration we're getting from the last audio qbox is shorter than we already have, then don't bother
        ulong liveDuration = (ulong) GetDurationFromLastQBox(); // seek all the way forward and back, just to determine duration
        if (liveDuration > DurationIn100NanoSecs)
          DurationIn100NanoSecs = liveDuration;
        // might as well set audio and video durations
        foreach (IMediaTrack track in MediaTracks)
          track.TrackDurationIn100NanoSecs = DurationIn100NanoSecs;
      }
    }

    /// <summary>
    /// Forget current qbox lists in each track, then
    /// set stream position to input position.
    /// The caller will do a LazyRead.
    /// </summary>
    /// <param name="position"></param>
    /// <param name="time"></param>
    public override void SynchronizeAllTracks(long position, ulong time) {
      foreach (IMediaTrack track in MediaTracks) {
        track.TrackFormat.ResetTrack(time); // we don't need the return value (index) bec. in version 2 every qbox has a frame counter
      }
      Stream.Position = position;
    }

    public override ulong UpdateDuration() { return GetDurationFromLastQBox(); }


    /// <summary>
    /// CreateTracksForWriting
    /// Method for creating tracks given a list of RawBaseTrackInfo.
    /// </summary>
    /// <param name="tracksInfo">list of RawBaseTrackInfo</param>
    private void CreateTracksForWriting(List<IsochronousTrackInfo> tracksInfo) {
      // We can't just use the input tracksInfo and assign it to our _MediaTracks (that would not work because the types won't match).
      // The input or source media tracks will normally have different types than our _MediaTracks (although they are derived from the same GenericMediaTrack).
      foreach (IsochronousTrackInfo rawTrack in tracksInfo) {
        QBoxTrackFormat trackFormat = new QBoxTrackFormat(rawTrack);
        trackFormat.WriteFirstQBox(_binaryWriter);
        switch (rawTrack.HandlerType) {
          case "Audio":
            GenericAudioTrack audioTrack = new GenericAudioTrack(trackFormat, this);
            base.AddTrack(audioTrack);
            break;
          case "Video":
            GenericVideoTrack videoTrack = new GenericVideoTrack(trackFormat, this);
            base.AddTrack(videoTrack);
            break;
          default:
            throw new Exception("Unknown source handler type");
        }
      }
    }


    /// <summary>
    /// InitializeForWriting
    /// </summary>
    /// <param name="tracksInfo">input media tracks</param>
    public override void InitializeForWriting(List<IsochronousTrackInfo> mediaTracksInfo) {
      // create tracks and trackformats, write out first QBox for each track
      CreateTracksForWriting(mediaTracksInfo);
      base.InitializeForWriting(mediaTracksInfo);
    }

    public void WriteSamples(IMediaTrack sourceTrack) {
      CodecTypes codecType = sourceTrack.Codec.CodecType;
      WriteSamples(sourceTrack, codecType);
    }

    /// <summary>
    /// WriteSamples
    /// Writing out a slice of both the audio and video tracks means that the tracks are going to be interleaved in the final mdat.
    /// NOTE: For fragments, the derived class ISMVStreamWriter takes care of fragments having a separate mdat box for each fragment.
    /// </summary>
    /// <param name="sourceAudio"></param>
    /// <param name="sourceVideo"></param>
    public override void WriteSamples(IMediaTrack sourceAudio, IMediaTrack sourceVideo) {
      // NOTE: the sequence order of tracks is important!
      this.WriteSamples(sourceAudio);
      this.WriteSamples(sourceVideo);
    }

    /// <summary>
    /// WriteSamples
    /// Overloaded method for writing slices directly.
    /// </summary>
    /// <param name="slices">A List of Sample(s)</param>
    /// <param name="codecType">Member of CodecTypes enum</param>
    public override void WriteSamples(IEnumerable<Slice> slices, CodecTypes codecType) {
      if (codecType == CodecTypes.Audio) {
        GenericAudioTrack audioTrack = (GenericAudioTrack) MediaTracks.First(tr => tr.Codec.CodecType == codecType);
        QBoxTrackFormat trackFormat = (QBoxTrackFormat) audioTrack.TrackFormat;
        trackFormat.WriteSamples(_binaryWriter, slices);
      } else if (codecType == CodecTypes.Video) {
        GenericVideoTrack videoTrack = (GenericVideoTrack) MediaTracks.First(tr => tr.Codec.CodecType == codecType);
        QBoxTrackFormat trackFormat = (QBoxTrackFormat) videoTrack.TrackFormat;
        trackFormat.WriteSamples(_binaryWriter, slices);
      } else throw new Exception("WriteSamples: unknown codec type");
    }

    /// <summary>
    /// FinalizeStream
    /// Nothing needs to be done to finalize output stream.
    /// </summary>
    public override void FinalizeStream() {
      if (_binaryWriter != null)
        _binaryWriter.Close();
    }
  }
}
