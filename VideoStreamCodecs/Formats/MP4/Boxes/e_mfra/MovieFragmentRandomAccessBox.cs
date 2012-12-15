using System.Text;

namespace Media.Formats.MP4
{
    using System.Collections.Generic;

    public class MovieFragmentRandomAccessBox : Box
    {
        public MovieFragmentRandomAccessBox() : base(BoxTypes.MovieFragmentRandomAccess)
        {
          TrackFragmentRandomAccessBoxes = new List<TrackFragmentRandomAccessBox>();
          TrackFragmentRandomAccessBoxes.Clear();
          this.Size += 16UL; // 16 is the size of MovieFragmentRandomAccessOffsetBox
          MovieFragmentRandomAccessOffsetBox = new MovieFragmentRandomAccessOffsetBox(this.Size);
          //this.Size += MovieFragmentRandomAccessOffsetBox.Size; // this is already added (see above)
        }

        public override void Read(BoxReader reader)
        {
            using (new SizeChecker(this, reader))
            {
              base.Read(reader);
              this.TrackFragmentRandomAccessBoxes = new List<TrackFragmentRandomAccessBox>();
              while (reader.PeekNextBoxType() == BoxTypes.TrackFragmentRandomAccess) {
                  TrackFragmentRandomAccessBox item = new TrackFragmentRandomAccessBox();
                  item.Read(reader);
                  this.TrackFragmentRandomAccessBoxes.Add(item);
              }
              this.MovieFragmentRandomAccessOffsetBox = new MovieFragmentRandomAccessOffsetBox();
              this.MovieFragmentRandomAccessOffsetBox.Read(reader);
            }
        }

        public override void Write(BoxWriter writer)
        {
          using (new SizeCalculator(this, writer)) {
            base.Write(writer);
            foreach (TrackFragmentRandomAccessBox item in TrackFragmentRandomAccessBoxes) {
              item.Write(writer);
            }
            MovieFragmentRandomAccessOffsetBox.Write(writer);
          }
        }

        public override string ToString() {
          StringBuilder xml = new StringBuilder();
          xml.Append(base.ToString());
          xml.Append(MovieFragmentRandomAccessOffsetBox.ToString());
          for (int i=0; i<TrackFragmentRandomAccessBoxes.Count; i++)
            xml.Append(TrackFragmentRandomAccessBoxes[i].ToString());
          xml.Append("</box>");
          return (xml.ToString());
        }

        public MovieFragmentRandomAccessOffsetBox MovieFragmentRandomAccessOffsetBox { get; set; }

        public List<TrackFragmentRandomAccessBox> TrackFragmentRandomAccessBoxes { get; set; }
    }
}
