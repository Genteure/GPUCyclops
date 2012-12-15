using System;
using System.Text;

namespace Media.Formats.MP4 {
  public class EdtsBox : Box {
    ElstBox elstBox;
    public uint movieTimeScale;
    public uint trackTimeScale;

    public EdtsBox() : base(BoxTypes.Edts) {}

    /// <summary>
    /// Edit List Container Box
    /// Kludge: we don't process this box. All we do is store it away in RawTrackInfo
    /// for passing to the stream writer when recoding.
    /// </summary>
    /// <param name="reader"></param>
    public override void Read(BoxReader reader) {
      //mp4Stream.ReadChildBoxes(boxes, (offset + (long)size));
      base.Read(reader);
      elstBox = new ElstBox();
      elstBox.Read(reader);
    }

      /// <summary>
      /// Write - unimplemented for this box.
      /// CCT: FIXME: we will have to add Write capability when we're dealing with RTSP and RTP protocols.
      /// </summary>
      /// <param name="writer"></param>
    public override void Write(BoxWriter writer)
    {
      using (new SizeCalculator(this, writer))
      {
        base.Write(writer);
        if (elstBox != null)
        {
          elstBox.Write(writer);
        }
      }
    }

    public void ScaleToTarget(uint newMovieTimeScale, uint newTrackTimeScale)
    {
      elstBox.ScaleToTarget(newMovieTimeScale, newTrackTimeScale, this);
    }

    /// <summary>
    /// GetEditTrackTime
    /// Calculate the additional track time in scale time units resulting from all edits.
    /// </summary>
    /// <returns></returns>
    public decimal GetEditTrackDuration(uint trackTimeScale)
    {
      this.trackTimeScale = trackTimeScale;
      return elstBox.GetEditTrackDuration(movieTimeScale, trackTimeScale);
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<edts/>");
        xml.Append("</box>");
        return (xml.ToString());
    }

  }

  struct EditList
  {
    public ulong segmentDuration; // may be only 32 bits
    public long mediaTime; // may be only 32 bits
    public short mediaRateInteger;
    public short mediaRateFraction;
  }

  public class ElstBox : FullBox
  {
    uint entryCount;
    EditList[] editList;

    public ElstBox() : base(BoxTypes.Elst) { }

    public override void Read(BoxReader reader)
    {
      using (new SizeChecker(this, reader))
      {
        base.Read(reader);
        entryCount = reader.ReadUInt32();
        editList = new EditList[entryCount];
        for (int i = 0; i < entryCount; i++)
        {
          if (Version == 0)
          {
            editList[i].segmentDuration = reader.ReadUInt32();
            editList[i].mediaTime = reader.ReadInt32();
          }
          else // must be 1
          {
            editList[i].segmentDuration = reader.ReadUInt64();
            editList[i].mediaTime = reader.ReadInt64();
          }
          editList[i].mediaRateInteger = (short)reader.ReadInt16();
          editList[i].mediaRateFraction = (short)reader.ReadInt16();
        }
      }
    }

    public override void Write(BoxWriter writer)
    {
      using (new SizeCalculator(this, writer))
      {
        base.Write(writer);
        writer.Write(entryCount);
        for (int i = 0; i < entryCount; i++)
        {
          if (Version == 0)
          {
            writer.Write((uint)editList[i].segmentDuration);
            writer.Write((int)editList[i].mediaTime);
          }
          else // must be 1
          {
            writer.Write(editList[i].segmentDuration);
            writer.Write(editList[i].mediaTime);
          }
          writer.Write(editList[i].mediaRateInteger);
          writer.Write(editList[i].mediaRateFraction);
        }
      }
    }

    public void ScaleToTarget(uint newMovieTimeScale, uint newTrackTimeScale, EdtsBox parent)
    {
      for (int i = 0; i < entryCount; i++)
      {
        editList[i].segmentDuration = (ulong)(((decimal)editList[i].segmentDuration / parent.movieTimeScale) * newMovieTimeScale);
        editList[i].mediaTime = (long)(((decimal)editList[i].mediaTime / parent.trackTimeScale) * newTrackTimeScale);
      }
    }

    public decimal GetEditTrackDuration(uint movieTimeScale, uint trackTimeScale) {
      decimal totalDuration = 0ul;

      for (int i = 0; i < entryCount; i++) {
        if ((editList[i].mediaRateInteger == 1) && (editList[i].mediaRateFraction == 0)) {
          if (editList[i].mediaTime == -1) {
            totalDuration -= ((decimal)editList[i].segmentDuration / (decimal)movieTimeScale);
          } else {
            totalDuration += ((decimal)editList[i].segmentDuration / (decimal)movieTimeScale);
            totalDuration += ((decimal)editList[i].mediaTime / (decimal)trackTimeScale);
          }
        } else {
          totalDuration += ((decimal)editList[0].segmentDuration / (decimal)movieTimeScale);
          totalDuration += ((decimal)editList[0].mediaTime / (decimal)trackTimeScale);
        }
      }

      // NBL, 9/7/12 The following was removed as it was not spec H.264
      // it assumed only one entry and thus only one rate integer and fraction
      //if ((mediaRateInteger == 1) && (mediaRateFraction == 0))
      //{
      //  for (int i = 0; i < entryCount; i++)
      //  {
      //    if (editList[i].mediaTime == -1)
      //    {
      //      totalDuration -= ((decimal)editList[i].segmentDuration / (decimal)movieTimeScale);
      //    }
      //    else
      //    {
      //      totalDuration += ((decimal)editList[i].segmentDuration / (decimal)movieTimeScale);
      //      totalDuration += ((decimal)editList[i].mediaTime / (decimal)trackTimeScale);
      //    }
      //  }
      //}
      //else if ((mediaRateInteger == 0) && (mediaRateFraction == 0))
      //{
      //  // dwell
      //  totalDuration += ((decimal)editList[0].segmentDuration / (decimal)movieTimeScale);
      //  totalDuration += ((decimal)editList[0].mediaTime / (decimal)trackTimeScale);
      //}
      return totalDuration;
    }
  }
}
