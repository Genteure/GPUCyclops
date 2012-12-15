using System.Text;

namespace Media.Formats.MP4
{
    public class MovieFragmentBox : Box
    {
        public MovieFragmentBox() : base(BoxTypes.MovieFragment)
        {
          this.MovieFragmentHeaderBox = new MovieFragmentHeaderBox();
          this.TrackFragmentBox = new TrackFragmentBox();
        }

        public MovieFragmentBox(uint sequenceNum, uint trackID, int sampleCount, uint fragRunFlags, uint defaultSampleFlags, uint sampleSize)
          : base(BoxTypes.MovieFragment)
        {
          this.MovieFragmentHeaderBox = new MovieFragmentHeaderBox(sequenceNum);
          this.Size += this.MovieFragmentHeaderBox.Size;
          this.TrackFragmentBox = new TrackFragmentBox(trackID, sampleCount, fragRunFlags, defaultSampleFlags, sampleSize); // just overwrite what's already there
          this.Size += this.TrackFragmentBox.Size;
        }

        public override void Read(BoxReader reader)
        {
            using (new SizeChecker(this, reader))
            {
                base.Read(reader);
                this.MovieFragmentHeaderBox.Read(reader);
                this.TrackFragmentBox.Read(reader);
            }
        }

        public uint GetTrackID(BoxReader reader)
        {
          base.Read(reader);
          this.MovieFragmentHeaderBox.Read(reader);
          return this.TrackFragmentBox.GetTrackID(reader);
        }

        public override void Write(BoxWriter writer) {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);

            // The movie fragment header contains a sequence number MovieFragmentHeaderBox.SequenceNumber, as a safety check.
            // The sequence number usually starts at 1 and must increase for each movie fragment in the file, on the order in which they occur.
            // This allows readers to verify integrity of the sequence;  it is an error to construct a file where the fragments are out
            // of sequence....
            // See one of the constructors for the Fragment class where this SequenceNumber is managed.
            this.MovieFragmentHeaderBox.Write(writer);
            this.TrackFragmentBox.Write(writer);
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          xml.Append(MovieFragmentHeaderBox.ToString());
          xml.Append(TrackFragmentBox.ToString());
          xml.Append("</box>");
          return (xml.ToString());
        }


        public MovieFragmentHeaderBox MovieFragmentHeaderBox { get; set; }
        public TrackFragmentBox TrackFragmentBox { get; set; }
    }
}
