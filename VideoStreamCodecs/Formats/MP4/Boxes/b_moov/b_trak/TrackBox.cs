/*
 aligned(8) class TrackBox extends Box(‘trak’) {
 }
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{

  /// <summary>
  /// TrackBox
  /// Each track in a media stream is defined by a TrackBox. All details about that track is contained in this box.
  /// </summary>
  public class TrackBox : Box {
    uint movieTimeScale;

    public MovieMetadataBox parent;

    public TrackBox() : base(BoxTypes.Track) {
    }

    public TrackBox(MovieMetadataBox inParent, uint movieTScale) : this()
    {
      parent = inParent;
      movieTimeScale = movieTScale;
    }

    public TrackBox(MovieMetadataBox inParent, IsochronousTrackInfo trackInfo)
      : this(trackInfo)
    {
      parent = inParent;
    }

    public TrackBox(IsochronousTrackInfo trackInfo)
      : this()
    {
      float height = 0.0f;
      float width = 0.0f;
      if (trackInfo is RawVideoTrackInfo)
      {
        // set the TRACK width, which may differ from SampleDescription width and height, depending on Aspect Ratio
        RawVideoTrackInfo rvti = (RawVideoTrackInfo)trackInfo;
        height = rvti.Height;
        width = rvti.Width * ((float)rvti.AspectRatioX / (float)rvti.AspectRatioY);
      }
      ulong scaledDuration = (ulong)TimeArithmetic.ConvertToTimeScale(trackInfo.MovieTimeScale, trackInfo.DurationIn100NanoSecs);
      TrackHeaderBox = new TrackHeaderBox((uint)trackInfo.TrackID, scaledDuration, height, width); 
      // TrackHeaderBox = new TrackHeaderBox((uint)trackInfo.TrackID, (trackInfo.Duration * oneSecondTicks) / trackInfo.TimeScale, height, width);
      this.Size += TrackHeaderBox.Size;

      // skip the TrackReferenceBox for now
      //TrackReferenceBox = new TrackReferenceBox((uint)trackInfo.TrackID);
      //this.Size += TrackReferenceBox.Size;

#if EDTS_OUT
      EdtsBox = (EdtsBox)trackInfo.GetEdtsBox();
      if (EdtsBox != null)
      {
        this.Size += EdtsBox.Size;
        EdtsBox.ScaleToTarget(trackInfo.MovieTimeScale, trackInfo.TimeScale);
      }
#endif

      MediaBox = new MediaBox(trackInfo);
      // MediaBox.Size can only be determined during FinalizeBox
      // NOTE: NO Esds Box
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        TrackHeaderBox.Read(reader);
        while (this.Size > (ulong)(reader.BaseStream.Position - (long)this.Offset)) {
          long pos = reader.BaseStream.Position;
          BoxType next = reader.PeekNextBoxType();
          if (next == BoxTypes.Edts)
          {
              EdtsBox = new EdtsBox();
              EdtsBox.movieTimeScale = movieTimeScale;
              EdtsBox.Read(reader);
          }
          else if (next == BoxTypes.TrackReference)
          {
              TrackReferenceBox = new TrackReferenceBox();
              TrackReferenceBox.Read(reader);
          }
          else if (next == BoxTypes.Media)
          {
              MediaBox = new MediaBox(this);
              MediaBox.Read(reader);
          }
          else
          {
              Box unknown = new Box(BoxTypes.Any);
              unknown.Read(reader);
              Debug.WriteLine(string.Format("Unknow box type {0} in Trak box, skipped", next.ToString()));
          }
        }
      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            TrackHeaderBox.Write(writer);
            if (EdtsBox != null)
            {
              EdtsBox.Write(writer);
              //this.Size -= EdtsBox.Size;
            }
            MediaBox.Write(writer);
            if (TrackReferenceBox != null)
                TrackReferenceBox.Write(writer);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());

      xml.Append(TrackHeaderBox.ToString());
      if (MediaBox != null) xml.Append(MediaBox.ToString());
      if (TrackReferenceBox != null) xml.Append(TrackReferenceBox.ToString());
      if (EdtsBox != null) xml.Append(EdtsBox.ToString());
      xml.Append("</box>");
      return (xml.ToString());
    }

    public string PayloadType 
    { 
      get 
      {
        string pType = "";
        foreach (SampleEntry sampleinfo in this.MediaBox.MediaInformationBox.SampleTableBox.SampleDescriptionsBox.Entries)
           pType = sampleinfo.Type.ToString(); // there should only be one SampleEntry
        return pType; 
      } 
    }

    public void FinalizeBox()
    {
      MediaBox.FinalizeBox();
      this.Size += MediaBox.Size;
    }

    public TrackHeaderBox TrackHeaderBox = new TrackHeaderBox();
    public MediaBox MediaBox = null;
    public EdtsBox EdtsBox = null;
    public TrackReferenceBox TrackReferenceBox = null;

  }
}
