using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace Media.Formats.MicrosoftFMP4 {
  /// <summary>
  /// ISMLFile -- this for use by LiveSmoothIngestor.
  /// </summary>
  public class ISMLFile {
    public string strFileName { get; set; }
    public string strDir { get; set; }
    public string strISMCFileName { get; set; }

    public string Duration { get; set; }
    public string MajorVersion { get; set; }
    public string MinorVersion { get; set; }

    public List<StreamIndex> indexs = new List<StreamIndex>();
    private StreamIndex tmpLastIndex;
  	private QualityLevel tmpLastQL;

		public ISMLFile(StreamReader inStream) {
			Init(inStream);
		}

		public ISMLFile(string inDir, string inFileName) {
      strDir = inDir;
      strFileName = inFileName;
      //Console.WriteLine("OPENING: " + inDir + inFileName);
      FileStream fs = new FileStream(inDir + inFileName, FileMode.Open);
      StreamReader sr = new StreamReader(fs);
    	Init(sr);
      sr.Close();
    }

		public void Init(StreamReader inStream) {
			XmlReader reader = XmlReader.Create(inStream);

			while (reader.Read()) {
				if (reader.NodeType != XmlNodeType.Element) continue;

				if (reader.Name == "audio" || reader.Name == "video") {
					if ((tmpLastIndex == null) || (tmpLastIndex.Type != reader.Name)) {
						StreamIndex idx = new StreamIndex();
						idx.Type = reader.Name;
						tmpLastIndex = idx;
						indexs.Add(tmpLastIndex);
					}

					QualityLevel ql = new QualityLevel();
					ql.Bitrate = reader.GetAttribute("systemBitrate");
					tmpLastIndex.QualityLevels.Add(ql);
					tmpLastQL = ql;
				}

				if (reader.Name == "param") {
					string name = reader.GetAttribute("name");
					string vtype = reader.GetAttribute("valuetype");
					string value = reader.GetAttribute("value");
					
					switch (name) {
						case "systemBitrate": tmpLastQL.Bitrate = value; break;
						case "trackID": tmpLastQL.TrackID = value; break;
						case "FourCC": tmpLastQL.FourCC = value; break;
						case "CodecPrivateData": tmpLastQL.CodecPrivateData = value; break;
						case "AudioTag": tmpLastQL.AudioTag = value; break;
						case "Channels": tmpLastQL.Channels = value; break;
						case "SamplingRate": tmpLastQL.SamplingRate = value; break;
						case "BitsPerSample": tmpLastQL.BitsPerSample = value; break;
//						case "PacketSize": break;
						case "SubType": tmpLastIndex.Subtype = value; break;
						case "MaxWidth": tmpLastQL.Width = value; break;
						case "MaxHeight": tmpLastQL.Height = value; break;
					}

					//QualityLevel ql = new QualityLevel();
					//ql.Bitrate = reader.GetAttribute("Bitrate");
					//ql.FourCC = reader.GetAttribute("FourCC");
					//ql.Width = reader.GetAttribute("Width");
					//ql.Height = reader.GetAttribute("Height");
					//ql.CodecPrivateData = reader.GetAttribute("CodecPrivateData");
					//tmpLastIndex.QualityLevels.Add(ql);
				}

			}
		}
  }
}
