using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Media.Formats.MP4;
using Media.Formats.Generic;

// kludge : we use only WaveFormatEx in MP4Handler

namespace Media.Formats.QBOX {
  public class QBoxTrackFormat : GenericTrackFormat {
    private List<QBox> _qBoxes;
    private QBox firstQB;
    private QBox.QBoxSample audioMetaSample;
    private QBox.QBoxSample videoMetaSample;
    private ulong _prevTimeStamp = 0L;
    private MediaTimeUtils _mediaTime;

    /// <summary>
    /// Default constructor.
    /// For writing to a QBox file.
    /// </summary>
    public QBoxTrackFormat() {
      firstQB = null;
      audioMetaSample = null;
      videoMetaSample = null;
      DurationIn100NanoSecs = 0;
    }

    /// <summary>
    /// Constructor accepting a list of qboxes as input.
    /// (For reading a QBox file.)
    /// FIXME: we need to pick up the rest of the tracks (other than the first one)
    /// </summary>
    /// <param name="qboxes"></param>
    public QBoxTrackFormat(List<QBox> qboxes, ushort trackID, MediaTimeUtils mediaTime)
      : this() {
      _qBoxes = new List<QBox>();
      qboxes.ForEach(delegate(QBox q) { if (q.mSampleStreamId == trackID) _qBoxes.Add(q); });
      if (_qBoxes.Count == 0)
        throw new Exception(string.Format("There is no track with ID = {0}", trackID));

      _mediaTime = mediaTime;

      HasIFrameBoxes = _qBoxes.Any(box => (((uint) box.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_SYNC_POINT) != 0));

      firstQB = _qBoxes[0];
      if (firstQB.mSampleStreamType == QBox.QBOX_SAMPLE_TYPE_H264) {
        Codec = new Codec(CodecTypes.Video);
        firstQB = _qBoxes.First(q => ((q.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_CONFIGURATION_INFO) != 0u));
        if (firstQB.mSample.v != null) {
          this.videoMetaSample = firstQB.mSample;
          seqParamSetData = firstQB.mSample.v.sps;
          picParamSetData = firstQB.mSample.v.pps;
          Codec.PrivateCodecData = this.VideoCodecPrivateData;
        }
        else
          Codec.PrivateCodecData = ToHexString(firstQB.mSample.privateCodecData);
      }
      else if (firstQB.mSampleStreamType == QBox.QBOX_SAMPLE_TYPE_AAC) {
        Codec = new Codec(CodecTypes.Audio);
        firstQB =
          _qBoxes.First(q => ((q.mSample.a != null) && ((q.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_META_INFO) != 0u)) ||
                             ((q.mSample.qmed != null) && ((q.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_QMED_PRESENT) != 0u)));
        this.audioMetaSample = firstQB.mSample;

        if (audioMetaSample.privateCodecData != null)
          Codec.PrivateCodecData = ToHexString(audioMetaSample.privateCodecData);
        else {
#if USE_WAVEFORMATEX
          GetAudioPrivateCodecDataFromWaveFormatEx();
#else
          GetAudioPrivateCodecDataAdHoc();
#endif
        }
      }
      else if (firstQB.mSampleStreamType == QBox.QBOX_SAMPLE_TYPE_JPEG) {
        Codec = new Codec(CodecTypes.Video);
        if (firstQB.mSample.privateCodecData != null)
          Codec.PrivateCodecData = ToHexString(firstQB.mSample.privateCodecData);
      }
      else
        throw new Exception(string.Format("QBox sample type not implemented: {0}", firstQB.mSampleStreamType));
    }


    /// <summary>
    /// QBoxTrackFormat
    /// Constructor to use when writing out to a stream.
    /// </summary>
    /// <param name="trackInfo"></param>
    public QBoxTrackFormat(IsochronousTrackInfo trackInfo)
      : this() {
      _qBoxes = new List<QBox>();
      firstQB = new QBox(trackInfo);
      CodecTypes codecType = (trackInfo.HandlerType == "Audio")
                               ? CodecTypes.Audio
                               : (trackInfo.HandlerType == "Video") ? CodecTypes.Video : CodecTypes.Unknown;
      Codec = new Codec(codecType);
      Codec.PrivateCodecData = trackInfo.CodecPrivateData;
      DurationIn100NanoSecs = trackInfo.DurationIn100NanoSecs;
    }

    // properties
    public override string PayloadType {
      get { return firstQB.SampleStreamTypeString(); }
    }

    public override Codec Codec
    {
      get;
      set;
    }

    public override bool HasIFrameBoxes { get; protected set; }

    public override uint TimeScale
    {
      get { return _mediaTime.ClockTimeScale; }
      set { throw new Exception("QBoxTrackFormat: can't change qbox time scale"); }
    }

    private ulong _duration; // jus so we can set breakpoints here
    public override ulong DurationIn100NanoSecs
    {
      get { return _duration; }
      set { _duration = value; }
    }

    public override uint TrackID {
      get { return firstQB.mSampleStreamId; }
    }

    // audio
    public override int ChannelCount {
      get { return QBox.GetChannelCount(audioMetaSample); }
    }

    public override int SampleSize {
      get { return QBox.GetSampleSize(audioMetaSample); }
    }

    public override int SampleRate {
      get { return QBox.GetSampleRate(audioMetaSample); }
    }

    // video
    public override Size FrameSize {
      get {
        Size size = new Size();
        if (videoMetaSample != null) {
          size.Height = (int) videoMetaSample.v.height;
          size.Width = (int) videoMetaSample.v.width;
        }
        else if (firstQB != null) {
          if (firstQB.mSampleStreamType == QBox.QBOX_SAMPLE_TYPE_H264) {
            size.Height = (int)firstQB.mSample.v.height;
            size.Width = (int)firstQB.mSample.v.width;
            //QMed.QMedH264 h264 = (QMed.QMedH264) firstQB.mSample.qmed;
            //size.Height = (int) h264.height;
            //size.Width = (int) h264.width;
          }
          else if (firstQB.mSampleStreamType == QBox.QBOX_SAMPLE_TYPE_JPEG) {
            QMed.QMedJpeg jpeg = (QMed.QMedJpeg) firstQB.mSample.qmed;
            size.Height = (int) jpeg.height;
            size.Width = (int) jpeg.width;
          }
        }
        return size;
      }
    }

    // Sequence Param Set (SPS)
    private byte[] seqParamSetData; // Sequence Parameter Set data

    // Picture Parameter Set (PPS)
    private byte[] picParamSetData; // Picture Parameter Set data

    /// <summary>
    /// VideoCodecPrivateData returns both SPS and PPS in byte stream format that the Media Element expects.
    /// </summary>
    private string VideoCodecPrivateData {
      get {
        if (((seqParamSetData == null) && (picParamSetData == null)) ||
            ((seqParamSetData.Length == 0) && (picParamSetData.Length == 0)))
          return "";
        StringBuilder sb = new StringBuilder(seqParamSetData.Length*2 + picParamSetData.Length*2 + 8);
        sb.Append("00000001");

        for (int i = 0; i < seqParamSetData.Length; i++) {
          sb.Append(string.Format("{0:X2}", seqParamSetData[i]));
        }

        sb.Append("00000001");
        for (int i = 0; i < picParamSetData.Length; i++) {
          sb.Append(string.Format("{0:X2}", picParamSetData[i]));
        }
        return sb.ToString();
      }
    }

    public override int SampleAvailable(int index) {
      if ((_qBoxes.Count > 0) && _qBoxes.Any(q => (q.mFrameCounter - 1) == (ulong)index))
        return (int)_qBoxes[0].mFrameCounter - 1;
      return -1;
    }

    public override int ResetTrack(ulong time) {
      _qBoxes.Clear();
      return 0; // return value unused by caller
    }

    public void AddMore(List<QBox> qboxes) {
      List<QBox> list = qboxes;
      if (qboxes[0].mSampleStreamType == QBox.QBOX_SAMPLE_TYPE_H264) {
        int count = 0;
        list = new List<QBox>();
        qboxes.ForEach(delegate(QBox q) {
                         if (q.mSampleStreamId == TrackID) {
                           list.Add(q);
                           count++;
                         }
                       });
        if (count == 0)
          throw new Exception(string.Format("There is no track with ID = {0}", TrackID));
      }

      _qBoxes.AddRange(list);
    }

    private void GetAudioPrivateCodecData() {
      WaveFormatEx waveFormat;
      waveFormat = new WaveFormatEx();
      waveFormat.BitsPerSample = (short) this.SampleSize;
      waveFormat.AvgBytesPerSec = (this.SampleSize/8)*this.SampleRate*this.ChannelCount;
      waveFormat.Channels = (short) this.ChannelCount;
      waveFormat.FormatTag = WaveFormatEx.FormatMpegHEAAC; // 0xFF; // WaveFormatEx.FormatPCM; // Raw_AAC
      waveFormat.SamplesPerSec = (int) this.SampleRate;
      waveFormat.BlockAlign = 1; // (short)(waveFormat.Channels * (waveFormat.BitsPerSample / 8));
      byte[] config = QBox.GetAudioSpecificConfig(this.audioMetaSample);
      waveFormat.ext = new byte[12 + config.Length];
      for (int i = 0; i < 12; i++)
        waveFormat.ext[i] = 0;
      //waveFormat.ext[0] = 3; // payload type
      waveFormat.Size = (short) waveFormat.ext.Length;
      for (int i = 12, j = 0; i < waveFormat.Size; i++, j++)
        waveFormat.ext[i] = config[j];
      waveFormat.ValidateWaveFormat();
      Codec.PrivateCodecData = waveFormat.ToHexString();
    }

    private void GetAudioPrivateCodecDataFromWaveFormatEx() {
      WaveFormatEx waveFormat;
      waveFormat = new WaveFormatEx();
      waveFormat.BitsPerSample = (short) this.SampleSize;
      waveFormat.AvgBytesPerSec = (this.SampleSize/8)*this.SampleRate*this.ChannelCount;
      waveFormat.Channels = (short) this.ChannelCount;
      waveFormat.FormatTag = 0xFF; // WaveFormatEx.FormatPCM; // Raw_AAC
      waveFormat.SamplesPerSec = (int) this.SampleRate;
      waveFormat.BlockAlign = (short) (waveFormat.Channels*(waveFormat.BitsPerSample/8));
      waveFormat.ext = null; // new byte[12]; // QBox.GetAudioSpecificConfig(this.audioMetaSample);
      waveFormat.Size = 0; // 12; // (short)waveFormat.ext.Length;
      //waveFormat.ext[0] = 1;
      //waveFormat.ext[4] = 0xFE;
      waveFormat.ValidateWaveFormat();
      Codec.PrivateCodecData = waveFormat.ToHexString();
    }

    private string ToHexString(byte[] bArray) {
      string s = "";

      foreach (byte b in bArray) {
        s += string.Format("{0:X2}", b);
      }

      return s;
    }

    private void GetAudioPrivateCodecDataAdHoc() {
      // all of following three values work with Windows Media Player, only first one works with MediaElement when AAC bit stream
      // is written out to an MP4 file.
      Codec.PrivateCodecData = "038080220000000480801640150020000001F4000001F4000580800511900000000680800102";
      //Codec.PrivateCodecData = "0380801D0000000480801640150020000001F4000001F400058080051190000000";
      //Codec.PrivateCodecData = "038080220000000480801640150020000001F4000001F400058080021190";
    }


    // QBoxCompare is not used
    private int QBoxCompare(QBox x, QBox y) {
      if (x == null) {
        if (y == null) {
          // If x is null and y is null, they're
          // equal. 
          return 0;
        }
        else {
          // If x is null and y is not null, y
          // is greater. 
          return -1;
        }
      }
      else {
        // If x is not null...
        //
        if (y == null)
          // ...and y is null, x is greater.
        {
          return 1;
        }
        else {
          // ...and y is not null, compare the 
          // time stamps of the two QBoxes.
          //
          return x.mSampleCTS.CompareTo(y.mSampleCTS);
        }
      }
    }

    /// <summary>
    /// NearEnd
    /// For every IFrame Qbox, check whether this is the last IFrame before the end of this run.
    /// If it is, don't include this IFrame in the current run; it will be the first QBox in the NEXT run.
    /// NOTE: This is not used anywhere.
    /// </summary>
    /// <param name="boxCount"></param>
    /// <param name="inEndSampleTime"></param>
    /// <param name="lastEnd"></param>
    /// <returns></returns>
    private bool NearEnd(int boxCount, UInt64 inEndSampleTime, ulong lastEnd, float scaleFactor) {
      if (inEndSampleTime < lastEnd)
        return true;

      if (boxCount >= _qBoxes.Count) // it is not near the end, it's AT the end
        return false;

      int index = boxCount + 1;
      ulong blockTime = lastEnd;
      QBox box = _qBoxes[index];
      while (((uint) box.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_SYNC_POINT) == 0) {
        string streamType = box.SampleStreamTypeString();
        if (streamType == "H264")
          blockTime += (ulong) (scaleFactor*box.mSampleDuration);
        else if (streamType == "AAC")
          blockTime = (ulong) (scaleFactor*box.mSampleCTS);
        else throw new Exception(string.Format("Unsupported qbox stream type: {0}", streamType));

        if (inEndSampleTime < blockTime)
          return true;
        index++;
        if (index == _qBoxes.Count)
          return false;
        box = _qBoxes[index];
      }
      return false;
    }


    /// <summary>
    /// PrepareSampleReading
    /// In MP4, reading of box headers is separate from reading of the H264 and audio bits. This is because the bits are stored
    /// in a different place in the file (or may in fact be in a separate file). In a QBox file, however, both headers and bits 
    /// are stored in the qbox. It makes no sense to separate the two. Therefore, in this implementation of PrepareSampleReading,
    /// we actually read the bits together with the headers. The routine WriteSamples doesn't do much.
    /// 
    /// There are two signatures for this method: one that accepts qbox indices (this one), and another that accepts ulong start
    /// and end times.
    /// 
    /// We don't keep the qboxes. QBoxes already processed are disposed of as a last step. If we run out of qboxes, we read-in
    /// more.
    /// </summary>
    /// <param name="inStartSampleIndex">int index to first qbox to be processed</param>
    /// <param name="inEndSampleIndex">int index to last qbox to be processed</param>
    /// <param name="dummy">not used</param>
    /// <returns></returns>
    public override List<StreamDataBlockInfo> PrepareSampleReading(int inStartSampleIndex, int inEndSampleIndex,
                                                                   ref ulong dummy) {
      List<StreamDataBlockInfo> retList = new List<StreamDataBlockInfo>();

      if (_qBoxes.Count == 0)
        return retList;


      float scaleFactor = TimeSpan.FromSeconds(1.0).Ticks/this.TimeScale;
      bool foundFirstSlice = false;
      int boxCount = 0;

      // we traverse the _qBoxes list from the beginning;
      // can't use foreach because _qBoxes can change;
      // box.mIndex is NOT the same as index i.
      // we use a for loop only because we are adding qboxes to _qBoxes as part of the loop
      for (int i = 0; i < _qBoxes.Count; i++) {
        QBox box = _qBoxes[i];
        boxCount++;

        // reject qboxes with sample size zero (no data)
        if (box.mSampleSize == 0) {
          continue;
        }

        // we shouldn't be searching for the first box of interest, because it should always be the first one
        // it should always be the first one because we threw away all boxes already processed
        if (((ulong)inStartSampleIndex > (box.mFrameCounter - 1)) ||
            ((!foundFirstSlice) && (((uint) box.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_SYNC_POINT) == 0))) {
          continue; // skip
        }
        else if ((ulong)inStartSampleIndex == (box.mFrameCounter - 1))
        {
          foundFirstSlice = true;
        }
        else if (!foundFirstSlice) {
          _qBoxes.Clear();
          base.GetNextBatch(0, inStartSampleIndex); // throw new Exception("First IFrame not found");
          i = -1; // this gets incremented to zero
          boxCount = 0; // start all over
          continue;
        }

        StreamDataBlockInfo datBlock = new Slice();

        switch (box.SampleStreamTypeString()) {
          case "AAC":
            datBlock = new ADTSDataBlockInfo();
            datBlock.SliceType = SliceType.AAC;
            break;
          case "Q711":
          case "PCM":
            datBlock.SliceType = SliceType.Unknown; // FIXME: add sample type for PCM
            break;
          case "MP2A":
            datBlock.SliceType = SliceType.MP4A;
            break;
          case "Q722": // ADPCM
          case "Q726":
          case "Q728":
            datBlock.SliceType = SliceType.Unknown; // FIXME: add sample type for ADPCM
            break;
          case "H264":
          case "H264_SLICE":
            datBlock = new NaluDelimiterBlockInfo();
            if (((uint) box.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_SYNC_POINT) == 0)
              datBlock.SliceType = SliceType.DFrame;
            else {
              datBlock.SliceType = SliceType.IFrame;
            }
            if ((box.mSample != null) && (box.mSample.v != null)) {
              NaluDelimiterBlockInfo blockInfo = datBlock as NaluDelimiterBlockInfo;
              blockInfo.AccessUnitDelimiter = box.mSample.v.aud;
            }
            break;
          case "JPEG":
            datBlock.SliceType = SliceType.JPEG;
            break;
          case "MPEG2_ELEMENTARY":
            datBlock.SliceType = SliceType.Unknown; // FIXME: add sample type for MPEG2
            break;
          case "VIN_STATS_GLOBAL":
          case "VIN_STATS_MB":
          case "USER_METADATA":
          case "DEBUG":
          default:
            System.Diagnostics.Debug.WriteLine("Unknown QBox: {0}", box.SampleStreamTypeString());
            break;
        }

        datBlock.CTS = (ulong)((box.mSampleCTS - (box.mStreamDuration - box.mSampleDuration)) * scaleFactor);
        datBlock.SliceDuration = (uint)(scaleFactor * box.mSampleDuration);
				if (box.mFrameCounter == 0 && box.mStreamDuration == 0) {
					datBlock.TimeStampNew = 0;
				} else if (box.mStreamDuration == 0) {
					datBlock.TimeStampNew = null;
				} else {
					datBlock.TimeStampNew = (ulong) (scaleFactor*(box.mStreamDuration - box.mSampleDuration));
				}
      	datBlock.SliceSize = box.mSampleSize;
        datBlock.index = (int)box.mFrameCounter - 1; // boxCount;

        // NOTE! For qbox, StreamOffset has a different meaning than in MP4.
        // Here, StreamOffset is the offset to the qbox itself; whereas in
        // MP4, StreamOffset is the offset to the H264 payload.
        // In GenericMediaTrack.GetSample, StreamOffset is used as in MP4, but
        // this method is overriden by another in QBoxVideoTrack that does not use StreamOffset.
        // For flashback to work for both MP4 and qbox files, the caching mechanism
        // is different in MP4 from than in qbox.
        datBlock.StreamOffset = (ulong) box.mHeaderPosition; // needed for flashback to work

        // set payload
        Slice slice = datBlock as Slice;
        slice.SliceBytes = box.mSample.mPayload;

#if ADTS
        if (box.mSampleStreamType == QBox.QBOX_SAMPLE_TYPE_AAC)
        {
          QMed.QMedAAC qmedaac = (QMed.QMedAAC)box.mSample.qmed;
#if PES
          datBlock.PESandADTSHeaders = new byte[qmedaac.pesHeader.Length + qmedaac.adtsHeader.Length];
          qmedaac.pesHeader.CopyTo(datBlock.PESandADTSHeaders, 0);
          qmedaac.adtsHeader.CopyTo(datBlock.PESandADTSHeaders, qmedaac.pesHeader.Length);
#else
          datBlock.PESandADTSHeaders = new byte[qmedaac.adtsHeader.Length];
          qmedaac.adtsHeader.CopyTo(datBlock.PESandADTSHeaders, 0);
#endif
          datBlock.SampleSize += datBlock.PESandADTSHeaders.Length;
        }
#endif
        if (datBlock.SliceDuration == 0) {
          datBlock.SliceDuration = (uint) (scaleFactor*box.mSampleDuration); // any non-zero duration is better
        }

        if ((((uint) box.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_SYNC_POINT) != 0) && ((box.mFrameCounter - 1) >= (ulong)inEndSampleIndex)) {
          boxCount--;
          break; // don't put last IFrame box in return list
        }

        retList.Add(datBlock);

        if (box == _qBoxes.Last()) {
          base.GetNextBatch(GenericMediaStream.MAX_BOXES_TO_READ, 0);
            // callee should set end FIXME: is box.mCurrentPosition being set?
        }

      } // end of for loop

      _qBoxes.RemoveRange(0, boxCount);

      return retList;
    }


    /// <summary>
    /// PrepareSampleReading
    /// There are two signatures for this method: one that accepts qbox indices (see above), and another that accepts ulong start
    /// and end times (this one).
    /// 
    /// If we run out of qboxes, we read-in more.
    /// </summary>
    /// <param name="inStartSampleTime">QBoxes with time stamps equal to or more than this are included in the output list</param>
    /// <param name="inEndSampleTime">QBoxes with time stamps equal to or less than this are included in the output list</param>
    /// <param name="dummy">unused</param>
    /// <returns></returns>
    public override List<StreamDataBlockInfo> PrepareSampleReading(UInt64 inStartSampleTime, UInt64 inEndSampleTime,
                                                                   ref ulong dummy) {
      if (_qBoxes.Count == 0)
        return new List<StreamDataBlockInfo>(); // empty list

      long oneSecTicks = TimeSpan.FromSeconds(1.0).Ticks;
      float scaleFactor = oneSecTicks/this.TimeScale;
      ulong averageSliceDuration = 0UL;

      int boxCount = 0;
      ulong timeStamp = 0UL;

      int startIndex = 0;
      int endIndex = 0;
      bool startSet = false;

      // we traverse the _qBoxes list from the beginning (one of two traversals, because we call the other PrepareSampleReading after this)
      // the purpose of this traversal is just to determine the start and end indices.
      // FIXME: we should optimize the search for the first qbox (we can use binary search if we first convert all mSampleCTS to mean
      // the same thing -- a time stamp) CCT.
      for (int i = 0; i < _qBoxes.Count; i++) {
        QBox box = _qBoxes[i];

        boxCount++;

        // reject qboxes with sample size zero (no data)
        if (box.mSampleSize == 0) {
          boxCount--;
          continue;
        }

        timeStamp = (ulong) (scaleFactor * (box.mStreamDuration - box.mSampleDuration));
        averageSliceDuration += (ulong) (scaleFactor * box.mSampleDuration);

        if (!startSet)
        {
          long diff = ((long)inStartSampleTime - (long)timeStamp) >> 1; // divided by 2

          // the first qbox should be the start because we dispose of qboxes already processed
          if (((uint)box.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_SYNC_POINT) != 0)
          {
            startIndex = (int)box.mFrameCounter - 1;
            startSet = true;
          }

          if (!startSet)
            throw new Exception("Track problem: first box in queue is not sync point");
        }

        if ((((uint) box.mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_SYNC_POINT) != 0) && (inEndSampleTime <= timeStamp))
          // NearEnd(boxCount, inEndSampleTime, timeStamp, scaleFactor))
        {
          endIndex = (int)box.mFrameCounter - 1;
            // do not put this sync box in List; it should instead be the first box in next fragment
          break;
        }

#if ADTS
        if (box.mSampleStreamType == QBox.QBOX_SAMPLE_TYPE_AAC)
        {
          QMed.QMedAAC qmedaac = (QMed.QMedAAC)box.mSample.qmed;
#if PES
          datBlock.PESandADTSHeaders = new byte[qmedaac.pesHeader.Length + qmedaac.adtsHeader.Length];
          qmedaac.pesHeader.CopyTo(datBlock.PESandADTSHeaders, 0);
          qmedaac.adtsHeader.CopyTo(datBlock.PESandADTSHeaders, qmedaac.pesHeader.Length);
#else
          datBlock.PESandADTSHeaders = new byte[qmedaac.adtsHeader.Length];
          qmedaac.adtsHeader.CopyTo(datBlock.PESandADTSHeaders, 0);
#endif
          datBlock.SampleSize += datBlock.PESandADTSHeaders.Length;
        }
#endif

        if (box == _qBoxes.Last()) {
          base.GetNextBatch(GenericMediaStream.MAX_BOXES_TO_READ, 0);
            // callee should set end FIXME: is box.mCurrentPosition being set?
        }

      } // end of for loop

      // we did not find the end, which means we ran out of qboxes to process
      if (endIndex == 0) {
        averageSliceDuration /= (uint) boxCount;
        int desiredBoxCount =
          (int) (((inEndSampleTime - inStartSampleTime) + averageSliceDuration)/averageSliceDuration);
        endIndex = startIndex + desiredBoxCount;
      }

      if (startIndex == endIndex)
        throw new Exception("Traversing QBoxes did not yield any qbox.");

      return PrepareSampleReading(startIndex, endIndex, ref dummy);
    }

    public override void PrepareSampleWriting(List<StreamDataBlockInfo> sampleLocations, ref ulong currMdatOffset) {
      bool firstSlice = true;
      long oneSecTicks = TimeSpan.FromSeconds(1.0).Ticks;
      _qBoxes.Clear(); // discard boxes from previous batch
      foreach (StreamDataBlockInfo blockInfo in sampleLocations) {
        ulong timeStamp = ((ulong) blockInfo.SliceDuration*(ulong) TimeScale);
        timeStamp /= (ulong) oneSecTicks;
        string trackType = (Codec.CodecType == CodecTypes.Audio)
                             ? "Audio"
                             : (Codec.CodecType == CodecTypes.Video) ? "Video" : "Unknown";
        ulong sampleFlags = ((blockInfo.SliceType == SliceType.IFrame) || (trackType == "Audio" && firstSlice))
                              ? QBox.QBOX_SAMPLE_FLAGS_SYNC_POINT
                              : 0;
        QBox qbox = new QBox(blockInfo.SliceSize, 0u, _prevTimeStamp, trackType, sampleFlags);
        _prevTimeStamp += timeStamp;
        _qBoxes.Add(qbox);
        if (blockInfo.SliceSize > 0)
          // for audio, first two qboxes will be marked for sync because very first one is empty
          firstSlice = false;
      }
      base.PrepareSampleWriting(sampleLocations, ref currMdatOffset); // to update duration
    }

    public void WriteFirstQBox(BinaryWriter bw) {
      firstQB.Write(bw, firstQB.mSample.privateCodecData);
    }

    public override void WriteSamples(BinaryWriter bw, IEnumerable<Slice> slices) {
      int errCount = 0;
      if (slices.Count() != _qBoxes.Count)
        throw new Exception("Box to slice count discrepancy in output");
      Slice[] sliceArray = slices.ToArray();
      for (int i = 0; i < _qBoxes.Count; i++) {
        if (_qBoxes[i].mSampleDuration != sliceArray[i].SliceDuration)
          errCount++;
        _qBoxes[i].Write(bw, sliceArray[i].SliceBytes);
      }
    }

  }
}
