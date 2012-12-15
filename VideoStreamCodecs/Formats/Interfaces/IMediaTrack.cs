using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Media.Formats.Generic;
using Media.Formats.QBOX;

namespace Media.Formats {
	public enum SliceType {
		Unknown,
		IFrame, // this is any type of i frame
		DFrame, // this is anything but an i or b frame (d = delta)
		BFrame, // this is a b frame
		JPEG,
		AAC,
		MP4A,
		VC1,
		AVC1,
		WMA,
		SLIC,
		SPIX
	}

	public class StreamDataBlockInfo {
		public ulong StreamOffset;
		public int SliceSize;
		public decimal SliceDuration; // must be in 100 nanosec units, and be accurate

		// must be in 100 nanosec units
		public ulong? TimeStampNew;

		//public ulong TimeStamp;  // must be in 100 nanosec units
		public ulong CTS; // composition time stamp (maybe out of sequence)
		public ulong NonQuickTimeCTTS; // CTTS value that is not adjusted by subtracting the start of the stream's duration
		public int index; // maximum slices in a track is int.Max
		public SliceType SliceType;

		//public bool CheckTimeStamp(ulong previous) {
		//  decimal duration = (decimal) TimeStamp - (decimal) previous;
		//  if (Math.Abs(SliceDuration - duration) > (SliceDuration/100)) // if discrepancy is more than 1%
		//    return false;
		//  else {
		//    return true;
		//  }
		//}

		public void Copy(StreamDataBlockInfo copyFrom) {
			this.StreamOffset = copyFrom.StreamOffset;
			this.SliceSize = copyFrom.SliceSize;
			this.SliceDuration = copyFrom.SliceDuration;
			this.TimeStampNew = copyFrom.TimeStampNew;
			this.CTS = copyFrom.CTS;
			this.index = copyFrom.index;
			this.SliceType = copyFrom.SliceType;
		}

		public override string ToString() {
			return string.Format("type[{0}] index [{1}] timeStamp [{2}] CTS [{3}] streamOffset [{4}] sliceSize [{5}] sliceDuration [{6}]",
			                     SliceType, index, TimeStampNew.Value, CTS, StreamOffset, SliceSize, SliceDuration);
		}
	}

	public class Slice : StreamDataBlockInfo {
		public byte[] SliceBytes;


		public byte[] GetH264Nalu() {
			Stream rawPayload = new MemoryStream(SliceBytes);
			BinaryReader br = new BinaryReader(rawPayload);

			// first, determine actual length of NALU (without trailing bytes)
			int totalSize = SliceBytes.Length;
			int strippedCount = 0;
			while (totalSize > 4) {
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
			totalSize = SliceBytes.Length;
			int offset = 0;
			int naluCount = 0;
			while (totalSize > 4) {
				ulong naluLen = QBox.BE32(br.ReadUInt32());
				totalSize -= 4;
				if (naluLen > 0UL) {
					int readLen = (int)naluLen;
					outBytes[offset + 3] = (byte)1; // assume that outBytes[offset] to outBytes[offset + 2] are zero.
					offset += 4;
					rawPayload.Read(outBytes, offset, readLen);
					offset += readLen;
					totalSize -= readLen;
				} else naluLen = 0; // debugging break point
				naluCount++;
			} // end of while

			return outBytes;
		}


	}

	public delegate void SampleHandler(Slice sample);

	public delegate int NextBlock(int sliceIndex);

	public delegate int SlicePutRequest(int sliceIndex);


	public interface IMediaTrack : IEnumerable<Slice> {
		PerTrackCacheManager CacheMgr { get; set; }

		IMediaStream ParentStream { get; set; }

		List<StreamDataBlockInfo> SampleStreamLocations { get; }

		ITrackFormat TrackFormat { get; set; }

		// This allows the user to index into a virtual series of fragments when actually there is
		// only one CurrentFragment at a time (caching or not, but without caching random access of 
		// fragments is nt possible). CurrentFragment is declared in GenericTrackFormat.
		// NOTE: not all media are fragmented.
		IEnumerable<IFragment> Fragments { get; }

		int TrackID { get; }

		Codec Codec { get; }

		// how long in time this track is, 100 nanosec units
		ulong TrackDurationIn100NanoSecs { get; set; }

		// CurrentStartIndex is the start index of the currently active SampleStreamLocations
		int CurrentStartIndex { get; }

		int BlockSize { get; }

		bool HasIFrameBoxes { get; }

		/// <summary>
		/// IEnumerable
		/// </summary>
		/// <param name="SampleIndex">Starts from 0, can increase indefinitely</param>
		/// <returns></returns>
		Slice this[int SampleIndex] { get; set; }

		Slice GetSample(StreamDataBlockInfo SampleInfo);

		/// <summary>
		/// PutSample
		/// This is only used when caching is enabled.
		/// Slices are placed in the destination track in the order they are put.
		/// FIXME: In case the writes are non-sequential, gaps must be filled with zero size but non-zero duration slices.
		/// </summary>
		/// <param name="sample"></param>
		void PutSample(Slice sample);

		/// <summary>
		/// PrepareSampleWriting
		/// If this is a destination track, this needs to be called to initialize moov box structure.
		/// Derived classes implement this method, but still needs to call this base method.
		/// </summary>
		/// <param name="sourceTrack"></param>
		void PrepareSampleWriting(IMediaTrack sourceTrack, ref ulong currMdatOffset);

		/// <summary>
		/// PrepareSampleReading
		/// If what the time span the user is asking for is beyond the contents of the track, this method returns false.
		/// </summary>
		/// <param name="inStartSampleTime">in milliseconds</param>
		/// <param name="inEndSampleTime">in milliseconds</param>
		/// <returns>Returns false when inStartSampleTime is beyond duration of fragment/stream.</returns>
		bool PrepareSampleReading(UInt64 inStartSampleTime, UInt64 inEndSampleTime);

		bool PrepareSampleReading(int inStartSampleIndex, int inEndSampleIndex);

		// Sample Event
		event SampleHandler SampleAvailable;

		// a block maybe a fragment (if track is fragmented)
		// NextBlock looks for the block in which the requested slice index resides.
		// The index is converted to a time stamp by cache manager using track heuristics.
		event NextBlock BlockWithSlice;

		// SlicePutRequest determines whether a slice is the last in a block to be written
		// out to the destination track. If so, PerTrackCacheManager calls PrepareSampleWriting.
		event SlicePutRequest PrepareMediaHeaders;
	}
}
