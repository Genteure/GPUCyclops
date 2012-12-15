using System.Collections.Generic;
using Media;
using Media.Formats.Generic;

namespace Media.Formats.MicrosoftFMP4 {
  /// <summary>
  /// Fragmented Track
  /// This track resides in one of the ISMV files, together with one of
  /// the video tracks.
  /// </summary>
  /// 
  public class ISMVTrack : GenericMediaTrack
  {
    public ISMVTrack()
    {
    }
  }
}
