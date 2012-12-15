using System;
using System.Text;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{
    using System.Collections.Generic;

    public class IndependentAndDisposableSamplesBox : FullBox
    {
        private int m_numSamples;

        public IndependentAndDisposableSamplesBox(int numSamples) : base(BoxTypes.IndependentAndDisposableSamplesBox)
        {
            this.m_numSamples = numSamples;
            this.Samples = new List<IndependentAndDisposableSample>(this.m_numSamples);
            this.Size += (ulong)numSamples;
        }

        public override void Read(BoxReader reader)
        {
            using (new SizeChecker(this, reader))
            {
                base.Read(reader);
                for (int i = 0; i < this.m_numSamples; i++)
                {
                    IndependentAndDisposableSample item = new IndependentAndDisposableSample();
                    item.Read(reader);
                    this.Samples.Add(item);
                }
            }
        }

        public override void Write(BoxWriter writer) {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);
            for (int i = 0; i < this.m_numSamples; i++) {
              IndependentAndDisposableSample item = Samples[i];
              item.Write(writer);
            }
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          xml.Append("<samples>");
          for (int i=0; i<Samples.Count; i++)
            xml.Append(Samples[i].ToString());
          xml.Append("</samples>");
          xml.Append("</box>");
          return (xml.ToString());
        }

        public void AddOneSample(StreamDataBlockInfo data)
        {
          IndependentAndDisposableSample sample = new IndependentAndDisposableSample();
          sample.SampleHasRedundancy = 0;
          switch (data.SliceType)
          {
            case SliceType.WMA: // audio
            case SliceType.MP4A:
            case SliceType.AAC:
              sample.SampleDependsOn = 2;
              sample.SampleIsDependedOn = 2;
              break;
            case SliceType.IFrame:
              sample.SampleIsDependedOn = 1;
              sample.SampleDependsOn = 2;
              break;
            case SliceType.DFrame:
              sample.SampleIsDependedOn = 1;
              sample.SampleDependsOn = 1;
              break;
            default:
              throw new Exception("Invalid sample type, cannot add to independent and disposable sample");
          }
          this.Samples.Add(sample);
        }


        public List<IndependentAndDisposableSample> Samples { get; set; }
    }
}
