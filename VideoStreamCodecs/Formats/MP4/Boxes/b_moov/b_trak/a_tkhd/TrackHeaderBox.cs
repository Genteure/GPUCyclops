/*
  aligned(8) class TrackHeaderBox
    extends FullBox(‘tkhd’, version, flags){
      if (version==1) {
        unsigned int(64) creation_time;
        unsigned int(64) modification_time;
        unsigned int(32) track_ID;
        const unsigned int(32) reserved = 0;
        unsigned int(64) duration;
      } else { // version==0
        unsigned int(32) creation_time;
        unsigned int(32) modification_time;
        unsigned int(32) track_ID;
        const unsigned int(32) reserved = 0;
        unsigned int(32) duration;
      }
      const unsigned int(32)[2] reserved = 0;
      template int(16) layer = 0;
      template int(16) alternate_group = 0;
      template int(16) _volume = {if track_is_audio 0x0100 else 0};
      const unsigned int(16) reserved = 0;
      template int(32)[9] matrix=
        { 0x00010000,0,0,0,0x00010000,0,0,0,0x40000000 };
      // unity matrix
      unsigned int(32) _width;
      unsigned int(32) _height;
    }
*/

using System.Text;

namespace Media.Formats.MP4
{
  using System;

  public class TrackHeaderBox : FullBox {
    public TrackHeaderBox() : base(BoxTypes.TrackHeader) {
      this.Matrix = new RenderMatrix();
      this.Flags = 1; // 7; // default
    }

    public TrackHeaderBox(uint trackID, ulong duration, float height, float width) : this()
    {
      this.Version = (byte)((duration < uint.MaxValue) ? 0 : 1);
      this.TrackID = trackID; // TrackID is updated in MovieMetadataBox.cs when this trak is added to the moov
      this.creationTime = (ulong)DateTime.Now.Ticks;
      this.modificationTime = (ulong)DateTime.Now.Ticks;
      this.Duration = duration;
      if ((height == 0) && (width == 0))
        this._volume = 256; // 1.0
      else
        this._volume = 0;
      this._width = ((uint)Math.Floor(width)) << 16;
      this._width += (uint)((width - Math.Floor(width)) * 65536.0);
      this._height = ((uint)Math.Floor(height)) << 16;
      this._height += (uint)((height - Math.Floor(height)) * 65536.0);
      this.Size += (ulong)((this.Version == 0) ? (36 + 36 + 8) : (48 + 36 + 8)); // see calculation below, in Read
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        if (Version == 1) {
          creationTime = reader.ReadUInt64();
          modificationTime = reader.ReadUInt64();
          TrackID = reader.ReadUInt32();
          UInt32 reserved = reader.ReadUInt32(); // reserved = 0
          Duration = reader.ReadUInt64();
        } else {
          creationTime = (UInt32)reader.ReadUInt32();
          modificationTime = (UInt32)reader.ReadUInt32();
          TrackID = reader.ReadUInt32();
          reader.ReadUInt32(); // reserved = 0
          Duration = (UInt32)reader.ReadUInt32();
        }

        for (int x = 0; x < 2; x++) reader.ReadUInt32(); // int(32)[2] - reserved = 0  - 40 bytes
        Layer = reader.ReadUInt16(); // layer = 0
        AlternateGroup = reader.ReadUInt16(); // alternate_group = 0
        _volume = reader.ReadUInt16();
        reader.ReadUInt16(); // reserved = 0   -  48 bytes
        this.Matrix.Read(reader);
        _width = reader.ReadUInt32();
        _height = reader.ReadUInt32();
      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            if (Version == 1)
            {
                writer.WriteUInt64(creationTime);
                writer.WriteUInt64(modificationTime);
                writer.WriteUInt32(TrackID);
                writer.WriteUInt32((uint)0);
                writer.WriteUInt64(Duration);
            }
            else
            {
                writer.WriteUInt32((uint)creationTime);
                writer.WriteUInt32((uint)modificationTime);
                writer.WriteUInt32(TrackID);
                writer.WriteUInt32((uint)0);
                writer.WriteUInt32((uint)Duration);
            }

            for (int x = 0; x < 2; x++) writer.WriteUInt32((uint)0);
            writer.Write(Layer);
            writer.Write(AlternateGroup);
            writer.Write(_volume);
            writer.Write((UInt16)0);
            this.Matrix.Write(writer);
            writer.Write(_width);
            writer.Write(_height);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<creationTime>").Append(ToDateTime((long)creationTime).ToString()).Append("</creationTime>");
      xml.Append("<modificationTime>").Append(ToDateTime((long)modificationTime).ToString()).Append("</modificationTime>");
      xml.Append("<trackID>").Append(TrackID).Append("</trackID>");
      xml.Append("<duration>").Append(Duration).Append("</duration>");
      xml.Append("<layer>").Append(Layer).Append("</layer>");
      xml.Append("<alternateGroup>").Append(AlternateGroup).Append("</alternateGroup>");
      xml.Append("<volume>").Append(Volume).Append("</volume>");
      xml.Append(this.Matrix.ToString());
      xml.Append("<width>").Append(Width).Append("</width>");
      xml.Append("<height>").Append(Height).Append("</height>");
      xml.Append("</box>");

      return (xml.ToString());
    }


    private UInt64 creationTime;
    public DateTime CreationTime {
      get { return base.ToDateTime((long)creationTime); }
    }

    private UInt64 modificationTime;
    public DateTime ModificationTime {
      get { return base.ToDateTime((long)modificationTime); }
    }
    public uint TrackID { get; set; }
    public UInt64 Duration { get; set; }
    public ushort Layer { get; private set; }
    public ushort AlternateGroup { get; private set; }

    private ushort _volume;
    public float Volume 
    {
      get
      {
        float retVal = 0.0f;
        retVal = (_volume >> 8) + (_volume & 0xFF) / 0xFF;
        return retVal;
      }
    }

    public RenderMatrix Matrix;

    private uint _width;
    public float Width 
    {
      get
      {
        float retVal = 0.0F;
        retVal = (float)((_width >> 16) + (_width & 0xFFFF) / 0xFFFF);
        return retVal;
      }
    }

    private uint _height;
    public float Height
    {
      get
      {
        float retVal = 0.0F;
        retVal = (float)((_height >> 16) + (_height & 0xFFFF) / 0xFFFF);
        return retVal;
      }
    }
  }
}