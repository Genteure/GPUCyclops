using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Formats
{
  public interface IVideoTrack : IMediaTrack
  {
    Size FrameSize
    {
      get;
    }

    VideoPayloadType PayloadType
    {
      get;
    }

    bool IsAnamorphic
    {
      get;
      set;
    }
  }
}
