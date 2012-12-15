using System;
using System.Collections.Generic;
using System.Linq;

namespace Media.Formats.MP4
{
    public class MP4Utils
    {

        public static float CalculateTimeScale(MovieMetadataBox moovBox, TrackBox trackBox)
        {
            MovieHeaderBox headerBox = moovBox.MovieHeaderBox;
            ulong moovDuration = headerBox.Duration;
            uint moovTimeScale = headerBox.TimeScale;

            MediaHeaderBox mdhdBox = trackBox.MediaBox.MediaHeaderBox;
            ulong mediaDuration = mdhdBox.Duration;
            float mediaTimeScale = mdhdBox.TimeScale;

            // Note that time scales may differ between moov and each media (because sampling rate can differ?)
            moovDuration = moovDuration / moovTimeScale;
            mediaDuration = (ulong)(mediaDuration / mediaTimeScale);
            long diff = Math.Abs((long)moovDuration - (long)mediaDuration);
            if ((diff * diff) > (long)((moovDuration * moovDuration) / 100)) // must be within 1%
                throw new Exception("Media Box Header inconsistent with Track Header");

            // scale to 10,000,000 ticks per second
            mediaTimeScale /= TimeSpan.FromSeconds(1.0).Ticks;

            if (mediaTimeScale == 0)
                throw new Exception("MP4VideoTrack: media time scale is zero");

            return mediaTimeScale;
        }
    }
}
