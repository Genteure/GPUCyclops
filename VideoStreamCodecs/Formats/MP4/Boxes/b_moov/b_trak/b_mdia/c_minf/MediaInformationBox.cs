/*
aligned(8) class MediaInformationBox extends Box(‘minf’) {
}
*/

using System.Text;
using System.Diagnostics;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{
  public class MediaInformationBox : Box {
    public MediaBox parent;
    public MediaInformationBox(MediaBox inParent) : base(BoxTypes.MediaInformation) {
      parent = inParent;
    }

    public MediaInformationBox(MediaBox inParent, IsochronousTrackInfo trackInfo)
      : this(inParent)
    {
      if (trackInfo.GetType() == typeof(RawAudioTrackInfo))
      {
        SoundMediaHeaderBox = new SoundMediaHeaderBox();
        this.Size += SoundMediaHeaderBox.Size;
      }
      else if (trackInfo.GetType() == typeof(RawVideoTrackInfo))
      {
        VideoMediaHeaderBox = new VideoMediaHeaderBox();
        this.Size += VideoMediaHeaderBox.Size;
      }
      DataInformationBox = new DataInformationBox();
      this.Size += DataInformationBox.Size;
      SampleTableBox = new SampleTableBox(this, trackInfo);
      // Size for SampleTableBox is determined only during SampleTableBox.FinalizeBox
    }


    /// <summary>
    /// Read - read a MediaInformationBox
    /// We go in a loop with an if-else statement, so ordering of sub-boxes does not matter.
    /// </summary>
    /// <param name="reader"></param>
    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        while (reader.BaseStream.Position < (long)(this.Size + this.Offset)) {
          long pos = reader.BaseStream.Position;
          Box test = new Box(BoxTypes.Any);
          test.Read(reader);
          reader.BaseStream.Seek(pos, System.IO.SeekOrigin.Begin);

          pos = reader.BaseStream.Position;
          if (test.Type == BoxTypes.SoundMediaHeader) {
            this.SoundMediaHeaderBox = new SoundMediaHeaderBox();
            SoundMediaHeaderBox.Read(reader);
          }

          else if (test.Type == BoxTypes.VideoMediaHeader) {
            this.VideoMediaHeaderBox = new VideoMediaHeaderBox();
            VideoMediaHeaderBox.Read(reader);
          }

          else if (test.Type == BoxTypes.DataInformation) {
            this.DataInformationBox = new DataInformationBox();
            DataInformationBox.Read(reader);
          }

          else if (test.Type == BoxTypes.SampleTable) {
            this.SampleTableBox = new SampleTableBox(this);
            SampleTableBox.Read(reader);
          }

          else if (test.Type == BoxTypes.NullMediaHeader)
          {
              this.NullMediaHeaderBox = new NullMediaHeaderBox();
              NullMediaHeaderBox.Read(reader);
          }

          else
          {
              test.Read(reader);
              Debug.WriteLine(string.Format("Unknown box type {0} in MediaInformationBox (minf)", test.Type.ToString()));
          }
        }
      }
    }


    /// <summary>
    /// Write - write out MediaInforationBox with the following order of sub-boxes:
    /// SoundMediaHeaderBox or VideoMediaHeaderBox
    /// DataInformationBox
    /// SampleTableBox
    /// </summary>
    /// <param name="writer"></param>
    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            if (SoundMediaHeaderBox != null)
               SoundMediaHeaderBox.Write(writer);
            if (VideoMediaHeaderBox != null)
               VideoMediaHeaderBox.Write(writer);
            DataInformationBox.Write(writer);
            if (SampleTableBox != null)
               SampleTableBox.Write(writer);
        }
    }
    

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());

      if (SoundMediaHeaderBox != null) xml.Append(SoundMediaHeaderBox.ToString());
      if (VideoMediaHeaderBox != null) xml.Append(VideoMediaHeaderBox.ToString());
      if (DataInformationBox != null) xml.Append(DataInformationBox.ToString());
      if (SampleTableBox != null) xml.Append(SampleTableBox.ToString());
      if (NullMediaHeaderBox != null) xml.Append(NullMediaHeaderBox.ToString());

      xml.Append("</box>");
      return (xml.ToString());
    }

    public void FinalizeBox()
    {
      SampleTableBox.FinalizeBox();
      this.Size += SampleTableBox.Size;
    }


    public SoundMediaHeaderBox SoundMediaHeaderBox = null;
    public VideoMediaHeaderBox VideoMediaHeaderBox = null;
    public DataInformationBox DataInformationBox = null;
    public SampleTableBox SampleTableBox = null;
    public NullMediaHeaderBox NullMediaHeaderBox = null;

  }
}
