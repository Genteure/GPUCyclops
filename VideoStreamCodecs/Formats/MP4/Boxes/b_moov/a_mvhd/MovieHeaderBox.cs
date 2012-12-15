/*
  aligned(8) class MovieHeaderBox extends FullBox(‘mvhd’, version, 0) {
    if (version==1) {
      unsigned int(64) creation_time;
      unsigned int(64) modification_time;
      unsigned int(32) timescale;
      unsigned int(64) duration;
    } else { // version==0
      unsigned int(32) creation_time;
      unsigned int(32) modification_time;
      unsigned int(32) timescale;
      unsigned int(32) duration;
    }
    template int(32) rate = 0x00010000; // typically 1.0
    template int(16) _volume = 0x0100; // typically, full _volume
    const bit(16) reserved = 0;
    const unsigned int(32)[2] reserved = 0;
    template int(32)[9] matrix =
      { 0x00010000,0,0,0,0x00010000,0,0,0,0x40000000 };
    // Unity matrix
    bit(32)[6] pre_defined = 0;
    unsigned int(32) next_track_ID;
  }
*/

using System.Text;

namespace Media.Formats.MP4
{
    using System;

  public class MovieHeaderBox : FullBox
  {
    public MovieHeaderBox() : base(BoxTypes.MovieHeader) {
    }

    public MovieHeaderBox(uint timeScale, ulong duration, float rate, float volume, uint[] matrix)
      : this()
    {
      this.Version = (byte)((duration < uint.MaxValue) ? 0 : 1);
      creationTime = (ulong)DateTime.Now.Ticks;  // 8 bytes
      modificationTime = creationTime;           // 8 bytes
      this.TimeScale = timeScale;                // 4 bytes
      this.Duration = duration;                  // 8 bytes
      this.Rate = rate;                          // 4 bytes
      this.Volume = volume;                      // 2 bytes
      this.Matrix.CopyFromArray( matrix );       // 9 * 4 = 36 bytes plus 34 bytes for reserved
      this.NextTrackID = 1; // always start with 1
      this.Size += (ulong)((this.Version == 0) ? (34 + 36 + 26) : (34 + 36 + 38));
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        if (Version == 1) {
          creationTime = reader.ReadUInt64();
          modificationTime = reader.ReadUInt64();
          TimeScale = reader.ReadUInt32();
          Duration = reader.ReadUInt64();
        } else {
          creationTime = (UInt64)reader.ReadUInt32();
          modificationTime = (UInt64)reader.ReadUInt32();
          TimeScale = reader.ReadUInt32();
          Duration = (UInt64)reader.ReadUInt32();
        }

        // Set rate 16.16.
        rate[0] = reader.ReadUInt16();
        rate[1] = reader.ReadUInt16();

        // Set _volume.
        reader.Read(volume, 0, 2);

        reader.ReadInt16(); // bit[16] - reserved = 0
        for (int x = 0; x < 2; x++) reader.ReadUInt32(); // bit(32)[2] - pre_defined = 0
        this.Matrix.Read(reader);
        for (int x = 0; x < 6; x++) reader.ReadUInt32(); // bit(32)[6] - pre_defined = 0

        NextTrackID = reader.ReadUInt32();
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
                writer.WriteUInt32(TimeScale);
                writer.WriteUInt64(Duration);
            }
            else
            {
                writer.WriteUInt32((uint)creationTime);
                writer.WriteUInt32((uint)modificationTime);
                writer.WriteUInt32((uint)TimeScale);
                writer.WriteUInt32((uint)Duration);
            }

            writer.Write(rate[0]);
            writer.Write(rate[1]);
            writer.Write(volume, 0, 2);

            writer.Write((short)0);
            for (int x = 0; x < 2; x++) writer.WriteUInt32((uint)0);
            this.Matrix.Write(writer);
            for (int x = 0; x < 6; x++) writer.WriteUInt32((uint)0);

            writer.WriteUInt32(NextTrackID);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<creationTime>").Append(ToDateTime((long)creationTime).ToString()).Append("</creationTime>");
      xml.Append("<modificationTime>").Append(ToDateTime((long)modificationTime).ToString()).Append("</modificationTime>");
      xml.Append("<timeScale>").Append(TimeScale).Append("</timeScale>");
      xml.Append("<duration>").Append(Duration).Append("</duration>");
      xml.Append("<rate>").Append(rate[0]).Append(".").Append(rate[1]).Append("</rate>");
      xml.Append("<volume>").Append(volume[0]).Append(".").Append(volume[1]).Append("</volume>");
      xml.Append(this.Matrix.ToString());
      xml.Append("<nextTrackID>").Append(NextTrackID).Append("</nextTrackID>");
      xml.Append("</box>");

      return (xml.ToString());
    }

    private UInt64 creationTime;
    public DateTime CreationTime {
      get { return base.ToDateTime((long)creationTime); }
      //set { creationTime = (ulong)value.Ticks; }
    }

    private UInt64 modificationTime;
    public DateTime ModificationTime {
      get { return base.ToDateTime((long)modificationTime); }
      //set { modificationTime = (ulong)value.Ticks; }
    }

    public uint TimeScale { get; private set; }

    private ushort[] rate = new ushort[2];
    public float Rate {
      get {
        float r = (float)rate[0] + (float)rate[1] / (float)(1 << 16);
        return r;
      }
      private set { rate[0] = (ushort)Math.Round(value); rate[1] = (ushort)((value - (float)rate[0]) * ((float)(1 << 16))); }
    }

    private byte[] volume = new byte[2];
    // Audio _volume?
    public float Volume {
      get {
        float v = (float)volume[0] + (float)volume[1] / (float)(1 << 8);
        return v;
      }
      private set { volume[0] = (byte)Math.Round(value); volume[1] = (byte)((value - (float)volume[0]) * ((float)(1 << 8))); }
    }

    public UInt64 Duration { get; set; }
    public RenderMatrix Matrix = new RenderMatrix();
    public uint NextTrackID { get; set; }
  }
}
