using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Xml;
using Media;

namespace Media.Formats.MicrosoftFMP4 {
  public class c {
    public string n;
    public string d;
  }

  public class QualityLevel {
  	public string TrackID;
  	public string AudioTag;
  	public string Channels;
  	public string SamplingRate;
  	public string BitsPerSample;
    public string Bitrate;
    public string FourCC;
    public string Width;
    public string Height;
    public string CodecPrivateData;
  }

  public class StreamIndex {
    public string Type;
    public string Subtype;
    public string Chuncks;
    public string Url;

    public List<QualityLevel> QualityLevels = new List<QualityLevel>();
    public List<c> cs = new List<c>();
  }

  public class ISMCFile {
    public string strFileName { get; set; }
    public string strDir { get; set; }
    public string strISMCFileName { get; set; }

    public string Duration { get; set; }
    public string MajorVersion { get; set; }
    public string MinorVersion { get; set; }

    public List<StreamIndex> indexs = new List<StreamIndex>();
    private StreamIndex tmpLastIndex;

    /// <summary>
    /// Constructor to use when writing to an ISMC file.
    /// </summary>
    /// <param name="basePath"></param>
    public ISMCFile(string basePath)
    {
      strDir = Path.GetDirectoryName(basePath);
      strFileName = Path.GetFileName(basePath);
    }

    /// <summary>
    /// Constructor to use when reading an ismc file.
    /// </summary>
    /// <param name="inDir"></param>
    /// <param name="inFileName"></param>
    public ISMCFile(string inDir, string inFileName, List<ISMElement> ismElements) :
      this(new FileStream(Path.Combine(new string[]{inDir, inFileName + ".ismc"}), FileMode.Open, FileAccess.Read, FileShare.Read)) 
    {
      ISMElement qElement = null;
      List<uint> fragDurations = null;

      strDir = inDir;
      strFileName = inFileName;
      //Console.WriteLine("OPENING: " + inDir + inFileName);
      foreach (StreamIndex streamIndex in indexs)
      {
        fragDurations = new List<uint>(streamIndex.cs.Count);
        streamIndex.cs.ForEach((c) => fragDurations.Add(uint.Parse(c.d)));
        if (streamIndex.Type == "video")
        {
          if (streamIndex.QualityLevels.Count != ismElements.Count(el => el.FragmentType == MP4.FragmentType.Video))
            throw new Exception("ISMCFile: count of quality levels for video does not match count of video elements in ISM file");
          foreach (QualityLevel qlevel in streamIndex.QualityLevels)
          {
            qElement = ismElements.First(elmnt => (elmnt.TrackID == int.Parse(qlevel.TrackID) + 1) && (elmnt.FragmentType == MP4.FragmentType.Video));
            qElement.Codec = new Codec(CodecTypes.Video);
            qElement.Codec.PrivateCodecData = qlevel.CodecPrivateData;
            qElement.FourCC = qlevel.FourCC;
            qElement.Height = int.Parse(qlevel.Height);
            qElement.Width = int.Parse(qlevel.Width);
            qElement.FragmentDurations = fragDurations;
          }
        }
        else if (streamIndex.Type == "audio")
        {
          if (streamIndex.QualityLevels.Count != ismElements.Count(el => el.FragmentType == MP4.FragmentType.Audio))
            throw new Exception("ISMCFile: count of quality levels for audio does not match count of audio elements in ISM file");
          // we assume there is only ONE audio quality level
          qElement = ismElements.First(elmnt => (elmnt.FragmentType == MP4.FragmentType.Audio));
          qElement.Codec = new Codec(CodecTypes.Audio);
          QualityLevel qlevel = streamIndex.QualityLevels[0];
          qElement.Codec.PrivateCodecData = "038080220000000480801640150020000001F4000001F4000580800511900000000680800102"; // qlevel.CodecPrivateData;
          qElement.FourCC = qlevel.FourCC;
          qElement.ChannelCount = int.Parse(qlevel.Channels);
          qElement.SampleRate = int.Parse(qlevel.SamplingRate);
          qElement.SampleSize = int.Parse(qlevel.BitsPerSample);
          qElement.FragmentDurations = fragDurations;
        }
      }
    }

    public ISMCFile(Stream fs)
    {
      StreamReader sr = new StreamReader(fs);
      XmlReader reader = XmlReader.Create(sr);

      while (reader.Read()) {
        if (reader.NodeType != XmlNodeType.Element) continue;

        // A StreamIndex may include several tracks, all with the same contents but at different bit rates.
        if (reader.Name == "StreamIndex") {
          StreamIndex idx = new StreamIndex();
          idx.Type = reader.GetAttribute("Type");
          idx.Subtype = reader.GetAttribute("Subtype");
          idx.Chuncks = reader.GetAttribute("Chuncks");
          idx.Url = reader.GetAttribute("Url");
          indexs.Add(idx);
          tmpLastIndex = idx;
        }

        // A QualityLevel within a StreamIndex is basically a track in MP4 parlance.
        if (reader.Name == "QualityLevel") {
          QualityLevel ql = new QualityLevel();
					ql.TrackID = reader.GetAttribute("Index");
          ql.Bitrate = reader.GetAttribute("Bitrate");
          ql.FourCC = reader.GetAttribute("FourCC");
          ql.Width = reader.GetAttribute("Width");
          ql.Height = reader.GetAttribute("Height");
					if (ql.Width == null) {
						ql.Width = reader.GetAttribute("MaxWidth");
						ql.Height = reader.GetAttribute("MaxHeight");
					}

					ql.SamplingRate = reader.GetAttribute("SamplingRate");
					ql.Channels = reader.GetAttribute("Channels");
					ql.BitsPerSample = reader.GetAttribute("BitsPerSample");
					ql.AudioTag = reader.GetAttribute("AudioTag");
          ql.CodecPrivateData = reader.GetAttribute("CodecPrivateData");
          tmpLastIndex.QualityLevels.Add(ql);
        }

        if (reader.Name == "c") {
          c tc = new c();
          tc.n = reader.GetAttribute("n");
          tc.d = reader.GetAttribute("d");
          tmpLastIndex.cs.Add(tc);
        }

        // Do some work here on the data.
        //Console.WriteLine(reader.Name);
      }
      reader.Close();
    }
  }
}
