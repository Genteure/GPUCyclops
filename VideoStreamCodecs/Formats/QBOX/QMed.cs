using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Media.Formats.QBOX
{

  public class QBoxFlags
  {
    private ulong _value;

    // assume that the bytes have been swapped already from small-endian
    public ulong value
    {
      get { return (_value); }
      set
      {
        _value = value;
        _v = (byte)((value >> 24) & 0xFF); // first 8 bits
        _f = value & 0xFFFFFFUL; // last 24 bits
      }
    }

    private byte _v;  // 8 bits
    public byte version
    {
      get { return (_v); }
      set
      {
        _v = value;
        this.value = (this.value & 0xFFFFFFUL) | ((ulong)value << 24);
      }
    }

    // flags...
    public ulong _f;  // 24 bits
    public ulong flags
    {
      get { return (_f); }
      set
      {
        _f = value;
        this.value |= value & 0xFFFFFFUL;
      }
    }
  }

  public static class QMed {

    public const uint QMED_BOX_TYPE = 0x716d6564;
    public const uint QMED_MAJOR_MEDIA_TYPE_AAC = 0x1;
    public const uint QMED_MAJOR_MEDIA_TYPE_H264 = 0x2;
    public const uint QMED_MAJOR_MEDIA_TYPE_PCM = 0x3;
    public const uint QMED_MAJOR_MEDIA_TYPE_MP2 = 0x6;
    public const uint QMED_MAJOR_MEDIA_TYPE_JPEG = 0x7;
    public const uint QMED_MAJOR_MEDIA_TYPE_Q711 = 0x9;
    public const uint QMED_MAJOR_MEDIA_TYPE_Q728 = 0xa;
    public const uint QMED_MAJOR_MEDIA_TYPE_Q722 = 0xb;
    public const uint QMED_MAJOR_MEDIA_TYPE_Q726 = 0xc;
    public const uint QMED_MAJOR_MEDIA_TYPE_MAX = 0xd;

    public const uint QMED_MINOR_MEDIA_TYPE_Q711_ALAW = 0x0;
    public const uint QMED_MINOR_MEDIA_TYPE_Q711_ULAW = 0x1;
    public const uint QMED_MINOR_MEDIA_TYPE_Q726_ITU_BYTE_ORDER = 0x0;
    public const uint QMED_MINOR_MEDIA_TYPE_Q726_IETF_BYTE_ORDER = 0x1;

    public const uint QMED_SHA_SIZE = 8;

    public class QMedBase
    {

      // begin qmed binary data variables

      public ulong boxSize; // add 4 bytes for boxSize itself
      public ulong boxType;

      public QBoxFlags boxFlags = new QBoxFlags();

      public ulong majorMediaType;
      public ulong minorMediaType;

      // we support Version 1 only, so the following fields are always present
      public ulong hashSize;
      public ulong[] hashPayload = new ulong[QMED_SHA_SIZE];

      // end qmed binary data variables

      private ulong expectedMediaType;

      public QMedBase(ulong mediaType)
      {
        boxSize = 20; // just for QMedBase: derived objects add their own sizes
        boxType = QMED_BOX_TYPE;
        expectedMediaType = mediaType;
        boxFlags.version = 0;
      }

      public virtual int Read(BinaryReader br)
      {
        int total = 0;
        boxSize = QBox.BE32(br.ReadUInt32()); total += 4;
        boxType = QBox.BE32(br.ReadUInt32()); total += 4;
        if (QMED_BOX_TYPE != boxType)
          throw new Exception("Expecting a QMed, box type is incorrect");
        boxFlags.value = QBox.BE32(br.ReadUInt32()); total += 4;
        majorMediaType = QBox.BE32(br.ReadUInt32()); total += 4;
        if (majorMediaType != expectedMediaType)
          throw new Exception("Media type is not as expected");
        minorMediaType = QBox.BE32(br.ReadUInt32()); total += 4;
        if (boxFlags.version == 1)
        {
          hashSize = QBox.BE32(br.ReadUInt32()); total += 4;
          for (int i = 0; i < (int)hashSize; i++) {
            hashPayload[i] = QBox.BE32(br.ReadUInt32());
            total += 4;
          }
        }
        return (total);
      }

      public virtual void Write(BinaryWriter bw, int dataLen)
      {
        // this method must be called AFTER boxSize has been adjusted by derived object
        bw.Write((Int32)QBox.BE32(boxSize));
        bw.Write((Int32)QBox.BE32(boxType));
        bw.Write((Int32)QBox.BE32(boxFlags.value));
        bw.Write((Int32)QBox.BE32(majorMediaType));
        bw.Write((Int32)QBox.BE32(minorMediaType));
        if (boxFlags.version == 1)
        {
          bw.Write((Int32)QBox.BE32(hashSize));
          for (int i = 0; i < (int)hashSize; i++)
            bw.Write((Int32)QBox.BE32(hashPayload[i]));
        }
      }
    }

    public class QMedJpeg : QMedBase
    {
      public ulong version;
      public ulong width;
      public ulong height;
      public ulong frameTicks;

      public QMedJpeg()
        : base(QMED_MAJOR_MEDIA_TYPE_JPEG)
      {
        version = 1;
        boxSize += 32; // 4 UInt64s == 32 bytes
      }

      public override int Read(BinaryReader br)
      {
        int total = base.Read(br);
        version = QBox.BE64(br.ReadUInt64()); total += 8;
        width = QBox.BE64(br.ReadUInt64()); total += 8;
        height = QBox.BE64(br.ReadUInt64()); total += 8;
        frameTicks = QBox.BE64(br.ReadUInt64()); total += 8;
        return (total);
      }


      public override void Write(BinaryWriter bw, int dataLen)
      {
        base.Write(bw, dataLen);
        bw.Write((UInt64)QBox.BE64(version));
        bw.Write((UInt64)QBox.BE64(width));
        bw.Write((UInt64)QBox.BE64(height));
        bw.Write((UInt64)QBox.BE64(frameTicks));
      }
    }

    ///// <summary>
    ///// QMedH264: This type of QMedBase does not seem to exist in any qbox file.
    ///// </summary>
    //public class QMedH264 : QMedBase
    //{
    //  public ulong version;
    //  public ulong width;
    //  public ulong height;
    //  public ulong sampleTicks;
    //  public ulong motionCounter;
    //  public ulong motionBitmapSize;

    //  public QMedH264()
    //    : base(QMED_MAJOR_MEDIA_TYPE_H264)
    //  {
    //    version = 1;
    //    boxSize += 48; // 6 UInt64's == 48 bytes
    //  }

    //  // There is no QMedH264 object that exists in the qbox file; and so,
    //  // the Read method below is never called when reading from a qbox file.
    //  // Instead, width and height are parsed directly from the H264 payload.
    //  // See also: QBoxSample::GetVideoParamsFromH264SPS in QBox.cs.
    //  public override int Read(BinaryReader br)
    //  {
    //    int total = base.Read(br);
    //    version = QBox.BE64(br.ReadUInt64()); total += 8;
    //    width = QBox.BE64(br.ReadUInt64()); total += 8;
    //    height = QBox.BE64(br.ReadUInt64()); total += 8;
    //    sampleTicks = QBox.BE64(br.ReadUInt64()); total += 8;
    //    motionCounter = QBox.BE64(br.ReadUInt64()); total += 8;
    //    motionBitmapSize = QBox.BE64(br.ReadUInt64()); total += 8;
    //    // NOTE: payload data, if any, is read later
    //    return (total);
    //  }

    //  public override void Write(BinaryWriter bw, int dataLen)
    //  {
    //    base.Write(bw, dataLen);
    //    bw.Write((UInt64)QBox.BE64(version));
    //    bw.Write((UInt64)QBox.BE64(width));
    //    bw.Write((UInt64)QBox.BE64(height));
    //    bw.Write((UInt64)QBox.BE64(sampleTicks));
    //    bw.Write((UInt64)QBox.BE64(motionCounter));
    //    bw.Write((UInt64)QBox.BE64(motionBitmapSize));
    //  }
    //}

    public class QMedPCM : QMedBase
    {
      public ulong version;
      public uint samplingFrequency;
      public uint accessUnits;
      public uint accessUnitSize;
      public uint channels;

      public QMedPCM()
        : base(QMED_MAJOR_MEDIA_TYPE_PCM)
      {
        boxSize += 24;
      }

      public override int Read(BinaryReader br)
      {
        int total = base.Read(br);
        version = QBox.BE64(br.ReadUInt64()); total += 8;
        samplingFrequency = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        accessUnits = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        accessUnitSize = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        channels = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        return (total);
      }

      public override void Write(BinaryWriter bw, int dataLen)
      {
        base.Write(bw, dataLen);
        bw.Write((UInt64)QBox.BE64(version));
        bw.Write((UInt32)QBox.BE32(samplingFrequency));
        bw.Write((UInt32)QBox.BE32(accessUnits));
        bw.Write((UInt32)QBox.BE32(accessUnitSize));
        bw.Write((UInt32)QBox.BE32(channels));
      }
    }

    public class QMedMP2 : QMedBase
    {
      public ulong version;
      public uint samplingFrequency;
      public uint channels;

      public QMedMP2()
        : this(QMED_MAJOR_MEDIA_TYPE_MP2)
      {
      }

      public QMedMP2(ulong mediaType)
        : base(mediaType)
      {
        boxSize += 12;
      }

      public override int Read(BinaryReader br)
      {
        int total = base.Read(br);
        version = QBox.BE32(br.ReadUInt32()); total += 4;
        samplingFrequency = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        channels = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        return (total);
      }

      public override void Write(BinaryWriter bw, int dataLen)
      {
        base.Write(bw, dataLen);
        bw.Write((UInt32)QBox.BE32(version));
        bw.Write((UInt32)QBox.BE32(samplingFrequency));
        bw.Write((UInt32)QBox.BE32(channels));
      }
    }

    public class QMedAAC : QMedMP2
    {
      public uint sampleSize;
      public uint audioSpecificConfigSize;
      public byte[] audioSpecificConfig;
      public int payloadSize;
      public byte[] pesHeader; // TS audio header
      public byte[] adtsHeader; // ADTS header derived from audioSpecificConfig

      public QMedAAC()
        : base(QMED_MAJOR_MEDIA_TYPE_AAC)
      {
        boxSize += 8 + 2 * audioSpecificConfigSize; // FIXME: audioSpecificConfigSize must be zero at this point
      }

      public override int Read(BinaryReader br)
      {
        int total = base.Read(br);
        sampleSize = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        audioSpecificConfigSize = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        audioSpecificConfig = new byte[audioSpecificConfigSize * 2];
        for (int i = 0; i < (2 * audioSpecificConfigSize); i++)
        {
          audioSpecificConfig[i] = br.ReadByte(); total += 1;
        }

#if ADTS
        payloadSize -= (int)boxSize;
        payloadSize += 7;

        // get samplerate, channels and audio coding informations from QMED header
        int objectType = audioSpecificConfig[0] >> 3;
        int samplingFrequencyIndex = ((audioSpecificConfig[0] & 0x07) << 1) | ((audioSpecificConfig[1] & 0x80) >> 7);
        int channelConfiguration = (audioSpecificConfig[1] & 0x78) >> 3;

        adtsHeader = new byte[7];

        adtsHeader[0] = 0xFF;
        adtsHeader[1] = 0xF0;
        adtsHeader[1] |= 0x1;
        adtsHeader[2] = (byte)(((objectType - 1) << 6) & 0xFF); // 0x40 for AAC-LC
        adtsHeader[2] |= (byte)(samplingFrequencyIndex << 2);
        adtsHeader[3] = (byte)(channelConfiguration << 6);
        adtsHeader[3] |= (byte)((payloadSize & 0x1800) >> 11);
        adtsHeader[4] = (byte)((payloadSize & 0x07f8) >> 3);
        adtsHeader[5] = (byte)((payloadSize & 0x0007) << 5);
        adtsHeader[5] |= 0x1F;
        adtsHeader[6] = 0xFC;
#endif
        return (total);
      }

      public override void Write(BinaryWriter bw, int dataLen)
      {
        base.Write(bw, dataLen);
        bw.Write((UInt32)QBox.BE32(sampleSize)); //sampleSize = (uint)QBox.BE32(br.ReadUInt32());
        bw.Write((UInt32)QBox.BE32(audioSpecificConfigSize)); //audioSpecificConfigSize = (uint)QBox.BE32(br.ReadUInt32());
        for (int i = 0; i < (2 * audioSpecificConfigSize); i++)
          bw.Write(audioSpecificConfig[i]); // //audioSpecificConfig[i] = br.ReadByte();
      }

#if ADTS
      const ulong INITIAL_AUDIO_DTS = 200000;   // offset for audio DTS (was 90000)
      const ulong AUDIO_DTS_TO_PTS_DISTANCE = 3003;   //  value 3003 taken from EyeTV recordings,  a value of 0 supresses the DTS in the PES (was 0)
      const byte AUDIO_STREAM = 0xC0;  // PES ID

      public void Read(BinaryReader br, ulong cts)
      {
        Read(br);

        ulong dts = cts + INITIAL_AUDIO_DTS - AUDIO_DTS_TO_PTS_DISTANCE;
        ulong pts = cts + INITIAL_AUDIO_DTS;
        CreatePESHeader(AUDIO_STREAM, payloadSize, pts, dts, true, true);
      }

      private void CreatePESHeader(byte pesID, Int32 dataLen, ulong pts, ulong dts, bool hasPTS, bool hasDTS)
      {
        Int32 headerLen = 0;

        if (hasPTS)
        {
          headerLen += 5;
          if (hasDTS)
            headerLen += 5;
        }
        pesHeader = new byte[4 + 2 + 3 + headerLen];

        UInt32 packetLen = (UInt32)((dataLen > 0 && 3 + headerLen + dataLen <= 0x7FFF) ? 3 + headerLen + dataLen : 0);

        pesHeader[0] = 0x00;
        pesHeader[1] = 0x00;
        pesHeader[2] = 0x01;
        pesHeader[3] = pesID;

        pesHeader[4] = (byte)((packetLen >> 8) & 0xFF);
        pesHeader[5] = (byte)(packetLen & 0xFF);
        pesHeader[6] = 0x80;
        pesHeader[7] = (byte)((hasPTS ? (hasDTS ? 3 : 2) : 0) << 6); // hasDTS = true and hasPTS = false is NOT possible
        pesHeader[8] = (byte)headerLen;

        if (hasPTS)
        {
          UInt32 val = (UInt32)(pts >> 29) & 0x0E;

          pesHeader[9] = (byte)(0x21 | val);

          val = (UInt32)(pts >> 14) & 0xFFFE;

          pesHeader[10] = (byte)(val >> 8);
          pesHeader[11] = (byte)(val | 0x01);

          val = (UInt32)(pts << 1) & 0xFFFE;

          pesHeader[12] = (byte)(val >> 8);
          pesHeader[13] = (byte)(val | 0x01);

          if (hasDTS)
          {
            val = (UInt32)(dts >> 29) & 0x0E;

            pesHeader[14] = (byte)(0x21 | val);

            val = (UInt32)(dts >> 14) & 0xFFFE;

            pesHeader[15] = (byte)(val >> 8);
            pesHeader[16] = (byte)(val | 0x01);

            val = (UInt32)(dts << 1) & 0xFFFE;

            pesHeader[17] = (byte)(val >> 8);
            pesHeader[18] = (byte)(val | 0x01);
          }
        }
      }
#endif
    }

    public class QMed711 : QMedBase
    {
      // Always: 8KHz sample rate, 64Kbps
      // Minor type in header denotes A-law or U-law.
      public ulong version;
      public uint accessUnits;   // Total number of samples in box
      public uint sampleSize;
      //unsigned int channels;    // Always 1 channel?

      public QMed711()
        : base(QMED_MAJOR_MEDIA_TYPE_Q711)
      {
        boxSize += 16;
      }

      public override int Read(BinaryReader br)
      {
        int total = base.Read(br);
        version = QBox.BE64(br.ReadUInt64()); total += 8;
        accessUnits = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        sampleSize = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        return (total);
      }

      public override void Write(BinaryWriter bw, int dataLen)
      {
        base.Write(bw, dataLen);
        throw new Exception("QMed711.Write not implemented");
      }
    }

    public class QMed722 : QMedBase
    {
      // Always: 1 channel, 16KHz sample rate 
      // No minor type; AMR-WB will have its own QMED type.
      public ulong version;
      public uint bitrate;   // 64000, 56000, or 48000 bps for decoder; enc always 64Kbps
      public uint accessUnits;   // Total number of samples
      public uint sampleSize;
      //unsigned int channels;    // Always 1 channel?

      public QMed722()
        : base(QMED_MAJOR_MEDIA_TYPE_Q722)
      {
      }

      public QMed722(ulong mediaType)
        : base(mediaType)
      {
        boxSize += 20;
      }

      public override int Read(BinaryReader br)
      {
        int total = base.Read(br);
        version = QBox.BE64(br.ReadUInt64()); total += 8;
        bitrate = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        accessUnits = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        sampleSize = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        return (total);
      }

      public override void Write(BinaryWriter bw, int dataLen)
      {
        base.Write(bw, dataLen);
        throw new Exception("QMed722.Write not implemented");
      }
    }

    public class QMed726 : QMed722
    {
      public QMed726()
        : base(QMED_MAJOR_MEDIA_TYPE_Q726)
      {
      }

      public override int Read(BinaryReader br)
      {
        int total = base.Read(br);
        return (total);
      }

      public override void Write(BinaryWriter bw, int dataLen)
      {
        base.Write(bw, dataLen);
      }
    }

    public class QMed728 : QMedBase
    {
      public ulong version;
      public uint accessUnits;   // Total number of samples in box

      public QMed728()
        : base(QMED_MAJOR_MEDIA_TYPE_Q728)
      {
        boxSize += 12;
      }

      public override int Read(BinaryReader br)
      {
        int total = base.Read(br);
        version = QBox.BE64(br.ReadUInt64()); total += 8;
        accessUnits = (uint)QBox.BE32(br.ReadUInt32()); total += 4;
        return (total);
      }

      public override void Write(BinaryWriter bw, int dataLen)
      {
        base.Write(bw, dataLen);
        throw new Exception("QMed728.Write not implemented");
      }
    }

    public static void Dump() {
//    QMedStruct *pQMedBase = (QMedStruct *) pQMed;
//	char type[5];
//    int2str(GetQMedBaseBoxType(pQMed), type);
//
//	fprintf(stdout, "QMed Base: Size %lu, Type %s, v%lu, Flags %lX, Media %s/%s, HashSize %lu\n",
//					GetQMedBaseBoxSize(pQMed), type, 
//					GetQMedBaseVersion(pQMed), GetQMedBaseFlags(pQMed), GetQMedBaseMajorMediaString(pQMed),
//					GetQMedBaseMinorMediaString(pQMed), GetQMedBaseHashSize(pQMed));
    }

    public static void DumpH264() {
      Console.WriteLine("QMed H264: Ver {%0}, Width {%1}, Height {%2}, SampleTicks {%3}, MotionCounter {%4}, BitmapSize {%5}"
//					GetQMedH264Version(pQMed), GetQMedH264Width(pQMed), GetQMedH264Height(pQMed),
//					GetQMedH264SampleTicks(pQMed), GetQMedH264MotionCounter(pQMed), GetQMedH264MotionBitmapSize(pQMed));
        );
    }

    public static string int2str(ulong i) {
      string s = "";
      s += (char)((i >> 24) & 0xFF);
      s += (char)((i >> 16) & 0xFF);
      s += (char)((i >> 8) & 0xFF);
      s += (char)(i & 0xFF);
      return (s);
    }
  }
}
