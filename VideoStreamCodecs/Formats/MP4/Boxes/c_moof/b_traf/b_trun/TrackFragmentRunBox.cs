using System;
using System.Text;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{
    using System.Collections.Generic;

    public class TrackFragmentRunBox : FullBox
    {
        public TrackFragmentRunBox() : base(BoxTypes.TrackFragmentRun)
        {
          this.Duration = 0;
        }

        public TrackFragmentRunBox(int sampleCount, uint fragRunFlags, uint dataOffset, uint firstSampleFlags)
          : this()
        {
          base.Flags = fragRunFlags;

          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.DataOffsetPresent))
          {
            this.DataOffset = dataOffset;
            this.Size += 4UL;
          }
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.FirstSampleFlagsPresent))
          {
            this.FirstSampleFlags = firstSampleFlags;
            this.Size += 4UL;
          }
          this.SampleCount = (uint)sampleCount;
          this.Samples = new List<TrackFragmentRunSample>(sampleCount); // don't add items to list yet at this point
          uint ssize = 0;
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.SampleDurationPresent))
          {
            ssize += 4;
          }
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.SampleSizePresent))
          {
            ssize += 4;
          }
          if (!EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.FirstSampleFlagsPresent) &&
            EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.SampleFlagsPresent))
          {
            ssize += 4;
          }
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.SampleCompositionTimeOffsetsPresent))
          {
            ssize += 4;
          }
          this.Size += 4UL + (ulong)sampleCount * ssize;
        }

        /// <summary>
        /// Read
        /// NOTE: FirstSampleFlags and the flags in TrackFragmentRunSample are as follows:
        /// bit(6) reserved=0;                              // 6 bits reserved
        /// unsigned int(2) sample_depends_on;              // 2 bits used as int
        /// unsigned int(2) sample_is_depended_on;          // 2 bits used as int
        /// unsigned int(2) sample_has_redundancy;          // 2 bits used as int
        /// bit(3) sample_padding_value;                    // 3 bits
        /// bit(1) sample_is_difference_sample;             // 1 bit i.e. when 1 signals a non-key or non-sync sample
        /// unsigned int(16) sample_degradation_priority;   // 16 bits, total of 32 bits
        /// </summary>
        /// <param name="reader"></param>
        public override void Read(BoxReader reader)
        {
          using (new SizeChecker(this, reader)) {
            base.Read(reader);
            this.SampleCount = reader.ReadUInt32();
            if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.DataOffsetPresent))
            {
                this.DataOffset = reader.ReadUInt32();
            }
            if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.FirstSampleFlagsPresent))
            {
              this.FirstSampleFlags = reader.ReadUInt32();
            }
            this.Samples = new List<TrackFragmentRunSample>((int) this.SampleCount);
            for (int i = 0; i < this.SampleCount; i++) {
              TrackFragmentRunSample item = new TrackFragmentRunSample();
              item.Read(reader, base.Flags);
              this.Samples.Add(item);
              this.Duration += item.SampleDuration;
            }
          }
        }

        public override void Write(BoxWriter writer) {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);

            writer.WriteUInt32(SampleCount);
            if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.DataOffsetPresent))
            {
              writer.WriteUInt32(DataOffset);
            }
            if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)base.Flags, TrackFragmentRunBoxFlags.FirstSampleFlagsPresent))
            {
              writer.WriteUInt32(FirstSampleFlags);
            }
            for (int i = 0; i < this.SampleCount; i++) {
              TrackFragmentRunSample item = Samples[i];
              item.Write(writer, base.Flags);
            }

          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags) base.Flags, TrackFragmentRunBoxFlags.FirstSampleFlagsPresent)) 
            xml.Append("<firstsampleflags>").Append(FirstSampleFlags).Append("</firstsampleflags>");
          xml.Append("<samplecount>").Append(SampleCount).Append("</samplecount>");

          xml.Append("<samples>");
          for (int i = 0; i < this.SampleCount; i++) {
            TrackFragmentRunSample item = Samples[i];
            xml.Append(item.ToString());
          }
          xml.Append("</samples>");

          xml.Append("</box>");
          return (xml.ToString());
        }

        public void AddOneSample(StreamDataBlockInfo data, uint timeScale, uint defSize, uint defFlags, ref ulong currMdatOffset)
        {
          // at this point the samples List should have been created
          TrackFragmentRunSample sample = new TrackFragmentRunSample();
          sample.SampleCompositionTimeOffset = 0; // FIXME: let's see whether we can get by without setting this composition time offset
          // careful with overflow
          ulong product = ((ulong)data.SliceDuration) * ((ulong)timeScale);
          sample.SampleDuration = (uint)(product / (ulong)TimeSpan.FromSeconds(1.0).Ticks);
          this.Duration += sample.SampleDuration;
          if (defFlags == 0)
            sample.SampleFlags = 0; // FIXME: we are not setting the sample flags at all
          if (defSize == 0)
          {
            sample.SampleSize = (uint)data.SliceSize;
            currMdatOffset += sample.SampleSize;
          }
          else currMdatOffset += defSize;
          this.Samples.Add(sample);
        }


        public uint SampleCount { get; private set; }
        public uint DataOffset { get; private set; }
        public uint FirstSampleFlags { get; private set; }
        public uint Duration { get; private set; }
        public List<TrackFragmentRunSample> Samples { get; private set; }
    }
}
