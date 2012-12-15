using System.Collections.Generic;
using Media.Formats.MP4;
using Media.Formats.Generic;

namespace Media.Formats.MicrosoftFMP4 {
  public class ISMVStreamWriter : MP4StreamWriter {
    public ISMVStreamWriter() {
      base.IsMediaStreamFragmented = true;
    }


    /// <summary>
    /// WriteSamples
    /// Writing out a slice of both the audio and video tracks means that the fragments are going to be interleaved in the output file.
    /// Don't call base.WriteSamples from here because at this point, both ftyp and moov boxes are already complete.
    /// </summary>
    /// <param name="sourceAudio"></param>
    /// <param name="sourceVideo"></param>
    public override void WriteSamples(IMediaTrack sourceAudio, IMediaTrack sourceVideo)
    {
      // NOTE: the sequence order of tracks is important!
      this.WriteSamples(sourceAudio, CodecTypes.Audio);
      this.WriteSamples(sourceVideo, CodecTypes.Video);
    }

    public override void WriteSamples(IEnumerable<Slice> slices, CodecTypes codecType) {
      GenericMediaTrack track = (GenericMediaTrack)this[codecType, 0];
      ISMVTrackFormat format = (ISMVTrackFormat)track.TrackFormat;
      format.CurrentFragment.Write(m_writer);
      format.CurrentFragment.CheckFilePosition(m_writer);
      foreach (Slice sample in slices) {
        m_writer.Write(sample.SliceBytes, 0, sample.SliceBytes.Length);
      }
      format.CurrentFragment = null; // release
    }


    /// <summary>
    /// FinalizeStream - override FinalizeStream in MP4StreamWriter
    /// This deals mostly with the mfra boxes (if necessary), instead of moov boxes.
    /// All moof and mdat boxes should already have been written out at this point.
    /// </summary>
    public override void FinalizeStream() {
      this.MovieFragmentRandomAccessBox = new MovieFragmentRandomAccessBox();
      this.MovieFragmentRandomAccessBox.Write(m_writer);
    }
  }
}
