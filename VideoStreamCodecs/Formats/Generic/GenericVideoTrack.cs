namespace Media.Formats.Generic {

  public class GenericVideoTrack : GenericMediaTrack, IVideoTrack {

    // private var for debugging (be able to set a breakpoint
    private Size _frameSize;
    public Size FrameSize
    {
      get { return _frameSize; }
      private set { _frameSize = value; }
    }

    public VideoPayloadType PayloadType
    {
      get;
      protected set;
    }

    public bool IsAnamorphic
    {
      get;
      set;
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
        FrameSize = base.Format.FrameSize;
        switch (base.Format.PayloadType)
        {
          case "mp4v":
            this.PayloadType = VideoPayloadType.mp4v;
            break;
          case "H264": // QBox, ISMC (Microsoft)
          case "avc1":
            this.PayloadType = VideoPayloadType.avc1;
            break;
          case "vc-1":
            this.PayloadType = VideoPayloadType.vc1;
            break;
          default:
            break;
        }
        base.Format.Codec.CodecType = CodecTypes.Video;
      }
    }

    public GenericVideoTrack() {
      if (base.Format != null)
        base.Format.Codec.CodecType = CodecTypes.Video;
      FrameSize = new Size();
      PayloadType = VideoPayloadType.unknown;
      IsAnamorphic = false;
    }

    public GenericVideoTrack(GenericTrackFormat format, GenericMediaStream stream)
      : base(format)
    {
      base.ParentStream = stream;
      // NOTE: DO NOT initialize PayloadType and FrameSize here. These have already been initialized in the other constructor.
      //FrameSize = new Size();
      //PayloadType = VideoPayloadType.unknown;
    }

    ///// <summary>
    ///// Copy constructor
    ///// </summary>
    ///// <param name="vt"></param>
    //public IGenericVideoTrack(IGenericVideoTrack vt)
    //    : base((GenericMediaTrack)vt)
    //{
    //    this.Codec.CodecType = CodecTypes.Video;
    //    this.FrameSize = vt.FrameSize;
    //    this.PayloadType = vt.PayloadType;
    //}

    //public IGenericVideoTrack(RawVideoTrackInfo trakInfo) : this()
    //{
    //  this.FrameSize.Width = trakInfo.Width;
    //  this.FrameSize.Height = trakInfo.Height;
    //  this.PayloadType = trakInfo.PayloadType;
    //  this.Codec.PrivateCodecData = trakInfo.CodecPrivateData;
    //}

    public override void PrepareSampleWriting(IMediaTrack sourceTrack, ref ulong currMdatOffset)
    {
      base.PrepareSampleWriting(sourceTrack, ref currMdatOffset);
    }
  }
}
