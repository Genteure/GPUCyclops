using Media.Formats.MP4;

namespace Media.Formats.MicrosoftFMP4 {
  public class ISMAFile {
    public string strFileName { get; set; }
    public string strDir { get; set; }
    public MP4Stream mp4file { get; set; }

    public ISMAFile(string inDir, string inFileName) {
      strDir = inDir;
      strFileName = inFileName;

      // need to fix      mp4file = new MP4Stream(inDir + inFileName, System.IO.FileMode.Open);
    }

    //    public void GetFragmentPosition(ulong inTime, int TrackId, out ulong ChunkStart, out ulong ChunkLen) {
    //      mp4file.GetFragmentPosition(inTime, TrackId, out ChunkStart, out ChunkLen);
    //    }

  }
}
