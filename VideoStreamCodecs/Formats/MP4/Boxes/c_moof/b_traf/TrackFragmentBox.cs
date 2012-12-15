using System;
using System.Text;

namespace Media.Formats.MP4
{
    public class TrackFragmentBox : Box
    {
        public TrackFragmentBox() : base(BoxTypes.TrackFragment)
        {
          this.TrackFragmentHeaderBox = new TrackFragmentHeaderBox();
          this.TrackFragmentRunBox = new TrackFragmentRunBox();
        }

        public TrackFragmentBox(uint trackID, int sampleCount, uint fragRunFlags, uint defaultSampleFlags, uint sampleSize)
          : base(BoxTypes.TrackFragment)
        {
          // for now we set baseDataOffset to zero because we don't know yet where the base offset is
          // the base data offset should be set right after creation of all moof boxes
          ulong baseDatOffset = 0L;
          this.TrackFragmentHeaderBox = new TrackFragmentHeaderBox(trackID, defaultSampleFlags, sampleSize, baseDatOffset);
          this.Size += this.TrackFragmentHeaderBox.Size;
          uint firstSampleFlags;
          if (trackID == 1) // audio
            firstSampleFlags = 0x8002;
          else if (trackID == 2) // video
            firstSampleFlags = 0x4002;
          else throw new Exception("Fragments can't have more than two tracks for now");
          // set data offset to zero because we're just going to append one fragment after another
          uint dataOffset = 0;
          this.TrackFragmentRunBox = new TrackFragmentRunBox(sampleCount, fragRunFlags, dataOffset, firstSampleFlags); // FIXME: it seems there can be > 1 run boxes?
          this.Size += this.TrackFragmentRunBox.Size;
          this.IndependentAndDisposableSamplesBox = new IndependentAndDisposableSamplesBox(sampleCount);
          this.Size += this.IndependentAndDisposableSamplesBox.Size;
        }

        public override void Read(BoxReader reader)
        {
            using (SizeChecker checker = new SizeChecker(this, reader)) {
                base.Read(reader);
                this.TrackFragmentHeaderBox.Read(reader);
                this.TrackFragmentRunBox.Read(reader);

								while (checker.DataLeft() > 8) {
									// it appears that Independent and Disposable Sample Box is optional
									BoxType nextbox = reader.PeekNextBoxType();
									if (nextbox == BoxTypes.IndependentAndDisposableSamplesBox) {
										this.IndependentAndDisposableSamplesBox = new IndependentAndDisposableSamplesBox(this.TrackFragmentRunBox.Samples.Count);
										this.IndependentAndDisposableSamplesBox.Read(reader);
										continue;
									}

									if (nextbox == BoxTypes.UUID) {
										UserUniqueIDBox = new UserUniqueIDBox();
										UserUniqueIDBox.Read(reader);
										continue;
									}

									break; // this shouldn't happen, and it should force the SizeChecker to throw an exception as it means we didn't read everything...
								}
            }
        }

        public uint GetTrackID(BoxReader reader)
        {
          base.Read(reader);
          this.TrackFragmentHeaderBox.Read(reader);
          return this.TrackFragmentHeaderBox.TrackId;
        }

        public override void Write(BoxWriter writer) {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);
            this.TrackFragmentHeaderBox.Write(writer);
            this.TrackFragmentRunBox.Write(writer);
            this.IndependentAndDisposableSamplesBox.Write(writer);
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          xml.Append(TrackFragmentHeaderBox.ToString());
          xml.Append(TrackFragmentRunBox.ToString());
          xml.Append(IndependentAndDisposableSamplesBox.ToString());
          xml.Append("</box>");
          return (xml.ToString());
        }


        public TrackFragmentRunBox TrackFragmentRunBox { get; set; }
        public TrackFragmentHeaderBox TrackFragmentHeaderBox { get; set; }

			  // optional boxes:
        public IndependentAndDisposableSamplesBox IndependentAndDisposableSamplesBox { get; set; }
				public UserUniqueIDBox UserUniqueIDBox { get; set; }
    }
}
