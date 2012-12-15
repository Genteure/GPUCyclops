using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using Media.Formats.MP4;

namespace Media.Formats.MicrosoftFMP4 {

  public class ISMFile {
    private XmlReader _xmlReader = null;
    private List<ISMElement> _ismElements = new List<ISMElement>();
    private ISMCFile _ismc;
    private string _baseName;
    private string _baseFolder;

    /// <summary>
    /// Constructor to use when writing to an ISM file.
    /// </summary>
    /// <param name="basePath">Path to folder plus base file name (no extension)</param>
    public ISMFile(string basePath)
    {
      _baseName = basePath;
      _baseFolder = _baseName.Substring(0, _baseName.LastIndexOf(Path.DirectorySeparatorChar));
      _ismc = new ISMCFile(_baseName);
    }

    /// <summary>
    /// Constructor to use when reading.
    /// </summary>
    /// <param name="inPath">Path to folder where all ISM files are</param>
    /// <param name="ismFileName">The ISM main file name, with NO extension</param>
    public ISMFile(string inPath, string ismFileName) : 
      this(new FileStream(Path.Combine(new string[] {inPath, ismFileName + ".ism"}), FileMode.Open, FileAccess.Read, FileShare.Read))
    {
      _baseName = Path.Combine(new string[] { inPath, ismFileName });
      _baseFolder = inPath;
      _ismc = new ISMCFile(inPath, ismFileName, _ismElements);
    }

    /// <summary>
    /// Constructor to use when reading, can also be used for network streams (theoretically).
    /// </summary>
    /// <param name="stream"></param>
    public ISMFile(Stream stream) {
      ISMElement ismElement = null;
      try {
        _xmlReader = XmlReader.Create(stream);

        int trackID = 1; // mark each ism element with a unique track ID

        // crude parser
        while (_xmlReader.Read()) {
          if (_xmlReader.NodeType != XmlNodeType.Element) {
            continue;
          }

          if (_xmlReader.Name == "audio" || _xmlReader.Name == "video") {
            // ismElement MUST be null at this point
            if (ismElement != null)
              throw new Exception("ISMFile: time scale param not found");

            ismElement = new ISMElement(_baseFolder);
            if (_xmlReader.Name == "audio") {
              ismElement.FragmentType = FragmentType.Audio;
            } 
            else if (_xmlReader.Name == "video") {
              ismElement.FragmentType = FragmentType.Video;
            }

            ismElement.Source = _xmlReader.GetAttribute("src");
            ismElement.Bitrate = long.Parse(_xmlReader.GetAttribute("systemBitrate"));
            ismElement.TrackID = trackID++;
          }
          else if (_xmlReader.Name == "param")
          {
            string paramName = _xmlReader.GetAttribute("name");
            string attributeValue = _xmlReader.GetAttribute("value");
            if (paramName == "trackID") 
            {
              ismElement.ISMVTrackID = int.Parse(attributeValue);
            }
            else if (paramName == "timeScale")
            {
              ismElement.TimeScale = uint.Parse(attributeValue);
              _ismElements.Add(ismElement);
              ismElement = null;
            }
          }
        }
      } catch {
        throw;
      } finally {
        if (_xmlReader != null) {
          _xmlReader.Close();
        }
      }
    }

    public string GetSource(FragmentType mediaType, long bitrate) {
      ISMElement ismElement = GetISMElement(mediaType, bitrate);

      if (ismElement != null) {
        return (ismElement.Source);
      }

      return (null);
    }

    public ISMElement GetISMElement(FragmentType fragmentType, long bitrate) {
      foreach (ISMElement ismElement in _ismElements) {
        if (ismElement.FragmentType == fragmentType && ismElement.Bitrate == bitrate) {
          return (ismElement);
        }
      }

      return (null);
    }

    public List<ISMElement> ISMElements {
      get {
        return (_ismElements);
      }
    }

    public ISMCFile ISMC
    {
      get { return _ismc; }
    }

    /// <summary>
    /// WriteISMFile
    /// Writes out the ISM file, which then also writes out the ISMC file.
    /// </summary>
    /// <param name="inBaseDir"></param>
    /// <param name="inName"></param>
    /// <param name="inExt"></param>
    /// <param name="inWriteChunkInfo"></param>
    /// <param name="inOrigISMCDir"></param>
    public void WriteISMFile(bool inWriteChunkInfo) {

      TextWriter ISM = new StreamWriter(_baseName + ".ism", false, Encoding.UTF8);
      string fName = Path.GetFileName(_baseName);
      string baseFolder = Path.GetDirectoryName(_baseName);
      ISM.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
      ISM.WriteLine("<!--Created with OrionsHD Encoder version 2.1.1216.0-->");
      ISM.WriteLine("<smil xmlns=\"http://www.w3.org/2001/SMIL20/Language\">");
      ISM.WriteLine("  <head>");
      ISM.WriteLine("    <meta");
      ISM.WriteLine("      name=\"clientManifestRelativePath\"");
      ISM.WriteLine("      content=\"" + fName + ".ismc\" />");
      ISM.WriteLine("  </head>");
      ISM.WriteLine("  <body>");
      ISM.WriteLine("    <switch>");

      foreach (ISMElement ism in _ismElements)
      {
        ism.Write(ISM, baseFolder, fName, _ismc);
      }

      ISM.WriteLine("    </switch>");
      ISM.WriteLine("  </body>");
      ISM.WriteLine("</smil>");
      ISM.Close();
    }

  }
}
