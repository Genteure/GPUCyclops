//namespace Media.Formats.MP4
//{
//  using System;
//  using System.Collections.Generic;
//  using System.IO;
//  using System.Runtime.InteropServices;

//  public enum FrameType {
//    Audio,
//    Video
//  }

//  public class DataFrame {
//    public byte[] Data;
//    public long Duration;
//  }

//  public class FragmentStat {
//    public ulong TimeStamp;
//    public ulong Duration;
//    public ulong FileOffset;
//  }

//  public class FragmentGenerator
//  {
//    public uint TrackID;
//    public uint DefaultSampleSize;
//    public uint DefaultSampleFlags;
//    public uint FirstSampleFlags;
//    public bool IncludeFrameSizes = false;
//    public uint SequenceNumber = 0;
//    public FrameType FrameType;

//    public List<DataFrame> FrameQueue = new List<DataFrame>();

//    // general stats...
//    public uint FragmentsGenerated = 0;
//    public ulong TotalDuration = 0;
//    public List<FragmentStat> FragmentStats = new List<FragmentStat>();

//    public void AddFrame(byte[] inData, long inDuration) {
//      DataFrame frame = new DataFrame();
//      frame.Data = inData;
//      frame.Duration = inDuration;

//      FrameQueue.Add(frame);
//    }

//    public ulong FinalizeFragment(BoxWriter writer, long inDuration) {
//      SequenceNumber++;
//      FragmentsGenerated++;

//      uint LiveDuration = 0;
//      if (inDuration > 0) {
//        LiveDuration = (uint)(inDuration / FrameQueue.Count);
//      }

//      Fragment NewFragment = new Fragment();

//        NewFragment.MovieFragmentBox = new MovieFragmentBox();
//          NewFragment.MovieFragmentBox.MovieFragmentHeaderBox = new MovieFragmentHeaderBox();
//            NewFragment.MovieFragmentBox.MovieFragmentHeaderBox.SequenceNumber = SequenceNumber;

//          NewFragment.MovieFragmentBox.TrackFragmentBox = new TrackFragmentBox();

//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox = new TrackFragmentHeaderBox();
//            TrackFragmentHeaderBoxFlags tfhbFlags = new TrackFragmentHeaderBoxFlags();
//            if (DefaultSampleSize != 0) tfhbFlags |= TrackFragmentHeaderBoxFlags.DefaultSampleSizePresent;
//            if (DefaultSampleFlags != 0) tfhbFlags |= TrackFragmentHeaderBoxFlags.DefaultSampleFlagsPresent;
//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.Flags = (uint)tfhbFlags;
//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.TrackId = TrackID; //question
//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.DefaultSampleSize = DefaultSampleSize; //question
//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentHeaderBox.DefaultSampleFlags = DefaultSampleFlags; //question

//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentRunBox = new TrackFragmentRunBox();
//            TrackFragmentRunBoxFlags tfrbFlags = new TrackFragmentRunBoxFlags();
//            tfrbFlags |= TrackFragmentRunBoxFlags.SampleDurationPresent;
//            if (FirstSampleFlags != 0) tfrbFlags |= TrackFragmentRunBoxFlags.FirstSampleFlagsPresent;
//            if (IncludeFrameSizes == true) tfrbFlags |= TrackFragmentRunBoxFlags.SampleSizePresent;

//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentRunBox.Flags = (uint)tfrbFlags;
//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentRunBox.FirstSampleFlags = FirstSampleFlags; //question
//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentRunBox.SampleCount = (uint)FrameQueue.Count; //question
//            NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentRunBox.Samples = new List<TrackFragmentRunSample>();
//            for (int i=0; i< (uint)FrameQueue.Count; i++) {
//              DataFrame frame = FrameQueue[i];
//              TrackFragmentRunSample RunSample = new TrackFragmentRunSample();
////              tfrbFlags = new TrackFragmentRunBoxFlags();
////              tfrbFlags |= TrackFragmentRunBoxFlags.SampleDurationPresent;
////              if (IncludeFrameSizes == true) tfrbFlags |= TrackFragmentRunBoxFlags.SampleSizePresent;
//              if (inDuration > 0) {
//                RunSample.SampleDuration = LiveDuration;
//              } else {
//                RunSample.SampleDuration = (uint)frame.Duration; //question
//              }
//              RunSample.SampleSize = (uint)frame.Data.Length; //question
//              NewFragment.MovieFragmentBox.TrackFragmentBox.TrackFragmentRunBox.Samples.Add(RunSample);
//            }

//            NewFragment.MovieFragmentBox.TrackFragmentBox.IndependentAndDisposableSamplesBox = 
//              new IndependentAndDisposableSamplesBox((int)(uint)FrameQueue.Count);
//            NewFragment.MovieFragmentBox.TrackFragmentBox.IndependentAndDisposableSamplesBox.Samples = new List<IndependentAndDisposableSample>();
//            for (int i=0; i<(uint)FrameQueue.Count; i++) {
//              IndependentAndDisposableSample IndpSample = new IndependentAndDisposableSample();
//              if (i==0) {
//                if (FrameType == FrameType.Audio) {
//                  IndpSample.SampleDependsOn = 3; //question
//                  IndpSample.SampleIsDependedOn = 0; //question
//                  IndpSample.SampleHasRedundancy = 0; //question
//                } else {
//                  IndpSample.SampleDependsOn = 1; //question
//                  IndpSample.SampleIsDependedOn = 1; //question
//                  IndpSample.SampleHasRedundancy = 0; //question
//                }
//              } else {
//                if (FrameType == FrameType.Audio) {
//                  IndpSample.SampleDependsOn = 3; //question
//                  IndpSample.SampleIsDependedOn = 1; //question
//                  IndpSample.SampleHasRedundancy = 0; //question
//                } else {
//                  IndpSample.SampleDependsOn = 3; //question
//                  IndpSample.SampleIsDependedOn = 1; //question
//                  IndpSample.SampleHasRedundancy = 0; //question
//                }
//              }
//              NewFragment.MovieFragmentBox.TrackFragmentBox.IndependentAndDisposableSamplesBox.Samples.Add(IndpSample);
//            }

//        ulong FragmentDuration = 0;
//        NewFragment.MediaDataBox = new MediaDataBox();
//        MemoryStream MemData = new MemoryStream();
//        foreach (DataFrame frame in FrameQueue) {
//          MemData.Write(frame.Data, 0, frame.Data.Length);
//          if (inDuration > 0) {
//            FragmentDuration += (ulong)LiveDuration;
//          } else {
//            FragmentDuration += (ulong)frame.Duration;
//          }
//        }
//        MemData.Seek(0, SeekOrigin.Begin);
//        NewFragment.MediaDataBox.MediaData = new byte[MemData.Length];
//        MemData.Read(NewFragment.MediaDataBox.MediaData, 0, (int)MemData.Length);

//      // keep track of a few stats about this fragment... at this point it is all in memory and nothing has 
//      // actually been written to disk....
//      FragmentStat stat = new FragmentStat();
//      stat.FileOffset = (ulong)writer.BaseStream.Position;
//      stat.TimeStamp = TotalDuration;
//      stat.Duration = FragmentDuration;
//      TotalDuration += FragmentDuration;
//      FragmentStats.Add(stat);

//      // final stuff... write the fragment header and all associated data...
//      NewFragment.Write(writer);

//      writer.BaseStream.Flush();

//      // only supported in .net / not Silverlight:
//      // FlushFileBuffers(((FileStream)writer.BaseStream).SafeFileHandle.DangerousGetHandle());

//      // remove all data in the queue getting ready for more...
//      FrameQueue.Clear();

//      return (FragmentDuration);
//    }

//    [DllImport("kernel32.dll")]
//    static extern bool FlushFileBuffers(IntPtr hFile);

//  }
//}
