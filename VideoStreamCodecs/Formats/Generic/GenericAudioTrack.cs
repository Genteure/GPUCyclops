using System;

namespace Media.Formats.Generic {

  public class GenericAudioTrack : GenericMediaTrack, IAudioTrack {

    public AudioPayloadType PayloadType
    {
      get;
      protected set;
    }

    public int ChannelCount
    {
      get;
      protected set;
    }

    public int SampleSize
    {
      get;
      protected set;
    }

    /// <summary>
    /// SampleRate - count of samples per second
    /// </summary>
    public int SampleRate
    {
      get;
      protected set;
    }

    public override ITrackFormat TrackFormat
    {
      get
      {
        return base.Format;
      }
      set
      {
        base.Format = value;
        ChannelCount = base.Format.ChannelCount;
        SampleSize = base.Format.SampleSize;
        SampleRate = base.Format.SampleRate;
        switch (base.Format.PayloadType)
        {
          case "mp4a":
            PayloadType = AudioPayloadType.mp4a;
            break;
          case "AAC":
          case "AACL": // from ISMC file (Microsoft)
            PayloadType = AudioPayloadType.aac;
            break;
          case "wma ":
            PayloadType = AudioPayloadType.wma;
            break;
          case "samr": // 3gp audio (quicktime)
            PayloadType = AudioPayloadType.samr;
            break;
          default:
            throw new Exception(string.Format("Unknown audio track payload type: {0}", base.Format.PayloadType));
        }
        base.Format.Codec.CodecType = CodecTypes.Audio;
      }
    }

    public GenericAudioTrack() {
      if (base.Format != null)
        base.Format.Codec.CodecType = CodecTypes.Audio;
      PayloadType = AudioPayloadType.unknown;
    }

    /// <summary>
    /// Constructor to use when reading from a stream.
    /// </summary>
    /// <param name="TrackFormat"></param>
    public GenericAudioTrack(GenericTrackFormat trackFormat, GenericMediaStream stream) : base(trackFormat)
    {
      base.ParentStream = stream;
    }

    ///// <summary>
    ///// Copy Constructor
    ///// </summary>
    ///// <param name="trak"></param>
    //public IGenericAudioTrack(IGenericAudioTrack trak)
    //    : base((GenericMediaTrack)trak)
    //{
    //    this.Codec.CodecType = CodecTypes.Audio;
    //    this.PayloadType = trak.PayloadType;
    //    this.ChannelCount = trak.ChannelCount;
    //    this.SampleSize = trak.SampleSize;
    //    this.SampleRate = trak.SampleRate;
    //}

    public GenericAudioTrack(RawAudioTrackInfo rawAudioInfo)
      : this()
    {
      this.ChannelCount = rawAudioInfo.ChannelCount;
      this.Codec.PrivateCodecData = rawAudioInfo.CodecPrivateData;
      this.PayloadType = rawAudioInfo.PayloadType;
      this.SampleRate = rawAudioInfo.SampleRate;
      this.SampleSize = rawAudioInfo.SampleSize;
    }

    public void PrepareSampleWriting(GenericMediaTrack sourceTrack, ref ulong currMdatOffset)
    {
      base.PrepareSampleWriting(sourceTrack, ref currMdatOffset);
    }
  }
}
