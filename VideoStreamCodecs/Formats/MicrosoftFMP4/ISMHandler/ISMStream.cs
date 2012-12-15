using System;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using Media.Formats.MP4;

namespace Media.Formats.MicrosoftFMP4 {
  public class ISMStream {
    private XmlReader xmlReader = null;
    private List<ISMElement> ismElements = new List<ISMElement>();

    public ISMStream(Stream stream) {
      ISMElement ismElement = null;

      try {
        xmlReader = XmlReader.Create(stream);

        // crude parser
        while (xmlReader.Read()) {
          if (xmlReader.NodeType != XmlNodeType.Element) {
            continue;
          }

          if (xmlReader.Name == "audio" || xmlReader.Name == "video") {
            // ismElement MUST be null at this point
            if (ismElement != null)
              throw new Exception("ISMFile: time scale param not found");

            ismElement = new ISMElement();
            if (xmlReader.Name == "audio") {
              ismElement.FragmentType = FragmentType.Audio;
            } 
            else if (xmlReader.Name == "video") {
              ismElement.FragmentType = FragmentType.Video;
            }

            ismElement.Source = xmlReader.GetAttribute("src");
            ismElement.Bitrate = long.Parse(xmlReader.GetAttribute("systemBitrate"));
          }
          else if (xmlReader.Name == "param")
          {
            string paramName = xmlReader.GetAttribute("name");
            string attributeValue = xmlReader.GetAttribute("value");
            if (paramName == "trackID") 
            {
              ismElement.TrackID = int.Parse(attributeValue);
            }
            else if (paramName == "timeScale")
            {
              ismElement.TimeScale = uint.Parse(attributeValue);
              ismElements.Add(ismElement);
              ismElement = null;
            }
          }
        }
      } catch {
        throw;
      } finally {
        if (xmlReader != null) {
          xmlReader.Close();
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
      foreach (ISMElement ismElement in ismElements) {
        if (ismElement.FragmentType == fragmentType && ismElement.Bitrate == bitrate) {
          return (ismElement);
        }
      }

      return (null);
    }

    public List<ISMElement> ISMElements {
      get {
        return (ismElements);
      }
    }
  }
}

