using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using Media.Formats.Generic;
using Media.H264;

namespace Media.Formats.MP4
{

  /// <summary>
  /// // Visual Sequences
  ///class PixelAspectRatioBox extends Box(‘pasp’) {
  ///  unsigned int(32) hSpacing;
  ///  unsigned int(32) vSpacing;
  ///}
  /// </summary>
  public class PixelAspectRatioBox : Box {
    public PixelAspectRatioBox() : base(BoxTypes.PixelAspectRatio) {
      this.Size += 8UL;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader))
      {
        base.Read(reader);
        hSpacing = reader.ReadUInt32();
        vSpacing = reader.ReadUInt32();
      }
    }


    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.WriteUInt32(hSpacing);
            writer.WriteUInt32(vSpacing);
        }
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<hspacing>").Append(hSpacing.ToString()).Append("</hspacing>");
        xml.Append("<vspacing>").Append(vSpacing.ToString()).Append("</vspacing>");
        xml.Append("</box>");
        return xml.ToString();
    }

    public uint hSpacing;
    public uint vSpacing;
  }

  /// <summary>
  /// class CleanApertureBox extends Box(‘clap’){
  ///  unsigned int(32) cleanApertureWidthN;
  ///  unsigned int(32) cleanApertureWidthD;
  ///  unsigned int(32) cleanApertureHeightN;
  ///  unsigned int(32) cleanApertureHeightD;
  ///  unsigned int(32) horizOffN;
  ///  unsigned int(32) horizOffD;
  ///  unsigned int(32) vertOffN;
  ///  unsigned int(32) vertOffD;
  /// }
  /// </summary>
  public class CleanApertureBox : Box {
    public CleanApertureBox() : base(BoxTypes.CleanApertureBox) 
    {
      this.Size += 32;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader))
      {
        base.Read(reader);
        CleanApertureWidthN = reader.ReadUInt32();
        CleanApertureWidthD = reader.ReadUInt32();
        CleanApertureHeightN = reader.ReadUInt32();
        CleanApertureHeightD = reader.ReadUInt32();
        HorizOffN = reader.ReadUInt32();
        HorizOffD = reader.ReadUInt32();
        VertOffN = reader.ReadUInt32();
        VertOffD = reader.ReadUInt32();
      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.WriteUInt32(CleanApertureWidthN);
            writer.WriteUInt32(CleanApertureWidthD);
            writer.WriteUInt32(CleanApertureHeightN);
            writer.WriteUInt32(CleanApertureHeightD);
            writer.WriteUInt32(HorizOffN);
            writer.WriteUInt32(HorizOffD);
            writer.WriteUInt32(VertOffN);
            writer.WriteUInt32(VertOffD);
        }
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<CleanApertureWidthN>").Append(CleanApertureWidthN.ToString()).Append("</CleanApertureWidthN>");
        xml.Append("<CleanApertureWidthD>").Append(CleanApertureWidthD.ToString()).Append("</CleanApertureWidthD>");
        xml.Append("<CleanApertureHeightN>").Append(CleanApertureHeightN.ToString()).Append("</CleanApertureHeightN>");
        xml.Append("<CleanApertureHeightD>").Append(CleanApertureHeightD.ToString()).Append("</CleanApertureHeightD>");
        xml.Append("<HorizOffN>").Append(HorizOffN.ToString()).Append("</HorizOffN>");
        xml.Append("<HorizOffD>").Append(HorizOffD.ToString()).Append("</HorizOffD>");
        xml.Append("<VertOffN>").Append(VertOffN.ToString()).Append("</VertOffN>");
        xml.Append("<VertOffD>").Append(VertOffD.ToString()).Append("</VertOffD>");
        xml.Append("</box>");
        return xml.ToString();
    }

    public uint CleanApertureWidthN;
    public uint CleanApertureWidthD;
    public uint CleanApertureHeightN;
    public uint CleanApertureHeightD;
    public uint HorizOffN;
    public uint HorizOffD;
    public uint VertOffN;
    public uint VertOffD;
  }

  #region Codec Specific Boxes

  /// <summary>
  /// AvcCBox
  /// This box is what's in a VisualSampleEntry box of BoxType avc1. (MP4)
  /// </summary>
  public class AvcCBox : Box
  {
      // Sequence Parameter Set (SPS) Header
      byte predefined1;
      byte profileIDC;
      byte constraintFlags;
      byte levelIDC;
      byte nalUnitLengthSizeByte;
      byte nalUnitLengthSize; // use 2 least significant bits plus 1 for length
      byte seqParamCountByte;
      byte seqParamCount; // use 5 least significant bits
      ushort seqParamLength; // in bytes
      byte[] seqParamSetData; // Sequence Parameter Set data

      // Picture Parameter Set (PPS) Header
      byte picParamCount;
      ushort picParamLength; // in bytes
      byte[] picParamSetData; // Picture Parameter Set data

      public AvcCBox() : base(BoxTypes.Avcc) 
      {
        this.predefined1 = 1;
        this.profileIDC = 77; // 66; // Baseline Profile (previously 77, 100) NOTE: this is now overwritten by the codec priv data setter
        this.constraintFlags = 0; // NOTE: this is now overwritten by the codec priv data setter
        this.levelIDC = 30; // Level 3 (previously 40, 21) NOTE: this is now overwritten by the codec priv data setter
        this.nalUnitLengthSize = 4;
        this.nalUnitLengthSizeByte = 255;
        this.seqParamCountByte = 0xE1;  // one count
        this.Size += (ulong)(8 + 3); // assume that SeqParamSetData and PicParamSetData are both zero length, increase size when setting privData
      }

      public override void Read(BoxReader reader)
      {
          using (new SizeChecker(this, reader))
          {
              base.Read(reader);
              predefined1 = reader.ReadByte(); // must be 01, configurationVersion
              profileIDC = reader.ReadByte(); // AVCProfileIndication
              constraintFlags = reader.ReadByte(); // profile_compatibility
              levelIDC = reader.ReadByte(); // AVCLevelIndication
              nalUnitLengthSizeByte = reader.ReadByte(); // first 6 bits are reserved and set to 1
              nalUnitLengthSize = (byte)((nalUnitLengthSizeByte & 0x3) + 1); // lengthSizeMinusOne

              seqParamCountByte = reader.ReadByte(); // first 3 bits are reserved and set to 1
              seqParamCount = (byte)(seqParamCountByte & 0x1F); // numOfSequenceParameterSets
              SPS = new SequenceParameterSet[seqParamCount];
              for (int i = 0; i < seqParamCount; i++)
              {
                seqParamLength = reader.ReadUInt16();
                seqParamSetData = new byte[seqParamLength];
                reader.Read(seqParamSetData, 0, seqParamSetData.Length);
                SPS[i] = ParseSPS(seqParamSetData);
              }

              picParamCount = reader.ReadByte();
              PPS = new PictureParameterSet[picParamCount];
              for (int i = 0; i < picParamCount; i++) {
                picParamLength = reader.ReadUInt16();
                picParamSetData = new byte[picParamLength];
                reader.Read(picParamSetData, 0, picParamSetData.Length);
                PPS[i] = ParsePPS(picParamSetData);
              }
          }
      }

      public override void Write(BoxWriter writer)
      {
          using (new SizeCalculator(this, writer))
          {
              base.Write(writer);
              writer.Write(predefined1);
              writer.Write(profileIDC);
              writer.Write(constraintFlags);
              writer.Write(levelIDC);
              writer.Write(nalUnitLengthSizeByte);
              writer.Write(seqParamCountByte);
              writer.Write(seqParamLength);
              if (seqParamLength > 0)
              {
                writer.Write(seqParamSetData, 0, seqParamSetData.Length);
              }
              writer.Write(picParamCount);
              writer.Write(picParamLength);
              if (picParamLength > 0)
              {
                writer.Write(picParamSetData, 0, picParamSetData.Length);
              }
          }
      }

      public override string ToString()
      {
          StringBuilder xml = new StringBuilder();

          xml.Append(base.ToString());
          xml.Append("<profileIDC>").Append(profileIDC.ToString()).Append("</profileIDC>");
          xml.Append("<constraintFlags>").Append(constraintFlags.ToString()).Append("</constraintFlags>");
          xml.Append("<levelIDC>").Append(levelIDC.ToString()).Append("</levelIDC>");
          xml.Append("<nalUnitLengthSize>").Append(nalUnitLengthSize.ToString()).Append("</nalUnitLengthSize>");
          xml.Append("<seqParamCount>").Append(seqParamCount.ToString()).Append("</seqParamCount>");
          xml.Append("<seqParamLength>").Append(seqParamLength.ToString()).Append("</seqParamLength>");
          xml.Append("<picParamCount>").Append(picParamCount.ToString()).Append("</picParamCount>");
          xml.Append("<picParamLength>").Append(picParamLength.ToString()).Append("</picParamLength>");
          xml.Append("<codecPrivateData>").Append(CodecPrivateData).Append("</codecPrivateData>");
          xml.Append("</box>");

          return xml.ToString();
      }

      /// <summary>
      /// CodecPrivateData returns both SPS and PPS in byte stream format that the Media Element expects.
      /// </summary>
      public string CodecPrivateData
      {
          get
          {
              if ((seqParamLength == 0) && (picParamLength == 0))
                  return "";
              StringBuilder sb = new StringBuilder(seqParamSetData.Length * 2 + picParamSetData.Length * 2 + 16);
              sb.Append("00000001");
              for (int i = 0; i < seqParamSetData.Length; i++)
              {
                  sb.Append(string.Format("{0:X2}", seqParamSetData[i]));
              }
              sb.Append("00000001");
              for (int i = 0; i < picParamSetData.Length; i++)
              {
                  sb.Append(string.Format("{0:X2}", picParamSetData[i]));
              }
              return sb.ToString();
          }
        set 
        { 
          string privData = (string)value;
          if (privData.Length == 0)
          {
            seqParamLength = 0;
            picParamLength = 0;
            return;
          }
          if (privData.StartsWith("00000001"))
          {
            int n = privData.Substring(8).IndexOf("00000001");
            seqParamSetData = H264Utilities.HexStringToBytes(privData.Substring(8, n));

            //MemoryStream stream = new MemoryStream(seqParamSetData);
            //BitReader bitReader = new BitReader(stream);
            //H264SPS sps = new H264SPS();
            //sps.Read(bitReader);

            this.profileIDC = seqParamSetData[1];
            this.constraintFlags = seqParamSetData[2];
            this.levelIDC = seqParamSetData[3];
            seqParamCount = (byte)(seqParamCountByte & 0x1F);
            seqParamLength = (ushort)seqParamSetData.Length;
            this.Size += (ulong)seqParamLength;
            picParamSetData = H264Utilities.HexStringToBytes(privData.Substring(n + 16));
            picParamCount = 1; // FIXME: find out the right value for this
            picParamLength = (ushort)picParamSetData.Length;
            this.Size += (ulong)picParamLength;
          }
          else
          {
            throw new Exception("CodecPrivateData for Avcc box must be delimited by 00000001");
          }
        }
      }

      private SequenceParameterSet ParseSPS(byte[] data)
      {
        SequenceParameterSet sps = new SequenceParameterSet((uint)data.Length);
        H264.BitReader reader = new H264.BitReader(new MemoryStream(data));
        sps.Read(reader);
        reader.Close();
        return sps;
      }

      private PictureParameterSet ParsePPS(byte[] data)
      {
        PictureParameterSet pps = new PictureParameterSet((uint)data.Length);
        H264.BitReader bitReader = new H264.BitReader(new MemoryStream(data));
        pps.Read(bitReader);
        bitReader.Close();
        return pps;
      }

      public SequenceParameterSet[] SPS;
      public PictureParameterSet[] PPS;
  }

  public class AnyPrivBox : Box
  {

      byte[] codecPrivateData;

      public AnyPrivBox() : base(BoxTypes.AnyDescription) { }

      public AnyPrivBox(BoxType btype, string privData)
        : base(btype)
      {
        int n = 0;
        if (privData.StartsWith("00000001"))
          n = 8;
        if (btype == BoxTypes.Dvc1) // ISMV video, which doesn't work in MP4 format
          n += 14; // ignore first seven bytes
        codecPrivateData = H264Utilities.HexStringToBytes(privData.Substring(n));
        this.Size += (ulong)codecPrivateData.Length;
      }

      public override void Read(BoxReader reader)
      {
          using (new SizeChecker(this, reader))
          {
              base.Read(reader);
              codecPrivateData = new byte[Size - 8L];
              reader.Read(codecPrivateData, 0, codecPrivateData.Length);
          }
      }

      public override void Write(BoxWriter writer)
      {
          using (new SizeCalculator(this, writer))
          {
              base.Write(writer);
              writer.Write(codecPrivateData, 0, codecPrivateData.Length);
          }
      }

      public override string ToString()
      {
          StringBuilder xml = new StringBuilder();

          xml.Append(base.ToString());
          xml.Append("<codecPrivateData>").Append(CodecPrivateData).Append("</codecPrivateData>");
          xml.Append("</box>");

          return xml.ToString();
      }

      /// <summary>
      /// CodecPrivateData returns sourceAudio codec private data.
      /// </summary>
      public string CodecPrivateData
      {
        get
        {
            StringBuilder sb = new StringBuilder(codecPrivateData.Length * 2 + 4);
            sb.Append("00000001");
            for (int i = 0; i < (codecPrivateData.Length); i++)
            {
                sb.Append(string.Format("{0:X2}", codecPrivateData[i]));
            }
            return sb.ToString();
        }
        internal set
        {
          int n = 0;
          if (value.StartsWith("00000001"))
            n = 8;
          codecPrivateData = H264Utilities.HexStringToBytes(value.Substring(n));
        }
      }
  }


  public class AnyPrivFullBox : FullBox
  {
      byte[] codecPrivateData;

      public AnyPrivFullBox() : base(BoxTypes.AnyDescription) { }

      public AnyPrivFullBox(BoxType btype, string privData)
        : base(btype)
      {
        int n = 0;
        if (privData.StartsWith("00000001"))
          n = 8;
        codecPrivateData = H264Utilities.HexStringToBytes(privData.Substring(n));
        if (codecPrivateData != null)
          this.Size += (ulong)codecPrivateData.Length;
      }

      public override void Read(BoxReader reader)
      {
          using (new SizeChecker(this, reader))
          {
              base.Read(reader);
              codecPrivateData = new byte[Size - 12L];
              reader.Read(codecPrivateData, 0, codecPrivateData.Length);
          }
      }

      public override void Write(BoxWriter writer)
      {
          using (new SizeCalculator(this, writer))
          {
              base.Write(writer);
              if (codecPrivateData != null)
                writer.Write(codecPrivateData, 0, codecPrivateData.Length);
          }
      }

      public override string ToString()
      {
          StringBuilder xml = new StringBuilder();

          xml.Append(base.ToString());
          xml.Append("<codecPrivateData>").Append(CodecPrivateData).Append("</codecPrivateData>");
          xml.Append("</box>");

          return xml.ToString();
      }

      /// <summary>
      /// CodecPrivateData returns sourceAudio codec private data.
      /// </summary>
      public string CodecPrivateData
      {
        get
        {
          if (codecPrivateData == null)
            return "";
          StringBuilder sb = new StringBuilder(codecPrivateData.Length * 2 + 4);
          sb.Append("00000001");
          for (int i = 0; i < (codecPrivateData.Length); i++)
          {
              sb.Append(string.Format("{0:X2}", codecPrivateData[i]));
          }
          return sb.ToString();
        }
        internal set
        {
          int n = 0;
          if (value.StartsWith("00000001"))
            n = 8;
          codecPrivateData = H264Utilities.HexStringToBytes(value.Substring(n));
        }
      }
  }

  #endregion

  #region Sample Entry types

  /// <summary>
  /// SampleEntry
  /// Base type for all sample entry types. Note: it's just a box (not a full box).
  /// 8.5.2.2 Syntax
  /// aligned(8) abstract class SampleEntry (unsigned int(32) format)
  /// extends Box(format) {
  ///   const unsigned int(8)[6] reserved = 0;
  ///   unsigned int(16) data_reference_index;
  /// }
  /// </summary>
  public class SampleEntry : Box {
    public SampleEntry(BoxType inType) : base(inType) {
      DataReferenceIndex = 1; // FIXME: assumes that there's only one DataReferenceBox
      this.Size += 8UL;
    }

    public override void Read(BoxReader reader) {
      base.Read(reader); 
      reader.ReadBytes(6); // unsinged int(8)[6] reserved = 0
      DataReferenceIndex = reader.ReadUInt16();
    }

    public override void Write(BoxWriter writer)
    {
        //using (new SizeCalculator(this, writer))
        //{
            base.Write(writer);
            writer.Write(new byte[6]);
            writer.Write(DataReferenceIndex);
        //}
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<datareferenceindex>").Append(DataReferenceIndex).Append("</datareferenceindex>");
        // do not close the <box> here

      return (xml.ToString());
    }

    public ushort DataReferenceIndex;
  }


  /// <summary>
  /// AudioSampleEntry
  /// This can come in different box types, which signifies the format of the track.
  /// --> If the BoxType is mp4a, then the box inside should be of BoxType esds.
  /// --> If the BoxType is wma , then the bocx inside should be of BoxType wfex. (Microsoft)
  /// 
  /// Audio Sequences
  ///class AudioSampleEntry(codingname) extends SampleEntry (codingname) {
  ///  const unsigned int(32)[2] reserved = 0;
  ///  template unsigned int(16) channelcount = 2;
  ///  template unsigned int(16) samplesize = 16;
  ///  unsigned int(16) pre_defined = 0;
  ///  const unsigned int(16) reserved = 0 ;
  ///  template unsigned int(32) samplerate = { default samplerate of media}<<16; }
  ///}
  /// </summary>
  public class AudioSampleEntry : SampleEntry {
    public AudioSampleEntry(BoxType inType) : base(inType) {
    }

    public AudioSampleEntry(BoxType inType, RawAudioTrackInfo audioInfo)
      : base(inType)
    {
      ChannelCount = (ushort)audioInfo.ChannelCount;
      SampleSize = (ushort)audioInfo.SampleSize;
      SampleRate = (uint)audioInfo.SampleRate;

      this.Size += 20UL;

      switch (audioInfo.PayloadType)
      {
        case AudioPayloadType.aac:
        case AudioPayloadType.mp4a:
          PrivDataFullBox = new AnyPrivFullBox(BoxTypes.Esds, audioInfo.CodecPrivateData); // AAC encoding
          this.Size += PrivDataFullBox.Size;
          break;
        case AudioPayloadType.wma:
          PrivDataBox = new AnyPrivBox(BoxTypes.Wfex, audioInfo.CodecPrivateData);
          this.Size += PrivDataBox.Size;
          break;
        case AudioPayloadType.samr: // 3gp audio
          PrivDataFullBox = new AnyPrivFullBox(BoxTypes.Damr, audioInfo.CodecPrivateData);
          this.Size += PrivDataFullBox.Size;
          break;
        default:
          throw new Exception(string.Format("Unknown audio track payload type: {0}", audioInfo.PayloadType));
      }
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        long pos = reader.BaseStream.Position;

        base.Read(reader);
        for (int i = 0; i < 2; i++) reader.ReadUInt32();  // unsigned int(32)[2] reserved = 0
        ChannelCount = reader.ReadUInt16();
        SampleSize = reader.ReadUInt16();
        PreDefined = reader.ReadUInt16(); // unsigned int(16) pre_defined = 0
        reader.ReadUInt16(); // unsigned int(16) reserved = 0
        SampleRate = reader.ReadUInt32() >> 16;

          // read esds or avcc boxes
        BoxType bt = reader.PeekNextBoxType();
        switch (bt.ToString().ToLower())
        {
            case "esds": // AudioSampleEntry type is mp4a (denotes AAC encoding)
            case "avcc":
                PrivDataFullBox = new AnyPrivFullBox(bt, string.Empty);
                PrivDataFullBox.Read(reader);
                break;
            case "wfex": // AudioSampleEntry type is wma
                PrivDataBox = new AnyPrivBox(bt, string.Empty);
                PrivDataBox.Read(reader);
                break;
           case "damr": // 3gpp sound
                PrivDataFullBox = new AnyPrivFullBox(bt, string.Empty);
                PrivDataFullBox.Read(reader);
                break;
            default:
                throw new Exception(string.Format("AudioSampleEntry has unknown contents: {0}", bt.ToString()));
        }
      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write((UInt32)0);
            writer.Write((UInt32)0);
            writer.Write(ChannelCount);
            writer.Write(SampleSize);
            writer.Write(PreDefined);
            writer.Write((UInt16)0);
            writer.Write(SampleRate << 16);
            if (PrivDataFullBox != null)
                PrivDataFullBox.Write(writer);
            if (PrivDataBox != null)
                PrivDataBox.Write(writer);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<channelcount>").Append(ChannelCount).Append("</channelcount>");
      xml.Append("<samplesize>").Append(SampleSize).Append("</samplesize>");
      xml.Append("<samplerate>").Append(SampleRate).Append("</samplerate>");
      if (PrivDataFullBox != null)
          xml.Append(PrivDataFullBox.ToString());
      if (PrivDataBox != null)
        xml.Append(PrivDataBox.ToString());
      xml.Append("</box>");

      return (xml.ToString());
    }


    public ushort ChannelCount;
    public ushort SampleSize;
    public ushort PreDefined;
    public uint SampleRate;
    public AnyPrivFullBox PrivDataFullBox { get; private set; }
    public AnyPrivBox PrivDataBox { get; private set; }
  }


  public class UuidBox : Box
  {
    public UuidBox()
      : base(BoxTypes.UUID)
    {
    }

    public override void Read(BoxReader reader)
    {
      using (new SizeChecker(this, reader)) 
      {
        long startpos = reader.BaseStream.Position;
        base.Read(reader);
        UserType = reader.ReadExtendedBoxType();
        int contentSize = (int)((long)base.Size - (reader.BaseStream.Position - startpos));
        if (contentSize > 0)
        {
          Contents = new byte[contentSize];
          reader.Read(Contents, 0, contentSize);
        }
      }
    }

    public override void Write(BoxWriter writer)
    {
      using (new SizeCalculator(this, writer))
      {
        base.Write(writer);
        writer.Write(UserType.GetBytes(), 0, 16);
        writer.Write(Contents, 0, Contents.Length);
      }
    }

    public ExtendedBoxType UserType;
    public byte[] Contents { get; private set; }
  }


  /// <summary>
  /// VisualSampleEntry
  /// This can come in different box types, which signifies the format of the track.
  /// --> If the BoxType is avc1, the box inside should be of BoxType avcC.
  /// --> If the BoxType is vc-1, the box inside should be of BoxType dvc1. (Microsoft)
  /// 
  ///class VisualSampleEntry(codingname) extends SampleEntry (codingname){
  ///  unsigned int(16) pre_defined = 0;
  ///  const unsigned int(16) reserved = 0;
  ///  unsigned int(32)[3] pre_defined = 0;
  ///  unsigned int(16) _width;
  ///  unsigned int(16) _height;
  ///  template unsigned int(32) horizresolution = 0x00480000; // 72 dpi
  ///  template unsigned int(32) vertresolution = 0x00480000; // 72 dpi
  ///  const unsigned int(32) reserved = 0;
  ///  template unsigned int(16) frame_count = 1;
  ///  string[32] compressorname;
  ///  template unsigned int(16) depth = 0x0018;
  ///  int(16) pre_defined = -1;
  ///  CleanApertureBox clap; // optional
  ///  PixelAspectRatioBox pasp; // optional
  ///}
  /// </summary>
  public class VisualSampleEntry : SampleEntry {
    public VisualSampleEntry(BoxType inType) : base(inType) {
    }

    public VisualSampleEntry(BoxType inType, RawVideoTrackInfo trackInfo)
      : base(inType)
    {
      Width = (ushort)trackInfo.Width;
      Height = (ushort)trackInfo.Height;
      HorizResolution = (uint)0x00480000; // 72 dpi
      VertResolution = (uint)0x00480000;
      FrameCount = (ushort)1;

      this.Size += 34UL;

      CompressorName = ""; // "Orions Digital MP4 Recoding";
      Depth = (ushort)0x18; // images in color with no alpha

      this.Size += 36UL; // compressor name is 30 bytes plus 2 for length, 2 for depth, 2 for reserved


      if (trackInfo.PayloadType == VideoPayloadType.avc1)
      {
        AvcCBox = new AvcCBox();
        AvcCBox.CodecPrivateData = trackInfo.CodecPrivateData;
        this.Size += AvcCBox.Size;
      }
      else if (trackInfo.PayloadType == VideoPayloadType.vc1)
      {
        PrivDataBox = new AnyPrivBox(BoxTypes.Dvc1, trackInfo.CodecPrivateData); // MS ISMV
        this.Size += PrivDataBox.Size;
      }
      else if (trackInfo.PayloadType == VideoPayloadType.mp4v)
      {
        //CleanApertureBox = new CleanApertureBox();  // We won't be putting a CleanApertureBox for now

        PrivDataFullBox = new AnyPrivFullBox(BoxTypes.Esds, trackInfo.CodecPrivateData); // 3gp mp4v --> esds
        this.Size += PrivDataFullBox.Size;
      }

      if (trackInfo.AspectRatioX != trackInfo.AspectRatioY)
      {
        PixelAspectRatioBox = new PixelAspectRatioBox();
        PixelAspectRatioBox.hSpacing = (uint)trackInfo.AspectRatioX;
        PixelAspectRatioBox.vSpacing = (uint)trackInfo.AspectRatioY;
        this.Size += PixelAspectRatioBox.Size;
      }
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        long startpos = reader.BaseStream.Position;

        base.Read(reader);
        reader.ReadUInt16(); // pre_defined = 0;
        reader.ReadUInt16(); // reserved = 0;
        for (int i = 0; i < 3; i++) reader.ReadUInt32();  // unsigned int(32)[3] pre_defined = 0
        Width = reader.ReadUInt16();
        Height = reader.ReadUInt16();
        HorizResolution = reader.ReadUInt32(); // = 0x0048000 = 72 dpi
        VertResolution = reader.ReadUInt32(); // = 0x0048000 = 72 dpi
        reader.ReadUInt32(); // reserved = 0
        FrameCount = reader.ReadUInt16(); // frame_count = 1

        // Compressor name has first 2 bytes which is the readable length, the rest are char's or null bytes
        CompressorName = "";
        ushort len = reader.ReadUInt16();
        // NOTE: Some encoders use only one byte for count of compressor name, so here
        // we test for whether the length is valid. If not valid, only the first byte is
        // used as the length.
        if (len > 30)
        {
          byte[] b = BitConverter.GetBytes(len);
          if (BitConverter.IsLittleEndian)
          {
            len = (ushort)b[1];
            CompressorName += (char)b[0];
          }
          else
          {
            len = (ushort)b[0];
            CompressorName += (char)b[1];
          }
        }
        for (int i=0; i<30; i++) {
          if (i < len)
            CompressorName += reader.ReadChar();
          else
            reader.ReadChar();
        }
        CompressorName = CompressorName.Trim().Replace("\0","");
        Depth = reader.ReadUInt16(); // depth = 0x0018
        reader.ReadUInt16();  // pre_defined = -1

        bool bOptionalBoxFound = true;
        while (bOptionalBoxFound) {
          bOptionalBoxFound = false;
          long pos = reader.BaseStream.Position;
          Box test = new Box(BoxTypes.Any);
          test.Read(reader);
          reader.BaseStream.Position = pos;

          if (test.Type == BoxTypes.CleanApertureBox) {
            CleanApertureBox = new CleanApertureBox();
            CleanApertureBox.Read(reader);
            bOptionalBoxFound = true;
          } else

          if (test.Type == BoxTypes.PixelAspectRatio) {
            PixelAspectRatioBox = new PixelAspectRatioBox();
            PixelAspectRatioBox.Read(reader);
            bOptionalBoxFound = true;
          } else

          // retrieve CodecPrivateData from avcC
          if (test.Type == BoxTypes.AvcC) {
            AvcCBox = new AvcCBox();
            AvcCBox.Read(reader);

            //if ((ulong) (reader.BaseStream.Position - pos) < this.Size) {
            //  // klude to work around Expression problem (missing uuid, but box size large enough for it)
            //  pos = reader.BaseStream.Position;
            //  test = new Box(BoxTypes.Any);
            //  test.Read(reader);
            //  reader.BaseStream.Position = pos;

            //  if (test.Type == BoxTypes.UUID) {
            //    Uuid = new UuidBox();
            //    Uuid.Read(reader);
            //  }
            //}
            bOptionalBoxFound = true;
          } else 
            
          if (test.Type == BoxTypes.UUID) {
            Uuid = new UuidBox();
            Uuid.Read(reader);
          } else 
            
          if (test.Type == BoxTypes.Mp4v) {
            PrivDataFullBox = new AnyPrivFullBox();
            PrivDataFullBox.Read(reader);
            bOptionalBoxFound = true;
          } else 
            
          if (test.Type == BoxTypes.Vc1) {
            PrivDataBox = new AnyPrivBox();
            PrivDataBox.Read(reader);
            bOptionalBoxFound = true;
          }
        }
      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write((UInt16)0);
            writer.Write((UInt16)0);
            writer.Write((UInt32)0);
            writer.Write((UInt32)0);
            writer.Write((UInt32)0);

            writer.Write(Width);
            writer.Write(Height);
            writer.Write(HorizResolution);
            writer.Write(VertResolution);
            writer.Write((UInt32)0);
            writer.Write(FrameCount);
            writer.Write((ushort)CompressorName.Length);
            int padding = 30 - CompressorName.Length;
            writer.Write(CompressorName.ToCharArray());
            writer.Write(new char[padding]);
            writer.Write(Depth);
            writer.Write((UInt16)0xFFFF);
            if (CleanApertureBox != null)
                CleanApertureBox.Write(writer);
            if (PixelAspectRatioBox != null)
              PixelAspectRatioBox.Write(writer);
            if (AvcCBox != null)
            {
              AvcCBox.Write(writer);
              if (Uuid != null)
                Uuid.Write(writer);
            }
            if (PrivDataFullBox != null)
                PrivDataFullBox.Write(writer);
            if (PrivDataBox != null)
                PrivDataBox.Write(writer);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<width>").Append(Width).Append("</width>");
      xml.Append("<height>").Append(Height).Append("</height>");
      xml.Append("<horizresolution>").Append(HorizResolution).Append("</horizresolution>");
      xml.Append("<vertresolution>").Append(VertResolution).Append("</vertresolution>");
      xml.Append("<framecount>").Append(FrameCount).Append("</framecount>");
      xml.Append("<compressorname>").Append(CompressorName).Append("</compressorname>");
      xml.Append("<depth>").Append(Depth).Append("</depth>");
      if (CleanApertureBox != null) xml.Append(CleanApertureBox.ToString());
      if (PixelAspectRatioBox != null) xml.Append(PixelAspectRatioBox.ToString());
      if (Type.Equals(BoxTypes.Avc1))
      {
          xml.Append(AvcCBox.ToString());
      }
      else if (Type.Equals(BoxTypes.Mp4v))
      {
          xml.Append(PrivDataFullBox.ToString());
      }
      else if (Type.Equals(BoxTypes.Vc1))
      {
          xml.Append(PrivDataBox.ToString());
      }
      xml.Append("</box>");

      return (xml.ToString());
    }


    public ushort Width;
    public ushort Height;
    public uint HorizResolution;
    public uint VertResolution;
    public ushort FrameCount;
    public string CompressorName;
    public ushort Depth;
    public CleanApertureBox CleanApertureBox;
    public PixelAspectRatioBox PixelAspectRatioBox;
    public AvcCBox AvcCBox { get; private set; }
    private UuidBox uuid;
    public UuidBox Uuid { get { return uuid; } set { uuid = value; } }
    public AnyPrivFullBox PrivDataFullBox { get; private set; }
    public AnyPrivBox PrivDataBox { get; private set; }
  }


  /// <summary>
  /// HintSampleEntry
  /// class HintSampleEntry() extends SampleEntry (protocol) {
  ///   unsigned int(8) data [];
  /// }
  /// </summary>
  public class HintSampleEntry : SampleEntry {
    private byte[] data;

    public HintSampleEntry(BoxType inType) : base(inType) {
      this.Size += (ulong)data.Length;
    }

    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        long startpos = reader.BaseStream.Position;
        base.Read(reader);
        long dataLen = (startpos + (long)Size) - reader.BaseStream.Position;
        if (dataLen > 0L) {
          data = new byte[dataLen];
          reader.Read(data, 0, data.Length);
        }
      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write(data);
        }
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<hint/>");
        xml.Append("</box>");
        return xml.ToString();
    }
  }


  /// <summary>
  /// MetaDataSampleEntry
  /// class MetaDataSampleEntry(codingname) extends SampleEntry (codingname) {
  ///}
  ///
  /// </summary>
  public class MetaDataSampleEntry : SampleEntry {
    public MetaDataSampleEntry(BoxType inType) : base(inType)
    {
      //this.Size += (uint)allStrings.Length; // do this in derived class
    }

    public override void  Read(BoxReader reader) {
      using (new SizeChecker(this, reader))
      {
        base.Read(reader);
        int strLen = (int)(Size - 16) << 1; // Size can't possibly be > int.MaxValue here
        allStrings = new char[strLen];

        // FIXME: ????
      }
    }

    protected char[] allStrings; // internal use only
    protected int nextIndex = 0; // internal use only

    protected string GetNextString() {
      int strLen = 0;
      int start = nextIndex;
      // count chars
      if (allStrings[start] == 0)
        return string.Empty;

      for (; nextIndex < allStrings.Length; nextIndex++, strLen++) {
        if (allStrings[nextIndex] == 0)
          break;
      }
      nextIndex++; // advance to next string
      return new string(allStrings, start, strLen);
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write(allStrings); // FIXME: this is not correct (it appears this should be done in derived class)
        }
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append( base.ToString());
        xml.Append("<metadatasampleentry>");
        return xml.ToString();
    }
  }


  /// <summary>
  /// XMLMetaDataSampleEntry
  /// 
  ///class XMLMetaDataSampleEntry() extends MetaDataSampleEntry (’metx‘) {
  ///  string content_encoding; // optional
  ///  string namespace;
  ///  string schema_location; // optional
  ///  BitRateBox (); // optional
  ///}
  /// </summary>
  public class XMLMetaDataSampleEntry : MetaDataSampleEntry {
    string contentEncoding; // optional
    string nameSpace;
    string schemaLocation; // optional
    BitRateBox bitRateBox; // optional

    public XMLMetaDataSampleEntry() : base(BoxTypes.XMLMetaDataSampleEntry) 
    {
      this.Size += (ulong)allStrings.Length;
      //bitRateBox = new BitRateBox(); // optional?
      //this.Size += bitRateBox.Size;
    }

    public override void  Read(BoxReader reader) {
      base.Read(reader);
    // scan three strings, 2 of which may be empty
      int strCount = 0;
      for (int i = 0; (strCount < 3) && (i < allStrings.Length); i++) {
        allStrings[i] = reader.ReadChar();
        if (allStrings[i] == 0)
          strCount++;
      }
      contentEncoding = GetNextString();
      nameSpace = GetNextString();
      schemaLocation = GetNextString();

      if ((reader.BaseStream.Position - (long)Offset) < (long)Size) {
        bitRateBox = new BitRateBox();
        bitRateBox.Read(reader);
      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write(allStrings);
            if (bitRateBox != null)
                bitRateBox.Write(writer);
        }
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<contentEncoding>").Append(contentEncoding).Append("</contentEncoding>");
        xml.Append("<nameSpace>").Append(nameSpace).Append("</nameSpace>");
        xml.Append("<schemaLocation>").Append(schemaLocation).Append("</schemaLocation>");
        if (bitRateBox != null)
            xml.Append(bitRateBox.ToString());
        xml.Append("</metadatasampleentry>");
        xml.Append("</box>");
        return xml.ToString();
    }

    //string contentEncoding; // optional
    public string ContentEncoding {
      get { return contentEncoding; }
    }

    //string nameSpace;
    public string NameSpace {
      get { return nameSpace; }
    }

    //string schemaLocation; // optional
    public string SchemaLocation {
      get { return schemaLocation; }
    }

    //BitRateBox bitRateBox; // optional
    public BitRateBox BitRateBox {
      get { return bitRateBox; }
    }
  }


  /// <summary>
  /// TextMetaDataSampleEntry
  /// 
  ///class TextMetaDataSampleEntry() extends MetaDataSampleEntry (‘mett’) {
  ///  string content_encoding; // optional
  ///  string mime_format;
  ///  BitRateBox (); // optional
  ///}
  /// </summary>
  public class TextMetaDataSampleEntry : MetaDataSampleEntry {
    string contentEncoding; // optional
    string mimeFormat;
    BitRateBox bitRateBox; // optional

    public TextMetaDataSampleEntry() : base(BoxTypes.TextMetaDataSampleEntry) 
    {
      this.Size += (ulong)allStrings.Length;
      //bitRateBox = new BitRateBox(); // optional?
      //this.Size += bitRateBox.Size;
    }

    public override void  Read(BoxReader reader) {
   	 base.Read(reader);

      // scan two strings, 1 of which may be empty
      int strCount = 0;
      for (int i = 0; (strCount < 2) && (i < allStrings.Length); i++) {
        allStrings[i] = reader.ReadChar();
        if (allStrings[i] == 0)
          strCount++;
      }
      contentEncoding = GetNextString();
      mimeFormat = GetNextString();

      if ((reader.BaseStream.Position - (long)Offset) < (long)Size) {
        bitRateBox = new BitRateBox();
        bitRateBox.Read(reader);
      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write(allStrings);
            if (bitRateBox != null)
                bitRateBox.Write(writer);
        }
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<contentEncoding>").Append(contentEncoding).Append("</contentEncoding>");
        xml.Append("<mimeFormat>").Append(mimeFormat).Append("</mimeFormat>");
        if (bitRateBox != null)
            xml.Append(bitRateBox.ToString());
        xml.Append("</metadatasampleentry>");
        xml.Append("</box>");
        return xml.ToString();
    }

    //string contentEncoding; // optional
    public string ContentEncoding {
      get { return contentEncoding; }
    }

    public string MimeFormat {
      get { return mimeFormat; }
    }

    //BitRateBox bitRateBox; // optional
    public BitRateBox BitRateBox {
      get { return bitRateBox; }
    }
  }


  /// <summary>
  /// BitRateBox
  /// class BitRateBox extends Box(‘btrt’){
  ///  unsigned int(32) bufferSizeDB;
  ///  unsigned int(32) maxBitrate;
  ///  unsigned int(32) avgBitrate;
  ///}
  /// </summary>
  public class BitRateBox : Box {
    private uint bufferSizeDB;
    private uint maxBitrate;
    private uint avgBitrate;

    public BitRateBox() : base(BoxTypes.BitRate) 
    {
      this.Size += 12UL;
    }

    public override void  Read(BoxReader reader) {
   	  base.Read(reader);
      bufferSizeDB = reader.ReadUInt32();
      maxBitrate = reader.ReadUInt32();
      avgBitrate = reader.ReadUInt32();
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write(bufferSizeDB);
            writer.Write(maxBitrate);
            writer.Write(avgBitrate);
        }
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<bitratebox>");
        xml.Append("<bufferSizeDB>").Append(bufferSizeDB.ToString()).Append("</bufferSizeDB>");
        xml.Append("<maxBitrate>").Append(maxBitrate.ToString()).Append("</maxBitrate>");
        xml.Append("<avgBitrate>").Append(avgBitrate.ToString()).Append("</avgBitrate>");
        xml.Append("</bitratebox>");
        xml.Append("</box>");
        return xml.ToString();
    }
  }


  /// <summary>
  /// SDSM : unknown sample entry
  /// </summary>
  public class UnknownEntry : SampleEntry {
    public UnknownEntry(BoxType inType) : base(inType) 
    { 
      // this should never be used when constructing boxes
    }

    public override void  Read(BoxReader reader) {
   	 base.Read(reader);
      int bufSize = (int)Size - 16; // assume that Size of this box cannot be greater than int.MaxValue (which is a reasonable assumption)
      if (bufSize != (int)((Offset + Size) - (ulong)reader.BaseStream.Position)) // do the addition first, then subtract
        throw new Exception("Byte alignment error in unknown sample entry box");
      byte[] buffer = new byte[bufSize];
      reader.Read(buffer, 0, bufSize);
    }

    public override void Write(BoxWriter writer)
    {
        // we don't write it out if it's unknown
    }

    public override string ToString()
    {
        StringBuilder xml = new StringBuilder();
        xml.Append(base.ToString());
        xml.Append("<unknownentry/>");
        xml.Append("</box>");
        return xml.ToString();
    }
  }

  #endregion


  /// <summary>
  /// SampleDescriptionsBox
  /// Normally, there should only be a single entry in this box.
  /// <!--
  /// aligned(8) class SampleDescriptionBox (unsigned int(32) handler_type)
  ///  extends FullBox('stsd', 0, 0) {
  ///    int i ;
  ///    unsigned int(32) entry_count;
  ///    for (i = 1 ; i <= entry_count ; i++) {
  ///      switch (handler_type){
  ///        case ‘soun’: // for sourceAudio tracks
  ///          AudioSampleEntry();
  ///        break;
  ///        case ‘vide’: // for sourceVideo tracks
  ///          VisualSampleEntry();
  ///        break;
  ///        case ‘hint’: // Hint rawTrack
  ///          HintSampleEntry();
  ///        break;
  ///        case ‘meta’: // Metadata rawTrack
  ///          MetadataSampleEntry();
  ///        break; 
  ///      }
  ///    }
  ///  }
  ///}
  ///8.5.2.3 Semantics
  ///  version is an integer that specifies the version of this box
  ///  entry_count is an integer that gives the number of entries in the following table
  ///  SampleEntry is the appropriate sample entry.
  ///  data_reference_index is an integer that contains the index of the data reference to use to retrieve
  ///  data associated with samples that use this sample description. Data references are stored in Data
  ///  Reference Boxes. The index ranges from 1 to the number of data references.
  ///  ChannelCount is either 1 (mono) or 2 (stereo)
  ///  SampleSize is in bits, and takes the default value of 16
  ///  SampleRate is the sampling rate expressed as a 16.16 fixed-point number (hi.lo)
  ///  resolution fields give the resolution of the image in pixels-per-inch, as a fixed 16.16 number
  ///  frame_count indicates how many frames of compressed sourceVideo are stored in each sample. The default is
  ///  1, for one frame per sample; it may be more than 1 for multiple frames per sample
  ///  Compressorname is a name, for informative purposes. It is formatted in a fixed 32-byte field, with the first
  ///  byte set to the number of bytes to be displayed, followed by that number of bytes of displayable data,
  ///  and then padding to complete 32 bytes total (including the size byte). The field may be set to 0.
  ///  depth takes one of the following values
  ///  0x0018 – images are in colour with no alpha
  ///  _width and _height are the maximum visual _width and _height of the stream described by this sample
  ///  description, in pixels
  ///  hSpacing, vSpacing: define the relative _width and _height of a pixel;
  ///  cleanApertureWidthN, cleanApertureWidthD: a fractional number which defines the exact clean
  ///  aperture _width, in counted pixels, of the sourceVideo image
  ///  cleanApertureHeightN, cleanApertureHeightD: a fractional number which defines the exact
  ///  clean aperture _height, in counted pixels, of the sourceVideo image
  ///  horizOffN, horizOffD: a fractional number which defines the horizontal offset of clean aperture
  ///  centre minus (_width-1)/2. Typically 0.
  ///  vertOffN, vertOffD: a fractional number which defines the vertical offset of clean aperture centre
  ///  minus (_height-1)/2. Typically 0.
  ///  content_encoding - is a null-terminated string in UTF-8 characters, and provides a MIME type which
  ///  identifies the content encoding of the timed metadata. It is defined in the same way as for an
  ///  ItemInfoEntry in this specification. If not present (an empty string is supplied) the timed metadata is
  ///  not encoded. An example for this field is ‘application/zip’. Note that no MIME types for BiM
  ///  [ISO/IEC 23001-1] and TeM [ISO/IEC 15938-1] currently exist. Thus the experimental MIME types
  ///  ‘application/x-BiM’ and ‘text/x-TeM’ shall be used to identify these encoding mechanisms.
  ///  namespace - gives the namespace of the schema for the timed XML metadata. This is needed for
  ///  identifying the type of metadata, e.g. gBSD or AQoS [MPEG-21-7] and for decoding using XML aware
  ///  encoding mechanisms such as BiM.
  ///  schema_location - optionally provides an URL to find the schema corresponding to the namespace.
  ///  This is needed for decoding of the timed metadata by XML aware encoding mechanisms such as BiM.
  ///  mime_format - provides a MIME type which identifies the content format of the timed metadata.
  ///  Examples for this field are ‘text/html’ and ‘text/plain’.
  ///  bufferSizeDB gives the size of the decoding buffer for the elementary stream in bytes.
  ///  maxBitrate gives the maximum rate in bits/second over any window of one second.
  ///  avgBitrate gives the average rate in bits/second over the entire presentation.
  ///  -->
  /// </summary>
  public class SampleDescriptionsBox : FullBox {
    public SampleTableBox parent;
    public SampleDescriptionsBox(SampleTableBox inParent) : base(BoxTypes.SampleDescription) {
      parent = inParent;
    }

    public SampleDescriptionsBox(SampleTableBox inParent, IsochronousTrackInfo trackInfo)
      : this(inParent)
    {
      EntryCount = 1; // FIXME: assume only one sample entry
      Entries = new SampleEntry[EntryCount];
      this.Size += 4UL;
      BoxType btype;
      if (trackInfo is RawAudioTrackInfo)
      {
        RawAudioTrackInfo rati = (RawAudioTrackInfo)trackInfo;
        switch (rati.PayloadType)
        {
          case AudioPayloadType.aac:
          case AudioPayloadType.mp4a:
            btype = BoxTypes.Mp4a;
            break;
          case AudioPayloadType.wma:
            btype = BoxTypes.Wma;
            break;
          case AudioPayloadType.samr: // 3gp audio
            btype = BoxTypes.Samr;
            break;
          default:
            throw new Exception(string.Format("Unknown audio track payload type: {0}", rati.PayloadType));
        }
        //btype = (rati.PayloadType == AudioPayloadType.wma) ? BoxTypes.Wma : ((rati.PayloadType == AudioPayloadType.mp4a) ? BoxTypes.Mp4a : BoxTypes.AudioSampleEntry);
        Entries[0] = new AudioSampleEntry(btype, (RawAudioTrackInfo)trackInfo);
        this.Size += Entries[0].Size;
      }
      else if (trackInfo is RawVideoTrackInfo)
      {
        RawVideoTrackInfo rvti = (RawVideoTrackInfo)trackInfo;
        switch (rvti.PayloadType)
        {
          case VideoPayloadType.vc1:
            btype = BoxTypes.Vc1;
            break;
          case VideoPayloadType.mp4v:
            btype = BoxTypes.Mp4v;
            break;
          case VideoPayloadType.mjpeg:
            btype = BoxTypes.VisualSampleEntry; // FIXME: this is not correct
            break;
          case VideoPayloadType.jpeg:
            btype = BoxTypes.VisualSampleEntry; // FIXME: this is not correct
            break;
          case VideoPayloadType.avc1:
            btype = BoxTypes.Avc1;
            break;
          default:
            btype = BoxTypes.Any;
            break;
        }
        Entries[0] = new VisualSampleEntry(btype, (RawVideoTrackInfo)trackInfo);
        this.Size += Entries[0].Size;
      }
      else //Entries[0] = new UnknownEntry(BoxTypes.UnknownSampleEntry);
        throw new Exception("unknown track type"); // error out instead of constructing an unknwon entry
    }



    public override void Read(BoxReader reader)
    {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);
        EntryCount = reader.ReadUInt32();
        Entries = new SampleEntry[EntryCount];
        for (int i=0; i<EntryCount; i++) {
          string FormatType = parent.parent.parent.HandlerReferenceBox.HandlerType;

          long pos = reader.BaseStream.Position;
          Box test = new Box(BoxTypes.Any);
          test.Read(reader);
          reader.BaseStream.Position = pos;


          switch (FormatType) {
            case "soun":
              AudioSampleEntry ase = new AudioSampleEntry(test.Type);
              ase.Read(reader);
              Entries[i] = ase;
              break;
            case "vide":
              VisualSampleEntry vse = new VisualSampleEntry(test.Type); // usually 'mp4v'
              vse.Read(reader);
              Entries[i] = vse;
              break;
            case "hint":
              HintSampleEntry hse = new HintSampleEntry(test.Type);
              hse.Read(reader);
              Entries[i] = hse;
              break;
            case "meta":
              switch (test.Type.ToString()) {
                case "metx":
                  XMLMetaDataSampleEntry xse = new XMLMetaDataSampleEntry();
                  xse.Read(reader);
                  Entries[i] = xse;
                  break;
                case "mett":
                  TextMetaDataSampleEntry tds = new TextMetaDataSampleEntry();
                  tds.Read(reader);
                  Entries[i] = tds;
                  break;
                default:
                  throw new Exception("Invalid Metadata Sample Entry in track");
              }
              break;
            //case "avcC":
            //    break;
            case "sdsm": // Apple MPEG-4 Scene Media Handler
              UnknownEntry ue = new UnknownEntry(test.Type);
              ue.Read(reader);
              Entries[i] = ue;
              break;
            case "odsm": // Apple MPEG-4 ODSM Media Handler
              UnknownEntry ue2 = new UnknownEntry(test.Type);
              ue2.Read(reader);
              Entries[i] = ue2;
              break;
            case "alis": // Apple iPhone
              UnknownEntry ue3 = new UnknownEntry(test.Type);
              ue3.Read(reader);
              Entries[i] = ue3;
              break;
            default:
              UnknownEntry ue4 = new UnknownEntry(test.Type);
              ue4.Read(reader);
              Entries[i] = ue4;
              break;
          }
        }
      }
    }

    public override void Write(BoxWriter writer)
    {
        using (new SizeCalculator(this, writer))
        {
            base.Write(writer);
            writer.Write(EntryCount);
            foreach (SampleEntry entry in Entries)
                entry.Write(writer);
        }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      xml.Append("<entrycount>").Append(EntryCount).Append("</entrycount>");
      xml.Append("<entries>");
      for (int i = 0; i < Entries.Length; i++) {
        xml.Append(Entries[i].ToString());
      }
      xml.Append("</entries>");
      xml.Append("</box>");

      return (xml.ToString());
    }

    public uint EntryCount;
    public SampleEntry[] Entries;
  }
}
