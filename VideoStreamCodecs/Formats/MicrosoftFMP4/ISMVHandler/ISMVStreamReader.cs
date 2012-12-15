using System;
using System.Text;
using Media.Formats.MP4;
using Media.Formats.Generic;

namespace Media.Formats.MicrosoftFMP4 {
  public class ISMVStreamReader : MP4StreamReader {

    public ISMVStreamReader()
      : base() {
    }

    public override void Read()
    {
      base.Read();


      if ((mmb != null) && (mmb.TrackBoxes.Length > 0))
      {
        CreateTracks<ISMVAudioTrack, ISMVVideoTrack, ISMVTrackFormat>(); // FIXME: create tracks based on what's in ISM file
      }
    }

    public override string ToString() {
      const string endXML = @"</MP4Stream>";
      StringBuilder xml = new StringBuilder();

      xml.Append(base.ToString());
      xml.Remove(xml.Length - endXML.Length, endXML.Length); // remove </MP4Stream>

      foreach (GenericMediaTrack track in this.MediaTracks) {
        ISMVTrackFormat format = (ISMVTrackFormat)track.TrackFormat;
        if (format.CurrentFragment != null)
          format.CurrentFragment.ToString(); // we can also print track.Fragments, but it can get very, very long
      }

      xml.Append(endXML);

      return (xml.ToString());
    }

  }
}
