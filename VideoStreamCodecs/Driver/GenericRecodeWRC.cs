using System;
using System.Collections.Generic;
using System.Linq;
using Media.Formats.Generic;
using Media.Formats;
using Media;
using Common; 
#if SILVERLIGHT
using System.Windows.Media;
#endif

namespace Driver
{
  public class GenericRecodeWRC : BaseRecode
  {
    struct RecodeSet
    {
      public IMediaTrackSliceEnumerator sourceTrack;
      public IMediaTrackSliceEnumerator destinationTrack;
      public IMediaTrack destination;
      public ulong timeStart;
      public int indexStart;

		public List<Slice> pendingChunkSlices;
    }

    public GenericRecodeWRC()
    {
    }

    public GenericRecodeWRC(IMediaStream srcStream, IMediaStream destStream, int videoTrackID, TracksIncluded audioOrVideoOnly = TracksIncluded.Both,
      bool cttsOut = false)
      : base(srcStream, destStream)
    {
      audioOrVideoOrBoth = audioOrVideoOnly;
      CTTSOut = cttsOut;

		  //Common.Logger.Instance.Info("[GenericRecodeWRC::Ctor] srcStream [" + srcStream.GetType().Name + "], destStream [" + destStream.GetType().Name + "], videoTrackId [" + videoTrackID + "]");

      // get characteristics of input stream, and set FetchNextBlock callback on each track.
      TrackInfo = IsochronousTrackInfo.GetTrackCharacteristics(SourceStream, audioOrVideoOrBoth, videoTrackID);

      if ((!TrackInfo.Any(t => t is RawVideoTrackInfo)) && (audioOrVideoOnly != TracksIncluded.Audio))
        throw new ArgumentOutOfRangeException("Video track specified does not exist");

      AdjustTrackSpecsToDestination(); // adjust recode params according to output

      // setup destination stream here (initialize headers in output tracks)
      DestStream.InitializeForWriting(TrackInfo);
    }

    public GenericRecodeWRC(IMediaStream srcStream, IMediaStream destStream)
      : this(srcStream, destStream, 0)
    {
    }

    // The video track is the track to which all other tracks should sync with.
    // SyncPoints are instances in time at which:
    // 1. the current video slice is an IFrame; or
    // 2. the current video slice is the beginning of a new block (in case the video track is all IFrames or no IFrames).
    // NOTE: This will only work if GenericMediaStream.CachingEnabled is true.
    public IEnumerable<ulong> EnumerateSyncPoints(IVideoTrack vTrack)
    {
      IMediaTrackSliceEnumerator sliceEnum = (IMediaTrackSliceEnumerator)vTrack.GetEnumerator();
    	ulong lastTimeStamp = 0;
      while (sliceEnum.MoveNext())
      {
        Slice slice = sliceEnum.Current;
        if (slice == null)
          break;
		    if (slice.SliceType == SliceType.IFrame) {
			    var timeStamp = sliceEnum.CurrentTimeStampNew.Value; // guaranteed as this is an iframe...
				 //Common.Logger.Instance.Info("[GenericRecodeWRC::SyncPoints] timeStamp [" + timeStamp + "]");
			    yield return timeStamp;
		    }

				if (sliceEnum.CurrentTimeStampNew.HasValue && sliceEnum.CurrentTimeStampNew.Value > 0)
					lastTimeStamp = sliceEnum.CurrentTimeStampNew.Value;
      }

			//Common.Logger.Instance.Info("[GenericRecodeWRC::SyncPoints] timeStamp [" + lastTimeStamp + "]");
			yield return lastTimeStamp; // last slice is NOT an IFrame, but it is a syncpoint nevertheless
    }

	 public override void Recode(ulong startTime100NanoSec, ulong endTime100NanoSec, ushort videoTrackID)
	 {
		 var vidTracks = DestStream.MediaTracks.Where(t => t is GenericVideoTrack);
		 int vidTrackCount = (vidTracks == null) ? 0 : vidTracks.Count();

		 if (endTime100NanoSec == 0)
		 { // special case when endTime == 0
			 // using duration here is ok as it is about the total time of the source
			 endTime100NanoSec = SourceStream.DurationIn100NanoSecs;
		 }

		 if (endTime100NanoSec - startTime100NanoSec < MaxIterateDuration)
			 throw new Exception("Desired time interval for output stream too short");

		 int outTracks = DestStream.MediaTracks.Count;
		 RecodeSet[] trackEnumerators = new RecodeSet[outTracks];
		 int k = 0;
		 int n = 0;
		 foreach (IMediaTrack track in SourceStream.MediaTracks)
		 {
			 if (((track.Codec.CodecType == CodecTypes.Audio) && (audioOrVideoOrBoth != TracksIncluded.Video)) ||
				((track.Codec.CodecType == CodecTypes.Video) && ((videoTrackID == 0) || (track.TrackID == videoTrackID)) &&
				(audioOrVideoOrBoth != TracksIncluded.Audio)))
			 {
				 RecodeSet recodeSet = new RecodeSet();
				 recodeSet.sourceTrack = (IMediaTrackSliceEnumerator)track.GetEnumerator();
				 recodeSet.sourceTrack.Reset();
				 recodeSet.pendingChunkSlices = new List<Slice>();

				 IMediaTrack destination = DestStream[recodeSet.sourceTrack.CodecType, 0];

				 if ((track.Codec.CodecType != CodecTypes.Video) || (vidTrackCount == 1))
					 destination = DestStream[recodeSet.sourceTrack.CodecType, 0];
				 else if (vidTrackCount > 1)
				 {
					 destination = vidTracks.ElementAt(n);
					 n++;
				 }

				 // normally the destination TrackDurationIn100NanoSecs is set to source duration;
				 // here we reset its value back to zero because it may be smaller than source duration
				 // (for example, if the start time is more than zero).
				 destination.TrackDurationIn100NanoSecs = 0UL;
				 recodeSet.destination = destination;
				 recodeSet.destinationTrack = (IMediaTrackSliceEnumerator)destination.GetEnumerator();
				 recodeSet.destinationTrack.Reset();

				 trackEnumerators[k++] = recodeSet;
			 }
		 }

		 RaiseRecodeProgressUpdate(0.01f, true, null); // Indicate we have completed a portion of the work.

		 // Need to call MoveNext() first for all source track enumerators
		 foreach (RecodeSet recodeSet in trackEnumerators)
		 {
			 while (recodeSet.sourceTrack.MoveNext())
				 if (recodeSet.sourceTrack.Current != null) break;
		 }

		 IVideoTrack videoTrack = (IVideoTrack)SourceStream[CodecTypes.Video, 0];
		 ulong prevSyncTime = 0UL;
		 bool validSyncPointsFound = false;
		 
		 foreach (ulong syncTime in EnumerateSyncPoints(videoTrack))
		 {// Cycle through all of the sync points in the video...
			 Logger.Instance.Info("[GenericRecodeWRC::Recode] [merge] iterating at syncTime [" + syncTime + "].");
			 
			 if ((syncTime > endTime100NanoSec) && (prevSyncTime > endTime100NanoSec))
				 break; // If we are past the requested end time, stop doing work

			 // Each source and destinatin track has its own, independent counter (enumerator).
			 // The slices are synced with respect to time, and NOT with respect to index.
			 // The outer for loop below iterates through each track being recoded;
			 // the inner while loop iterates through each slice skipped.
			 // .timeStart == time relative to source track at which recoding starts (should be first slice NOT skipped);
			 // .indexStart == index of first slice NOT skipped.
			 if (startTime100NanoSec > prevSyncTime)
			 { // Skip a portion of slices.

				 for (int i = 0; i < trackEnumerators.Length; i++)
				 {
					 if (trackEnumerators[i].sourceTrack.CurrentTimeStampNew.HasValue == false) 
						 continue; // b-frame and we can't use it to compare...

					 while (trackEnumerators[i].sourceTrack.CurrentTimeStampNew.Value < syncTime)
					 {
						 Slice slice = trackEnumerators[i].sourceTrack.Current;
						 if (slice == null) break;
						 if (slice.TimeStampNew.HasValue == false) continue; // its a b-frame, thus no time is available

						 trackEnumerators[i].timeStart = slice.TimeStampNew.Value; // at this point its guaranteed to have a value...
						 trackEnumerators[i].indexStart = slice.index;

						 // Find the next valid CurrentTimeStampNew value.
						 bool tmpEnd = false;
						 while (true)
						 {
							 if (!trackEnumerators[i].sourceTrack.MoveNext())
							 {
								 tmpEnd = true;
								 break; // Ended.
							 }

							 if (trackEnumerators[i].sourceTrack.CurrentTimeStampNew.HasValue == true)
								 break; // Found it.
						 }

						 if (tmpEnd == true) 
							 break;
					 }
				 }

				 prevSyncTime = syncTime;
				 continue;
			 }

			 // If we never hit this condition there is nothing actually taken in to process and this causes an exception down the road.
			 validSyncPointsFound = true;

			 // Each source and destinatin track has its own, independent counter (enumerator).
			 // The slices are synced with respect to time, and NOT with respect to index.
			 // The outer foreach loop below iterates through each track being recoded;
			 // the inner while loop iterates through each slice.
			 // recodeSet.sourceTrack ==> source track enumerator
			 // recodeSet.destinationTrack ==> destination track enumerator
			 ulong timeStamp100NanoSec = ulong.MaxValue;

			 foreach (RecodeSet recodeSet in trackEnumerators)
			 {
				 recodeSet.pendingChunkSlices.Clear();

				 // Start writing the actual data.
				 while (recodeSet.sourceTrack.CurrentTimeStampNew.HasValue == false 
					 || recodeSet.sourceTrack.CurrentTimeStampNew.Value <= syncTime)
				 {
					 Slice slice = recodeSet.sourceTrack.Current;
					 if (slice == null) 
						 break;

					 //Logger.Instance.Info("[GenericRecodeWRC::Recode] dumping slice [" + slice.TimeStampNew + ", dur " + (int)slice.SliceDuration + "], track type [" + recodeSet.sourceTrack.CodecType + "].");

					 // Prepare the slice; apply position and time compensation, to base it to the start of the extract.
					 slice.index -= recodeSet.indexStart;
					 if (slice.TimeStampNew.HasValue)
					 {// TimeStamp == null if we are a bframe, thus we are not here...

						 if (slice.TimeStampNew.Value < recodeSet.timeStart)
							 throw new Exception("GenericRecodeWRC.Recode: Offset time stamping error");

						 // adjust time-stamp and index (offset from time start)
						 slice.TimeStampNew -= recodeSet.timeStart;

							if (timeStamp100NanoSec == ulong.MaxValue || slice.TimeStampNew.Value > timeStamp100NanoSec)
								timeStamp100NanoSec = slice.TimeStampNew.Value; // Take the value for the progress report.
					 }

					 // Put the slices in the pending Chunk buffer for overview and confirmation.
					 recodeSet.pendingChunkSlices.Add(slice);

           // position to next output slice
           recodeSet.destinationTrack.MoveNext();

           // put slice in destination track
           recodeSet.destinationTrack.SetCurrent(slice);

           recodeSet.destination.TrackDurationIn100NanoSecs += (ulong)slice.SliceDuration;

					 // move to next input slice, exit if done
					 if (!recodeSet.sourceTrack.MoveNext())
						 break;
				 }
			 }

			 // Report progress.
			 if (timeStamp100NanoSec != ulong.MaxValue)
			 {
				 float progress = (float)(((double)timeStamp100NanoSec - (double)startTime100NanoSec) / ((double)endTime100NanoSec - (double)startTime100NanoSec));
				 if (progress > 1)
					 Common.Logger.Instance.Error("[GenericRecodeWRC::Recode] Progress value [" + progress + "] mis-calculated, progress report skipped.");
				 else
					 RaiseRecodeProgressUpdate(progress, true, null);
			 }

			 prevSyncTime = syncTime;
		 }

		 if (validSyncPointsFound == false)
		 {// Nothing meaningful found to process, end now.
			 // Do not DestStream.FinalizeStream() as this will try to write and cause an exception.
			 RaiseRecodeProgressUpdate(1, false, null);
			 RaiseRecodeProgressUpdate(2, false, null);
			 return;
		 }

		 RaiseRecodeProgressUpdate(1, true, null); // All the work is done, but there may be some finalizers left.

		 // Assemble all stbl or moof boxes.
		 // Write out the mdat slice in the case of MP4 output;
		 // in the case of fragmented files (ISMV output), all moof and mdat boxes have already been written out at this point, and we 
		 // only need to write out the mfra slice, if it is needed.
		 DestStream.FinalizeStream();

		 RaiseRecodeProgressUpdate(2, true, null); // Everything is completed.
	 }

    public override void Recode() {
      GenericVideoTrack vtrack = (GenericVideoTrack)SourceStream[CodecTypes.Video, 0];
      ulong movieLenIn100NanoSecs = vtrack.TrackDurationIn100NanoSecs;
      movieLenIn100NanoSecs += 90000000;
      Recode(0, movieLenIn100NanoSecs);
    }

    public override void Recode(ulong startTime, ulong endTime)
    {
      Recode(startTime, endTime, 2);
    }
  }
}
