using System;
using System.IO;
using System.Collections.Generic;
using Media.Formats.Generic;

namespace Media.Formats.JPGFileSequence {
  /// <summary>
  /// FileSequence
  /// Obviously we can't deal with audio in this format.
  /// Each jpeg image is its own file.
  /// </summary>
  public class FileSequence : GenericMediaStream {

    public string RootName = @"C:\temp\stripped8th";

    public FileSequence() {
      Stream = new MemoryStream(); // dummy
    }

    public override void Open(Stream stream) {
      // do not use stream at all (can be null)
    }

    public override void Create(Stream outStream) {
      // do not use stream at all (maybe null)
    }

    public override void Read() {
    }

    public void WriteSamples(IMediaTrack sourceTrack) {
      CodecTypes codecType = sourceTrack.Codec.CodecType;
      WriteSamples(sourceTrack, codecType);
    }

    public override void WriteSamples(IMediaTrack sourceAudio, IMediaTrack sourceVideo)
    {
      // ignore audio
      this.WriteSamples(sourceVideo);
    }

    public override void WriteSamples(IEnumerable<Slice> slices, CodecTypes codecType) {
      if (codecType == CodecTypes.Audio) {
        // ignore audio
      } else if (codecType == CodecTypes.Video) {
        int i = 0;
        foreach (Slice sample in slices) {
          string fileName = string.Format("{0}{1}{2}", RootName, i, ".jpg");
          FileStream stream = File.Create(fileName);
          stream.Write(sample.SliceBytes, 12, sample.SliceBytes.Length - 12);
          stream.Close();
          i++;
        }
      } else throw new Exception("WriteSamples: unknown codec type");
    }

    public override void FinalizeStream() {
    }
  }
}
