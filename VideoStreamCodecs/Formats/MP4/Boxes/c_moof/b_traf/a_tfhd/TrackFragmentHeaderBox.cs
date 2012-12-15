using System;
using System.Text;

namespace Media.Formats.MP4
{

    public class TrackFragmentHeaderBox : FullBox
    {
        public TrackFragmentHeaderBox() : base(BoxTypes.TrackFragmentHeader)
        {
        }

        public TrackFragmentHeaderBox(uint trackID, uint defaultSampleFlags, uint sampleSize, ulong baseDatOffset)
          : this()
        {
          this.TrackId = trackID;
          this.Size += 4UL;
          this.Flags = (uint)TrackFragmentHeaderBoxFlags.DefaultSampleFlagsPresent | (uint)TrackFragmentHeaderBoxFlags.BaseDataOffsetPresent;

          this.BaseDataOffset = 8UL; // just any value for now, or baseDatOffset;
          this.Size += 8UL; // ulong is 8 bytes
          this.DefaultSampleFlags = defaultSampleFlags;
          this.Size += 4UL;
          //this.DefaultSampleSize = sampleSize;
        }

        public override void Read(BoxReader reader)
        {
            using (new SizeChecker(this, reader))
            {
              base.Read(reader);
              this.TrackId = reader.ReadUInt32();
              if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags)base.Flags, TrackFragmentHeaderBoxFlags.BaseDataOffsetPresent))
              {
                  this._baseDataOffset = reader.ReadUInt64();
              }
              if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags)base.Flags, TrackFragmentHeaderBoxFlags.SampleDescriptionIndexPresent))
              {
                  this._sampleDescriptionIndex = reader.ReadUInt32();
              }
              if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags) base.Flags, TrackFragmentHeaderBoxFlags.DefaultSampleDurationPresent)) {
                  this._defaultSampleDuration = reader.ReadUInt32();
              }
              if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags) base.Flags, TrackFragmentHeaderBoxFlags.DefaultSampleSizePresent)) {
                  this._defaultSampleSize = reader.ReadUInt32();
              }
              if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags) base.Flags, TrackFragmentHeaderBoxFlags.DefaultSampleFlagsPresent)) {
                  this._defaultSampleFlags = reader.ReadUInt32();
              }
            }
        }

        public override void Write(BoxWriter writer) {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);

            writer.WriteUInt32(TrackId);
            if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags)base.Flags, TrackFragmentHeaderBoxFlags.BaseDataOffsetPresent))
            {
                writer.WriteUInt64(BaseDataOffset);
            }
            if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags)base.Flags, TrackFragmentHeaderBoxFlags.SampleDescriptionIndexPresent))
            {
                writer.WriteUInt32(SampleDescriptionIndex);
            }
            if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags) base.Flags, TrackFragmentHeaderBoxFlags.DefaultSampleDurationPresent)) {
                writer.WriteUInt32(DefaultSampleDuration);
            }
            if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags) base.Flags, TrackFragmentHeaderBoxFlags.DefaultSampleSizePresent)) {
                writer.WriteUInt32(DefaultSampleSize);
            }
            if (EnumUtils.IsBitSet<TrackFragmentHeaderBoxFlags>((TrackFragmentHeaderBoxFlags) base.Flags, TrackFragmentHeaderBoxFlags.DefaultSampleFlagsPresent)) {
                writer.WriteUInt32(DefaultSampleFlags);
            }
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          xml.Append("<trackid>").Append(TrackId).Append("</trackid>");
          xml.Append("<BaseDataOffset>").Append(BaseDataOffset).Append("</BaseDataOffset>");
          xml.Append("<defaultsampleduration>").Append(DefaultSampleDuration).Append("</defaultsampleduration>");
          xml.Append("<defaultsampleflags>").Append(DefaultSampleFlags).Append("</defaultsampleflags>");
          xml.Append("<defaultsamplesize>").Append(DefaultSampleSize).Append("</defaultsamplesize>");
          xml.Append("<sampledescriptionindex>").Append(SampleDescriptionIndex).Append("</sampledescriptionindex>");
          xml.Append("</box>");
          return (xml.ToString());
        }

        // properties
        // NOTE: modifying any of these properties CAN change the size of the box!

        ulong _baseDataOffset;
        public ulong BaseDataOffset 
        {
          get { return _baseDataOffset; }
          set
          {
            _baseDataOffset = value;
            if (0L != value)
            {
              if ((base.Flags & (uint)TrackFragmentHeaderBoxFlags.BaseDataOffsetPresent) == 0)
              {
                throw new Exception("TrackFragmentHeaderBox.BaseDataOffset cannot be set");
              }
            }
            else if ((base.Flags & (uint)TrackFragmentHeaderBoxFlags.BaseDataOffsetPresent) != 0)
            {
              throw new Exception("TrackFragmentHeaderBox.BaseDataOffset cannot be reset");
            }
          }
        }

        uint _defaultSampleDuration;
        public uint DefaultSampleDuration 
        {
          get { return _defaultSampleDuration; }
          set
          {
            _defaultSampleDuration = value;
            if (value != 0)
            {
              if ((base.Flags & (uint)TrackFragmentHeaderBoxFlags.DefaultSampleDurationPresent) == 0)
              {
                throw new Exception("TrackFragmentHeaderBox.DefaultSampleDuration cannot be set");
              }
            }
            else if ((base.Flags & (uint)TrackFragmentHeaderBoxFlags.DefaultSampleDurationPresent) != 0)
            {
              throw new Exception("TrackFragmentHeaderBox.DefaultSampleDuration cannot be reset");
            }
          }
        }

        uint _defaultSampleFlags;
        public uint DefaultSampleFlags 
        {
          get { return _defaultSampleFlags; }
          set
          {
            _defaultSampleFlags = value;
            if (value != 0)
            {
              if ((base.Flags & (uint)TrackFragmentHeaderBoxFlags.DefaultSampleFlagsPresent) == 0)
              {
                throw new Exception("TrackFragmentHeaderBox.DefaultSampleFlags cannot be set");
              }
            }
            else if ((base.Flags & (uint)TrackFragmentHeaderBoxFlags.DefaultSampleFlagsPresent) != 0)
            {
              throw new Exception("TrackFragmentHeaderBox.DefaultSampleFlags cannot be reset");
            }
          }
        }

        uint _defaultSampleSize;
        public uint DefaultSampleSize 
        {
          get { return _defaultSampleSize; }
          set
          {
            _defaultSampleSize = value;
            if (value != 0)
            {
              if ((base.Flags & (uint)TrackFragmentHeaderBoxFlags.DefaultSampleSizePresent) == 0)
              {
                throw new Exception("TrackFragmentHeaderBox.DefaultSampleSize cannot be set");
              }
            }
            else if ((base.Flags & (uint)TrackFragmentHeaderBoxFlags.DefaultSampleSizePresent) != 0)
            {
              throw new Exception("TrackFragmentHeaderBox.DefaultSampleSize cannot be reset");
            }
          }
        }

        uint _sampleDescriptionIndex;
        public uint SampleDescriptionIndex 
        {
          get { return _sampleDescriptionIndex; }
          set
          {
            _sampleDescriptionIndex = value;
            if (value != 0)
            {
              if ((this.Flags & (uint)TrackFragmentHeaderBoxFlags.SampleDescriptionIndexPresent) == 0)
              {
                throw new Exception("TrackFragmentHeaderBox.SampleDescriptionIndex cannot be set");
              }
            }
            else if ((this.Flags & (uint)TrackFragmentHeaderBoxFlags.SampleDescriptionIndexPresent) != 0)
            {
              throw new Exception("TrackFragmentHeaderBox.SampleDescriptionIndex cannot be reset");
            }
          }
        }

        public uint TrackId { get; set; }
    }
}
