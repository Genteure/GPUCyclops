using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Media.Formats.MP4;
using Media.Formats.Generic;

namespace Media.Formats.QBOX
{
  public class QBox {
    public static int QBOX_VERSION_NUM = 1;

    //////////////////////////////////////////////////////////////////////////////
    // version: 0
    //
    // SDL
    // aligned(8) class QBox extends FullBox('qbox', version = 0, boxflags) {
    //     unsigned short sample_stream_type;
    //     unsigned short sample_stream_id;
    //     unsigned long sample_flags;
    //     unsigned long sample_cts;
    //     unsigned char sample_data[];
    // }
    //
    // equivalent to 
    // typedef struct {
    //     unsigned long box_size;
    //     unsigned long box_type; // "qbox"
    //     unsigned long box_flags; // (version << 24 | boxflags)
    //     unsigned short sample_stream_type;
    //     unsigned short sample_stream_id;
    //     unsigned long sample_flags;
    //     unsigned long sample_cts;
    //     unsigned char sample_data[];
    // } QBox;
    //
    // version 0 does not use large box
    //
    // box_flags
    // 31 - 24         23 - 0
    // version         boxflags
    //
    // boxflags
    // 0x01 sample_data present after box header.
    // otherwise sample_data contain four bytes address and four byte size info for
    // the actual data.
    // 0x02 this is the last sample
    // 0x04 next qbox is word aligned
    // 0x08 audio only
    // 0x10 video only
    // 0x20 stuffing packet (i.e. no meaningful data included)
    //
    // sample_stream_type:
    // 0x01 AAC audio. sample_data contain audio frame or configuration info.
    // 0x02 H.264 video. sample_data contain video frame or configuration info.
    // It consists of 4 bytes length, NAL pairs and possible padding of 0s at the 
    // end to make sample size word aligned.
    // 0x05 H.264 video. sample_data contain video slice or configuration info.
    // 0x06 MP1 audio. sample_data contain audio frame.
    // 0x09 G.711 audio. sample_data contains one audio frame.
    //
    // sample_stream_id:
    // 0, 1, ... or just the stream type
    //
    // sample_flags:
    // 0x01 configuration info. sample_data contain configuration info.
    // 0x02 cts present. 90 kHz cts present.
    // 0x04 sync point. ex. I frame.
    // 0x08 disposable. ex. B frame.
    // 0x10 mute. Sample is mute/black.
    // 0x20 cts base increment. By 2^32.
    // 0x40 QBoxMeta present before configuration info or sample data.
    // 0x80 sample contain end of sequence NALU.
    // 0x100 sample contain end of stream NALU.
    // 0x200 qmed
    // 0xFF000000 padding mask, sample_data contain paddings "in front".
    // sample_size include padding.
    //
    //////////////////////////////////////////////////////////////////////////////

    // track ID to QBoxMetaV mapping
    //static Dictionary<int, QBoxMetaV> VDictionary = new Dictionary<int,QBoxMetaV>();

    #region Constants

    public const uint QBOX_TYPE = 0x71626f78;
    public const uint QBOX_FLAGS_SAMPLE_DATA_PRESENT = 0x1;
    public const uint QBOX_FLAGS_LAST_SAMPLE = 0x2;
    public const uint QBOX_FLAGS_PADDING_4 = 0x4;
    public const uint QBOX_FLAGS_AUDIO_ONLY = 0x8;
    public const uint QBOX_FLAGS_VIDEO_ONLY = 0x10;
    public const uint QBOX_FLAGS_STUFFING_PACKET = 0x20;
    public const uint QBOX_FLAGS_CONTINUITY_COUNTER = 0x100;  // v2 extension

    public const uint QBOX_SAMPLE_TYPE_AAC = 0x1;
    public const uint QBOX_SAMPLE_TYPE_QAC = 0x1;
    public const uint QBOX_SAMPLE_TYPE_H264 = 0x2;
    public const uint QBOX_SAMPLE_TYPE_QPCM = 0x3;
    public const uint QBOX_SAMPLE_TYPE_DEBUG = 0x4;
    public const uint QBOX_SAMPLE_TYPE_H264_SLICE = 0x5;
    public const uint QBOX_SAMPLE_TYPE_QMA = 0x6;
    public const uint QBOX_SAMPLE_TYPE_VIN_STATS_GLOBAL = 0x7;
    public const uint QBOX_SAMPLE_TYPE_VIN_STATS_MB = 0x8;
    public const uint QBOX_SAMPLE_TYPE_Q711 = 0x9;
    public const uint QBOX_SAMPLE_TYPE_Q722 = 0xa;
    public const uint QBOX_SAMPLE_TYPE_Q726 = 0xb;
    public const uint QBOX_SAMPLE_TYPE_Q728 = 0xc;
    public const uint QBOX_SAMPLE_TYPE_JPEG = 0xd;
    public const uint QBOX_SAMPLE_TYPE_MPEG2_ELEMENTARY = 0xe;
    public const uint QBOX_SAMPLE_TYPE_USER_METADATA = 0xf;
    public const uint QBOX_SAMPLE_TYPE_MAX = 0x10;

    public const uint QBOX_SAMPLE_FLAGS_CONFIGURATION_INFO = 0x01;
    public const uint QBOX_SAMPLE_FLAGS_CTS_PRESENT = 0x02;
    public const uint QBOX_SAMPLE_FLAGS_SYNC_POINT = 0x04;
    public const uint QBOX_SAMPLE_FLAGS_DISPOSABLE = 0x08;
    public const uint QBOX_SAMPLE_FLAGS_MUTE = 0x10;
    public const uint QBOX_SAMPLE_FLAGS_BASE_CTS_INCREMENT = 0x20;
    public const uint QBOX_SAMPLE_FLAGS_META_INFO = 0x40;
    public const uint QBOX_SAMPLE_FLAGS_END_OF_SEQUENCE = 0x80;
    public const uint QBOX_SAMPLE_FLAGS_END_OF_STREAM = 0x100;
    public const uint QBOX_SAMPLE_FLAGS_QMED_PRESENT = 0x200;
    public const uint QBOX_SAMPLE_FLAGS_PKT_HEADER_LOSS = 0x400;
    public const uint QBOX_SAMPLE_FLAGS_PKT_LOSS = 0x800;
    public const uint QBOX_SAMPLE_FLAGS_120HZ_CLOCK = 0x1000;
    public const uint QBOX_SAMPLE_FLAGS_TS = 0x1000;
    public const uint QBOX_SAMPLE_FLAGS_TS_FRAME_START = 0x2000;
    public const uint QBOX_SAMPLE_FLAGS_META_DATA = 0x008000;	// Sample data contains meta data.  In this case there is 
															                                // nothing else in the box except the meta data.
		public const uint QBOX_SAMPLE_FLAGS_STOP_POINT = 0x020000;  //*** Current sample is a point suited for easy stop or end of
													                                		   //    a cut.  At this point all frames are complete (no gaps).
    public const uint QBOX_SAMPLE_FLAGS_PADDING_MASK = 0xFF000000;
    public const uint QBOX_SAMPLE_FLAGS_FLAGS_MASK = 0xFF000000; // ROBA new

    // Fixed track IDs
    public const uint QBOX_AUDIO_TRACK = 1U;
    public const uint QBOX_VIDEO_TRACK = 2U;

    public uint QBOX_VERSION(uint box_flags) { return (box_flags >> 24); }
    public uint QBOX_BOXFLAGS(uint box_flags) { return (((box_flags) << 8) >> 8); }
    public uint QBOX_FLAGS(uint v, uint f) { return (((v) << 24) | (f)); }
    public ulong QBOX_SAMPLE_PADDING(ulong sample_flags) { return (((sample_flags) & QBOX_SAMPLE_FLAGS_PADDING_MASK) >> 24); }
    public uint QBOX_SAMPLE_FLAGS_PADDING(uint sample_flags, uint padding) { return ((sample_flags) | ((padding) << 24)); }

    public const uint BASE_TIME_SCALE = 90000; // base sampling rate = 90KHz

    #endregion

    /// <summary>
    /// Default constructor.
    /// Use this when reading from a qbox file, in which case all properties are read in.
    /// </summary>
    public QBox() : this(0UL)
    {
    }

    /// <summary>
    /// Constructor that sets invariant properties.
    /// </summary>
    public QBox(ulong boxFlags)
    {
      // all these initial values are overwritten during read, but are necessary when
      // creating a QBox from scratch. NOTE: mBoxSize is set in another constructor, not in this one
      mBoxType = QBOX_TYPE;
      mBoxFlags.flags = boxFlags;
      mBoxFlags.version = 1; // must set qbox flags before setting mSampleCTS, always set version to 1 for longer durations
      mSampleFlags = QBOX_SAMPLE_FLAGS_CTS_PRESENT | QBOX_SAMPLE_FLAGS_120HZ_CLOCK;
    }

    /// <summary>
    /// Constructor for writing to a qbox file.
    /// FIXME: Need to add v2 properties like mFrameCounter, mContinuityCounter, mSampleDuration, and mStreamDuration.
    /// </summary>
    /// <param name="dataSize"></param>
    /// <param name="flags"></param>
    /// <param name="timeStamp"></param>
    /// <param name="sampleStreamType"></param>
    /// <param name="sampleFlags"></param>
    public QBox(int dataSize, uint qboxFlags, ulong timeStamp, string sampleStreamType, ulong sampleFlags)
      : this(QBOX_FLAGS_SAMPLE_DATA_PRESENT)
    {
      mBoxFlags.flags |= qboxFlags;
      //mSampleCTS = timeStamp;
      // set stream type: at this time we only accept AAC and H264 payload
      mSampleStreamType = (ushort)((sampleStreamType == "Audio") ? QBOX_SAMPLE_TYPE_AAC : 
        ((sampleStreamType == "Video") ? QBOX_SAMPLE_TYPE_H264 : 0u));
      mSampleStreamId = (ushort)((sampleStreamType == "Audio") ? QBOX_AUDIO_TRACK :
        ((sampleStreamType == "Video") ? QBOX_VIDEO_TRACK : 0u));
      mSampleFlags |= sampleFlags;

      if (mBoxFlags.version == 0)
        mHeaderSize = 24;
      else if (mBoxFlags.version == 1)
        mHeaderSize = 28;
      else // version 2;
        mHeaderSize = 80;

      mBoxSize = (ulong)mHeaderSize;
      if ((mBoxFlags.flags & QBOX_FLAGS_SAMPLE_DATA_PRESENT) != 0)
      {
        mSample = new QBoxSample(dataSize, sampleFlags, mSampleStreamType);
        mBoxSize = (ulong)(mHeaderSize + mSample.mSampleHeaderSize + dataSize);
        mSampleSize = dataSize;
      }
    }

    /// <summary>
    /// Constructor to use for recoding, with standard RawBaseTrackInfo input.
    /// Use this to create the very first qbox, which should be a QMed box.
    /// </summary>
    /// <param name="trackInfo">RawBaseTrackInfo</param>
    public QBox(IsochronousTrackInfo trackInfo)
      : this(trackInfo.CodecPrivateData.Length/2, 0, 0UL, trackInfo.HandlerType, QBOX_SAMPLE_FLAGS_QMED_PRESENT)
    {
      this.mSample.privateCodecData = HEAACWaveInfo.HexString2Bytes(trackInfo.CodecPrivateData);
      this.mSampleFlags |= QBOX_SAMPLE_FLAGS_CONFIGURATION_INFO | QBOX_SAMPLE_FLAGS_SYNC_POINT;
      if (trackInfo.HandlerType == "Audio")
      {
        QMed.QMedAAC qmedaac = (QMed.QMedAAC)this.mSample.qmed;
        RawAudioTrackInfo audioInfo = (RawAudioTrackInfo)trackInfo;
        qmedaac.channels = (uint)audioInfo.ChannelCount;
        qmedaac.majorMediaType = QMed.QMED_MAJOR_MEDIA_TYPE_AAC;
        qmedaac.minorMediaType = 0;
        qmedaac.payloadSize = 0;
        qmedaac.sampleSize = (uint)audioInfo.SampleSize;
        qmedaac.samplingFrequency = (uint)audioInfo.SampleRate;
        qmedaac.version = 0;
      }
      else if (trackInfo.HandlerType == "Video")
      {
        RawVideoTrackInfo videoInfo = (RawVideoTrackInfo)trackInfo;
        this.mSample.v.height = (ulong)videoInfo.Height;
        this.mSample.v.width = (ulong)videoInfo.Width;
        this.mSample.v.frameticks = videoInfo.TimeScale;

        //QMed.QMedH264 qmedh264 = (QMed.QMedH264)this.mSample.qmed;
        //RawVideoTrackInfo videoInfo = (RawVideoTrackInfo)trackInfo;
        //qmedh264.height = (ulong)videoInfo.Height;
        //qmedh264.majorMediaType = QMed.QMED_MAJOR_MEDIA_TYPE_H264;
        //qmedh264.minorMediaType = 0;
        //qmedh264.sampleTicks = videoInfo.TimeScale;
        //qmedh264.version = 0;
        //qmedh264.width = (ulong)videoInfo.Width;
      }
    }


    #region Nested Classes

    [StructLayout(LayoutKind.Sequential,Pack=1)]

    public class QBoxMetaA {
      public ulong samplerate;
      public ulong samplesize;
      public ulong channels;

      public int Read(BinaryReader br)
      {
        int count = 0;
        samplerate = QBox.BE64(br.ReadUInt64()); count += 8;
        samplesize = QBox.BE64(br.ReadUInt64()); count += 8;
        channels = QBox.BE64(br.ReadUInt64()); count += 8;
        return count;
      }

      public int Write(BinaryWriter bw)
      {
        int count = 0;
        bw.Write(QBox.BE64(samplerate)); count += 8;
        bw.Write(QBox.BE64(samplesize)); count += 8;
        bw.Write(QBox.BE64(channels)); count += 8;
        return count;
      }
    } 
    
    public class QBoxMetaV {
      public ulong width;
      public ulong height;
      public ulong gop;
      public ulong frameticks;
      public byte[] aud; // access unit delimiter
      public byte[] sps; // sequence param set
      public byte[] pps; // picture param set

      // This Read method is never called when reading from a qbox file.
      // Instead, width and height are parsed directly from the H264 payload.
      // See also: QBoxSample::GetVideoParamsFromH264SPS().
      public int Read(BinaryReader br)
      {
        int count = 0;
        width = QBox.BE64(br.ReadUInt64()); count += 8;
        height = QBox.BE64(br.ReadUInt64()); count += 8;
        gop = QBox.BE64(br.ReadUInt64()); count += 8;
        frameticks = QBox.BE64(br.ReadUInt64()); count += 8;
        return count;
      }

      public int Write(BinaryWriter bw)
      {
        int count = 0;
        bw.Write(QBox.BE64(width)); count += 8;
        bw.Write(QBox.BE64(height)); count += 8;
        bw.Write(QBox.BE64(gop)); count += 8;
        bw.Write(QBox.BE64(frameticks)); count += 8;
        return count;
      }

      public bool Compare(QBoxMetaV compareTo)
      {
        return (sps.SequenceEqual(compareTo.sps) && pps.SequenceEqual(compareTo.pps));
      }
    } 

    /// <summary>
    /// QBoxSample
    /// This is a nested class.
    ///  version 1 sample, although in some areas it looks like we support version 0, we only support this sample type version at the moment...
    /// </summary>
    public class QBoxSample {
      public int mSampleHeaderSize; // not in the actual header, used for keep track of header size... (essentially 8 bytes for addr + size)
      public ulong addr;
      public ulong size;

      public QBoxMetaA a = null;  // these two are mutually exclusive: if one is null, the other is not
      public QBoxMetaV v = null;  // these two are mutually exclusive: if one is null, the other is not

      public QMed.QMedBase qmed;

      public byte[] privateCodecData;

      public byte[] mPayload; // this is not null when CanSeek is false

      public QBoxSample()
      {        
      }

      public QBoxSample(int dataSize, ulong sampleFlags, ushort sampleStreamType)
      {
        if ((sampleFlags & QBox.QBOX_SAMPLE_FLAGS_META_INFO) != 0)
        {
          if (sampleStreamType == QBox.QBOX_SAMPLE_TYPE_H264)
          {
            if (v != null)
              throw new Exception("QBoxSample.constructor: QBoxMetaV v already set");
            v = new QBoxMetaV();
            mSampleHeaderSize = 32;
          }
          else if ((sampleStreamType == QBox.QBOX_SAMPLE_TYPE_QMA) || (sampleStreamType == QBox.QBOX_SAMPLE_TYPE_AAC))
          {
            if (a != null)
              throw new Exception("There should only be one QBoxMetaA for audio");
            a = new QBoxMetaA();
            mSampleHeaderSize = 24;
          }
          else throw new Exception("QBoxSample.Read: Sample stream type not found.");
        }
        else if ((sampleFlags & QBox.QBOX_SAMPLE_FLAGS_QMED_PRESENT) > 0)
        {
          switch ((uint)sampleStreamType)
          {
            case QBOX_SAMPLE_TYPE_AAC:
              QMed.QMedAAC qmedaac = new QMed.QMedAAC();
              qmed = qmedaac;
              break;
            //case QBOX_SAMPLE_TYPE_H264:
            //case QBOX_SAMPLE_TYPE_H264_SLICE:
            //  QMed.QMedH264 qmedh264 = new QMed.QMedH264();
            //  qmed = qmedh264;
            //  break;
            case QBOX_SAMPLE_TYPE_QPCM:
              QMed.QMedPCM qmedpcm = new QMed.QMedPCM();
              qmed = qmedpcm;
              break;
            case QBOX_SAMPLE_TYPE_Q711:
              QMed.QMed711 qmed711 = new QMed.QMed711();
              qmed = qmed711;
              break;
            case QBOX_SAMPLE_TYPE_Q722:
              QMed.QMed722 qmed722 = new QMed.QMed722();
              qmed = qmed722;
              break;
            case QBOX_SAMPLE_TYPE_Q726:
              QMed.QMed726 qmed726 = new QMed.QMed726();
              qmed = qmed726;
              break;
            case QBOX_SAMPLE_TYPE_Q728:
              QMed.QMed728 qmed728 = new QMed.QMed728();
              qmed = qmed728;
              break;
            case QBOX_SAMPLE_TYPE_JPEG:
            case QBOX_SAMPLE_TYPE_MPEG2_ELEMENTARY:
            case QBOX_SAMPLE_TYPE_USER_METADATA:
            case QBOX_SAMPLE_TYPE_QMA:
            case QBOX_SAMPLE_TYPE_DEBUG:
            case QBOX_SAMPLE_TYPE_VIN_STATS_GLOBAL:
            case QBOX_SAMPLE_TYPE_VIN_STATS_MB:
              break;
            default:
              throw new Exception(string.Format("Unknown QMed type: {0}", sampleStreamType));
          }
          mSampleHeaderSize = (int)qmed.boxSize;
        }
        else
        {
          mSampleHeaderSize = 0;
        }
      }

      /// <summary>
      /// QBox.QBoxSample.Read
      /// This should not read the sample data as yet, just the box header.
      /// </summary>
      /// <param name="br"></param>
      /// <param name="inTotalSize"></param>
      /// <param name="inBoxFlags"></param>
      /// <param name="sampleFlags"></param>
      /// <param name="sampleStreamType"></param>
      public int Read(BinaryReader br, int inTotalSize, ulong inBoxFlags, ulong sampleFlags, ushort sampleStreamType, 
                      ushort streamID) {
        mSampleHeaderSize = 0;
        if ((inBoxFlags & QBOX_FLAGS_SAMPLE_DATA_PRESENT) == 0)
          return 0; // no data, no QBoxSample
        
        if ((sampleFlags & QBox.QBOX_SAMPLE_FLAGS_META_INFO) > 0)
        {
          ReadMetaInfo(br, sampleStreamType, inTotalSize);
        }
        else if ((sampleFlags & QBox.QBOX_SAMPLE_FLAGS_QMED_PRESENT) > 0)
        {
          ReadQMed(br, sampleStreamType, inTotalSize, sampleFlags);
        }
        else
        {
          int tmpSize = inTotalSize - mSampleHeaderSize;
          int h264HeaderSize = 0;
          int residualByteCount = 0;

          if (sampleStreamType == QBox.QBOX_SAMPLE_TYPE_H264)
          {
            if ((sampleFlags & QBox.QBOX_SAMPLE_FLAGS_CONFIGURATION_INFO) != 0UL) // video may not have meta box at all
            {
              bool VSet = false;
#if REMOVE_EXTRA_SPS
              VSet = VDictionary.ContainsKey(streamID);
#endif
              // convert to h.264 Byte Stream Format (see Annex B of h.264 spec), then
              // extract SPS from byte stream; this is only necessary if the input qbox file has no config box.
              // setting mSampleHeaderSize to the total size of delimiter, sps, and pps removes these.
              // last parameter is set to residual bytes after NALU
              h264HeaderSize = GetVideoParamsFromH264SPS(br, tmpSize, VSet, out residualByteCount);
#if REMOVE_EXTRA_SPS
              if (VSet)
              {
                // is it the same sps and pps?
                if (!VDictionary[streamID].Compare(v))
                  throw new Exception("SPS or PPS changed");
                mSampleHeaderSize = h264HeaderSize;
              }
              else VDictionary.Add(streamID, v);
#endif
              // increase return value by total number of extra bytes after some NALUs
              // the effect of this is to reduce the size of the H264 payload for this box,
              // while keeping the sample header size the same
              return (mSampleHeaderSize + residualByteCount);
            }
            else // for all other video boxes, remove residual bytes also
            {
              mPayload = br.ReadBytes(tmpSize);
              BinaryReader reader = new BinaryReader(new MemoryStream(mPayload));
              int count = tmpSize;
              while (count > 4)
              {
                ulong naluLen = BE32(reader.ReadUInt32());
                count -= (int)(naluLen + 4);
                reader.BaseStream.Position += (long)naluLen;
              }

              return count;
            }
          }
          else
          {
            mPayload = br.ReadBytes(tmpSize);
          }
        }
        return mSampleHeaderSize;
      }

      /// <summary>
      /// Write
      /// When writing, we write both headers and data in a single operation; 
      /// there is no need to separate the writing of data from writing of headers because, unlike in MP4,
      /// data immediately follows the headers in the QBox.
      /// </summary>
      /// <param name="bw"></param>
      /// <param name="sampleFlags"></param>
      /// <param name="sampleStreamType"></param>
      public void Write(BinaryWriter bw, ulong sampleFlags, ushort sampleStreamType, byte[] data)
      {
        int dataLen = (data == null) ? 0 : data.Length;
        if ((sampleFlags & QBox.QBOX_SAMPLE_FLAGS_META_INFO) > 0)
        {
          WriteMetaInfo(bw, sampleStreamType, dataLen);
        }
        else if ((sampleFlags & QBox.QBOX_SAMPLE_FLAGS_QMED_PRESENT) > 0)
        {
          this.qmed.Write(bw, dataLen);
        }

        if (dataLen > 0)
          bw.Write(data);
      }

      /// <summary>
      /// ReadMetaInfo
      /// </summary>
      /// <param name="br"></param>
      /// <param name="sampleStreamType"></param>
      /// <param name="inTotalSize"></param>
      private void ReadMetaInfo(BinaryReader br, uint sampleStreamType, int inTotalSize)
      {
        if (sampleStreamType == QBox.QBOX_SAMPLE_TYPE_H264)
        {
          if (v != null)
            throw new Exception("ReadMetaInfo: QBoxMetaV v already set");
          v = new QBoxMetaV();
          mSampleHeaderSize = v.Read(br);
          if ((inTotalSize - mSampleHeaderSize) != 0)
            throw new Exception("QBoxSample.Read: Video size incorrect.");
        }
        else if ((sampleStreamType == QBox.QBOX_SAMPLE_TYPE_QMA) || (sampleStreamType == QBox.QBOX_SAMPLE_TYPE_AAC))
        {
          if (a != null)
            throw new Exception("There should only be one QBoxMetaA for audio");
          a = new QBoxMetaA();
          mSampleHeaderSize = a.Read(br);
          if ((inTotalSize - mSampleHeaderSize) != 0)
            throw new Exception("QBoxSample.Read: Audio size incorrect.");
        }
        else throw new Exception("QBoxSample.Read: Sample stream type not found.");
      }

      /// <summary>
      /// WriteMetaInfo
      /// </summary>
      /// <param name="bw"></param>
      /// <param name="sampleStreamType"></param>
      private void WriteMetaInfo(BinaryWriter bw, ushort sampleStreamType, int dataLen)
      {
        int count = 0;
        if (sampleStreamType == QBox.QBOX_SAMPLE_TYPE_H264)
        {
          if (v == null)
            throw new Exception("Missing QBoxMetaV for video");
          count = v.Write(bw);
        }
        else if ((sampleStreamType == QBox.QBOX_SAMPLE_TYPE_QMA) || (sampleStreamType == QBox.QBOX_SAMPLE_TYPE_AAC))
        {
          if (a == null)
            throw new Exception("Missing QBoxMetaA for audio");
          count = a.Write(bw);
        }
        else throw new Exception("QBoxSample.WriteMetaInfo: Sample stream type not found.");
        if (mSampleHeaderSize != count)
          throw new Exception("QBoxSample.WriteMetaInfo: Sample size incorrect.");
      }

      /// <summary>
      /// ReadQMed
      /// Read different types of QMed.
      /// Side-effects: sets qmed and privateCodecData vars.
      /// </summary>
      /// <param name="br">BinaryReader</param>
      /// <param name="sampleStreamType"></param>
      /// <param name="inTotalSize"></param>
      private void ReadQMed(BinaryReader br, ushort sampleStreamType, int inTotalSize, ulong sampleFlags)
      {
        long byteCount = 0;
        switch ((uint)sampleStreamType)
        {
          case QBOX_SAMPLE_TYPE_AAC:
            QMed.QMedAAC qmedaac = new QMed.QMedAAC();
            qmedaac.payloadSize = inTotalSize - mSampleHeaderSize;
#if ADTS
            qmedaac.Read(br, cts); 
#endif
            qmed = qmedaac;
            break;
          //case QBOX_SAMPLE_TYPE_H264:
          //case QBOX_SAMPLE_TYPE_H264_SLICE:
          //  QMed.QMedH264 qmedh264 = new QMed.QMedH264();
          //  qmed = qmedh264;
          //  break;
          case QBOX_SAMPLE_TYPE_QPCM:
            QMed.QMedPCM qmedpcm = new QMed.QMedPCM();
            qmed = qmedpcm;
            break;
          case QBOX_SAMPLE_TYPE_Q711:
            QMed.QMed711 qmed711 = new QMed.QMed711();
            qmed = qmed711;
            break;
          case QBOX_SAMPLE_TYPE_Q722:
            QMed.QMed722 qmed722 = new QMed.QMed722();
            qmed = qmed722;
            break;
          case QBOX_SAMPLE_TYPE_Q726:
            QMed.QMed726 qmed726 = new QMed.QMed726();
            qmed = qmed726;
            break;
          case QBOX_SAMPLE_TYPE_Q728:
            QMed.QMed728 qmed728 = new QMed.QMed728();
            qmed = qmed728;
            break;
          case QBOX_SAMPLE_TYPE_JPEG:
            // unknown 12-byte jpeg prefix
            byte[] unknown = new byte[12];
            br.Read(unknown, 0, 12);
            QMed.QMedJpeg qmedJpeg = new QMed.QMedJpeg();
            qmed = qmedJpeg;
            break;
          case QBOX_SAMPLE_TYPE_MPEG2_ELEMENTARY:
          case QBOX_SAMPLE_TYPE_USER_METADATA:
          case QBOX_SAMPLE_TYPE_QMA:
          case QBOX_SAMPLE_TYPE_DEBUG:
          case QBOX_SAMPLE_TYPE_VIN_STATS_GLOBAL:
          case QBOX_SAMPLE_TYPE_VIN_STATS_MB:
            break;
          default:
            throw new Exception(string.Format("Unexpected QBox type: {0}", sampleStreamType));
        }

        int count;
#if ADTS
        if (qmed.GetType() != typeof(QMed.QMedAAC))
         count = qmed.Read(br);
#else
        count = qmed.Read(br);
#endif
        if (count != (int)qmed.boxSize)
          throw new Exception("QMed header count inconsistent");

        mSampleHeaderSize += (int)qmed.boxSize;

        byteCount = inTotalSize - count;

        if (byteCount < 0)
          throw new Exception("QMed read: bad box size");

        if (byteCount > 0)
        {
          // read-in data; set mPayload, except when it's a config box
          if ((sampleFlags & QBOX_SAMPLE_FLAGS_CONFIGURATION_INFO) != 0)
          {
            // get private codec data
            privateCodecData = br.ReadBytes((int)byteCount);
            mSampleHeaderSize += (int)byteCount;
            byteCount = 0;
          }
          else // read payload now
          {
            mPayload = br.ReadBytes((int)byteCount);
          }
        }
      }

      /// <summary>
      /// GetVideoParamsFromH264SPS
      /// This private method sets v, the video meta sample.
      /// It returns the total size of delimiter, sps, and pps NALUs.
      /// </summary>
      /// <param name="br"></param>
      /// <param name="sampleSize"></param>
      /// <param name="VSetAlready">if V is already set, remove sps and pps and move access unit delimiter</param>
      int GetVideoParamsFromH264SPS(BinaryReader br, int sampleSize, bool VSetAlready, out int countToZero)
      {
        int totalSize = 0;
        countToZero = sampleSize;
        if (sampleSize < 60)
          throw new Exception("Payload too small");
        mPayload = br.ReadBytes(sampleSize);
        BinaryReader reader = new BinaryReader(new MemoryStream(mPayload));
        while (countToZero > 4)
        {
          ulong naluLen = BE32(reader.ReadUInt32());
          long nextPos = reader.BaseStream.Position + (long)naluLen;
          uint typ = reader.ReadByte();
          if ((naluLen > (ulong)countToZero) || (naluLen < 2))
            throw new Exception("Invalid QBox video payload");

          // access unit delimiter (aud) always comes first and its size is not added to total size because
          // it is be added back to the payload (see QBoxVideoTrack).
          if ((typ & 0x1Fu) == 9u)
          {
            if (v != null)
              throw new Exception("QBoxSample: QBoxMetaV object already exists, and a second one cannot be constructed");
            v = new QBoxMetaV();
            if (naluLen != 2)
              throw new Exception("Wrong nalu delimiter length");
            if (VSetAlready)
            {
              v.aud = new byte[naluLen];
              v.aud[0] = (byte)typ;
              v.aud[1] = reader.ReadByte();
            }
            else if (v.aud != null)
              throw new Exception("QBox.QBoxSample.GetVideoParamsFromH264SPS: v.aud should be null");
          }

          // if nalu type is Sequence Param Set, pick up width and height
          // also, build private codec data from this SPS
          // NOTE: it matters which video track this qbox belongs!
          if ((typ & 0x1Fu) == 7u)
          {
            v.sps = new byte[naluLen];
            v.sps[0] = (byte)typ;
            reader.Read(v.sps, 1, (int)naluLen - 1);
            totalSize += (int)(4 + naluLen);
            // parse the SPS bit stream, just to get the correct width and height of video.
            BitReader bitReader = new BitReader(new MemoryStream(v.sps));
            H264SPS sps = new H264SPS();
            sps.Read(bitReader);
            v.width = (ulong)sps.gWidth;
            v.height = (ulong)sps.gHeight;
          }
          else if ((typ & 0x1Fu) == 8u)
          {
            v.pps = new byte[naluLen];
            v.pps[0] = (byte)typ;
            reader.Read(v.pps, 1, (int)naluLen - 1);
            totalSize += (int)(4 + naluLen);
          }

          countToZero -= ((int)naluLen + 4);
          reader.BaseStream.Position = nextPos;
        }

        return totalSize;
      }

      /// <summary>
      /// ConvertToByteStream
      /// (NOTE: this is not used)
      /// </summary>
      /// <param name="br"></param>
      /// <param name="length"></param>
      /// <param name="refSample"></param>
      void ConvertToByteStream(BinaryReader br, int length, ref byte[] refSample)
      {
        MemoryStream outStream = new MemoryStream(refSample);
        BinaryWriter writer = new BinaryWriter(outStream);

        int countToZero = length;
        while (countToZero > 4)
        {
          long naluLen = (long)BE32(br.ReadUInt32());
          writer.Write((uint)BE32(0x00000001));
          if ((naluLen <= 0) || (naluLen > countToZero))
            throw new Exception("Invalid QBox video payload");
          writer.Write(br.ReadBytes((int)naluLen));

          countToZero -= ((int)naluLen + 4);
        }

        if (countToZero > 0)
          writer.Write(br.ReadBytes(countToZero));

        writer.Close();
      }
    } // end of QSample class

    #endregion


    public ulong mBoxSize = 0;
    public ulong mBoxType = 0;
    public QBoxFlags mBoxFlags = new QBoxFlags();
    public ushort mSampleStreamType;
    public ushort mSampleStreamId = ushort.MaxValue;
    public ulong mSampleFlags;

    // version 1 uses the following additional fields which are optionals based on the flags that are set

    // version 0 uses only the CTSHigh and the CTSLow is NOT present, so you need to account for NOT reading the Low value for header sizes, etc.
    public ulong _SampleCTSHigh;
    public ulong _SampleCTSLow;

    public UInt64 mSampleCTS {
      set {
        if (mBoxFlags.version == 0)
          _SampleCTSHigh = (ulong)(value & 0xFFFFFFFF);
        else {
          _SampleCTSHigh = (ulong)(value >> 32);
          _SampleCTSLow = (ulong)(value & 0xFFFFFFFF);
        }
      }

      get {
        if (mBoxFlags.version == 0) {
          return _SampleCTSHigh;
        }
        return ((UInt64)(_SampleCTSHigh) << 32) | (_SampleCTSLow);
      }
    }

		// The following are for v2.0 of the qbox spec, not defined by Maxim/Mobiligen but by Orions Systems!
#region QBOXV2
		private uint _mBoxContinuityCounter = 0;
    public uint mBoxContinuityCounter {
      get { return (_mBoxContinuityCounter); }
      set {
        UpgradeToV2();
        _mBoxContinuityCounter = value;
      }
    }
    public bool ContinuityCounterValid { get { return ((mBoxFlags.flags & QBOX_FLAGS_CONTINUITY_COUNTER) > 0L); } }

    private uint _mFrameCounter = 0;
    public uint mFrameCounter {
      get { return (_mFrameCounter); }
      set {
        UpgradeToV2();
        _mFrameCounter = value;
      }
    }

    private uint _mSampleDuration = 0;
    public uint mSampleDuration {
      get { return (_mSampleDuration); }
      set {
        UpgradeToV2();
        _mSampleDuration = value;
      }
    }
    public bool SampleDurationValid { get { return (_mSampleDuration != 0); } }

    private ulong _mStreamDuration = 0;
    public ulong mStreamDuration {
      get { return (_mStreamDuration); }
      set {
        UpgradeToV2();
        _mStreamDuration = value;
      }
    }

		//// v2 flags used to help find cut points in a qbox stream...
		//public bool mStartOfCutPoint { 
		//  get { return ((mBoxFlags.flags & QBOX_SAMPLE_FLAGS_START_OF_CUT_POINT) > 0L); }
		//  set {
		//    UpgradeToV2();
		//    if (value == false) {
		//      mBoxFlags.flags &= ~QBOX_SAMPLE_FLAGS_START_OF_CUT_POINT; 
		//    } else {
		//      mBoxFlags.flags |= QBOX_SAMPLE_FLAGS_START_OF_CUT_POINT;
		//    }
		//  }
		//}

    public bool mStopPoint {
			get { return ((mSampleFlags & QBOX_SAMPLE_FLAGS_STOP_POINT) > 0L); }
      set {
        UpgradeToV2();
        if (value == false) {
					mSampleFlags &= ~QBOX_SAMPLE_FLAGS_STOP_POINT;
        } else {
					mSampleFlags |= QBOX_SAMPLE_FLAGS_STOP_POINT;
        }
      }
    }

		public bool mSyncPoint {
			get { return ((mSampleFlags & QBOX_SAMPLE_FLAGS_SYNC_POINT) > 0L); }
			set {
				UpgradeToV2();
				if (value == false) {
					mSampleFlags &= ~QBOX_SAMPLE_FLAGS_SYNC_POINT;
				} else {
					mSampleFlags |= QBOX_SAMPLE_FLAGS_SYNC_POINT;
				}
			}
		}


    private void UpgradeToV2() {
      if (mBoxFlags.version == 1) { mBoxSize += 52; mHeaderSize += 52; }
      mBoxFlags.version = 2;
    }

		public uint mOffsetFromLastSyncPoint = 0;
		public uint mOffsetFromLastFrame = 0;

    private ulong _mReserved2 = 0;
    public ulong mReserved2 {
      get { return (_mReserved2); }
      set {
        mBoxFlags.version = 2;
        _mReserved2 = value;
      }
    }

    private ulong _mReserved3 = 0;
    public ulong mReserved3 {
      get { return (_mReserved3); }
      set {
        mBoxFlags.version = 2;
        _mReserved3 = value;
      }
    }

    private ulong _mReserved4 = 0;
    public ulong mReserved4 {
      get { return (_mReserved4); }
      set {
        mBoxFlags.version = 2;
        _mReserved4 = value;
      }
    }
    /////////// END OF v2 fields //////////////
#endregion


#region helpers

    //public ulong mSampleDuration10nanoUnits { get { return (MediaTimes[mSampleStreamId].TicksToTime(mSampleDuration, MediaTimeUtils.TimeUnitType.TenNanoSeconds)); } }
    //public ulong mStreamDuration10nanoUnits { get { return (MediaTimes[mSampleStreamId].TicksToTime(mStreamDuration, MediaTimeUtils.TimeUnitType.TenNanoSeconds)); } }
    //public ulong mSampleStreamDurationPreSample10nanoUnits { get { return (MediaTimes[mSampleStreamId].TicksToTime(mSampleStreamDurationPreSample, MediaTimeUtils.TimeUnitType.TenNanoSeconds)); } }
		public ulong mSampleStreamDurationPreSample {
			get {
				if (mStreamDuration <= 0) return (0); // this is true if we are an out of order box and thus the duration is unknown at this time
				ulong ans = mStreamDuration - mSampleDuration;
				return (ans);
			}
		}
		
#endregion

		// the following is included in all box versions
    public int mSampleSize = 0; // includes sample headers, etc. thus not just the payload
    public QBoxSample mSample;

    // The following are only used for bookkeeping, but are not actually part of the binary data
    // NOTE: these are not set during writes
    public int mHeaderSize = 0;
    public long mHeaderPosition = 0;
    public long mSamplePosition = 0;
    public long mCurrentPosition = 0;

    #region FixTimeStamp kludge

    // Fix the time stamp (mStreamDuration) if it is zero
//nbl it doesn't make sense fixing the time stamps of bframes, which is where mStreamDuration = 0
		//public void FixTimeStamp(Dictionary<ushort, ulong> PrevTimes, Dictionary<ushort, int> PrevIndices)
		//{
		//  if (mFrameCounter == 0) // can't fix time stamp at time = 0
		//    return;

		//  ushort k = mSampleStreamId;
		//  if (!PrevTimes.Keys.Contains(k))
		//    PrevTimes.Add(k, 0UL);
		//  if (!PrevIndices.Keys.Contains(k))
		//    PrevIndices.Add(k, 0);

		//  ulong prevTime = PrevTimes[k];
		//  int prevIndex = PrevIndices[k];

		//  int sliceCounter = (int)mFrameCounter;
		//  // use previous TimeStamp to calc TimeStamp instead of relying solely on box.mStreamDuration
		//  if (mStreamDuration == 0UL)
		//  {
		//    if (sliceCounter != prevIndex + 1)
		//      throw new Exception("QBox.FixTimeStamp: bad frame index");
		//    if (mSampleDuration == 0)
		//      mSampleDuration = (uint)(prevTime / (uint)prevIndex + 1); // use average of all previous sample durations

		//    mStreamDuration = prevTime + mSampleDuration; // repair the time stamp
		//  }
		//  PrevTimes[k] = mStreamDuration;
		//  PrevIndices[k] = sliceCounter;
		//}

    #endregion

    /// <summary>
    /// SearchForNextQBox
    /// Position the stream so that it starts at a qbox right after the qbox type signature.
    /// QBOX_TYPE = 0x71626f78
    /// Returns: qbox position
    /// </summary>
    public static bool SeekNextQBox(BinaryReader br)
    {
      if (br.BaseStream.CanSeek == false) return (false);
      long storedPosition = br.BaseStream.Position;

      while (true) {
        if (br.BaseStream.Position + 4 >= br.BaseStream.Length) {
          // not enough data...
          br.BaseStream.Position = storedPosition;
          return (false);
        }

        byte[] qT = br.ReadBytes(4);
        // look for Q B O X bytes...
        if (qT[0] == 0x71 && qT[1] == 0x62 && qT[2] == 0x6f && qT[3] == 0x78) {
          if (br.BaseStream.Position + 4 >= br.BaseStream.Length) {
            // not enough data...
            br.BaseStream.Position = storedPosition;
            return (false);
          }
          byte[] qLenBytes = br.ReadBytes(4);
          uint qLen = BitConverter.ToUInt32(qLenBytes, 0);
          qLen = (uint)BE32((ulong)qLen);

          if (br.BaseStream.Position + qLen >= br.BaseStream.Length) {
            // not enough data...
            br.BaseStream.Position = storedPosition;
            return (false);
          }

          br.BaseStream.Position -= 8; // point back to the start of the qbox we found
          return (true);
        }

        br.BaseStream.Position -= 3; // try again, one byte further along...
      } // end of while (true)
      // this part should never be reached because there is no break statement in the while(true) loop
      //return true;
    }

    /// <summary>
    /// SearchForNextQBox
    /// Position the stream so that it starts at a qbox right after the qbox type signature.
    /// QBOX_TYPE = 0x71626f78
    /// Returns: qbox position
    /// </summary>
    public static bool SeekPrevQBox(BinaryReader br) {
      if (br.BaseStream.CanSeek == false) return (false);
      long storedPosition = br.BaseStream.Position;

      while (true) {
        if (br.BaseStream.Position + 4 >= br.BaseStream.Length) {
          // not enough data...
          br.BaseStream.Position = storedPosition;
          return (false);
        }

        byte[] qT = br.ReadBytes(4);
        // look for Q B O X bytes...
        if (qT[0] == 0x71 && qT[1] == 0x62 && qT[2] == 0x6f && qT[3] == 0x78) {
          if (br.BaseStream.Position - 8 < 0) {
            // not enough data... prior to QBOX to read length...
            br.BaseStream.Position = storedPosition;
            return (false);
          }

          br.BaseStream.Position -= 8;
          byte[] qLenBytes = br.ReadBytes(4);
          uint qLen = BitConverter.ToUInt32(qLenBytes, 0);
          qLen = (uint)BE32((ulong)qLen);
          br.BaseStream.Position -= 4; // point back to the start of the qbox we found

          if (br.BaseStream.Position + qLen > br.BaseStream.Length) {
            // not enough data...
            br.BaseStream.Position = storedPosition;
            return (false);
          }

          return (true);
        }

        if (br.BaseStream.Position - 5 < 0) {
          // can't back up any further...
          br.BaseStream.Position = storedPosition;
          return (false);
        }
        br.BaseStream.Position -= 5; // try again, one byte further along...
      }
      //return true;
    }

    /// <summary>
    /// SearchForAnyQBox
    /// This is an alternative to SeekNextQBox and SeekPrevQBox above.
    /// Position the stream so that it starts at a qbox right after the qbox type signature.
    /// This alternative does not require 8-byte alignment.
    /// QBOX_TYPE = 0x71626f78
    /// Returns: size of qbox.
    /// </summary>
    public static uint SearchForAnyQBox(BinaryReader br)
    {
      uint qboxSize = 0;
      try
      {
        int state = 0;
        byte b;
        int i = 0;
        byte[] circularByteBuf = new byte[8];
        while (true)
        {
          b = br.ReadByte();
          switch (state)
          {
            case 0:
              if (b == 0x71) // the letter Q
                state++;
              break;
            case 1:
              if (b == 0x62) // the letter B
                state++;
              else state = 0;
              break;
            case 2:
              if (b == 0x6f) // the letter O
                state++;
              else state = 0;
              break;
            case 3:
              if (b == 0x78) // the letter X
                state++;
              else state = 0;
              break;
            default:
              break; // done
          }
          circularByteBuf[i] = b;
          i = (i + 1) % 8;
          if (state == 4)
            break; // at this point, i should point to first byte of size
        } // end of while true
        byte[] uintBytes = new byte[4];
        for (int j = 0; j < 4; j++)
        {
          uintBytes[j] = circularByteBuf[i];
          i = (i + 1) % 8;
        }
        qboxSize = BitConverter.ToUInt32(uintBytes, 0);
        qboxSize = (uint)BE32((ulong)qboxSize);
      }
      catch (Exception ex)
      {
        if (ex is EndOfStreamException)
          qboxSize = 0U;
        else throw ex;
      }
      return qboxSize;
    }

    /// <summary>
    /// QBox.Read
    /// Read-in a QBox
    /// NOTE: stream position is NOT assumed to start at the beginning of a qbox.
    /// </summary>
    /// <param name="br"></param>
    public void Read(BinaryReader br) {
		 //ODS.Processing.Core.Logger.Instance.Info("Reading QBox");
      mHeaderSize = 0; 
      if (br.BaseStream.CanSeek) {
        mHeaderPosition = br.BaseStream.Position; // needed for flashback only
        if (br.BaseStream.Position + 4 > br.BaseStream.Length) 
          throw new Exception("QBox:Read, Not enough data to read qbox length");
      }
      mBoxSize = QBox.BE32(br.ReadUInt32()); mHeaderSize += 4;
      if (mBoxSize == 0)
        throw new Exception("QBox:Read, Invalid box size");

      if (br.BaseStream.CanSeek) {
        // subtract 4 as the size includes the 4 bytes we read already which was the length...
        if (br.BaseStream.Position + (long)(mBoxSize - 4) > br.BaseStream.Length) {
          // reset the location to the start of the qbox
          // this allows the reader to try again later...
          br.BaseStream.Position = mHeaderPosition; 
          throw new Exception("QBox:Read, Not enough data to read qbox content");
        }
      }

      try {
        mBoxType = QBox.BE32(br.ReadUInt32());
        mHeaderSize += 4;
        if (mBoxType != QBOX_TYPE)
          throw new Exception("QBox:Read, Invalid box type (not a qbox)");
        mBoxFlags.value = (uint) QBox.BE32(br.ReadUInt32());
        mHeaderSize += 4;
        mSampleStreamType = QBox.BE16(br.ReadUInt16());
        mHeaderSize += 2;
        mSampleStreamId = QBox.BE16(br.ReadUInt16());
        mHeaderSize += 2; // replacement for all of commented lines below
        mSampleFlags = QBox.BE32(br.ReadUInt32());
        mHeaderSize += 4;

        // version 0 only uses a single int for the CTS, version 1 uses 64 bit high/low value
        _SampleCTSHigh = QBox.BE32(br.ReadUInt32());
        mHeaderSize += 4;
        if (mBoxFlags.version == 1 || mBoxFlags.version == 2) {
          _SampleCTSLow = QBox.BE32(br.ReadUInt32());
          mHeaderSize += 4;
        }

        if (mBoxFlags.version == 2) {
          mBoxContinuityCounter = (uint)QBox.BE32(br.ReadUInt32()); mHeaderSize += 4;
          mFrameCounter = (uint)QBox.BE32(br.ReadUInt32()); mHeaderSize += 4;
          mSampleDuration = (uint)QBox.BE32(br.ReadUInt32()); mHeaderSize += 4;
          mStreamDuration = (ulong)QBox.BE64(br.ReadUInt64()); mHeaderSize += 8;

					mOffsetFromLastSyncPoint = (uint)QBox.BE32(br.ReadUInt32()); mHeaderSize += 4;
					mOffsetFromLastFrame = (uint)QBox.BE32(br.ReadUInt32()); mHeaderSize += 4;

          mReserved2 = (ulong)QBox.BE64(br.ReadUInt64()); mHeaderSize += 8;
          mReserved3 = (ulong)QBox.BE64(br.ReadUInt64()); mHeaderSize += 8;
          mReserved4 = (ulong)QBox.BE64(br.ReadUInt64()); mHeaderSize += 8;
        }

        //// adjust time stamp if 120KHz bit is ON (doesn't matter which track)
        //if ((mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_120HZ_CLOCK) != 0U) {
        //  mSampleCTS = (mSampleCTS * QBox.BASE_TIME_SCALE) / 120000U;
        //  mSampleDuration = (mSampleDuration * QBox.BASE_TIME_SCALE) / 120000U;
        //  mStreamDuration = (mStreamDuration * QBox.BASE_TIME_SCALE) / 120000U;
        //}

        mSampleSize = (int) mBoxSize - mHeaderSize;
        if (mSampleSize == 0)
          return;

        mSample = new QBoxSample();
        mSampleSize -= mSample.Read(br, mSampleSize, mBoxFlags.flags, mSampleFlags, mSampleStreamType, mSampleStreamId);
        mSamplePosition += mSample.mSampleHeaderSize;
      } catch (Exception ex) {
        // reset the location to the start of the qbox
        // this allows the reader to try again later...
        if (br.BaseStream.CanSeek) 
          br.BaseStream.Position = mHeaderPosition;
        throw ex;
      }
#if REMOVE_EXTRA_SPS
      // deal with the special case in which the box is a Meta V box
      if ((mSample.v != null) && (mSample.mSampleHeaderSize > 0))
        mSamplePosition += 6; // nalu delimiter length is always 6
#endif
    }


    /// <summary>
    /// QBox.Write
    /// Write out all of a QBox: properties and payload.
    /// </summary>
    /// <param name="bw"></param>
    public void Write(BinaryWriter bw)
    {
      this.Write(bw, this.mSample.mPayload);
    }

    /// <summary>
    /// QBox.Write
    /// </summary>
    /// <param name="bw"></param>
    public int Write(BinaryWriter bw, byte[] data)
    {
		 //ODS.Processing.Core.Logger.Instance.Info("Writing QBox");

      int count = 0;
      bw.Write((Int32)QBox.BE32(mBoxSize)); count += 4;
      bw.Write((Int32)QBox.BE32(mBoxType)); count += 4;
      bw.Write((Int32)QBox.BE32(mBoxFlags.value)); count += 4;
      bw.Write((Int16)QBox.BE16(mSampleStreamType)); count += 2;
      bw.Write((Int16)QBox.BE16(mSampleStreamId)); count += 2;
      bw.Write((Int32)QBox.BE32(mSampleFlags)); count += 4;
      bw.Write((Int32)QBox.BE32(_SampleCTSHigh)); count += 4;
      if (mBoxFlags.version == 1 || mBoxFlags.version == 2)
        bw.Write((Int32)QBox.BE32(_SampleCTSLow)); count += 4;

      if (mBoxFlags.version == 2) {
        bw.Write((uint)QBox.BE32(mBoxContinuityCounter)); count += 4;
        bw.Write((uint)QBox.BE32(mFrameCounter)); count += 4;
        bw.Write((uint)QBox.BE32(mSampleDuration)); count += 4;
        bw.Write((ulong)QBox.BE64(mStreamDuration)); count += 8;
				bw.Write((uint)QBox.BE32(mOffsetFromLastSyncPoint)); count += 4;
				bw.Write((uint)QBox.BE32(mOffsetFromLastFrame)); count += 4;
        bw.Write((ulong)QBox.BE64(mReserved2)); count += 8;
        bw.Write((ulong)QBox.BE64(mReserved3)); count += 8;
        bw.Write((ulong)QBox.BE64(mReserved4)); count += 8;
      }

      mSample.Write(bw, mSampleFlags, mSampleStreamType, data);
      return (count);
    }

    #region Byte Swap Routines

    // reverse byte order (8-bit)
    public static UInt16 BE8(UInt16 value) {
      return (value);
    }

    // reverse byte order (16-bit)
    public static UInt16 BE16(UInt16 value)
    {
      if (BitConverter.IsLittleEndian == false) return (value);
      return (UInt16)(((value & 0xFFU) << 8) | ((value & 0xFF00U) >> 8));
    }

    // reverse byte order (24-bit)
    public static ulong BE24(ulong value)
    {
      if (BitConverter.IsLittleEndian == false) return (value);
      return ((((value) >> 16) & 0xFF) | ((value) & 0xFF00) | (((value) << 16) & 0xFF0000));
    }

    // reverse byte order (32-bit)
    public static ulong BE32(ulong value)
    {
      if (BitConverter.IsLittleEndian == false) return (value);
      return ((value & 0x000000FFU) << 24) | ((value & 0x0000FF00U) << 8) |
             ((value & 0x00FF0000U) >> 8) | ((value & 0xFF000000U) >> 24);
    }

    // reverse byte order (64-bit)
    public static UInt64 BE64(UInt64 value)
    {
      if (BitConverter.IsLittleEndian == false) return (value);
      return ((value & 0x00000000000000FFUL) << 56) | ((value & 0x000000000000FF00UL) << 40) |
             ((value & 0x0000000000FF0000UL) << 24) | ((value & 0x00000000FF000000UL) << 8) |
             ((value & 0x000000FF00000000UL) >> 8) | ((value & 0x0000FF0000000000UL) >> 24) |
             ((value & 0x00FF000000000000UL) >> 40) | ((value & 0xFF00000000000000UL) >> 56);
    }

    #endregion

    #region String Handling

    public static int str2int(string s)
    {
      return (s[0] << 24) | (s[1] << 16) | (s[2] << 8) | s[3];
    }

    public static string int2str(ulong i)
    {
      string s = "";
      s += (char)((i >> 24) & 0xFF);
      s += (char)((i >> 16) & 0xFF);
      s += (char)((i >> 8) & 0xFF);
      s += (char)(i & 0xFF);
      return (s);
    }


    public string FlagsString() {  // ROBA: added for debugging
	    string flags = "";
      ulong f = mBoxFlags.flags;
			flags += ((f & QBOX_FLAGS_SAMPLE_DATA_PRESENT) > 0) ? 'D' : '_';
			flags += ((f & QBOX_FLAGS_LAST_SAMPLE) > 0) ? 'L' : '_';
			flags += ((f & QBOX_FLAGS_PADDING_4) > 0) ? '4' : '_';
			flags += ((f & QBOX_FLAGS_AUDIO_ONLY) > 0) ? 'A' : '_';
			flags += ((f & QBOX_FLAGS_VIDEO_ONLY) > 0) ? 'V' : '_';
			flags += ((f & QBOX_FLAGS_STUFFING_PACKET) > 0) ? '0' : '_';
			flags += ((f & QBOX_FLAGS_CONTINUITY_COUNTER) > 0) ? 'C' : '_';
			return flags;
    }

    public string SampleStreamTypeString() {   // ROBA: added for debugging
	    switch ((uint)mSampleStreamType) {
		    case QBOX_SAMPLE_TYPE_AAC: return "AAC";
		    case QBOX_SAMPLE_TYPE_H264: return "H264";
		    case QBOX_SAMPLE_TYPE_QPCM: return "PCM";
		    case QBOX_SAMPLE_TYPE_DEBUG: return "DEBUG";
		    case QBOX_SAMPLE_TYPE_H264_SLICE: return "H264_SLICE";
		    case QBOX_SAMPLE_TYPE_QMA: return "MP2A";
		    case QBOX_SAMPLE_TYPE_VIN_STATS_GLOBAL: return "VIN_STATS_GLOBAL";
		    case QBOX_SAMPLE_TYPE_VIN_STATS_MB: return "VIN_STATS_MB";
		    case QBOX_SAMPLE_TYPE_Q711: return "Q711";
		    case QBOX_SAMPLE_TYPE_Q722: return "Q722";
		    case QBOX_SAMPLE_TYPE_Q726: return "Q726";
		    case QBOX_SAMPLE_TYPE_Q728: return "Q728";
		    case QBOX_SAMPLE_TYPE_JPEG: return "JPEG";
		    case QBOX_SAMPLE_TYPE_MPEG2_ELEMENTARY: return "MPEG2_ELEMENTARY";
		    case QBOX_SAMPLE_TYPE_USER_METADATA: return "USER_METADATA";
		    default: return "(INVALID)";
	    }
    }

    public string SampleFlagsString() {  // ROBA: added for debugging
	    string flags = "";
      ulong f = mSampleFlags;
	    if ((f & QBOX_SAMPLE_FLAGS_CONFIGURATION_INFO) > 0) flags += "Config|"; // 0x01 configuration info. sample_data contain configuration info.
	    if ((f & QBOX_SAMPLE_FLAGS_CTS_PRESENT) > 0) flags += "CTSPres|"; // 0x02 cts present. 90 kHz cts present.
	    if ((f & QBOX_SAMPLE_FLAGS_SYNC_POINT) > 0) flags += "SyncI|"; // 0x04 sync point. ex. I frame.
	    if ((f & QBOX_SAMPLE_FLAGS_DISPOSABLE) > 0) flags += "DisposB|"; // 0x08 disposable. ex. B frame.
	    if ((f & QBOX_SAMPLE_FLAGS_MUTE) > 0) flags += "MuteBlack|"; // 0x10 mute. Sample is mute/black.
	    if ((f & QBOX_SAMPLE_FLAGS_BASE_CTS_INCREMENT) > 0) flags += "CTSBaseInc|"; // 0x20 cts base increment. By 2^32.
	    if ((f & QBOX_SAMPLE_FLAGS_META_INFO) > 0) flags += "Meta|"; // 0x40 QBoxMeta present before configuration info or sample data.
	    if ((f & QBOX_SAMPLE_FLAGS_END_OF_SEQUENCE) > 0) flags += "EOSeq|"; // 0x80 sample contain end of sequence NALU.
	    if ((f & QBOX_SAMPLE_FLAGS_END_OF_STREAM) > 0) flags += "EOStr|"; // 0x100 sample contain end of stream NALU.
	    if ((f & QBOX_SAMPLE_FLAGS_QMED_PRESENT) > 0) flags += "QMED|"; // 0x200 qmed
	    if ((f & QBOX_SAMPLE_FLAGS_PKT_HEADER_LOSS) > 0) flags += "PktHdrLoss|";
	    if ((f & QBOX_SAMPLE_FLAGS_PKT_LOSS) > 0) flags += "PktLoss|";
	    if ((f & QBOX_SAMPLE_FLAGS_120HZ_CLOCK) > 0) flags += "120Hz|"; // same as QBOX_SAMPLE_FLAGS_TS
      if ((f & QBOX_SAMPLE_FLAGS_TS_FRAME_START) > 0) flags += "TSFrame|";
			if ((f & QBOX_SAMPLE_FLAGS_META_DATA) > 0) flags += "MetaData|";
//			if ((f & QBOX_SAMPLE_FLAGS_DURATION_PRESENT) > 0) flags += "StrDur|";
//			if ((f & QBOX_SAMPLE_FLAGS_START_OF_CUT_POINT) > 0) flags += "CutIn|";
			if ((f & QBOX_SAMPLE_FLAGS_STOP_POINT) > 0) flags += "Stop|";
			//	    if ((flags[0]) flags[strlen(flags)-1] = 0;
	    return flags;
    }

		public void Dump() {
			string type = "";
			type = int2str(mBoxType);

			if (mSample != null && mSample.mPayload != null) {
				ulong qmedsize = 0;
				if (mSample.qmed != null) qmedsize = mSample.qmed.boxSize;
				Console.WriteLine("Box:    Size {0}, Header {1}, QMed {2}",
				                  mBoxSize.ToString(),
				                  mHeaderSize.ToString(),
				                  qmedsize);
			} else {
				Console.WriteLine("Box:    Size {0}, Header Size {1}, Payload {2}",
				                  mBoxSize.ToString(),
				                  mHeaderSize.ToString(),
				                  mSample.size);
			}

			Console.Write("        Type {0}, v{1}, Flags 0x{2} ({3})",
			              type,
			              mBoxFlags.version.ToString(),
			              mBoxFlags.flags.ToString("x4"),
			              FlagsString().ToString()
				);

			if (mBoxFlags.version == 2 && ContinuityCounterValid) {
				Console.WriteLine(", Continuity {0}", mBoxContinuityCounter);
			} else {
				Console.WriteLine();
			}

			Console.Write("Sample: Type 0x{0} ({1}), Stream {2}",
			              mSampleStreamType.ToString("x2"),
			              SampleStreamTypeString(),
			              mSampleStreamId);
			if ((mSampleFlags & QBOX_SAMPLE_FLAGS_CTS_PRESENT) > 0) {
				Console.Write(", CTS {0} ({1})", mSampleCTS, "?");
			} else {
				Console.Write(", no CTS {0}", mSampleCTS);
			}

			if ((mSampleFlags & QBOX_FLAGS_SAMPLE_DATA_PRESENT) > 0) {
				Console.WriteLine(", Size {0}", mSample.size);
			} else {
				Console.WriteLine(" -indexed: at x, Size {0}", mSample.size);
			}

			Console.WriteLine("        Flags {0} ({1}), Padding {2}",
                    ((uint)mSampleFlags).ToString("x4"),
										SampleFlagsString(),
										QBOX_SAMPLE_PADDING(mSampleFlags));

			if (mBoxFlags.version == 2) {
				if (mFrameCounter > 0) 
					Console.Write("        Frame {0}", mFrameCounter);
				else
					Console.Write("        Frame [not set]");

				if (mSampleDuration > 0)
					Console.Write(", Duration {0}", mSampleDuration);
				else
					Console.Write(", Duration [not set]");

				if (mStreamDuration > 0)
					Console.WriteLine(", Stream Duration {0}", mStreamDuration);
				else
					Console.WriteLine(", Stream Duration [not set]");

				if (mOffsetFromLastSyncPoint > 0 && mOffsetFromLastSyncPoint != 0x11121314) {
					Console.Write("        Backlink: sync point {0}", mOffsetFromLastSyncPoint);
				} else {
					Console.Write("        Backlink: sync point [not set]");
				}
				if (mOffsetFromLastFrame > 0 && mOffsetFromLastFrame != 0x15161718) {
					Console.Write(", frame {0}", mOffsetFromLastFrame);
				} else {
					Console.Write(", frame [not set]");
				}
				Console.WriteLine();



			}

			/*        Console.WriteLine("Box: Size {0}, Header Size {1}, Type {2}, Flags 0x{3}(v{4}, {5})\n"+
										"Sample: Type 0x{6} ({7}), Id 0x{8}, CTS {9}-{10}, Addr 0x{11}, Size {12}\n"+
										"        Flags 0x{13} ({14}), Padding {15}\n", 
										mBoxSize.ToString(), 
										mHeaderSize.ToString(), 
										type, 
										mBoxFlags.flags.ToString("x4"), 
										mBoxFlags.version.ToString(), 

										FlagsString().ToString(), 
										mSampleStreamType.ToString("x2"), 
										SampleStreamTypeString().ToString(), 
										mSampleStreamId.ToString("x2"), 
										((uint)(mSampleCTS >> 32)).ToString(), 
										((uint)(mSampleCTS & 0xFFFFFFFF)).ToString(), 
										((uint)mSample.addr).ToString("x4"), 
										mSampleSize.ToString(),
										((uint)mSampleFlags).ToString("x4"), 
										SampleFlagsString(),
										QBOX_SAMPLE_PADDING(mSampleFlags)
						); */
		}

  	// DumpHeader won't work if seek is not supported
    //public void DumpHeader(BinaryReader br) {
    //  byte[] headerBytes = new byte[mHeaderSize];
    //  br.BaseStream.Position = mHeaderPosition;
    //  headerBytes = br.ReadBytes(mHeaderSize);
    //  br.BaseStream.Position = mCurrentPosition;
    //  hexdump(headerBytes, mHeaderSize, 256);
    //}

    public void QMedBaseDump() {
      
    }

    public void QMedH264Dump() {
      
    }

    public void QMedAACDump() {
      
    }


    static void hexdump(byte[] buffer, int size, int maxsize) {
      int i;
      int j;
      byte c;
      for (i = 0; i < size; i++) {
        if ((i & 0xF) == 0x0) Console.Write(i.ToString().PadLeft(4, '0') + "   ");
        Console.Write(buffer[i].ToString("x2") + " ");
        if ((i & 0x3) == 0x3) Console.Write(" ");

        if ((i & 0xF) == 0xF || i + 1 >= size || (maxsize > 0 && i + 1 > maxsize)) {
          for (j = i + 1; (j & 0xF) != 0x0; j++) {
            Console.Write("   ");
            if ((j & 0x3) == 0x3) Console.Write(" ");
          }
          Console.Write(" ");
          for (j = i & ~0xF; j <= i; j++) {
            c = buffer[j];
            byte o = (32 <= c && c <= 127) ? c : (byte)'.';
            Console.Write((char)o);
            //            fprintf(stdout, "%c", 32 <= c && c <= 127 ? c : '.');
          }
          Console.WriteLine();
        }
        if (maxsize > 0 && i + 1 >= maxsize) break;
      }
    }

    #endregion


    public static int GetChannelCount(QBoxSample audioMetaSample)
    {
      if (audioMetaSample == null)
        return 0;

      if (audioMetaSample.a != null)
        return (int)audioMetaSample.a.channels;

      if (audioMetaSample.qmed != null)
      {
        QMed.QMedAAC aacQ = (QMed.QMedAAC)audioMetaSample.qmed; // FIXME: for now we assume it's a QMedAAC
        return (int)aacQ.channels;  
      }

      return 0;
    }


    public static int GetSampleSize(QBoxSample audioMetaSample)
    {
      if (audioMetaSample == null)
        return 0;

      if (audioMetaSample.a != null)
        return (int)audioMetaSample.a.samplesize;

      if (audioMetaSample.qmed != null)
      {
        QMed.QMedAAC aacQ = (QMed.QMedAAC)audioMetaSample.qmed; // FIXME: for now we assume it's a QMedAAC
        return (int)aacQ.sampleSize;
      }

      return 0;
    }

    public static int GetSampleRate(QBoxSample audioMetaSample)
    {
      if (audioMetaSample == null) return 0;
			
      if (audioMetaSample.a != null)
        return (int)audioMetaSample.a.samplerate;
			
      if (audioMetaSample.qmed != null)
      {
        QMed.QMedAAC aacQ = (QMed.QMedAAC)audioMetaSample.qmed; // FIXME: for now we assume it's a QMedAAC
        return (int)aacQ.samplingFrequency;
      }

      return 0;
    }

    public static byte[] GetAudioSpecificConfig(QBoxSample audioMetaSample)
    {
      if (audioMetaSample == null)
        return null;

      if (audioMetaSample.qmed != null)
      {
        QMed.QMedAAC aacQ = (QMed.QMedAAC)audioMetaSample.qmed; // FIXME: for now we assume it's a QMedAAC
        return aacQ.audioSpecificConfig;
      }

      return null;
    }

    #region extra method for playing qboxes directly in MediaElement

    public byte[] GetQBoxAudioPayload() {
      return (this.mSample.mPayload);
    }

    public byte[] GetQBoxH264Nalu()
    {
      Stream rawPayload = new MemoryStream(this.mSample.mPayload);
      BinaryReader br = new BinaryReader(rawPayload);

      // first, determine actual length of NALU (without trailing bytes)
      int totalSize = this.mSample.mPayload.Length;
      int strippedCount = 0;
      while (totalSize > 4)
      {
        ulong naluLen = QBox.BE32(br.ReadUInt32());
        if (naluLen > 0UL)
          rawPayload.Position += (long)naluLen; // don't read yet, just advance

        int totalNaluLen = (int)naluLen + 4;
        totalSize -= totalNaluLen;
        strippedCount += totalNaluLen;
      }

      // use actual length to declare outut array of bytes
      byte[] outBytes = new byte[strippedCount];

      // reset Position of memory stream
      rawPayload.Position = 0;

      // get rid of trailing bytes, if any
      // at the same time, convert to bit stream
      totalSize = this.mSample.mPayload.Length;
      int offset = 0;
      int naluCount = 0;
      while (totalSize > 4)
      {
        ulong naluLen = QBox.BE32(br.ReadUInt32());
        totalSize -= 4;
        if (naluLen > 0UL)
        {
          int readLen = (int)naluLen;
          outBytes[offset + 3] = (byte)1; // assume that outBytes[offset] to outBytes[offset + 2] are zero.
          offset += 4;
          rawPayload.Read(outBytes, offset, readLen);
          offset += readLen;
          totalSize -= readLen;
        }
        else naluLen = 0; // debugging break point
        naluCount++;
      } // end of while

      //// make sure there's no other 0001 sequences in the resulting bit stream
      //// note that this sequence can start at any byte index (that's why we have to parse the bit stream)
      //int nCnt = 0;
      //int state = 0;
      //br = new BinaryReader(new MemoryStream(outBytes));
      //while (br.BaseStream.Length > br.BaseStream.Position)
      //{
      //  byte b = br.ReadByte();
      //  switch (b)
      //  {
      //    case 0:
      //      if (b == 0)
      //        state++;
      //      break;
      //    case 1:
      //      if (b == 0)
      //        state++;
      //      else
      //        state = 0;
      //      break;
      //    case 2:
      //      if (b == 0)
      //        state++;
      //      //else if (b == 1)
      //      //  state = 4;
      //      else
      //        state = 0;
      //      break;
      //    case 3:
      //      if (b == 1)
      //        state++; // state 4
      //      else if (b != 0) // if zero, stay at state 3
      //        state = 0;
      //      break;
      //    case 4:
      //      nCnt++;
      //      state = 0;
      //      break;
      //    default:
      //      break;
      //  } // end of switch
      //} // end of while

      //if (nCnt != naluCount)
      //  throw new Exception("Extra bit stream delimiters, need to neutrlize extra 001 sequences");

      return outBytes;
    }

    #endregion

  }
}
