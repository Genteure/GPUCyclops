using System;
using System.Net;

namespace Media.Formats.Generic
{
  /// <summary>
  /// TimeArithmetic
  /// In all this class definition, "Standard Unit" means a unit that is 100 NanoSecs long.
  /// All generic classes will standardize measurement of time in 100 NanoSec units.
  /// NOTE: MediaTimeUtils should only be used in code that deals with QBoxes.
  /// MediaTimeUtils involves a clock rate that can only have two values, while here
  /// the timeScale parameter can have any uint value, except zero.
  /// </summary>
	public class TimeArithmetic
  {
    public static decimal ConvertToStandardUnit(uint timeScale, decimal time)
    {
      decimal scaleFactor = (decimal)TimeSpan.TicksPerSecond / (decimal)timeScale;
      return (time * scaleFactor);
    }

    public static decimal ConvertToTimeScale(uint timeScale, decimal standardTime)
    {
      return (((decimal)timeScale * (decimal)standardTime) / (decimal)TimeSpan.TicksPerSecond);
    }
  }
}
