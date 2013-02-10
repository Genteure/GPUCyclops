using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Media.Formats.Generic;
using Media.H264;

namespace Media.Formats.RawH264
{
  /// <summary>
  /// H264Stream
  /// This is for writing to a raw H264 file only.
  /// Write several samples to the output file at a time, and therefore works only when caching is ON.
  /// </summary>
  public class H264Stream : GenericMediaStream
  {
    private bool firstSample = true;

    public H264Stream()
    {
    }

    public override void InitializeForWriting(List<IsochronousTrackInfo> mediaTracksInfo)
    {
      foreach (IsochronousTrackInfo trak in mediaTracksInfo)
      {
        if (trak.HandlerType.Equals("Video"))
        {
          GenericTrackFormat format = new H264Format();
          format.Codec = new Codec(CodecTypes.Video);
          format.Codec.PrivateCodecData = trak.CodecPrivateData;
          IMediaTrack item = new GenericVideoTrack(format, this);
          MediaTracks.Add(item);
        }
      }

      base.InitializeForWriting(mediaTracksInfo);
    }

    public void WriteSamples(IMediaTrack sourceTrack)
    {
      CodecTypes codecType = sourceTrack.Codec.CodecType;
      WriteSamples(sourceTrack, codecType);
    }

    public override void WriteSamples(IMediaTrack sourceAudio, IMediaTrack sourceVideo)
    {
      // ignore audio
      this.WriteSamples(sourceVideo);
    }

    /// <summary>
    /// WriteSamples
    /// Write several samples to the output file.
    /// </summary>
    /// <param name="slices"></param>
    /// <param name="codecType"></param>
    public override void WriteSamples(IEnumerable<Slice> slices, CodecTypes codecType)
    {
      if (codecType == CodecTypes.Audio)
      {
        // ignore audio
      }
      else if (codecType == CodecTypes.Video)
      {
        IMediaTrack trak = MediaTracks.First(t => t.Codec.CodecType == CodecTypes.Video);
        foreach (Slice sample in slices)
        {
          // convert to bit-stream format (prefix with 0001)
          Stream mstrm = new MemoryStream(sample.SliceBytes);
          Stream ostr = H264.H264Utilities.H264Stream(firstSample, trak.Codec.PrivateCodecData, mstrm, 0, 
            (uint)sample.SliceBytes.Length);
          firstSample = false;
          mstrm.Close();
          ostr.Position = 0L;
          byte[] buf = new byte[ostr.Length];
          ostr.Read(buf, 0, (int)ostr.Length);
          ostr.Close();
          sample.SliceBytes = buf;
          Stream.Write(sample.SliceBytes, 0, sample.SliceBytes.Length);
        }
      }
      else throw new Exception("WriteSamples: unknown codec type");
    }

    public override void FinalizeStream()
    {
      Stream.Close();
    }
  }

  public class H264Format : GenericTrackFormat
  {
    public override string PayloadType
    {
      get
      {
        return "Raw H264";
      }
    }

    private Codec _codec;

    public override Codec Codec
    {
      get
      {
        return _codec;
      }
      set
      {
        _codec = value;
      }
    }

    private ulong _duration;

    // Duration should be in units of 100 Nanosecs.
    public override ulong DurationIn100NanoSecs
    {
      get { return _duration; }
      set { _duration = value; }
    }

    public override void PrepareSampleWriting(List<StreamDataBlockInfo> sampleLocations, ref ulong currMdatOffset)
    {
      return;
    }
  }
}
