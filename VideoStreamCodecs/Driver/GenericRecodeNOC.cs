// no longer used

//using System;
//using System.Collections.Generic;
//using Media;
//using Media.Formats;
//using ODS.Processing.Core;

//#if SILVERLIGHT
//using System.Windows.Media;
//#endif

//namespace Driver
//{
//  public class GenericRecodeNOC : BaseRecode
//  {
//    ulong blockLen = 28000000;

//    public GenericRecodeNOC()
//    {
//    }

//    public GenericRecodeNOC(IMediaStream srcStream, IMediaStream destStream, TracksIncluded audioOrVideoOnly = TracksIncluded.Both,
//      bool cttsOut = false)
//      : base(srcStream, destStream)
//    {
//      audioOrVideoOrBoth = audioOrVideoOnly;
//      CTTSOut = cttsOut;
//    }

//    public override void Recode()
//    {
//      Recode(0UL, 0UL);
//    }

//    public override void Recode(ulong startTime, ulong endTime)
//    {
//      Recode(startTime, endTime, 0); // default video track
//    }

//    public override void Recode(ulong startTime, ulong endTime, ushort videoTrackID)
//    {
//      SourceStream.Read(); // first read initializes headers

//      if (endTime == 0) // special case when endTime == 0
//      {
//        endTime = SourceStream.DurationIn100NanoSecs; // 100 nanosec units
//      }

//      if (endTime > 0)
//      {
//        ulong movieLen = endTime - startTime;
//        if (movieLen > blockLen) // if longer than 6 secs
//          MaxIterateDuration = blockLen;
//        else
//        {
//          throw new Exception("Desired movie length too short");
//        }
//      }

//      if (endTime - startTime < MaxIterateDuration)
//        throw new Exception("Desired time interval for output stream too short");

//      IAudioTrack audio = (IAudioTrack)SourceStream[CodecTypes.Audio, 0]; // get the first sourceAudio track
//      IVideoTrack video = (IVideoTrack)SourceStream[CodecTypes.Video, videoTrackID]; // get the desired sourceVideo track

//      TrackInfo = new List<IsochronousTrackInfo>(2);
//      TrackInfo.Add(new RawAudioTrackInfo(SourceStream));
//      TrackInfo.Add(new RawVideoTrackInfo(video));

//      // FIXME: kludge: this is specific to QBox when recoding to MP4
//      if (video.TrackFormat.GetType().ToString().Contains("QBoxTrackFormat"))
//      foreach (IsochronousTrackInfo tinfo in TrackInfo)
//      {
//        tinfo.Brands = "qbox";
//      }

//      AdjustTrackSpecsToDestination(); // adjust recode params according to output
      
//      // setup destination stream here (initialize headers in output tracks)
//      DestStream.InitializeForWriting(TrackInfo);

//      Int64 duration = (long)SourceStream.FragmentDuration;  // if no fragments then this is the same as Duration
//      UInt64 startIterateTime = 0; // lastEnd + 1; // start time in milliseconds of the first sample for an enumerator over the tracks...
//      UInt64 endIterateTime = MaxIterateDuration; // end time in milliseconds of the first sample for an enumerator over the tracks...

//      do
//      {
//        SourceStream.Read(); // subsequent reads

//        // optional, you can set an iterator window if you like... for fragments, you don't realy need this as each one is managable, 
//        // however with mp4 files the amount of sourceAudio and sourceVideo could become quite large...
//        // so we do it in an indefinite number of batches
//        while ((startIterateTime < endTime) && (SourceStream.PrepareSampleReading(startIterateTime, endIterateTime)))
//        {
//          if (startIterateTime >= startTime)
//          {
//            switch (audioOrVideoOrBoth)
//            {
//              case TracksIncluded.Audio:
//                DestStream.PrepareSampleWriting(audio.SampleStreamLocations, CodecTypes.Audio);
//                DestStream.WriteSamples(audio, CodecTypes.Audio);
//                break;
//              case TracksIncluded.Both:
//                // one batch
//                DestStream.PrepareSampleWriting(audio, video); // write sample metadata to temp files
//                DestStream.WriteSamples(audio, video); // write content bits to temp file
//                break;
//              case TracksIncluded.Video:
//                DestStream.PrepareSampleWriting(video.SampleStreamLocations, CodecTypes.Video);
//                DestStream.WriteSamples(video, CodecTypes.Video);
//                break;
//              default:
//                throw new Exception("Invalid track");
//            }
//          }

//          // slide the "window" forward (increment both startIterateTime and endIterateTime)
//          startIterateTime = endIterateTime;
//          endIterateTime = startIterateTime + MaxIterateDuration;
//        }

//        if (SourceStream.IsMediaStreamFragmented == false) break;
//      } while (!SourceStream.EOF);

//      // Assemble all stbl or moof boxes.
//      // Write out the mdat box in the case of MP4 output;
//      // in the case of fragmented files (ISMV output), all moof and mdat boxes have already been written out at this point, and we 
//      // only need to write out the mfra box, if it is needed.
//      DestStream.FinalizeStream();
//    }
//  }
//}
