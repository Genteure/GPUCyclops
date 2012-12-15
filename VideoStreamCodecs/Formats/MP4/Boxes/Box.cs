using System;
using System.Text;

namespace Media.Formats.MP4
{
    using System.IO;

    /// <summary>
    /// Box
    /// This is the base Box type. All box classes derive from this box.
    /// </summary>
    public class Box
    {
        private BoxType expectedType;
        private ulong largeSize;
        private uint size;
        private BoxType type;

        public Box(BoxType expectedType)
        {
            this.expectedType = expectedType;
            this.type = expectedType;
            this.size = 8; // base class size
        }

        public virtual void Read(BoxReader reader)
        {
          this.Offset = (ulong)reader.BaseStream.Position;
            this.size = reader.ReadUInt32();

            if (size == 0) {
              this.type = BoxTypes.Error;
              return;
            }

            this.type = reader.ReadBoxType();
            if (this.size == 1) {
              this.largeSize = reader.ReadUInt64();
            }
            if (this.expectedType == BoxTypes.Any) 
            {
                reader.BaseStream.Seek((long) (this.size - 8), SeekOrigin.Current);
            }
            else if (this.expectedType == BoxTypes.AnyDescription) 
            {
                return;
            }
            else if (this.type != this.expectedType)
            {
              throw new UnexpectedBoxException(this.expectedType, this.type);
            }
        }

        public virtual void Write(BoxWriter writer) {
			  
			  Common.Logger.Instance.Info("[Box::Write] writing a box of type [" + this.Type + ", " + this.GetType().Name + "], size [" + this.Size + "], offset [" + Offset + "], details: " + this.ToString());
          if (this.expectedType == BoxTypes.Any)  return;
          writer.WriteUInt32(this.size);
          writer.WriteBoxType(expectedType);
          if (this.size == 1)
          {
            writer.WriteUInt64(largeSize);
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();

          xml.Append("\n<box name=\"" + Type + "\">");
          xml.Append("<type>").Append(Type).Append("</type>");
          xml.Append("<offset>").Append(Offset).Append("</offset>");
          xml.Append("<size>").Append(Size).Append("</size>");
          return (xml.ToString());
        }

        public DateTime ToDateTime(long tenthsOfMicrosecondFrom1904) {
          DateTime referenceDate = new DateTime(1904, 1, 1);
          long time = (tenthsOfMicrosecondFrom1904 * 10000000) + referenceDate.Ticks;
			 if (time > DateTime.MinValue.Ticks && time < DateTime.MaxValue.Ticks)
			 {
				 return (new DateTime(time));
			 }

			 Common.Logger.Instance.Warning("[Box::ToDateTime] Invalid time calculated.");
			  // Invalid time calculated.
			 return DateTime.MinValue;
        }


        public ulong Offset { get; private set; }

        public ulong Size {
            get {
              if (this.size == 1)
                return this.largeSize;
              return this.size;
            }
            set {
              if (value > uint.MaxValue)
              {
                this.largeSize = value;
                if (this.size != 1)
                {
                  this.largeSize += 8UL; // 8 bytes additional for ulong largeSize
                  this.size = 1;
                }
              }
              else
                this.size = (uint)value; 
            }
        }

        public bool IsLarge
        {
          get { return this.size == 1; }
        }

        public BoxType Type
        {
            get
            {
                return this.type;
            }
        }
    }
}
