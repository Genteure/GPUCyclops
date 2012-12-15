using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;
using System.Text;
using System.IO;
using Media.Formats.Generic;
using Media;

namespace Media.Formats
{
  public enum AudioPayloadType { unknown, mp4a, wma, samr, aac }
  public enum VideoPayloadType { unknown, mp4v, mjpeg, jpeg, vc1, avc1 }
  public enum TracksIncluded { Audio, Video, Both }

  [DataContract]
  public class BaseTrackInfo
  {
    protected DataContractSerializer serializer;

    // Track Duration
    [DataMember]
    public ulong DurationIn100NanoSecs { get; set; }

    // Track TimeScale
    [DataMember]
    public uint TimeScale { get; set; }

    // Destination Track ID
    [DataMember]
    public int TrackID { get; set; }

    // Source Track ID
    [DataMember]
    public int SourceTrackID { get; set; }

    [DataMember]
    public string HandlerType { get; set; }

    [DataMember]
    public string Brands { get; set; } // MP4-specific, used when recoding from QBox to MP4 (space delimited words)

    [DataMember]
    public bool CTTSOut { get; set; } // MP4 output specific for enabling CTTS box
  }

  [DataContract]
  public class IsochronousTrackInfo : BaseTrackInfo
  {
    [DataMember]
    public string CodecPrivateData { get; set; }

    [DataMember]
    public string ObjectDescriptor { get; set; }

    [DataMember]
    public string UserData { get; set; }

    [DataMember]
    public bool IsFragment { get; set; } // MP4 - specific

    [DataMember]
    public ulong MovieDurationIn100NanoSecs { get; set; } // MP4 - specific

    [DataMember]
    public uint MovieTimeScale { get; set; } // MP4 - specific

    Hints _hints;
    protected Hints Hints
    {
      get { return _hints; }
      set
      {
        _hints = value;

        if (_hints.CompatibleBrands != null)
        {
          StringBuilder sb = new StringBuilder();
          foreach (string s in _hints.CompatibleBrands)
          {
            sb.Append(s);
            sb.Append(",");
          }
          this.Brands = sb.ToString().TrimEnd(new char[1] { ',' });
        }
      }
    }


    public object GetEdtsBox()
    {
      switch (HandlerType)
      {
        case "Audio":
          return Hints.object1;
        case "Video":
          return Hints.object2;
        default:
          throw new Exception("Unknown source handler type");
      }
    }

    public static List<IsochronousTrackInfo> GetTrackCharacteristics(IMediaStream source, TracksIncluded audioVideo, int videoTrackID)
    {
      List<IsochronousTrackInfo> tracksInfo = new List<IsochronousTrackInfo>(source.MediaTracks.Count);
      IsochronousTrackInfo baseTrkInfo = null;
      int videoID = 1; // first destination video track ID should be 1
      foreach (IMediaTrack track in source.MediaTracks)
      {
        if (((track.Codec.CodecType == CodecTypes.Audio) && (audioVideo != TracksIncluded.Video)) ||
          ((track.Codec.CodecType == CodecTypes.Video) && ((videoTrackID == 0) || (track.TrackID == videoTrackID)) &&
          (audioVideo != TracksIncluded.Audio)))
        {
          switch (track.Codec.CodecType)
          {
            case CodecTypes.Audio:
              baseTrkInfo = new RawAudioTrackInfo(source);
              break;
            case CodecTypes.Augment:
              baseTrkInfo = null; // FIXME: need IsochronousTrackInfo class for Augment
              break;
            case CodecTypes.Meta:
              baseTrkInfo = null; // FIXME: need IsochronousTrackInfo classe for Meta
              break;
            case CodecTypes.Video:
              int sourceVideoID = (videoTrackID == 0) ? track.TrackID : videoTrackID;
              baseTrkInfo = new RawVideoTrackInfo(source, sourceVideoID);
              baseTrkInfo.TrackID = videoID++;
              break;
            default:
              throw new Exception("Unknown track type in input");
          }
          if (baseTrkInfo != null)
            tracksInfo.Add(baseTrkInfo);
        }
      }

      if ((videoID == 1) && (videoTrackID > 0))
        throw new Exception("Track ID specified not found in source stream");

      return tracksInfo;
    }
  }


  [DataContract]
  public class RawAudioTrackInfo : IsochronousTrackInfo
  {
    [DataMember]
    public int Volume { get; set; }

    [DataMember]
    public int Balance { get; set; }

    [DataMember]
    public short ChannelCount { get; set; }

    [DataMember]
    public short SampleSize { get; set; }

    [DataMember]
    public int SampleRate { get; set; }

    [DataMember]
    public AudioPayloadType PayloadType { get; set; }

    public RawAudioTrackInfo()
    {
      this.HandlerType = "Audio";
    }

    public RawAudioTrackInfo(IMediaStream stream)
      : this((IAudioTrack)stream[CodecTypes.Audio, 0])
    {
      this.MovieDurationIn100NanoSecs = stream.DurationIn100NanoSecs;
      this.MovieTimeScale = Hints.StreamTimeScale;
    }

    public RawAudioTrackInfo(IAudioTrack audio)
      : this()
    {
      this.Hints = audio.ParentStream.Hints;
      if ((this.ChannelCount != (short)audio.ChannelCount) ||
          (this.SampleSize != (short)audio.SampleSize) ||
          (this.SampleRate != audio.SampleRate))
      {
        // overwrite what was derived from PrivateCodecData
        this.ChannelCount = (short)audio.ChannelCount;
        this.SampleSize = (short)audio.SampleSize;
        this.SampleRate = audio.SampleRate;
      }
      this.PayloadType = audio.PayloadType;
      this.TrackID = 0; // set destination track ID for audio to zero
      this.SourceTrackID = audio.TrackID;
      if (audio.TrackFormat != null)
      {
        this.DurationIn100NanoSecs = audio.TrackFormat.DurationIn100NanoSecs; // track specific duration, in 100 nanosec units
        this.TimeScale = audio.TrackFormat.TimeScale; // track specific time scale
      }
      else
      {
        this.DurationIn100NanoSecs = audio.TrackDurationIn100NanoSecs; // track specific duration, in 100 nanosec units
        this.TimeScale = (uint)audio.SampleRate; // track specific time scale
      }

      this.CodecPrivateData = audio.Codec.PrivateCodecData;
    }

  }


  [DataContract]
  public class RawVideoTrackInfo : IsochronousTrackInfo
  {
    [DataMember]
    public VideoPayloadType PayloadType { get; set; }

    [DataMember]
    public int Height { get; set; }

    [DataMember]
    public int Width { get; set; }

    [DataMember]
    public int AspectRatioX { get; set; }

    [DataMember]
    public int AspectRatioY { get; set; }

    public RawVideoTrackInfo()
    {
    }

    public RawVideoTrackInfo(IVideoTrack video)
    {
      Hints = video.ParentStream.Hints;
      string pcd = video.Codec.PrivateCodecData;
      this.CodecPrivateData = pcd;
      this.HandlerType = "Video";
      this.PayloadType = video.PayloadType;
      this.Height = video.FrameSize.Height;
      this.Width = video.FrameSize.Width;
      this.SourceTrackID = video.TrackID; // set source track ID here
      if (video.TrackFormat != null)
      {
        this.DurationIn100NanoSecs = video.TrackFormat.DurationIn100NanoSecs;
        this.TimeScale = video.TrackFormat.TimeScale;
      }
      else
      {
        this.DurationIn100NanoSecs = video.TrackDurationIn100NanoSecs;
        this.TimeScale = (uint)TimeSpan.TicksPerSecond;
      }

      if (video.IsAnamorphic)
      {
        AspectRatioX = 4;
        AspectRatioY = 3;
      }
      else
      {
        AspectRatioY = AspectRatioX = 1;
      }
    }

    public RawVideoTrackInfo(IMediaStream inStream, int trackID)
      : this((IVideoTrack)inStream[CodecTypes.Video, trackID])
    {
      if (inStream.ObjectDescriptor != null)
      {
        this.ObjectDescriptor = inStream.ObjectDescriptor;
      }
      if (inStream.UserData != null)
      {
        this.UserData = inStream.UserData;
      }
      this.MovieDurationIn100NanoSecs = inStream.DurationIn100NanoSecs;
      this.MovieTimeScale = Hints.StreamTimeScale;
    }

    private ulong BE32(uint val)
    {
      ulong retVal = val;
      if (BitConverter.IsLittleEndian)
      {
        byte[] bytes = BitConverter.GetBytes(val);
        byte tmp = bytes[0];
        bytes[0] = bytes[3];
        bytes[3] = tmp;
        tmp = bytes[1];
        bytes[1] = bytes[2];
        bytes[2] = tmp;
        retVal = BitConverter.ToUInt32(bytes, 0);
      }
      return retVal;
    }

    /// <summary>
    /// Look into a slice just to get width, height, and aspect ratio.
    /// NOTE: This is no longer used.
    /// </summary>
    private void GetScreenDimensions(IVideoTrack video)
    {
      if ((video.PayloadType == VideoPayloadType.unknown) || (video.PayloadType == VideoPayloadType.jpeg) || (video.PayloadType == VideoPayloadType.mjpeg))
        return;

      // this will only work for H.264 video source

      IMediaTrackSliceEnumerator slices = (IMediaTrackSliceEnumerator)video.GetEnumerator();
      slices.MoveNext();
      Slice slice = slices.Current; ;
      int countToZero = slice.SliceSize;
      ulong totalSize = 0UL;

      BinaryReader reader = new BinaryReader(new MemoryStream(slice.SliceBytes));
      while (countToZero > 4)
      {
        ulong naluLen = BE32(reader.ReadUInt32());
        long nextPos = reader.BaseStream.Position + (long)naluLen;
        uint typ = reader.ReadByte();
        if ((naluLen > (ulong)countToZero) || (naluLen < 2))
          throw new Exception("Invalid video payload");

        // access unit delimiter (aud) always comes first and its size is not added to total size because
        // it is be added back to the payload.
        if ((typ & 0x1Fu) == 9u)
        {
          if (naluLen != 2)
            throw new Exception("Wrong nalu delimiter length");
          reader.ReadByte(); // discard (we don't need it here)
        }

        // if nalu type is Sequence Param Set, pick up width and height
        // also, build private codec data from this SPS
        // NOTE: it matters which video track this qbox belongs!
        if ((typ & 0x1Fu) == 7u)
        {
          byte[] buf = new byte[naluLen];
          reader.Read(buf, 1, (int)naluLen - 1);
          totalSize += (4 + naluLen);
          // parse the SPS bit stream, just to get the correct width and height of video.
          BitReader bitReader = new BitReader(new MemoryStream(buf));
          H264SPS sps = new H264SPS();
          sps.Read(bitReader);
          Width = (int)sps.gWidth;
          Height = (int)sps.gHeight;
          if (sps.VUIParametersPresent)
          {
            AspectRatioX = sps.vuiParams.AspectRatioX;
            AspectRatioY = sps.vuiParams.AspectRatioY;
          }
        }

        countToZero -= ((int)naluLen + 4);
        reader.BaseStream.Position = nextPos;
      }
    }
  }

  [DataContract]
  public class RawTrackInfo
  {
    [DataMember]
    public RawAudioTrackInfo AudioTrackInfo { get; set; }

    [DataMember]
    public RawVideoTrackInfo VideoTrackInfo { get; set; }

    public RawTrackInfo()
    {
      AudioTrackInfo = new RawAudioTrackInfo();
      VideoTrackInfo = new RawVideoTrackInfo();
    }

    public RawTrackInfo(GenericAudioTrack audio, GenericVideoTrack video)
    {
      AudioTrackInfo = new RawAudioTrackInfo(audio);
      VideoTrackInfo = new RawVideoTrackInfo(video);
    }

    public void WriteToStream(StreamWriter stream)
    {
      DataContractSerializer serializer = new DataContractSerializer(typeof(RawTrackInfo));
      serializer.WriteObject(stream.BaseStream, this);
    }

    public static RawTrackInfo ReadFromStream(StreamReader stream)
    {
      DataContractSerializer deserializer = new DataContractSerializer(typeof(RawTrackInfo));
      XmlReader xmlReader = XmlReader.Create(stream.BaseStream);
      return (RawTrackInfo)deserializer.ReadObject(xmlReader, false);
    }
  }
}
