/*
 aligned(8) class TrackBox extends Box(‘trak’) {
 }
*/

using System.Text;
using System.Collections.Generic;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{
  class MovieExtendsBox : Box {
    MovieMetadataBox parent;
    public MovieExtendsBox(MovieMetadataBox inParent) : base(BoxTypes.MovieExtends) {
      parent = inParent;
    }

    public MovieExtendsBox(MovieMetadataBox inParent, List<IsochronousTrackInfo> trackInfos) : this(inParent)
    {
      MovieExtendsHeaderBox = new MovieExtendsHeaderBox((uint)0); // initial duration should be zero. prev: trackInfos[0].MovieDuration);
      this.Size += MovieExtendsHeaderBox.Size;

      if (TrackExtendBoxes == null)
        TrackExtendBoxes = new TrackExtendsBox[trackInfos.Count];
      //TrackBox[] tracks = parent.TrackBoxes;
      int i = 0;
      foreach (IsochronousTrackInfo tri in trackInfos)
      {
        if (tri.GetType() == typeof(RawAudioTrackInfo))
        {
          RawAudioTrackInfo rati = (RawAudioTrackInfo)tri;
          TrackExtendBoxes[i] = new TrackExtendsBox((uint)(i + 1), 1, 0, 0, 0); // trackID for audio is 1, sample description index within audio track is 1
          this.Size += TrackExtendBoxes[i].Size;
        }
        else if (tri.GetType() == typeof(RawVideoTrackInfo))
        {
          RawVideoTrackInfo rvti = (RawVideoTrackInfo)tri;
          TrackExtendBoxes[i] = new TrackExtendsBox((uint)(i + 1), 1, 0, 0, 0); // trackID for video is 2, sample description index within video track is 1
          this.Size += TrackExtendBoxes[i].Size;
        }
        i++;
      }
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        // Apple either omits the header, or puts it after the TrackExtendBoxes
        BoxType boxType = reader.PeekNextBoxType();

        if (boxType == BoxTypes.MovieExtendsHeader)
        {
            MovieExtendsHeaderBox = new MovieExtendsHeaderBox();
            MovieExtendsHeaderBox.Read(reader);
        }

        // Apple puts the MovieExtendsBoxes BEFORE TrackBoxes, so TrackBoxes may be NULL below.
        if (parent.TrackBoxes != null)
        {
            TrackExtendBoxes = new TrackExtendsBox[parent.TrackBoxes.Length];
            for (int i = 0; i < parent.TrackBoxes.Length; i++)
            {
                TrackExtendBoxes[i] = new TrackExtendsBox();
                TrackExtendBoxes[i].Read(reader);
            }
        }
        else
        {
            // get the size of a TrackExtendsBox, then use that to determine the size of the array
            Box test = new Box(BoxTypes.Any);
            test.Read(reader);
            reader.BaseStream.Position = (long)test.Offset;

            if (MovieExtendsHeaderBox == null)
                TrackExtendBoxes = new TrackExtendsBox[(this.Size - 8) / test.Size];
            else
                TrackExtendBoxes = new TrackExtendsBox[(this.Size - 8 - MovieExtendsHeaderBox.Size) / test.Size];
            for (int i = 0; i < TrackExtendBoxes.Length; i++)
            {
                TrackExtendBoxes[i] = new TrackExtendsBox();
                TrackExtendBoxes[i].Read(reader);
            }
        }
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            if (MovieExtendsHeaderBox != null)
                MovieExtendsHeaderBox.Write(writer);
            foreach (TrackExtendsBox teb in TrackExtendBoxes)
            {
                teb.Write(writer);
            }
        }
    }


    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      if (MovieExtendsHeaderBox != null)
          xml.Append(MovieExtendsHeaderBox.ToString());
      xml.Append("<TrackExtendBoxes>");
      for (int i=0; i<TrackExtendBoxes.Length; i++)
        xml.Append(TrackExtendBoxes[i].ToString());
      xml.Append("</TrackExtendBoxes>");
      xml.Append("</box>");
      return (xml.ToString());
    }


    MovieExtendsHeaderBox MovieExtendsHeaderBox;
    public TrackExtendsBox[] TrackExtendBoxes;
  }


}
