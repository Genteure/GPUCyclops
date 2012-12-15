using System;
using System.Net;

namespace Media.Formats.MP4
{
    public class WaveFormatEx
    {
		 public WaveFormatEx()
		 {

		 }

        #region Data
        public short FormatTag { get; set; }
        public short Channels { get; set; }
        public int SamplesPerSec { get; set; }
        public int AvgBytesPerSec { get; set; }
        public short BlockAlign { get; set; }
        public short BitsPerSample { get; set; }
        public short Size { get; set; }
        public const uint SizeOf = 18;
        public byte[] ext { get; set; }
        #endregion Data

        public static byte[] HexString2Bytes(string hexString) {
          //check for null
          if(hexString == null) return null;
          //get length
          int len = hexString.Length;
          if (len <= 0) return null;
          if (len % 2 == 1) return null;
          int len_half = len/2;
          //create a byte array
          byte[] bs = new byte[len_half];
          try {
          //convert the hexstring to bytes
            for (int i = 0; i != len_half; i++) {
            bs[i] = (byte) Int32.Parse(hexString.Substring(i *2, 2), System.Globalization.NumberStyles.HexNumber);
            }
          } catch(Exception ex) {
            throw ex;
          }
          //return the byte array
          return bs;
        }

        private static string Bytes2HexString(byte[] inBytes) {
          if (inBytes == null) return ("");
          string ans = "";
          foreach (byte b in inBytes) {
            ans += string.Format("{0:x2}", b);
          }
          return ans;
        }

        /// <summary>
        /// Convert the data to a hex string
        /// </summary>
        /// <returns></returns>
        public string ToHexString()
        {
            string s = "";

            s += ToLittleEndianString(string.Format("{0:X4}", FormatTag));
            s += ToLittleEndianString(string.Format("{0:X4}", Channels));
            s += ToLittleEndianString(string.Format("{0:X8}", SamplesPerSec));
            s += ToLittleEndianString(string.Format("{0:X8}", AvgBytesPerSec));
            s += ToLittleEndianString(string.Format("{0:X4}", BlockAlign));
            s += ToLittleEndianString(string.Format("{0:X4}", BitsPerSample));
            s += ToLittleEndianString(string.Format("{0:X4}", Size));
            s += Bytes2HexString(ext);
            return s;
        }

        public void FromHexString(string inFormatEx) {
          FormatTag = (short)System.Convert.ToInt32(FromLittleEndianString(inFormatEx.Substring(0, 4)), 16);
          Channels = (short)System.Convert.ToInt32(FromLittleEndianString(inFormatEx.Substring(4, 4)), 16);
          SamplesPerSec = (int)System.Convert.ToInt32(FromLittleEndianString(inFormatEx.Substring(8, 8)), 16);
          AvgBytesPerSec = (int)System.Convert.ToInt32(FromLittleEndianString(inFormatEx.Substring(16, 8)), 16);
          BlockAlign = (short)System.Convert.ToInt32(FromLittleEndianString(inFormatEx.Substring(24, 4)), 16);
          BitsPerSample = (short)System.Convert.ToInt32(FromLittleEndianString(inFormatEx.Substring(28, 4)), 16);
          Size = (short)System.Convert.ToInt32(FromLittleEndianString(inFormatEx.Substring(32, 4)), 16);
          ext = HexString2Bytes(inFormatEx.Substring(36));
        }

        /// <summary>
        /// Set the data from a byte array (usually read from a file)
        /// </summary>
        /// <param name="byteArray"></param>
        public void SetFromByteArray(byte[] byteArray)
        {
            if ((byteArray.Length + 2) < SizeOf)
            {
                throw new ArgumentException("Byte array is too small");
            }

            FormatTag = BitConverter.ToInt16(byteArray, 0);
            Channels = BitConverter.ToInt16(byteArray, 2);
            SamplesPerSec = BitConverter.ToInt32(byteArray, 4);
            AvgBytesPerSec = BitConverter.ToInt32(byteArray, 8);
            BlockAlign = BitConverter.ToInt16(byteArray, 12);
            BitsPerSample = BitConverter.ToInt16(byteArray, 14);
            if (byteArray.Length >= SizeOf)
            {
                Size = BitConverter.ToInt16(byteArray, 16);
            }
            else
            {
                Size = 0;
            }

            if (byteArray.Length > WaveFormatEx.SizeOf)
            {
                ext = new byte[byteArray.Length - WaveFormatEx.SizeOf];
                Array.Copy(byteArray, (int)WaveFormatEx.SizeOf, ext, 0, ext.Length);
            }
            else
            {
                ext = null;
            }
        }

        public static string FromLittleEndianString(string littleEndianString) {
            if (littleEndianString == null) { return ""; }

            char[] littleEndianChars = littleEndianString.ToCharArray();

            // Guard
            if (littleEndianChars.Length % 2 != 0) { return ""; }

            int i, ai, bi, ci, di;
            char a, b, c, d;
            for (i = 0; i < littleEndianChars.Length / 2; i += 2)
            {
                // front byte
                ai = i;
                bi = i + 1;

                // back byte
                ci = littleEndianChars.Length - 2 - i;
                di = littleEndianChars.Length - 1 - i;

                a = littleEndianChars[ai];
                b = littleEndianChars[bi];
                c = littleEndianChars[ci];
                d = littleEndianChars[di];

                littleEndianChars[ci] = a;
                littleEndianChars[di] = b;
                littleEndianChars[ai] = c;
                littleEndianChars[bi] = d;
            }

            return new string(littleEndianChars);
        }

        /// <summary>
        /// Convert a BigEndian string to a LittleEndian string
        /// </summary>
        /// <param name="bigEndianString"></param>
        /// <returns></returns>
        public static string ToLittleEndianString(string bigEndianString)
        {
            if (bigEndianString == null) { return ""; }

            char[] bigEndianChars = bigEndianString.ToCharArray();

            // Guard
            if (bigEndianChars.Length % 2 != 0) { return ""; }

            int i, ai, bi, ci, di;
            char a, b, c, d;
            for (i = 0; i < bigEndianChars.Length / 2; i += 2)
            {
                // front byte
                ai = i;
                bi = i + 1;

                // back byte
                ci = bigEndianChars.Length - 2 - i;
                di = bigEndianChars.Length - 1 - i;

                a = bigEndianChars[ai];
                b = bigEndianChars[bi];
                c = bigEndianChars[ci];
                d = bigEndianChars[di];

                bigEndianChars[ci] = a;
                bigEndianChars[di] = b;
                bigEndianChars[ai] = c;
                bigEndianChars[bi] = d;
            }

            return new string(bigEndianChars);
        }

        /// <summary>
        /// Ouput the data into a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            char[] rawData = new char[18];
            BitConverter.GetBytes(FormatTag).CopyTo(rawData, 0);
            BitConverter.GetBytes(Channels).CopyTo(rawData, 2);
            BitConverter.GetBytes(SamplesPerSec).CopyTo(rawData, 4);
            BitConverter.GetBytes(AvgBytesPerSec).CopyTo(rawData, 8);
            BitConverter.GetBytes(BlockAlign).CopyTo(rawData, 12);
            BitConverter.GetBytes(BitsPerSample).CopyTo(rawData, 14);
            BitConverter.GetBytes(Size).CopyTo(rawData, 16);
            return new string(rawData);
        }

        readonly ulong oneSecondTicks = (ulong)TimeSpan.FromSeconds(1.0).Ticks;

        /// <summary>
        /// Calculate the duration of sourceAudio based on the size of the buffer
        /// </summary>
        /// <param name="cbAudioDataSize"></param>
        /// <returns></returns>
        public Int64 AudioDurationFromBufferSize(UInt32 cbAudioDataSize)
        {
            if (AvgBytesPerSec == 0)
            {
                return 0;
            }

            return (Int64)(cbAudioDataSize * oneSecondTicks / (uint)AvgBytesPerSec);
        }

        /// <summary>
        /// Calculate the buffer size necessary for a duration of sourceAudio
        /// </summary>
        /// <param name="duration"></param>
        /// <returns></returns>
        public Int64 BufferSizeFromAudioDuration(Int64 duration)
        {
          Int64 size = duration * AvgBytesPerSec / (uint)oneSecondTicks;
            UInt32 remainder = (UInt32)(size % BlockAlign);
            if (remainder != 0)
            {
                size += BlockAlign - remainder;
            }

            return size;
        }

        /// <summary>
        /// Validate that the Wave format is consistent.
        /// </summary>
        public void ValidateWaveFormat()
        {
            //if (FormatTag != FormatPCM && FormatTag != FormatWmaPro)
            //{
            //    throw new ArgumentException("Only PCM or WmaPro format is supported");
            //}

            if (Channels != 1 && Channels != 2)
            {
                throw new ArgumentException("Only 1 or 2 channels are supported");
            }

            if (BitsPerSample != 8 && BitsPerSample != 16 && BitsPerSample != 0)
            {
                throw new ArgumentException("Only 8 or 16 bit samples are supported");
            }

            if (BlockAlign < 1)
              throw new ArgumentException("BlockAlign cannot be zero or less");
            if (BlockAlign > 1)
            {
              if (BlockAlign != Channels * (BitsPerSample / 8))
              {
                throw new ArgumentException("Block Alignment is incorrect");
              }

              if (SamplesPerSec > (UInt32.MaxValue / BlockAlign))
              {
                throw new ArgumentException("SamplesPerSec overflows");
              }

              if (AvgBytesPerSec != SamplesPerSec * BlockAlign)
              {
					 //throw new ArgumentException("AvgBytesPerSec is wrong");
              }
            }

            if (ext != null && (Size != ext.Length))
            {
              throw new ArgumentException("Size is not correct");
            }
        }

        public const Int16 FormatPCM = 1;
        public const Int16 FormatIEEE = 3;
				public const Int16 FormatRAWAAC1 = 0x0ff;
        public const Int16 FormatWmaPro = 0x0162;
        public const Int16 FormatWmaLossless = 0x0163;
        public const Int16 FormatMpegHEAAC = 0x1610; // see HEAACWaveInfo below
    }



    public class HEAACWaveInfo : WaveFormatEx
    {
      public Int16 PayloadType { get; set; }
      public Int16 AudioProfileLevelIndication { get; set; }
      public Int16 StructType { get; set; }
      public Int16 Reserved1 { get; set; }
      public Int32 Reserved2 { get; set; }
      public const uint HEAACSize = 12;

      public HEAACWaveInfo()
      {
        base.FormatTag = FormatMpegHEAAC;
        base.BlockAlign = 1;
        base.Size = (short)HEAACSize;
        PayloadType = 0; // raw
        AudioProfileLevelIndication = 0; // unknown profile
        StructType = 0; // Audio Specific Config
        base.ext = new byte[HEAACSize];
        base.ext[0] = (byte)(PayloadType & 0xFF);
        base.ext[1] = (byte)(PayloadType >> 8);
        base.ext[2] = (byte)(AudioProfileLevelIndication & 0xFF);
        base.ext[3] = (byte)(AudioProfileLevelIndication >> 8);
        base.ext[4] = (byte)(StructType & 0xFF);
        base.ext[5] = (byte)(StructType >> 8);
        for (int i = 6; i < HEAACSize; i++)
          base.ext[i] = 0;
      }
    }
  }
