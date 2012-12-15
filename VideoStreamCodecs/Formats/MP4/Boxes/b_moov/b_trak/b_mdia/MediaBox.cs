/*
 aligned(8) class MediaBox extends Box(‘mdia’) {
 }
*/

using System.Text;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{
  public class MediaBox : Box {
    public TrackBox parent;
    public MediaBox() : base(BoxTypes.Media) {
    }

    public MediaBox(TrackBox inParent)
      : base(BoxTypes.Media)
    {
      parent = inParent;
    }

    public MediaBox(IsochronousTrackInfo trackInfo)
      : this()
    {
      ulong scaledDuration = (ulong)TimeArithmetic.ConvertToTimeScale(trackInfo.TimeScale, trackInfo.DurationIn100NanoSecs);
      MediaHeaderBox = new MediaHeaderBox(this, scaledDuration, trackInfo.TimeScale);
      this.Size += MediaHeaderBox.Size;
      Codec codec = null;
      if (trackInfo.GetType() == typeof(RawAudioTrackInfo))
      {
        RawAudioTrackInfo audioInfo = (RawAudioTrackInfo)trackInfo;
        codec = new Codec(CodecTypes.Audio);
        codec.PrivateCodecData = audioInfo.CodecPrivateData;
      }
      else if (trackInfo.GetType() == typeof(RawVideoTrackInfo))
      {
        RawVideoTrackInfo videoInfo = (RawVideoTrackInfo)trackInfo;
        codec = new Codec(CodecTypes.Video);
        codec.PrivateCodecData = videoInfo.CodecPrivateData;
      }
      HandlerReferenceBox = new HandlerReferenceBox(this, codec);
      this.Size += HandlerReferenceBox.Size;
      MediaInformationBox = new MediaInformationBox(this, trackInfo);
      // MediaInformationBox.Size is indeterminate at this time; it is determined only during SampleTableBox.FinalizeBox
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        MediaHeaderBox = new MediaHeaderBox(this);
        HandlerReferenceBox = new HandlerReferenceBox(this);
        MediaInformationBox = new MediaInformationBox(this);

        MediaHeaderBox.Read(reader);
        HandlerReferenceBox.Read(reader);
        MediaInformationBox.Read(reader);
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            MediaHeaderBox.Write(writer);
            HandlerReferenceBox.Write(writer);
            MediaInformationBox.Write(writer);
        }
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append(MediaHeaderBox.ToString());
        xml.Append(HandlerReferenceBox.ToString());
        xml.Append(MediaInformationBox.ToString());
        xml.Append("</box>");
        return (xml.ToString());
    }

    public void FinalizeBox()
    {
      MediaInformationBox.FinalizeBox();
      this.Size += MediaInformationBox.Size;
    }


    public MediaHeaderBox MediaHeaderBox = null;
    public HandlerReferenceBox HandlerReferenceBox = null;
    public MediaInformationBox MediaInformationBox = null;

  }
}
