using System;
using System.Collections.Generic;
using System.IO;
using Media.Formats.Generic;
using Media.Formats.MP4;

namespace Media.Formats.MicrosoftFMP4 {
  public class ISMVFile {
    public string strFileName { get; set; }
    public string strDir { get; set; }
		public int TrackID { get; set; }
		public string CodecPrivateData { get; set; }
		public FileTypeBox ftb = new FileTypeBox();
		public MovieMetadataBox mmb = new MovieMetadataBox();

		public BoxReader boxReader { get; set; }
		public BoxReader boxReader2 { get; set; }
		public long tmpPosition = 0;
	  public int tmpIteration = 0;
	  public int bigTmpIteration = 0;

    public List<MediaDataBox> MediaDataBoxList = new List<MediaDataBox>();

    private bool audiovideoScanCompleted = false;
    private bool audioFound = false;
    private bool videoFound = false;
    private long nextVideoFragPosition = 0;
    private long nextAudioFragPosition = 0;
    private int audioTrackID;
    private int videoTrackID;
    private string _handlerType;

    public ISMVFile(string inDir, string inFileName) {
      strDir = inDir;
      strFileName = inFileName;
			boxReader = new BoxReader(File.Open(Path.Combine(inDir, inFileName), FileMode.Open, FileAccess.Read, FileShare.Read));
			boxReader2 = new BoxReader(File.Open(Path.Combine(inDir, inFileName), FileMode.Open, FileAccess.Read, FileShare.Read));

			ftb.Read(boxReader);
			mmb.Read(boxReader);
			BoxType nextType = boxReader.PeekNextBoxType();
    }

    public ISMVFile(string inDir, string inFileName, string handlerType)
      : this(inDir, inFileName)
    {
      _handlerType = handlerType;
    }
    public void InitializeForWriting(List<IsochronousTrackInfo> mediaTracks)
    {
      string[] brands = new string[3];
      brands[0] = "isml";
      brands[1] = "piff";
      brands[2] = "iso2";
      this.ftb = new FileTypeBox(brands); // overwrite base class's ftb
      this.ftb.MinorVersion = 1;

      uint[] matrix = new uint[9];
      matrix[0] = 0x10000; // 1.0
      matrix[4] = 0x10000; // 1.0
      matrix[8] = 0x40000000; // 1.0 (see description of RenderMatrix class)

      this.mmb = new MovieMetadataBox(mediaTracks, 1.0f, 1.0f, matrix);

      //CreateTracksForWriting<ISMVTrackFormat>(mediaTracks); // create tracks with ISMVTrackFormat

      // we can finalize the ftyp and moov boxes here, because they shouldn't change when moofs (fragments) are added
      //this.ftb.Write(m_writer);
      this.mmb.FinalizeBox();
      //this.mmb.Write(m_writer);

      //this.CurrMDatOffset = this.ftb.Size + this.mmb.Size; // for fragmented files, CurrMDatOffset is really the file offset

      InitializeForWriting(mediaTracks); // create our tracks (partial moov boxes, which should still exist, even for fragmented tracks)
    }

  }
}
