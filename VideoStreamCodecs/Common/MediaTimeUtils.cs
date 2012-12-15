using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media {
	public class MediaTimeUtils {

    public enum TimeUnitType
    {
      NanoSeconds,
      TenNanoSeconds,
      OneHundredNanoSeconds,
      MicroSeconds,
      MilliSeconds
    }

    private const double HIGH_CLOCK_RATE = 120.0; // this should be the only place we define these constants
    private const double LOW_CLOCK_RATE = 90.0;

    // _clockRate is limited to two values 90 and 120 and is determined from a flag (see methods below).
    // This property is very specific to the MG3500 hardware. Strictly speaking, the clock rate should only 
    // be determined by the MG3500 hardware. However, it is also used and set in HyperAssets. It is first set when
    // ingesting from a qbox source, and during DoLoad in HyperTrack. It can only be set through SetClockRate.
    // Its value is not stored directly in the HyperStore: only the flag that determines it is stored.
    // NOTE: This property is per-track, and so we now attach a MediaTimeUtils instance to each track.
    private double _clockRate = HIGH_CLOCK_RATE;

    public uint ClockTimeScale
    {
      get { return (uint)_clockRate * 1000U; }
    }

    // clock rate defaults to high
    private bool _high = true;

    private bool _clockAlreadySet = false;

    public void SetClockRate(bool high)
    {
      if (high != _high)
      {
        if (_clockAlreadySet)
          throw new Exception("MediaTimeUtils: can't set the same clock twice");
        _clockAlreadySet = true;
        _clockRate = (high ? HIGH_CLOCK_RATE : LOW_CLOCK_RATE); // this should be executed only once, or not at all
        _high = high;
      }
    }

    public bool GetClockRateFlag()
    {
      bool high = (_clockRate == HIGH_CLOCK_RATE);
      if (high != _high)
        throw new Exception("MediaTimeUtils: clock rate inconsistency");
      return _high;
    }

    /// <summary>
    /// TicksToTime
    /// One Tick == 1.0 clock cycle, so this converts from clock ticks to time in NanoSecs, TenNanoSecs, OneHundredNanoSecs, or MilliSecs.
    /// NOTE: This only works for QBox time data for which TimeScale == _clockRate * 1000.
    /// </summary>
    /// <param name="inTicks">Clock Ticks</param>
    /// <param name="inOutputUnits">Choice of which time unit to output</param>
    /// <returns></returns>
		public ulong TicksToTime(ulong inTicks, TimeUnitType inOutputUnits) {
			if (inTicks == 0) return (0);
			 double unit = 0;
			 if (inOutputUnits == TimeUnitType.NanoSeconds) unit = 1000000.0;
			 if (inOutputUnits == TimeUnitType.TenNanoSeconds) unit = 100000.0;
			 if (inOutputUnits == TimeUnitType.OneHundredNanoSeconds) unit = 10000.0;
       if (inOutputUnits == TimeUnitType.MicroSeconds) unit = 1000.0;
			 if (inOutputUnits == TimeUnitType.MilliSeconds) unit = 1.0;
			ulong ans = (ulong)(unit * ((double)inTicks / _clockRate));
			return(ans);
		}

    /// <summary>
    /// TimeToTicks
    /// The inTime parameter is a count of ticks at _clockRate.
    /// NOTE: This only works for QBox time data for which TimeScale == _clockRate * 1000.
    /// </summary>
    /// <param name="inTime">Time in NanoSecs, TenNanoSecs, OneHundredNanoSecs, or MilliSecs</param>
    /// <param name="inInputUnits">Specify unit of first parameter</param>
    /// <returns></returns>
		public ulong TimeToTicks(ulong inTime, TimeUnitType inInputUnits)
		{
			if (inTime == 0) return (0);

			double unit = 0;
      if (inInputUnits == TimeUnitType.NanoSeconds) unit = 1000000.0;
      if (inInputUnits == TimeUnitType.TenNanoSeconds) unit = 100000.0;
      if (inInputUnits == TimeUnitType.OneHundredNanoSeconds) unit = 10000.0;
      if (inInputUnits == TimeUnitType.MicroSeconds) unit = 1000.0;
      if (inInputUnits == TimeUnitType.MilliSeconds) unit = 1;

			// time = unit * ticks / inClockRate;
			// ticks = inClockRate * time / unit;

			double ticks = _clockRate * (double)inTime / unit;
			return (ulong)ticks;
		}
	}
}
