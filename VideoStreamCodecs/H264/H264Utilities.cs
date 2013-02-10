// NOTE: This file is also in the MP4Media project.
// That's why the conditional compile logic was added below.
using System;
using System.IO;
using System.Net;
using System.Globalization;
using Common;
using Media.Formats.Generic;
using Media.Formats;

namespace Media.H264
{
  public class H264Utilities {
    // Methods

		// for the MediaElement, it likes the codec data on the first sample after a seek.
		// thus this routine is nice for adding this on-the-fly.
		public static byte[] H264AddCodecDataToPayload(byte[] inPayload, string inCodecPrivateData) {
			Stream stream = new MemoryStream();
			byte[] codecBytes = HexStringToBytes(inCodecPrivateData);
			byte[] delim = new byte[] {0, 0, 0, 1};
			stream.Write(delim, 0, delim.Length);

			int index = 4;
			while (index < codecBytes.Length) {
				if (((codecBytes[index] == 0) && (codecBytes[index + 1] == 0)) && ((codecBytes[index + 2] == 0) && (1 == codecBytes[index + 3]))) {
					break;
				}
				stream.WriteByte(codecBytes[index++]);
			}
			index += 4;
			stream.WriteByte(0);
			stream.WriteByte(0);
			stream.WriteByte(0);
			stream.WriteByte(1);
			stream.Write(codecBytes, index, codecBytes.Length - index);

			stream.Write(inPayload, 0, inPayload.Length);

			byte[] ans = new byte[stream.Length];
			stream.Position = 0;
			stream.Read(ans, 0, (int)stream.Length);
			return (ans);
		}

    /// <summary>
    /// H264Stream
    /// Used by Players to convert from blocks (which has byte counts) to a bit stream delimited by 001 or 0001.
    /// </summary>
    /// <param name="inFirst">Is this the first block? If so, insert SPS and PPS (CodecPrivateData) into the bit stream.</param>
    /// <param name="inCodecPrivateData">CodecPrivateData taken from containing MP4.</param>
    /// <param name="inStream">Input block stream.</param>
    /// <param name="inStart">Starting location in the input block stream.</param>
    /// <param name="inSize">Count of bytes to process.</param>
    /// <returns></returns>
    public static Stream H264Stream(bool inFirst, String inCodecPrivateData, Stream inStream, uint inStart, uint inSize) {
      Stream stream = new MemoryStream();
      // ag ag = A_0 as ag;
      //string methodName = "ProcessH264";
      if (inFirst) {
          string str2 = inCodecPrivateData; // ag.c()[MediaStreamAttributeKeys.CodecPrivateData];
          byte[] buffer = HexStringToBytes(str2);
          if (buffer != null) {
            int index = 0;
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.WriteByte(1);
            //          Tracing.Trace(a, methodName, TraceArea.MediaStreamSource, TraceLevel.Verbose, "Appending sps & pps for {0}", new object[] { A_2.b() });
            index = 4;
            while (index < buffer.Length) {
              if (((buffer[index] == 0) && (buffer[index + 1] == 0)) && ((buffer[index + 2] == 0) && (1 == buffer[index + 3]))) {
                  break;
              }
              stream.WriteByte(buffer[index++]);
            }
            index += 4;
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.WriteByte(0);
            stream.WriteByte(1);
            stream.Write(buffer, index, buffer.Length - index);
          }
      }

      ulong num2 = (ulong)inSize;
      inStream.Seek((long)inStart, SeekOrigin.Begin);
      BinaryReader reader = new BinaryReader(inStream);
      // FIXME: Need to check for occurrence of 001 within data. If found replace by data replacement token that prevents
      // false detection of start token. As it stands, there is a possibility of a false token in the data that could make the output
      // invalid.
      while (num2 > 4L) {
          ulong num3 = CPUGenderDependencies.UINT(reader.ReadUInt32());
          num2 -= ((ulong)4L) + num3;
          stream.WriteByte(0);
          stream.WriteByte(0);
          stream.WriteByte(0);
          stream.WriteByte(1);
          int count = (int)num3;
          stream.Write(reader.ReadBytes(count), 0, count);
      }

      return stream;
    }

    private static void OverwriteMarker(byte[] inBuf, int i, ref int markerIndex, ref int count)
    {
      if (markerIndex > -1)
      {
        count -= 4;
        byte[] bytes = BitConverter.GetBytes(count);
        if (BitConverter.IsLittleEndian)
        {
          byte tmp;
          tmp = bytes[0];
          bytes[0] = bytes[3];
          bytes[3] = tmp;
          tmp = bytes[1];
          bytes[1] = bytes[2];
          bytes[2] = tmp;
        }
        bytes.CopyTo(inBuf, markerIndex);
      }
      markerIndex = i - 3;
      count = 0;
    }

    // Inverse of H264Stream
    public static void ToH264Block(byte[] inBuf)
    {
      int size = inBuf.Length;
      uint totalSize = 0;

      // Test whether inBuf is already in block format, exit if it is.
      BinaryReader reader = new BinaryReader(new MemoryStream(inBuf));
      if (IsNALBlockFormat(reader, (uint)size, out totalSize))
        return;

      int count = 0;
      int markerIndex = -1;
      int i = 0; // index
      int state = 0;

      while (i < size)
      {
        switch (state)
        {
          case 0: // start state
            if (inBuf[i] == 0)
            {
              state = 1;
            }
            else state = 4;
            break;
          case 1:
            if (inBuf[i] == 0)
            {
              state = 2;
            }
            else state = 4;
            break;
          case 2:
            if (inBuf[i] == 0)
            {
              state = 3;
            }
            else if (inBuf[i] == 1)
            {
              OverwriteMarker(inBuf, i, ref markerIndex, ref count);
              state = 4;
            }
            else state = 4;
            break;
          case 3:
            if (inBuf[i] == 1)
            {
              OverwriteMarker(inBuf, i, ref markerIndex, ref count);
              state = 4;
            }
            else state = 4;
            break;
          case 4:
            if (inBuf[i] == 0)
            {
              state = 1;
            }
            else state = 4;
            break;
          default:
            break;
        }

        // always take next byte
        i++;

        if ((i == size) && (markerIndex > -1))
        {
          byte[] bytes = BitConverter.GetBytes(count);
          if (BitConverter.IsLittleEndian)
          {
            byte tmp;
            tmp = bytes[0];
            bytes[0] = bytes[3];
            bytes[3] = tmp;
            tmp = bytes[1];
            bytes[1] = bytes[2];
            bytes[2] = tmp;
          }
          bytes.CopyTo(inBuf, markerIndex);
        }

        // increment byte count for all cases except for last marker
        count++;
      }
    }

      /// <summary>
      /// IsBitStream
      /// Determine whether the input stream contains H264 bit stream (as opposed to blocks).
      /// </summary>
      /// <param name="mdatStream">Input H264 stream.</param>
      /// <returns></returns>
      public static bool IsBitStream(Stream mdatStream)
      {
          BinaryReader reader = new BinaryReader(mdatStream);
          uint first4ByteValue = CPUGenderDependencies.UINT(reader.ReadUInt32());
          if ((first4ByteValue < 512) && (first4ByteValue > 255) && CheckNALUType((byte)first4ByteValue, (byte)0)) // first 3 byte pattern is 001
          {
              return true; // H264 bit stream
          }

          return false;
      }


      /// <summary>
      /// IsNALBlockFormat
      /// Determine whether _contents have byte counts.
      /// </summary>
      /// <returns>True if byte counts are found.</returns>
      public static bool IsNALBlockFormat(BinaryReader reader, uint size, out uint totalSize)
      {
          uint count = CPUGenderDependencies.UINT(reader.ReadUInt32());
          totalSize = size; // size is unchanged
          if (count == 0)
          {
            reader.BaseStream.Position -= 4;
            return false;
          }
          else if (count == 1)
            return false;  // count cannot be 1
          else if (count > size)
          {
            reader.BaseStream.Position -= 4;
            return false;
          }
          byte b = reader.ReadByte();
          byte c = reader.ReadByte();
          if (!CheckNALUType(b, c))
          {
              reader.BaseStream.Position -= 6;
              return false;
          }

          reader.BaseStream.Position += ((int)(count - 2)); // ignore all subsequent bytes
          totalSize = size - count;
          return true;
      }


      /// <summary>
      /// Is264Data
      /// Determine whether input stream contains H264.
      /// </summary>
      /// <param name="mdatStream">Input stream</param>
      /// <param name="fileOffset">Location in stream to start</param>
      /// <param name="inSize">Count of bytes to read</param>
      /// <returns></returns>
      public static bool IsH264Data(Stream mdatStream, long fileOffset, uint inSize, out bool isByteStream)
      {
          uint num2 = inSize;
          int patternCount = 0;
          mdatStream.Position = fileOffset;
          bool patternFound = false;
          BinaryReader reader = new BinaryReader(mdatStream);
          if (!IsNALBlockFormat(reader, num2, out num2))
          {
                int zeroCount = 0;
                byte currByte;
                int k;
                // must be byte stream format, search for first delimiter
                for (k = 0; k < num2; k++)
                {
                    currByte = reader.ReadByte();
                    if (currByte == 0)
                    {
                        zeroCount++;
                    }
                    else if ((currByte == 1) && (zeroCount == 2))
                    {
                        patternCount++;
                        patternFound = true;
                    }
                    else
                    {
                        zeroCount = 0;
                    }
                }
                num2 -= (uint)k;

                zeroCount = 0;
                //bool b;
                isByteStream = patternFound;
                //if (!patternFound)
                    //b = true;   // for debugging only
                return (patternFound);
          }
          isByteStream = false;
          return true;
      }


      /// <summary>
      /// HexStringToBytes
      /// Convert a string of Hex charaters to its byte array value.
      /// </summary>
      /// <param name="hexStr">Input string of hexadecimal numbers</param>
      /// <returns></returns>
      public static byte[] HexStringToBytes(string hexStr)
      {
          if (string.IsNullOrEmpty(hexStr)) // cannot be empty string
              return null;
          if ((hexStr.Length % 2) != 0) // must have even length (2 chars for each byte)
              return null;
          int len = hexStr.Length / 2;
          byte[] buf = new byte[len];
          for (int i = 0; i < len; i++)
          {
              if (!byte.TryParse(hexStr.Substring(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out buf[i]))
                  return null;
          }

          return buf;
      }

      static byte previousC = 0;

      const byte NALUTypeMask = 0x1F;
      const byte RefIDCMask = 0x60;
      const byte MSBMask = 0x80;

      /// <summary>
      /// CheckNALUType
      /// </summary>
      /// <param name="b">First byte</param>
      /// <param name="c">Second byte.</param>
      /// <returns>Returns true if NALU is OK.</returns>
      static bool CheckNALUType(byte b, byte c)
      {
			//System.Collections.BitArray a = new System.Collections.BitArray(new byte[] { b, c });
			//for (int i = 0; i < a.Length; i++)
			//{
			//   var gg = a.Get(i);
			//}

          byte NALUType = (byte)(NALUTypeMask & b);
          byte NALRefIDC = (byte)(RefIDCMask & b);
          if ((MSBMask & b) != 0)
              return false;  // MSB must be zero
          switch (NALUType)
          {
              case 1:
                  //if (NALRefIDC == 0)
                  //    return false;
                  if (previousC != 0)
                  {
                      if (((previousC != 48) || (c != 154)) && ((previousC != 48) || (c != 155)) && ((previousC != 80) || (c != 158)) && ((previousC != 80) || (c != 159)))
                          return false;
                  }
                  previousC = 0;
                  break;
              case 2:
                  break;
              case 3:
                  break;
              case 4:
                  break;
              case 5:   // IDR
                  if (NALRefIDC == 0)
                      return false;
                  if ((previousC != 0) && (previousC != 16))
                      return false;
                  previousC = 0;
                  break;
              case 6:   // supplemental enhancement information (SEI)
                  break;
              case 7:   // sequence parameter set (SPS)
              case 8:   // picture parameter set (PPS)
                  if (NALRefIDC != 0)
                      return false;
                  break;
              case 9:   // access unit delimiter
                  if (NALRefIDC != 0)
                      return false;
                  previousC = c;
                  break;
              case 10:  // end of sequence
                  if (NALRefIDC != 0)
                      return false;
                  break;
              case 11:  // end of stream
                  if (NALRefIDC != 0)
                      return false;
                  break;
              case 12:  // filler
                  break;
              default:
                  return false;
          }
          return true;
      }


      /// <summary>
      /// InsertAccessUnitDelimiter
      /// NALU type 9 may be necessary for MediaElement to play a video stream.
      /// </summary>
      /// <param name="inStream">Input stream with Position set to beginning of a H264 block.</param>
      public static Stream InsertAccessUnitDelimiter(BinaryReader inStream, long offset, uint size)
      {
          inStream.BaseStream.Position = offset;

          uint count = 0;
          byte[] bytes = new byte[4];
          byte[] copyBytes = new byte[4];
          byte[] byteCount2 = new byte[4];
          byteCount2[0] = 0;
          byteCount2[1] = 0;
          byteCount2[2] = 0;
          byteCount2[3] = 2;
          byte NALByte;
          Stream stream = new MemoryStream();
          for (; inStream.BaseStream.Position < (offset + size) ; )
          {
              count = CPUGenderDependencies.UINT(inStream.ReadUInt32());
              if ((count == 0) || (count > (inStream.BaseStream.Length - inStream.BaseStream.Position)))
                  return stream;
              NALByte = inStream.ReadByte();
              byte NALUType = (byte)(NALUTypeMask & NALByte);
              byte NALRefIDC = (byte)(RefIDCMask & NALByte);
              if ((MSBMask & NALByte) != 0)
                  return stream;  // MSB must be zero
              switch (NALUType)
              {
                  case 1:
                      if (NALRefIDC == 0)
                          return stream;
                      stream.Write(byteCount2, 0, 4);
                      stream.WriteByte((byte)9);
                      stream.WriteByte((byte)48);
                      break;
                  case 2:
                  case 3:
                  case 4:
                      break; // FIXME: just pass 2, 3, and 4 through?
                  case 5:   // IDR
                      if (NALRefIDC == 0)
                          return stream;
                      stream.Write(byteCount2, 0, 4);
                      stream.WriteByte((byte)9);
                      stream.WriteByte((byte)16);
                      break;
                  case 6:   // supplemental enhancement information (SEI)
                  case 7:   // sequence parameter set (SPS)
                  case 8:   // picture parameter set (PPS)
                      if (NALRefIDC != 0)
                          return stream;
                      break;
                  case 9:   // access unit delimiter
                      // This is what we're trying to insert.
                      // It's already there, so
                      // use input stream as output, and we're done.
                      return inStream.BaseStream;
                  case 10:  // end of sequence
                      if (NALRefIDC != 0)
                          return stream;
                      break;
                  case 11:  // end of stream
                      if (NALRefIDC != 0)
                          return stream;
                      break;
                  case 12:  // filler
                      break;
                  default:
                      return stream;
              }
              stream.Write(copyBytes, 0, 4);
              stream.WriteByte(NALByte);
              stream.Write(inStream.ReadBytes((int)count - 5), 0, (int)count - 5);
          }
          return stream;
      }

		  public static SliceType GetSliceType(Slice inSlice) {
				// tryout
				byte[] payload = new byte[inSlice.SliceBytes.Length];
				inSlice.SliceBytes.CopyTo(payload, 0);
				H264Utilities.ToH264Block(payload);
				SliceType sliceT = H264Utilities.GetSliceTypeFromH264Payload(payload);
		  	return (sliceT);
		  }


      public static SliceType GetSliceTypeFromH264Payload(byte[] payload)
      {
        int countToZero = payload.Length;
        BinaryReader reader = new BinaryReader(new MemoryStream(payload));
        while (countToZero > 4)
        {
					ulong naluLen = BitReader.GetUIntValue(reader.ReadUInt32());
          long nextPos = reader.BaseStream.Position + (long)naluLen;


					//Table 7-1 – NAL unit type codes
					//nal_unit_type Content of NAL unit and RBSP syntax structure C
					//0 Unspecified
					//1 Coded slice of a non-IDR picture
					//  slice_layer_without_partitioning_rbsp( )
					//2 Coded slice data partition A
					//  slice_data_partition_a_layer_rbsp( )
					//3 Coded slice data partition B
					//  slice_data_partition_b_layer_rbsp( )
					//4 Coded slice data partition C
					//  slice_data_partition_c_layer_rbsp( )
					//5 Coded slice of an IDR picture
					//  slice_layer_without_partitioning_rbsp( )
					//6 Supplemental enhancement information (SEI)
					//  sei_rbsp( )
					//7 Sequence parameter set
					//  seq_parameter_set_rbsp( )
					//8 Picture parameter set
					//  pic_parameter_set_rbsp( )
					//9 Access unit delimiter
					//  access_unit_delimiter_rbsp( )
					//10 End of sequence
					//  end_of_seq_rbsp( )
					//11 End of stream
					//  end_of_stream_rbsp( )
					//12 Filler data
					//  filler_data_rbsp( )
					//13..23 Reserved
					//24..31 Unspecified

					uint typ = reader.ReadByte() & 0x1Fu;


          if ((naluLen > (ulong)countToZero) || (naluLen < 2))
            return SliceType.Unknown;

          // NALU type 5 is IDR picture
          if ((typ == 5u) || (typ == 1u)) {
						// when the type == 5 its an IDR picture, now slice_type shall be equal to
						// 2, 4, 7, or 9 (I or SI);
						// slice_type specified the coding type of the slice according to Table 7-6: e.g. P, B, I, SP, SI
						// 
						// nalu header:
						// first_mb_in_slice = get_ue_golomb
						// slice_type = get_ue_golomb
						//

						// http://www.iitk.ac.in/mwn/vaibhav/Vaibhav%20Sharma_files/h.264_standard_document.pdf
						//Table 7-3 – Name association to slice_type
						//0 P (P slice)
						//1 B (B slice)
						//2 I (I slice)
						//3 SP (SP slice)
						//4 SI (SI slice)
						//5 P (P slice)
						//6 B (B slice)
						//7 I (I slice)
						//8 SP (SP slice)
						//9 SI (SI slice)

						BitReader br = new BitReader(reader.BaseStream);
						uint first_mb_in_slice = br.DecodeUnsignedExpGolomb();
						uint slice_type = br.DecodeUnsignedExpGolomb();
						uint pic_parameter_set_id = br.DecodeUnsignedExpGolomb();
						uint frame_num = br.DecodeUnsignedExpGolomb();

						if (typ == 5u) return (SliceType.IFrame);
						if (slice_type == 1 || slice_type == 6) return (SliceType.BFrame);
          	return (SliceType.DFrame);
          }

          countToZero -= ((int)naluLen + 4);
          reader.BaseStream.Position = nextPos;
        }
        return SliceType.Unknown;
      }
  }
}
