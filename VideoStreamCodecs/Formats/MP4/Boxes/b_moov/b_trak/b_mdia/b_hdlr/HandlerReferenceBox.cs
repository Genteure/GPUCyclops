/*
  8.4.3.2 Syntax
    aligned(8) class HandlerBox extends FullBox(‘hdlr’, version = 0, 0) {
      unsigned int(32) pre_defined = 0;
      unsigned int(32) handler_type;
      const unsigned int(32)[3] reserved = 0;
      string name;
    }
  8.4.3.3 Semantics
    version is an integer that specifies the version of this box
    handler_type when present in a media box, is an integer containing one of the following values, or a
    value from a derived specification:
    ‘vide’ Video rawTrack
    ‘soun’ Audio rawTrack
    ‘hint’ Hint rawTrack
    ‘meta’ Timed Metadata rawTrack
    handler_type when present in a meta box, contains an appropriate value to indicate the format of the
    meta box contents. The value ‘null’ can be used in the primary meta box to indicate that it is
    merely being used to hold resources.
    name is a null-terminated string in UTF-8 characters which gives a human-readable name for the rawTrack
    type (for debugging and inspection purposes).
   */

using System.Text;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{
  using System;

  public class HandlerReferenceBox : FullBox {
    public MediaBox parent;

    public HandlerReferenceBox(MediaBox inParent) : base(BoxTypes.HandlerReference) {
      parent = inParent;
      handler_type = new byte[4];
      this.Size += 20UL;  // does not include Name (see below)
    }

    public HandlerReferenceBox(MediaBox inParent, Codec codec)
      : this(inParent)
    {
      base.Version = 0;
      if (codec.CodecType == CodecTypes.Audio)
      {
        HandlerType = "soun";
        Name = "Sound Media Handler";
      }
      else if (codec.CodecType == CodecTypes.Video)
      {
        HandlerType = "vide";
        Name = "Video Media Handler";
      }
      else HandlerType = "unkn"; // unknown
      this.Size += (ulong)Name.Length + 1UL;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        reader.ReadInt32();
        reader.Read(handler_type, 0, 4);
        for (int i = 0; i < 3; i++) reader.ReadUInt32();
        Name = reader.ReadNullTerminatedString();
          // special case to take care of Apple's bug
          // (Apple and GoPro prepends the string with char count, but then adds one byte too many to the box size.)
        if (reader.BaseStream.Position != (long)(this.Size + this.Offset))
        {
            reader.BaseStream.Position = (long)(this.Size + this.Offset);
            Name = Name.Substring(1);
        }
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write((Int32)0);
            writer.Write(handler_type, 0, 4);
            for (int i = 0; i < 3; i++) writer.Write((Int32)0);
            writer.WriteNullTerminatedString(Name);
        }
    }


    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<handlerType>").Append(HandlerType).Append("</handlerType>");
      xml.Append("<name>").Append(Name).Append("</name>");
      xml.Append("</box>");

      return (xml.ToString());
    }


    public byte[] handler_type;
    public string Name; // 'Audio' or 'Video', etc.

    // 'soun' = Audio Track
    // 'vide' = Video Track
    // 'hint' = Hint Track
    // 'meta' = Timed Metadata Track
    public string HandlerType { 
      get {
        System.Text.UTF8Encoding encoding = new UTF8Encoding();
        string sval = encoding.GetString(handler_type,0,handler_type.Length);
        return (sval);
      }

      set {
        System.Text.UTF8Encoding encoding = new UTF8Encoding();
        handler_type = encoding.GetBytes(value);
      }
    }

    public static string ReverseString(string s) {
      char[] arr = s.ToCharArray();
      Array.Reverse(arr);
      return new string(arr);
    }

  }
}
