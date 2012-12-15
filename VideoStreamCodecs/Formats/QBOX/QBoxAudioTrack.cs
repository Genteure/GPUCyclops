using System.Collections;
using Media.Formats.Generic;

namespace Media.Formats.QBOX
{

  public class ADTSDataBlockInfo : Slice
  {
    public byte[] PESandADTSHeaders;
  }

  /// <summary>
  /// QBoxAudioTrack
  /// The whole purpose of this derived class is to implement a special case of GetSample for when
  /// there is an ADTS header that needs to be prepended to the data.
  /// </summary>
  public class QBoxAudioTrack : GenericAudioTrack
  {

    public QBoxAudioTrack(QBoxTrackFormat format, QBoxStream stream)
      : base(format, stream)
    {
    }

    public override Slice GetSample(StreamDataBlockInfo SampleInfo)
    {
      ADTSDataBlockInfo adtsInfo = (ADTSDataBlockInfo)SampleInfo;
      Slice ans = new Slice();
      ans.Copy(SampleInfo);
#if ADTS
      if (adtsInfo.PESandADTSHeaders == null) // ADTS header may be absent, in which case we use the normal base.GetSample
      {
        return base.GetSample(SampleInfo);
      }
      int headerCount = adtsInfo.PESandADTSHeaders.Length;
      ans.SliceBytes = new byte[adtsInfo.SliceSize]; // SampleSize has already been incremented by length of PES + ADTS header
      adtsInfo.PESandADTSHeaders.CopyTo(ans.SliceBytes, 0);
      //if (ParentStream.Stream.Position != (long)adtsInfo.StreamOffset)
      //  ParentStream.Stream.Position = (long)adtsInfo.StreamOffset; // this if statement for debugging: just to be able to put a breakpoint here
      BinaryReader reader = new BinaryReader(new MemoryStream(adtsInfo.SliceBytes));
      //ParentStream.Stream.Read(ans.SliceBytes, headerCount, adtsInfo.SliceSize - headerCount);
      reader.Read(ans.SliceBytes, headerCount, adtsInfo.SliceSize - headerCount);
#else
      ans.SliceBytes = adtsInfo.SliceBytes;
#endif
      return (ans);
    }



    //public override IEnumerator GetEnumerator()
    //{
    //  return ((IEnumerator)new QBoxTrackEnumerator(this));
    //}

  }


  /// <summary>
  /// QBoxTrackEnumerator
  /// The purpose of this whole new class derived from IGenericMediaTrackEnumerator is just to return
  /// a different value for CurrentTimeStamp for audio tracks. We half the base value.
  /// NOTE: This is just a kludge to fix the QBox audio time stamp problem. Rather than
  /// modify CurrentTimeStamp in IGenericMediaTrackEnumerator, we isolate the kludge here;
  /// that way, to put the CurrentTimeStamp to its original state, all we have to do is
  /// remove the override method QBoxAudioTrack.GetEnumerator above.
  /// GenericMediaTrack.CurrentTimeStamp is used in GenericRecodeWRC.cs in the main recoding loop.
  /// Audio in the sample video UnderWorldv2.qbox has inconsistent time values. Whereas each audio
  /// qbox itself has a duration which is double the actual duration of its payload, we half the 
  /// mClockRate to compensate for this. However, this also has an undesirable effect of making the 
  /// total duration of audio longer than video, thereby cutting the audio track midway, to exactly
  /// half of total audio track. To fix this undesirable effect, we kludge the CurrentTimeStamp 
  /// to return half its value, only for audio.
  /// Now the audio plays for as long as the video, and both video and audio are almost in sync.
  /// There is still about half a second drift towards the end.
  /// </summary>
  public class QBoxTrackEnumerator : IGenericMediaTrackEnumerator
  {
    public QBoxTrackEnumerator(IMediaTrack track) : base(track)
    {
    }

  	public override ulong? CurrentTimeStampNew {
  		get {
				if (base.CurrentTimeStampNew.HasValue == false) return (base.CurrentTimeStampNew);
  			return base.CurrentTimeStampNew.Value/2;
  		}
  	}
  }
}
