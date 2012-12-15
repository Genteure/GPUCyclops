using System.Diagnostics;
using System.Text;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Media.Formats.Generic;


namespace Media.Formats.MP4
{

  public class MP4StreamReader : MP4Stream, IDisposable {

      protected BoxReader m_reader = null;


        public MP4StreamReader() {
        }

        public override void Open(Stream inStream) {
          base.Open(inStream); // base.Stream here is an input file (read only)
          this.m_reader = new BoxReader(inStream);
          //base.EOF = false;
        }

        public override void Open(string fileName) {
          FileStream input = new FileStream(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
          this.Open(input);
        }

        public void Close() {
          if (this.m_reader != null) { this.m_reader.Close(); this.m_reader = null; }
          //base.EOF = false;
        }

        public void Dispose()
        {
          if (this.m_reader != null) this.m_reader.Close();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.m_reader.Close();
            }
        }

        protected void CreateTracks<T1, T0,  T2>() where T1 : GenericAudioTrack, new() where T0 : GenericVideoTrack, new() where T2 : MP4TrackFormat, new()
        {
          // boolean for calling event MediaTrackLogicalBreak only once
          bool breakCalled = false;
          GenericMediaTrack trak = null;
          T2 trackFormat;
          foreach (TrackBox trackb in mmb.TrackBoxes)
          {
            trackFormat = new T2();
            trackFormat.TrackBox = trackb;
            switch (trackb.PayloadType)
            {
              case "samr": // 3gpp audio
              case "wma ":
              case "mp4a":
                trak = new T1();
                Hints.object1 = trackb.EdtsBox;
                break;
              case "mp4v": // 3gpp video
              case "vc-1":
              case "avc1":
                trak = new T0();
                IVideoTrack vt = trak as IVideoTrack;
                MP4TrackFormat mp4format = trackFormat as MP4TrackFormat;
                vt.IsAnamorphic = mp4format.IsAnamorphic;
                Hints.object2 = trackb.EdtsBox;
                break;
              case "mp4s":
                break;  // ignore - in the case of vc-1 and wma, these are processed in ISMVStreamReader

              default:
                throw new Exception(string.Format("Unknown track type: {0}", trackb.PayloadType));
            }
            if (trak != null)
            {
              trak.TrackFormat = trackFormat;
              trak.ParentStream = this;
              AddTrack(trak);

              if (!breakCalled)
              {
                TriggerLogicalBreak(trak);
                breakCalled = true;
              }
            }
          }
        }

        // This read routine is responsible only for gaining enough information about the input file
        // so that the samples can be understood and decoded... it should not read the samples themselves.
        //
        // Note that, as it stands, only the first Read matters in the case of MP4. Subsequent Reads don't
        // do anything. This is because the first Read reads all boxes in MP4 except the MDAT box. If the MP4 file
        // is very large, we don't expect the header boxes to be very large also. MP4 spends only about
        // 32 bytes of header data for every block of samples, and a block of video samples can be as large
        // as a megabyte or even more, for larger pictures. Each block's duration ranges from a second to 
        // three seconds, which means that even a 24-hour movie can take up only about 2.7Mbytes of header data.
        // FIXME: The while loop won't work if the stream does not end (e.g., live streams). This Read
        // should end when all non-MDAT box headers are read-in. (Maybe we should rename this method to
        // ReadHeaders?) --CCT 3/4/2012
        public override void Read() {
          LazyRead(int.MaxValue);
        }

        public override void LazyRead(int requestedBoxCount) {
          //this.m_reader.BaseStream.Seek(0L, SeekOrigin.Begin);

          BoxType boxType;

          while (this.m_reader.BaseStream.Position < this.m_reader.BaseStream.Length) {

            boxType = this.m_reader.PeekNextBoxType();
            if (boxType == BoxTypes.MovieFragment) {
                IsMediaStreamFragmented = true;
                break; // don't process fragment here, do it in the ISMV class (which is derived from this one)
            }
            else if (boxType == BoxTypes.FileType)
            {
              ftb = new FileTypeBox();
              ftb.Read(this.m_reader);
              Hints.CompatibleBrands = ftb.CompatibleBrands;
            }
            else if (boxType == BoxTypes.Movie)
            {
              mmb = new MovieMetadataBox();
              mmb.Read(this.m_reader);
              if (mmb.ObjectDescriptorBox != null)
                base.ObjectDescriptor = mmb.ObjectDescriptorBox.Contents;
              if (mmb.UserDataBox != null)
                base.UserData = mmb.UserDataBox.Data;
            }
            else if (boxType == BoxTypes.Free)
            {
              FreeBox freeb = new FreeBox();
              freeb.Read(this.m_reader);
              FreeBoxList.Add(freeb);
            }
            else if (boxType == BoxTypes.MediaData) // mdat
            {
              MediaDataBox mdb = new MediaDataBox();
              mdb.Read(this.m_reader);  // this doesn't really read all of mdat: payload is skipped
              MediaDataBoxList.Add(mdb);
            }
            else if (boxType == BoxTypes.MovieFragmentRandomAccess)
            {
              MovieFragmentRandomAccessBox = new MovieFragmentRandomAccessBox();
              MovieFragmentRandomAccessBox.Read(this.m_reader);
            }
            else if (boxType == BoxTypes.Free)
            {
              FreeBox freeBox = new FreeBox();
              freeBox.Read(this.m_reader);
              FreeBoxList.Add(freeBox);
            } else {
              // invalid box, just stop reading
              break;
              //Box box2 = new Box(boxType);
              //box2.Read(this.m_reader);
              //FreeBoxList.Add(box2);
              //Debug.WriteLine(string.Format("Unknown BoxType: {0}", box2.Type.ToString()));
            }
          } // end of while

          // now that we know all about the input file in memory... fill a few structures to help others gain access to this information...
          // this is for the case in which the mp4 file contains moov boxes (MovieMetadataBoxes).
          if ((mmb != null) && (MediaTracks.Count == 0)) {
            DurationIn100NanoSecs = (ulong)TimeArithmetic.ConvertToStandardUnit(mmb.MovieHeaderBox.TimeScale, mmb.MovieHeaderBox.Duration);
            Hints.StreamTimeScale = mmb.MovieHeaderBox.TimeScale;
            if (!IsMediaStreamFragmented)
              CreateTracks<GenericAudioTrack, GenericVideoTrack, MP4TrackFormat>();
          }
        } // end of Read method


      public override string ToString() {
        StringBuilder xml = new StringBuilder();

        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.Append("<MP4Stream>");

        if (ftb != null) xml.Append(ftb.ToString());
        if (mmb != null) xml.Append(mmb.ToString());

        foreach (FreeBox fr in FreeBoxList) { xml.AppendLine(fr.ToString()); }
        foreach (MediaDataBox md in MediaDataBoxList) { xml.AppendLine(md.ToString()); }

        if (MovieFragmentRandomAccessBox != null) xml.Append(MovieFragmentRandomAccessBox.ToString());

        xml.Append("</MP4Stream>");
        return (xml.ToString());
      }

    }


}
