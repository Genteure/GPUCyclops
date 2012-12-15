using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.Formats
{
  public interface IAudioTrack : IMediaTrack
  {
    AudioPayloadType PayloadType
    {
      get;
    }

    int ChannelCount
    {
      get;
    }

    int SampleSize
    {
      get;
    }

    /// <summary>
    /// SampleRate - count of samples per second
    /// </summary>
    int SampleRate
    {
      get;
    }

  }
}
