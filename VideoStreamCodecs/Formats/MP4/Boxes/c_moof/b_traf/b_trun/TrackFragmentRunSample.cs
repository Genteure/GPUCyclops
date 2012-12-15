using System.Text;

namespace Media.Formats.MP4
{
    /// <summary>
    /// TrackFragmentRunSample
    /// These are not boxes.
    /// </summary>
    public class TrackFragmentRunSample
    {
        public void Read(BoxReader reader, uint flags)
        {
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags) flags, TrackFragmentRunBoxFlags.SampleDurationPresent)) {
              this.SampleDuration = reader.ReadUInt32();
//System.Diagnostics.Debug.WriteLine("Sample Duraiton: " + SampleDuration);
					}
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags) flags, TrackFragmentRunBoxFlags.SampleSizePresent)) {
              this.SampleSize = reader.ReadUInt32();
          }
          // We follow the logic in  Blue: presence of SampleFlags solely depends on SampleFlagsPresent
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags) flags, TrackFragmentRunBoxFlags.SampleFlagsPresent)) 
          {
              this.SampleFlags = reader.ReadUInt32();
          }
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags) flags, TrackFragmentRunBoxFlags.SampleCompositionTimeOffsetsPresent)) {
              this.SampleCompositionTimeOffset = reader.ReadUInt32();
          }
        }

        public void Write(BoxWriter writer, uint flags) {
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags) flags, TrackFragmentRunBoxFlags.SampleDurationPresent)) {
              writer.WriteUInt32(SampleDuration);
          }
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags) flags, TrackFragmentRunBoxFlags.SampleSizePresent)) {
              writer.WriteUInt32(SampleSize);
          }
          if (!EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)flags, TrackFragmentRunBoxFlags.FirstSampleFlagsPresent) &&
            EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags)flags, TrackFragmentRunBoxFlags.SampleFlagsPresent)) 
          {
            writer.WriteUInt32(SampleFlags);
          }
          if (EnumUtils.IsBitSet<TrackFragmentRunBoxFlags>((TrackFragmentRunBoxFlags) flags, TrackFragmentRunBoxFlags.SampleCompositionTimeOffsetsPresent)) {
              writer.WriteUInt32(SampleCompositionTimeOffset);
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append("<samplecompositiontimeoffset>").Append(SampleCompositionTimeOffset).Append("</samplecompositiontimeoffset>");
          xml.Append("<sampleduration>").Append(SampleDuration).Append("</sampleduration>");
          xml.Append("<sampleflags>").Append(SampleFlags).Append("</sampleflags>");
          xml.Append("<samplesize>").Append(SampleSize).Append("</samplesize>");
          return (xml.ToString());
        }

        public uint SampleCompositionTimeOffset { get; set; }
        public uint SampleDuration { get; set; }
        public uint SampleFlags { get; set; }
        public uint SampleSize { get; set; }
    }
}
