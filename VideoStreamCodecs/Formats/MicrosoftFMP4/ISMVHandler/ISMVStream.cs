using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Media.Formats.MP4;
using Media.Formats.Generic;

namespace Media.Formats.MicrosoftFMP4 {
  /// <summary>
  /// ISMVStream
  /// This class represents all tracks in the ISM stream. This List MediaTracks
  /// holds all tracks, which are contained in separate files.
  /// For the moment we implement only read or ingestion methods and properties.
  /// </summary>
  public class ISMVStream : GenericMediaStream {

    private ISMFile _ismFile;
    private string _mainFolderPath;

    public ISMVStream()
      : base() {
        IsMediaStreamFragmented = true;
        IsForReading = true; // we implement only reading methods for now
        Stream = new MemoryStream(); // dummy
        DurationIn100NanoSecs = 0L;
        FragmentDuration = 0L;
      }

    /// <summary>
    /// Open all files associated with the ISM.
    /// </summary>
    /// <param name="inPath">path name to ISM file without extension</param>
    public override void Open(string inPath, bool withCaching)
    {
      _mainFolderPath = Path.GetDirectoryName(inPath);
      _ismFile = new ISMFile(_mainFolderPath, Path.GetFileNameWithoutExtension(inPath));
      foreach (ISMElement element in _ismFile.ISMElements)
      {
        GenericMediaTrack ismTrack = null;
        if (element.FragmentType == FragmentType.Video)
        {
          ismTrack = new ISMVideoTrack(new ISMVTrackFormat(_mainFolderPath, element.Source, element), this);
        }
        else if (element.FragmentType == FragmentType.Audio)
        {
          ismTrack = new ISMAudioTrack(new ISMVTrackFormat(_mainFolderPath, element.Source, element), this);
        }
        MediaTracks.Add(ismTrack);

        // choose longest track duration to be stream duration
        if (DurationIn100NanoSecs < ismTrack.TrackDurationIn100NanoSecs)
          DurationIn100NanoSecs = ismTrack.TrackDurationIn100NanoSecs;
      }

      CachingEnabled = withCaching;
    }

    public override void Read()
    {
      // With Microsoft's ISM, we don't need to read the MP4 headers to figure out how many tracks there are;
      // we already know at this point because both the .ism and .ismc files have been scanned.
      foreach (GenericMediaTrack track in MediaTracks)
      {
        ISMVTrackFormat trackFormat = track.TrackFormat as ISMVTrackFormat;
        trackFormat.Read();
      }
    }

    /// <summary>
    /// LazyRead in this case simply means we read only one fragment at a time.
    /// </summary>
    /// <param name="requestedBoxCount"></param>
    public override void LazyRead(int requestedBoxCount)
    {
      Read();
    }

    public override string ToString() {
      const string endXML = @"</MP4Stream>";
      StringBuilder xml = new StringBuilder();

      xml.Append(base.ToString());
      xml.Remove(xml.Length - endXML.Length, endXML.Length); // remove </MP4Stream>

      //foreach (GenericMediaTrack track in this.MediaTracks) {
      //  ISMVTrackFormat format = (ISMVTrackFormat)track.TrackFormat;
      //  if (format.CurrentFragment != null)
      //    format.CurrentFragment.ToString(); // we can also print track.Fragments, but it can get very, very long
      //}

      xml.Append(endXML);

      return (xml.ToString());
    }

  }
}
