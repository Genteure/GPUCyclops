using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{

  /// <summary>
  /// MovieMetadataBox
  /// The moov box defines the whole movie in file. It can consist of several TrackBoxes.
  /// </summary>
  public class MovieMetadataBox : Box {

    private ObjectDescriptorBox _objectDescriptor;
    private UserDataBox _userData;

    public MovieMetadataBox() : base(BoxTypes.Movie) 
    {
      MovieHeaderBox = new MovieHeaderBox();
    }

    public MovieMetadataBox(List<IsochronousTrackInfo> trackInfos, float rate, float volume, uint[] matrix)
      : base(BoxTypes.Movie)
    {
      // initialize movie duration to zero, then increment it for every slice that is written
      ulong scaledDuration = (ulong)TimeArithmetic.ConvertToTimeScale(trackInfos[0].MovieTimeScale, trackInfos[0].MovieDurationIn100NanoSecs);
      MovieHeaderBox = new MovieHeaderBox(trackInfos[0].MovieTimeScale, scaledDuration, rate, volume, matrix);
      this.Size += MovieHeaderBox.Size;
      TrackBoxes = new TrackBox[trackInfos.Count];  // may have more than 2 tracks
      // MovieExtendsBox should only exist if this is a fragment
      if (trackInfos[0].IsFragment)
      {
        MovieExtendsBox = new MovieExtendsBox(this, trackInfos);
        this.Size += MovieExtendsBox.Size;
      }
    }


    public MovieHeaderBox MovieHeaderBox;
    public TrackBox[] TrackBoxes;
    MovieExtendsBox MovieExtendsBox = null;
    public ulong CurrMdatOffset = 8L; // current mdat offset for use when building moof and stbl boxes

    public ObjectDescriptorBox ObjectDescriptorBox
    {
      get { return _objectDescriptor; }
      set
      {
        _objectDescriptor = (ObjectDescriptorBox)value;
        this.Size += _objectDescriptor.Size;
      }
    }

    public UserDataBox UserDataBox
    {
      get { return _userData; }
      set
      {
        _userData = (UserDataBox)value;
        this.Size += _userData.Size;
      }
    }

    public void AddTrackBox(TrackBox tbox)
    {
      TrackBoxes[MovieHeaderBox.NextTrackID - 1] = tbox;
      tbox.TrackHeaderBox.TrackID = MovieHeaderBox.NextTrackID;
      MovieHeaderBox.NextTrackID++;
    }

    public override void Read(BoxReader reader) {
      long testpos = reader.BaseStream.Position;
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        MovieHeaderBox.Read(reader);

        // Don't assume any order in the boxes within this after the header.
        // (The order depends on the brand.)
        int tbcount = 0;
        TrackBox[] tmpTBs = new TrackBox[10];
        while (reader.BaseStream.Position < (long)(this.Size + this.Offset)) {
          long pos = reader.BaseStream.Position;

          Box tmpBox = new Box(BoxTypes.Any);
          tmpBox.Read(reader);

          reader.BaseStream.Position = pos;

          if (tmpBox.Type == BoxTypes.Track)
          {
              TrackBox tb = new TrackBox(this, MovieHeaderBox.TimeScale);
              tb.Read(reader);
              tmpTBs[tbcount] = tb;
              tbcount++;
          }

          else if (tmpBox.Type == BoxTypes.MovieExtends)
          {
              MovieExtendsBox = new MovieExtendsBox(this);
              MovieExtendsBox.Read(reader);
          }

          else if (tmpBox.Type == BoxTypes.ObjectDescriptor) // iods
          {
            _objectDescriptor = new ObjectDescriptorBox();
            _objectDescriptor.Read(reader);
          }

          else if (tmpBox.Type == BoxTypes.UserData) // udta
          {
            _userData = new UserDataBox();
            _userData.Read(reader);
          }

          // some cases below for things we currently ignore, thus we skip over them...
          else
          {
            byte[] buffer = new byte[tmpBox.Size];
            reader.Read(buffer, 0, (int)tmpBox.Size);
            //reader.BaseStream.Position = (long)(tmpBox.Size + tmpBox.Offset); // ignore for now
            Debug.WriteLine(string.Format("Unknown box type {0} in MovieMetadataBox (this), skipped", tmpBox.Type.ToString()));
          }
        }

        TrackBoxes = new TrackBox[tbcount];
        for (int i = 0; i < tbcount; i++)
        {
            TrackBoxes[i] = tmpTBs[i];
        }

      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);

            MovieHeaderBox.Write(writer);

            if (ObjectDescriptorBox != null)
            {
                ObjectDescriptorBox.Write(writer);
            }

            foreach (TrackBox tbox in TrackBoxes)
            {
                tbox.Write(writer);
            }

            if (UserDataBox != null)
            {
              UserDataBox.Write(writer);
            }

            // write out MovieExtendsBox during finalize
            if (MovieExtendsBox != null)
                MovieExtendsBox.Write(writer);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append(MovieHeaderBox.ToString());
      xml.Append("<TrackBoxes>");
      for (int i = 0; i < TrackBoxes.Length; i++)
        xml.Append(TrackBoxes[i].ToString());
      xml.Append("</TrackBoxes>");
      if (MovieExtendsBox != null) xml.Append(MovieExtendsBox.ToString());
      xml.Append("</box>");
      return (xml.ToString());
    }

    public void FinalizeBox()
    {
      foreach (TrackBox tbox in TrackBoxes)
      {
        tbox.FinalizeBox();
        this.Size += tbox.Size;
      }
    }

  }
}
