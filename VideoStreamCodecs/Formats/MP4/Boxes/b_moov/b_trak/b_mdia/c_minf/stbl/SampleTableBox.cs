/*
aligned(8) class SampleTableBox extends Box(‘stbl’) {
}
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using Media.Formats.Generic;

namespace Media.Formats.MP4
{
  public class SampleTableBox : Box {
    private bool fragmented;
    public MediaInformationBox parent;
    public SampleTableBox(MediaInformationBox inParent) : base(BoxTypes.SampleTable) {
      parent = inParent;
    }

    /// <summary>
    /// Constructor to use when building the box from scratch.
    /// NOTE: We don't compute the Size of this box in this constructor.
    /// The Size of this box is computed during FinalizeBox.
    /// NOTE: The ordering of the sub-boxes is not determined in the constructor.
    /// Writing out the sub-boxes (see the Write method below) determines the order of sub-boxes.
    /// </summary>
    /// <param name="inParent">MediaInformationBox</param>
    /// <param name="trackInfo">IsochronousTrackInfo</param>
    public SampleTableBox(MediaInformationBox inParent, IsochronousTrackInfo trackInfo)
      : this(inParent)
    {
      CTTSOut = trackInfo.CTTSOut;
      fragmented = trackInfo.IsFragment;
      SampleDescriptionsBox = new SampleDescriptionsBox(this, trackInfo);
      DecodingTimeToSampleBox = new DecodingTimeToSampleBox(this);
      SampleToChunkBox = new SampleToChunkBox(this);
      SampleSizeBox = new SampleSizeBox(this);
      ChunkOffSetBox = new ChunkOffSetBox(this);
      if ((trackInfo is RawVideoTrackInfo) && !fragmented)
      {
        SyncSampleMapBox = new SyncSampleMapBox();
        //CompositionTimeToSample = new CompositionTimeToSample(this);
      }
    }


    public DecodingTimeToSampleBox DecodingTimeToSampleBox = null;
    public CompositionTimeToSample CompositionTimeToSample = null;
    public SampleDescriptionsBox SampleDescriptionsBox = null;
    public SampleToChunkBox SampleToChunkBox = null;
    public SampleSizeBox SampleSizeBox = null;
    public ChunkOffSetBox ChunkOffSetBox = null;
    public SyncSampleMapBox SyncSampleMapBox = null;
    public bool CTTSOut = false;


    /// <summary>
    /// Read - read in the SampleTableBox from the input MP4 file.
    /// Sub-boxes can come in in any order.
    /// </summary>
    /// <param name="reader">BoxReader</param>
    public override void Read(BoxReader reader) {
      using (new SizeChecker(this, reader)) {
        base.Read(reader);

        while (reader.BaseStream.Position < (long)(this.Size + this.Offset)) {
          long pos = reader.BaseStream.Position;
          Box test = new Box(BoxTypes.Any);
          test.Read(reader);
          reader.BaseStream.Position = pos;

          if (test.Type == BoxTypes.TimeToSample) {
            this.DecodingTimeToSampleBox = new DecodingTimeToSampleBox(this);
            DecodingTimeToSampleBox.Read(reader);
          }

          else if (test.Type == BoxTypes.CompositionOffset) {
            this.CompositionTimeToSample = new CompositionTimeToSample(this);
            CompositionTimeToSample.Read(reader);
          }

          else if (test.Type == BoxTypes.SampleDescription) {
            this.SampleDescriptionsBox = new SampleDescriptionsBox(this);
            SampleDescriptionsBox.Read(reader);
          }

          else if (test.Type == BoxTypes.SampleToChunk) {
            this.SampleToChunkBox = new SampleToChunkBox(this);
            SampleToChunkBox.Read(reader);
          }

          else if (test.Type == BoxTypes.SampleSize) {
            this.SampleSizeBox = new SampleSizeBox(this);
            SampleSizeBox.Read(reader);
          }

          else if (test.Type == BoxTypes.ChunkOffset) {      // FIXME: this can be a "co64" box
            this.ChunkOffSetBox = new ChunkOffSetBox(this); 
            ChunkOffSetBox.Read(reader);
          }

          else if (test.Type == BoxTypes.SyncSampleMap)
          {
              this.SyncSampleMapBox = new SyncSampleMapBox();
              SyncSampleMapBox.Read(reader);
          }

          else
          {
            reader.BaseStream.Position = (long)(test.Size + test.Offset); // skip unknown box
            Debug.WriteLine(string.Format("Unknown box type {0} in SampleTableBox (stbl), skipped", test.Type.ToString()));
          }
        } // end of while

        if (SampleToChunkBox != null)
          SampleToChunkBox.CheckIntegrityOfChunkData();
      }
    }

    public override string ToString() {
      StringBuilder xml = new StringBuilder();
      xml.Append(base.ToString());
      List<Box> list = new List<Box>();
      if (DecodingTimeToSampleBox != null) list.Add(DecodingTimeToSampleBox); // xml.Append(DecodingTimeToSampleBox.ToString());
      if (CompositionTimeToSample != null && CompositionTimeToSample.EntryCount > 0) list.Add(CompositionTimeToSample); // xml.Append(CompositionTimeToSample.ToString());
      if (SampleDescriptionsBox != null) list.Add(SampleDescriptionsBox); // xml.Append(SampleDescriptionsBox.ToString());
      if (SampleToChunkBox != null) list.Add(SampleToChunkBox); // xml.Append(SampleToChunkBox.ToString());
      if (SampleSizeBox != null) list.Add(SampleSizeBox); // xml.Append(SampleSizeBox.ToString());
      if (ChunkOffSetBox != null) list.Add(ChunkOffSetBox); // xml.Append(ChunkOffSetBox.ToString());
      if (SyncSampleMapBox != null) list.Add(SyncSampleMapBox); // xml.Append(SyncSampleMapBox.ToString());
      list = list.OrderBy(b => b.Offset).ToList();
      foreach (Box b in list)
        xml.Append(b.ToString());
      xml.Append("</box>");

      return (xml.ToString());
    }


    /// <summary>
    /// GetStartAndEndIndex
    /// Given a start time and an end time, determine the start slice index and end slice index.
    /// </summary>
    /// <param name="edtsBox"EditsBox></param>
    /// <param name="sampleTimeScale">uint - sample time scale</param>
    /// <param name="startTime">ulong - start time</param>
    /// <param name="endTime">ulong - end time</param>
    /// <param name="startIndex">out param: start index</param>
    /// <param name="endIndex">out param: end index</param>
    private void GetStartAndEndIndex(EdtsBox edtsBox, uint sampleTimeScale, ulong startTime, ulong endTime, out uint startIndex, out uint endIndex)
    {
      startIndex = 0;
      endIndex = 0;

      ulong ticksDuration = (ulong)TimeArithmetic.ConvertToStandardUnit(sampleTimeScale, parent.parent.MediaHeaderBox.Duration);
      if (edtsBox != null)
      {
        ticksDuration = (ulong)(edtsBox.GetEditTrackDuration(sampleTimeScale) * (decimal)TimeSpan.TicksPerSecond);
      }
      if (ticksDuration < startTime)
        return;

      DecodingTimeToSampleBox stts = this.DecodingTimeToSampleBox;
      SyncSampleMapBox stss = this.SyncSampleMapBox;

      uint sampleCount = 0;
      ulong timeT = 0;
      ulong currScaledT = 0;
      ulong prevScaledT = 0;
      uint[] counts = stts.SampleCount;
      uint[] deltaTimes = stts.SampleDelta;
      bool startSet = false;


      for (int i = 0; i < stts.EntryCount; i++)
      {
        for (int j = 0; j < counts[i]; j++)
        {
          if ((currScaledT >= startTime) && (!startSet) && ((stss == null) || (stss.IsIFrame(sampleCount + 1))))
          {
            startSet = true;
            startIndex = sampleCount + 1;
          }

          if (((stss == null) || stss.IsIFrame(sampleCount + 2)) && (currScaledT > endTime))
          {
            endIndex = sampleCount + 1;
            break;
          } // close of if (currScaledT > endTime)

          prevScaledT = currScaledT;
          timeT += deltaTimes[i];
          sampleCount++;
          currScaledT = (ulong)TimeArithmetic.ConvertToStandardUnit(sampleTimeScale, timeT);
        } // end of for j

        if (endIndex > 0) // end sample found
          break;
      } // end of for i

      if ((endIndex == 0) && startSet) // end sample not found
      {
        endIndex = sampleCount + 1;
      }
    }


    /// <summary>
    /// InitSampleStreamFromSampleTableBox
    /// Generate the list of StreamDataBlockInfo instances from this SampleTableBox.
    /// 1. Call GetStartAndEndAddress;
    /// 2. Call InitSampleStreamFromSampleTableBox.
    /// </summary>
    /// <param name="edtsBox">EdtsBox</param>
    /// <param name="sampleTimeScale">uint - sample time scale</param>
    /// <param name="startTime">ulong - start time</param>
    /// <param name="endTime">ulong - end time</param>
    /// <param name="lastEnd">ulong - last end</param>
    /// <returns></returns>
    public List<StreamDataBlockInfo> InitSampleStreamFromSampleTableBox(EdtsBox edtsBox, uint sampleTimeScale, ulong startTime, ulong endTime, ref ulong lastEnd)
    {

      uint startIndex, endIndex;

      GetStartAndEndIndex(edtsBox, sampleTimeScale, startTime, endTime, out startIndex, out endIndex);
      if (endIndex <= startIndex)
        return null;

      return InitSampleStreamFromSampleTableBox(sampleTimeScale, (int)startIndex, (int)endIndex, ref lastEnd);
    }


    /// <summary>
    /// InitSampleStreamFromSampleTableBox
    /// The idea is to collect information on slices starting from startindex to endIndex.
    /// This is a fairly complex method that traverses all boxes read from this SampleTableBox.
    /// </summary>
    /// <param name="sampleTimeScale">uint - sample time scale</param>
    /// <param name="startIndex">int - start index</param>
    /// <param name="endIndex">int - end index</param>
    /// <param name="lastEnd">ref ulong</param>
    /// <returns></returns>
    public List<StreamDataBlockInfo> InitSampleStreamFromSampleTableBox(uint sampleTimeScale, int startIndex, int endIndex, ref ulong lastEnd)
    {
      List<StreamDataBlockInfo> SampleStreamLocations = new List<StreamDataBlockInfo>();

      // local vars
      DecodingTimeToSampleBox stts = this.DecodingTimeToSampleBox;
      if (stts == null)
        throw new Exception("SampleTableBox.DecodingTimeToSampleBox missing for track");
      uint sampleCount = 0;
      ulong timeT = 0;
      ulong currScaledT = 0;
      ulong prevScaledT = 0;
      ulong endT = 0;
      uint[] counts = stts.SampleCount;
      uint[] deltaTimes = stts.SampleDelta;
      ulong currOffset = 0UL;
      SampleSizeBox stsz = this.SampleSizeBox;
      uint sampleSize = stsz.SampleSize;
      uint totalSamples = stsz.SampleCount;
      uint[] sizeArray = stsz.SampleSizeArray;
      int sampleCountInList = 0;

      if ((this.SampleDescriptionsBox == null) || (this.SampleDescriptionsBox.EntryCount != 1))
        throw new Exception("SampleTableBox.SampleDescriptionsBox error");
      BoxType sampleDescriptionBoxType = this.SampleDescriptionsBox.Entries[0].Type;

      // initialize (set) cttsIndex to the value that corresponds to startIndex
      int k = 0;
      int cttsIndex = 0;
      if (CompositionTimeToSample != null && CompositionTimeToSample.EntryCount > 0)
      {
        int sliceIndex = 1;
        while (cttsIndex < CompositionTimeToSample.SampleOffset.Length)
        {
          if (sliceIndex == startIndex)
            break;
          k++;
          if (k == CompositionTimeToSample.SampleCount[cttsIndex])
          {
            k = 0; // begin counting from zero again
            cttsIndex++;
          }
          sliceIndex++;
        }
      }

      for (int i = 0; i < stts.EntryCount; i++)
      {
        for (int j = 0; j < counts[i]; j++)
        {
          currScaledT = (ulong)((timeT * (ulong)TimeSpan.FromSeconds(1.0).Ticks) / sampleTimeScale);
          if ((sampleCount + 1 >= startIndex) && (sampleCount + 1 <= endIndex)) {
            StreamDataBlockInfo data = new StreamDataBlockInfo();
            data.index = (int)sampleCount;
            data.TimeStampNew = currScaledT;
            data.SliceDuration = (uint)((deltaTimes[i] * TimeSpan.TicksPerSecond) / sampleTimeScale) + 1;
            data.SliceSize = (int)sampleSize;
            if (sampleSize == 0)
              data.SliceSize = (int)sizeArray[sampleCount];
            data.StreamOffset = (ulong)this.SampleToChunkBox.GetFileOffset((uint)(sampleCount + 1));
            data.SliceType = sampleDescriptionBoxType == BoxTypes.Mp4a ? SliceType.MP4A :
              ((this.SyncSampleMapBox == null) || this.SyncSampleMapBox.IsIFrame(sampleCount + 1) ? SliceType.IFrame : SliceType.DFrame);

            // if necessary, increment cttsIndex
            if (CompositionTimeToSample != null && CompositionTimeToSample.EntryCount > 0) {
              k++;
							data.NonQuickTimeCTTS = CompositionTimeToSample.SampleOffset[cttsIndex];

              if (k == CompositionTimeToSample.SampleCount[cttsIndex]) {
                k = 0; // begin counting from zero again
                cttsIndex++;
              }
            }

            SampleStreamLocations.Add(data);
            sampleCountInList++;
          }

          if (sampleCount + 1 > endIndex)
          {
            endT = prevScaledT;
            break;
          } // close of if (currScaledT > endTimeJustBeforeIFrame)

          // keep track of offset
          if (sampleSize > 0)
            currOffset += sampleSize;
          else
          {
            if (sampleCount > totalSamples)
              throw new Exception("SampleTableBox error: sample count inconsistency bet. stts and stsz");
            currOffset += sizeArray[sampleCount];
          }

          prevScaledT = currScaledT;
          timeT += deltaTimes[i];
          sampleCount++;
        } // end of for j

        if (endT > 0UL) // end sample found
          break;
      } // end of for i

      if (endT == 0UL) // if we did not find end, endTime would not be set
        lastEnd = currScaledT;
      else
        lastEnd = endT;

      return SampleStreamLocations;
    }

    #region Box Construction Routines

    public override void Write(BoxWriter writer)
    {
      using (new SizeCalculator(this, writer))
      {
        base.Write(writer);

        if (SampleDescriptionsBox != null)
          SampleDescriptionsBox.Write(writer);
        if (DecodingTimeToSampleBox != null)
          DecodingTimeToSampleBox.Write(writer);
        if (CompositionTimeToSample != null && CompositionTimeToSample.EntryCount > 0)
          CompositionTimeToSample.Write(writer);
        if (SampleToChunkBox != null)
          SampleToChunkBox.Write(writer);
        if (SampleSizeBox != null)
          SampleSizeBox.Write(writer);
        if (ChunkOffSetBox != null)
          ChunkOffSetBox.Write(writer);
        if (SyncSampleMapBox != null)
          SyncSampleMapBox.Write(writer);
      }

      Cleanup(); // delete all temp files
    }

    Stream SttsCountsStream;           // stts
    BinaryWriter SttsCountsWriter;
    BinaryReader SttsCountsReader;
    string SttsCountsFileName;
    Stream SttsTimeDeltaStream;        // stts
    BinaryWriter SttsTimeDeltaWriter;
    BinaryReader SttsTimeDeltaReader;
    string SttsTimeDeltaFileName;
    Stream SampleSizeStream;           // stsz
    BinaryWriter SampleSizeWriter;
    BinaryReader SampleSizeReader;
    string SampleSizeFileName;
    Stream SyncSampleMapStream;        // stss
    BinaryWriter SyncSampleMapWriter;
    BinaryReader SyncSampleMapReader;
    string SyncSampleMapFileName;

    /// <summary>
    /// CreateTempFiles
    /// Create a temp file for each of the changing boxes: DecodingTimeToSampleBox, SampleToChunkBox, etc.
    /// The purpose of these temp files is to allow for processing very large video/audio files.
    /// </summary>
    private void CreateTempFiles()
    {
			Common.Logger.Instance.Info("[SampleTableBox::CreateTempFiles]");

      SttsCountsFileName = Path.GetTempFileName();
      SttsCountsStream = File.Create(SttsCountsFileName);
      SttsCountsWriter = new BinaryWriter(SttsCountsStream);
      SttsTimeDeltaFileName = Path.GetTempFileName();
      SttsTimeDeltaStream = File.Create(SttsTimeDeltaFileName);
      SttsTimeDeltaWriter = new BinaryWriter(SttsTimeDeltaStream);
      SampleSizeFileName = Path.GetTempFileName();
      SampleSizeStream = File.Create(SampleSizeFileName);
      SampleSizeWriter = new BinaryWriter(SampleSizeStream);
      SyncSampleMapFileName = Path.GetTempFileName();
      SyncSampleMapStream = File.Create(SyncSampleMapFileName);
      SyncSampleMapWriter = new BinaryWriter(SyncSampleMapStream);
    }

    private void WriteToSttsTempFile()
    {
      if (SttsCountsWriter == null)
        throw new Exception("Nothing to save. Movie too short?");
      SttsCountsWriter.Write(sampleCountInStts);
      SttsTimeDeltaWriter.Write(LastDuration);
      CurrSttsCount++;
    }

    /// <summary>
    /// Cleanup - close all temp files and delete.
    /// </summary>
    private void Cleanup()
    {
      if (SttsCountsStream != null)
      {
        SttsCountsStream.Close();
        SttsCountsWriter.Dispose();
        SttsCountsReader.Dispose();
        File.Delete(SttsCountsFileName);
        SttsCountsStream = null;
      }

      if (SttsTimeDeltaStream != null)
      {
        SttsTimeDeltaStream.Close();
        SttsTimeDeltaWriter.Dispose();
        SttsTimeDeltaReader.Dispose();
        File.Delete(SttsTimeDeltaFileName);
        SttsTimeDeltaStream = null;
      }

      if (SampleSizeStream != null)
      {
        SampleSizeStream.Close();
        SampleSizeWriter.Dispose();
        SampleSizeReader.Dispose();
        File.Delete(SampleSizeFileName);
        SampleSizeStream = null;
      }

      if (SyncSampleMapStream != null)
      {
        SyncSampleMapStream.Close();
        SyncSampleMapWriter.Dispose();
        SyncSampleMapReader.Dispose();
        File.Delete(SyncSampleMapFileName);
        SyncSampleMapStream = null;
      }

      if (SampleToChunkBox != null)
      {
        SampleToChunkBox.Cleanup();
        //SampleToChunkBox = null;
      }
    }

    // retained variables to keep track of SampleTableBox construction
    private ulong LastSynchTime = 0L;
    private int CurrSyncSampleMapCount = 0;
    private uint SampleIndex = 1;

    private int CurrSttsCount = 0; // count of entries in DecodingTimeToSampleBox arrays
    private uint LastDuration = 0;
    private uint sampleCountInStts = 0;

    private uint SampleCountInLastBatch = 0; // last batch sample count for this trak box (this is independent of the other trak box)

    /// <summary>
    /// InitSampleTableBoxFromStreamLocations
    /// Initialize the boxes that point to where the payload bits are, without writing them out to final destination file yet.
    /// Major change (06/06/2012): favor creating new chunks over accumulating slices in a chunk.
    /// We take it to the extreme here, like VLC does it: we create a new chunk for every slice/sample.
    /// What this does is make the stsc box small, but the stco box very large. The advantage is that
    /// every slice now has an offset into mdat (and the slice crawler can't possibly go out of sync).
    /// </summary>
    /// <param name="streamLocations">List of StreamDataBlockInfo extracted from source stream, possibly using InitSampleStreamFromSampleTableBox above.</param>
    public void InitSampleTableBoxFromStreamLocations(List<StreamDataBlockInfo> streamLocations, ref ulong currMdatOffset)
    {
      // if this is the first call, create temp files
      if (SttsCountsWriter == null) CreateTempFiles();

    	if (CompositionTimeToSample == null && (CTTSOut) && (streamLocations.Any(d => (d.CTS > 0UL) || (d.SliceType == SliceType.BFrame)))) {
        CompositionTimeToSample = new CompositionTimeToSample(this);
      }

      if (streamLocations.Count == 0)
        throw new Exception("InitSampleTableBoxFromStreamLocations: SampleStreamLocations list empty.");
      bool needNewChunk = true;

      foreach (StreamDataBlockInfo sample in streamLocations)
      {
        uint scaledDuration = (uint)TimeArithmetic.ConvertToTimeScale(parent.parent.MediaHeaderBox.TimeScale, sample.SliceDuration);
        if (LastDuration == 0)
        {
          sampleCountInStts = 1;
        }
        else if (LastDuration == scaledDuration)
        {
          sampleCountInStts++;
        }
        else
        {
          WriteToSttsTempFile();
          sampleCountInStts = 1; // this one for which duration is different counts as one
        }
        LastDuration = scaledDuration;
        //TimeTicks += sample.SliceDuration;
        if (sample.SliceType == SliceType.IFrame)
        {
          SyncSampleMapWriter.Write(SampleIndex); // if the SyncSampleMapStream has zero length when all is done, then its box should be null
          CurrSyncSampleMapCount++;
        }
        // compute CTTS from TimeStamp and CTS
        if (CompositionTimeToSample != null) {

        	// CTS = Composition Time of the Sample, so these values are ever-increasing
        	// CTTS = Composition Time relative to Time of the Sample, so these are really either 0 or some multiple of the typical sample duration

        	// CTTS values for an i-frame, for example, is always zero, as its composition time relative to the sample:
        	// CTTS-iframe = SampleTime - CTS = Always 0

        	if (sample.SliceType == SliceType.IFrame) {
						// relative time for an iframe is always 0
        		CompositionTimeToSample.AddEntry(0);
        		LastSynchTime = 0;
        	}  else {
						//if (sample.TimeStampNew.HasValue) {
						//  // relative time for a d-frame is always 0
						//  uint TimeFromLastSample = (uint)TimeArithmetic.ConvertToTimeScale(parent.parent.MediaHeaderBox.TimeScale, sample.SliceDuration);
						//  CompositionTimeToSample.AddEntry((uint)TimeFromLastSample);
						//} else {
						//  // this means we are a b-frame
						//  CompositionTimeToSample.AddEntry((uint)uint.MaxValue);
						//}

						
            if (!sample.TimeStampNew.HasValue || sample.SliceType == SliceType.BFrame)
            {
							// this means we are a b-frame
							uint TimeFromLastSample = (uint)TimeArithmetic.ConvertToTimeScale(parent.parent.MediaHeaderBox.TimeScale, sample.SliceDuration);
							// we get further from a sync time for each consecutive b-frame we have
							// as you can see above if we encounter an i or d frame we snap back to a delta of 0
							LastSynchTime += TimeFromLastSample; 
							CompositionTimeToSample.AddEntry((uint)LastSynchTime);
						}
						else if (sample.TimeStampNew.HasValue) {
							// relative time for a d-frame is always 0
							CompositionTimeToSample.AddEntry(0);
							LastSynchTime = 0;
						} 
					}
        }

        // determine which chunk to put this sample in
        SampleSizeWriter.Write((uint)sample.SliceSize);
        SampleToChunkBox.SetFileOffsetForChunk(SampleIndex, (uint)sample.SliceSize, 1u /* (uint)streamLocations.Count */, needNewChunk, ref currMdatOffset);
        needNewChunk = true; // always create a new chunk, thereby having only a single slice in every chunk (as in VLC output)
        SampleIndex++;
      }

      // set last count
      SampleCountInLastBatch = (uint)streamLocations.Count;
    }

    /// <summary>
    /// Finalize
    /// Open all temp files for reading, then write contents out to final destination file.
    /// </summary>
    public void FinalizeBox()
    {

      if (!fragmented)
      {
        // final stts entry (careful with this: call only for very LAST sample)
        WriteToSttsTempFile();

        SttsCountsStream.Position = 0L;
        SttsTimeDeltaStream.Position = 0L;
        SampleSizeStream.Position = 0L;
        SyncSampleMapStream.Position = 0L;

        SttsCountsReader = new BinaryReader(SttsCountsStream);
        SttsTimeDeltaReader = new BinaryReader(SttsTimeDeltaStream);
        SampleSizeReader = new BinaryReader(SampleSizeStream);
        SyncSampleMapReader = new BinaryReader(SyncSampleMapStream);

        DecodingTimeToSampleBox.FinalizeBox(SttsCountsReader, SttsTimeDeltaReader, (uint)CurrSttsCount);
        SampleToChunkBox.FinalizeBox(); // calls ChunkOffsetBox.FinalizeBox also ?
        SampleSizeBox.FinalizeBox(SampleSizeReader, SampleIndex - 1);

        SampleToChunkBox.ChunkOffsetWriter.BaseStream.Position = 0L;
        BinaryReader ChunkOffsetReader = new BinaryReader(SampleToChunkBox.ChunkOffsetWriter.BaseStream);
        ChunkOffSetBox.FinalizeBox(ChunkOffsetReader);

        if (SyncSampleMapBox != null)
          SyncSampleMapBox.FinalizeBox(SyncSampleMapReader, (uint)CurrSyncSampleMapCount);

        if (CompositionTimeToSample != null && CompositionTimeToSample.EntryCount > 0)
          CompositionTimeToSample.FinalizeBox();
      }

      this.Size += DecodingTimeToSampleBox.Size;
      this.Size += SampleToChunkBox.Size;
      this.Size += SampleSizeBox.Size;
      this.Size += ChunkOffSetBox.Size;
      if (SyncSampleMapBox != null)
        this.Size += SyncSampleMapBox.Size;
      if (CompositionTimeToSample != null && CompositionTimeToSample.EntryCount > 0)
        this.Size += CompositionTimeToSample.Size;
      this.Size += SampleDescriptionsBox.Size;
    }

    #endregion
  }
}

