using System;
using System.IO;
using System.Collections.Generic;
using Media.Formats.MP4;

namespace Media.Formats.MicrosoftFMP4 {
  public class ISMElement {
    private FragmentType _fragmentType;
    private string _source;
    private long _bitrate;
    private int _trackID;
    private uint _timeScale;
    private string _folderPath;

    private BinaryReader _ismvBinReader;

    public ISMElement(string path)
    {
      _folderPath = path;
    }

    public BinaryReader BinaryReader
    {
      get { return _ismvBinReader; }
    }

    public Codec Codec
    {
      get;
      set;
    }

    /// <summary>
    /// FragmentType - this property is set in ISMFile.
    /// </summary>
    public FragmentType FragmentType {
      get {
        return (_fragmentType);
      }

      set {
        _fragmentType = value;
      }
    }

    /// <summary>
    /// Source - this property is set in ISMFile.
    /// </summary>
    public string Source {
      get {
        return (_source);
      }

      set {
        _source = value;
      }
    }

    /// <summary>
    /// Bitrate - this property is set in ISMFile.
    /// </summary>
    public long Bitrate
    {
      get {
        return (_bitrate);
      }

      set {
        _bitrate = value;
      }
    }

    /// <summary>
    /// TrackID - this property is set in ISMFile.
    /// This is the track ID unique within the whole stream.
    /// </summary>
    public int TrackID
    {
      get;
      set;
    }

    /// <summary>
    /// ISMVTrackID - this property is set in ISMFile.
    /// This is the track ID within the fragmented ISMV file.
    /// </summary>
    public int ISMVTrackID
    {
      get {
        return (_trackID);
      }

      set {
        _trackID = value;
      }
    }

    /// <summary>
    /// TimeScale - this property is set in ISMFile.
    /// </summary>
    public uint TimeScale
    {
      get { return _timeScale; }
      set { _timeScale = value; }
    }

    public string FourCC
    {
      get;
      set;
    }

    public List<uint> FragmentDurations
    {
      get;
      set;
    }

    // video properties
    public int Height { get; set; }
    public int Width { get; set; }

    // audio propertes
    public int ChannelCount { get; set; }
    public int SampleSize { get; set; }
    public int SampleRate { get; set; }

    public void Open()
    {
      FileStream stream = new FileStream(_folderPath + @"\" + _source, FileMode.Open, FileAccess.Read, FileShare.Read);
      _ismvBinReader = new BinaryReader(stream);
    }

    public void Close()
    {
      _ismvBinReader.Close();
    }

    public Dictionary<string, string> ChunkInfo = new Dictionary<string, string>();
    public void Write(TextWriter ISM, string inOrigISMCDir, string inName, ISMCFile ismc)
    {
      string tmpRate = _bitrate.ToString();
      tmpRate = tmpRate.Substring(0, tmpRate.Length - 3);

      if (_fragmentType == MP4.FragmentType.Video)
        ISM.WriteLine("      <video");
      else if (_fragmentType == MP4.FragmentType.Audio)
        ISM.WriteLine("      <audio");
      else return;

      ISM.WriteLine("        src=\"" + inName + "_" + tmpRate + ".ismv\"");
      ISM.WriteLine("        systemBitrate=\"" + _bitrate + "\">");
      if (ismc == null)
      {
        ISM.WriteLine("        <param");
        ISM.WriteLine("          name=\"trackID\"");
        ISM.WriteLine("          value=\"2\"");
        ISM.WriteLine("          valuetype=\"data\" />");
      }
      else
      {
        ISM.WriteLine("        <param");
        ISM.WriteLine("          name=\"trackID\"");
        ISM.WriteLine("          value=\"2\"");
        ISM.WriteLine("          valuetype=\"data\"");
        ISM.Write("          chunks=\"");

        //          Console.WriteLine("Video Bitrate: " + vid.systemBitrate);
        ISMVFile vfile = new ISMVFile(inOrigISMCDir, _source);

        string chunkData = "";
        int chunkId = 0;
        ulong currTime = 0;
        Fragment frag = new Fragment();
        foreach (c cidx in ismc.indexs[0].cs)
        {
          ulong ChunkStart = 0;
          ulong ChunkLen = 0;
          // need to fix this line            vfile.GetFragmentPosition(currTime, 2, out ChunkStart, out ChunkLen);

          currTime += ulong.Parse(cidx.d);
          chunkId++;

          //            Console.WriteLine(" ChunkStart: " + ChunkStart + " ChunkLen: " + ChunkLen);
          chunkData += ChunkStart + "-" + ChunkLen + ",";
          ISM.Write(chunkData);
        }
        ISM.WriteLine("\">");
        ChunkInfo[_bitrate.ToString()] = chunkData;
      }

      if (_fragmentType == MP4.FragmentType.Video)
        ISM.WriteLine("      </video>");
      else if (_fragmentType == MP4.FragmentType.Audio)
        ISM.WriteLine("      </audio>");
    }
  }
}

