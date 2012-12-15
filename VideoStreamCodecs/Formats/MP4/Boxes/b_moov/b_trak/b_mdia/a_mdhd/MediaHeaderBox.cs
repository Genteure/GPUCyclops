/*
  aligned(8) class MediaHeaderBox extends FullBox(‘mdhd’, version, 0) {
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
    bit(1) pad = 0;
    unsigned int(5)[3] language; // ISO-639-2/T language code
    unsigned int(16) pre_defined = 0;
  }
  
version is an integer that specifies the version of this box (0 or 1)
creation_time is an integer that declares the creation time of the media in this rawTrack (in seconds since
midnight, Jan. 1, 1904, in UTC time)
modification_time is an integer that declares the most recent time the media in this rawTrack was
modified (in seconds since midnight, Jan. 1, 1904, in UTC time)
timescale is an integer that specifies the time-scale for this media; this is the number of time units that
pass in one second. For example, a time coordinate system that measures time in sixtieths of a
second has a time scale of 60.
duration is an integer that declares the duration of this media (in the scale of the timescale).
language declares the language code for this media. See ISO 639-2/T for the set of three character
codes. Each character is packed as the difference between its ASCII value and 0x60. Since the code
is confined to being three lower-case letters, these values are strictly positive.
 
*/

using System.Text;

namespace Media.Formats.MP4
{
  using System;

  public class MediaHeaderBox : FullBox {
    public MediaBox parent;

    public MediaHeaderBox(MediaBox inParent) : base(BoxTypes.MediaHeader) {
      parent = inParent;
    }

    public MediaHeaderBox(MediaBox inParent, ulong duration, uint timeScale)
      : this(inParent)
    {
      base.Version = (byte)((duration < uint.MaxValue) ? 0 : 1); // set this to 1 (see constructor above)
      //base.Version = 1;
      if (base.Version == 1)
        this.Size += 32UL;
      else
        this.Size += 20UL;
      this.creationTime = (ulong)DateTime.Now.Ticks;
      this.modificationTime = (ulong)DateTime.Now.Ticks;
      this.Duration = duration;
      this.TimeScale = timeScale;
      this.language = 5575; // 5575 (English language?)
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
          creationTime = (UInt32)reader.ReadUInt32();
          modificationTime = (UInt32)reader.ReadUInt32();
          TimeScale = reader.ReadUInt32();
          Duration = (UInt32)reader.ReadUInt32();
        }

        language = reader.ReadUInt16();
        reader.ReadUInt16(); // pre_defined = 0
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
                writer.WriteUInt32(TimeScale);
                writer.WriteUInt32((uint)Duration);
            }

            writer.Write((Int16)language);
            writer.Write((Int16)0);
        }
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<creationTime>").Append(ToDateTime((long)creationTime).ToString()).Append("</creationTime>");
        xml.Append("<modificationTime>").Append(ToDateTime((long)modificationTime).ToString()).Append("</modificationTime>");
        xml.Append("<timeScale>").Append(TimeScale).Append("</timeScale>");
        xml.Append("<duration>").Append(Duration).Append("</duration>");
        xml.Append("<language>").Append(Language).Append("</language>");
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
		private uint timeScale;
    public uint TimeScale { get { return(timeScale); }
			set {
				timeScale = value;
			}
		}
    public UInt64 Duration { get; set; }

    private ushort language;
    public string Language {
      get {
        // 16 bits:
        //    bit[0-4]
        //    bit[5-9]
        //    bit[10-14]
        char a = (char)((language << 1 >> 11) + 0x60);
        char b = (char)(((language << 6 & 0xF800) >> 11) + 0x60);
        char c = (char)(((language << 11 & 0xF800) >> 11) + 0x60);
        string ans = a.ToString() + b.ToString() + c.ToString();
        return (ans);
      }
      set {
        ushort a = (ushort)(value[0] - 0x60);
        ushort b = (ushort)(value[1] - 0x60);
        ushort c = (ushort)(value[2] - 0x60);
        language = 0;
        language = (ushort)((a << 11) | (b << 6) | (c));
      }
    }


  }
}
