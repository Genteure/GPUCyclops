/*
  sample_depends_on takes one of the following four values:
    0: the dependency of this sample is unknown;
    1: this sample does depend on others (not an I picture);
    2: this sample does not depend on others (I picture);
    3: reserved
  sample_is_depended_on takes one of the following four values:
    0: the dependency of other samples on this sample is unknown;
    1: other samples may depend on this one (not disposable);
    2: no other sample depends on this one (disposable);
    3: reserved
  sample_has_redundancy takes one of the following four values:
    0: it is unknown whether there is redundant coding in this sample;
    1: there is redundant coding in this sample;
    2: there is no redundant coding in this sample;
    3: reserved
*/

using System;
using System.Text;

namespace Media.Formats.MP4
{
    public class IndependentAndDisposableSample
    {
        public void Read(BoxReader reader)
        {
            uint num = reader.ReadByte();
            this.SampleDependsOn = (uint) ((num & 12) >> 2);
            this.SampleIsDependedOn = (uint) ((num & 0x30) >> 4);
            this.SampleHasRedundancy = (uint) ((num & 0xc0) >> 6);
        }

        public void Write(BoxWriter writer) {
          if ((SampleDependsOn > 3) || (SampleIsDependedOn > 3) || (SampleHasRedundancy > 3))
            throw new Exception("Invalid sdtp box value.");
          byte data = (byte)((SampleHasRedundancy << 6) | (SampleIsDependedOn << 4) | (SampleDependsOn << 2));
          writer.Write(data);
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append("<sampledependson>").Append(SampleDependsOn).Append("</sampledependson>");
          xml.Append("<samplehasredundancy>").Append(SampleHasRedundancy).Append("</samplehasredundancy>");
          xml.Append("<sampleisdependedon>").Append(SampleIsDependedOn).Append("</sampleisdependedon>");
          return (xml.ToString());
        }


        public uint SampleDependsOn { get; set; }
        public uint SampleHasRedundancy { get; set; }
        public uint SampleIsDependedOn { get; set; }
    }
}
