//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//namespace Media.Formats.QBOX
//{
//  public class QBoxUtils
//  {
//    // static methods for dealing with qbox files

//    // Search for a particular qbox
//    // Given a cts value and track ID, search for the only qbox with these properties.
//    // This only works if input stream is seekable.
//    // For speed, use binary search through the length of the stream.
//    // Use length of every qbox to jump from one qbox to another, reading only the cts and trackID values.
//    public static void WarpToQBox(BinaryReader br, ulong ctsToSearch, int trackID)
//    {
//      if (!br.BaseStream.CanSeek)
//        throw new Exception("WarpToQBox: can't use this method if the input stream is non-seekable.");

//      // Although the video mSampleCTS value is not monotonically increasing, the amount by which it deviates
//      // from being monotonic is really small. In this algorithm, we assume that it is monotonic (although it's not).
//      // We avoid recursion.
//      bool found = false;
//      long pos1 = 0L;
//      long pos2 = br.BaseStream.Length;
//      uint boxSize = 1024;

//      while ((!found) && (pos2 - pos1) > 2*boxSize)
//      {
//        long tryPos = (pos1 + pos2) >> 1; // divide by 2
//        br.BaseStream.Position = tryPos;
//        ulong cts = ScanForFirstQBoxWithTrackID(br, trackID, out boxSize);
//        if (cts == ulong.MaxValue)
//          break;
//        if (cts < ctsToSearch)
//          pos1 = tryPos;
//        else if (cts > ctsToSearch)
//          pos2 = tryPos;
//        else // equal
//          found = true;
//      }

//      // leave the stream position pointing to the beginning of the qbox with the closest cts
//      br.BaseStream.Position -= 8L;
//    }

//    // Look for the first QBox with given track ID. Output its mSampleCTS value.
//    // Assumption: Stream.Position is set by caller.
//    static ulong ScanForFirstQBoxWithTrackID(BinaryReader br, int trackID, out uint boxSize)
//    {
//      if (!br.BaseStream.CanSeek)
//        throw new Exception("ScanForFirstQBoxWithTrackID: can't use this method if the input stream is non-seekable.");

//      boxSize = 0;
//      try
//      {
//        boxSize = SearchForAnyQBox(br);
//      }
//      catch (EndOfStreamException eos)
//      {
//        return ulong.MaxValue; // signal end of stream
//      }
//      catch (Exception ex)
//      {
//        throw ex;
//      }

//      ulong cts = ulong.MaxValue;
//      int id = -1;

//      while (id != trackID)
//      {
//        try
//        {
//          int byteCount = GetCTSAndTrackID(br, out cts, out id);
//          br.BaseStream.Position += boxSize - byteCount - 8L;
//          boxSize = GetSizeOfCurrentQBox(br);
//        }
//        catch (EndOfStreamException eos)
//        {
//          return ulong.MaxValue; // signal end of stream
//        }
//        catch (Exception ex)
//        {
//          throw ex;
//        }
//      }

//      return cts;
//    }

//    /// <summary>
//    /// SearchForAnyQBox
//    /// Position the stream so that it starts at a qbox right after the qbox type signature.
//    /// QBOX_TYPE = 0x71626f78
//    /// Returns: size of qbox.
//    /// </summary>
//    public static uint SearchForAnyQBox(BinaryReader br)
//    {
//      if (!br.BaseStream.CanSeek)
//        throw new Exception("SearchForAnyQBox: can't use this method if the input stream is non-seekable.");

//      uint qboxSize = 0;
//      try
//      {
//        int state = 0;
//        byte b;
//        int i = 0;
//        byte[] circularByteBuf = new byte[8];
//        while (true)
//        {
//          b = br.ReadByte();
//          switch (state)
//          {
//            case 0:
//              if (b == 0x71) // the letter Q
//                state++;
//              break;
//            case 1:
//              if (b == 0x62) // the letter B
//                state++;
//              else state = 0;
//              break;
//            case 2:
//              if (b == 0x6f) // the letter O
//                state++;
//              else state = 0;
//              break;
//            case 3:
//              if (b == 0x78) // the letter X
//                state++;
//              else state = 0;
//              break;
//            default:
//              break; // done
//          }
//          circularByteBuf[i] = b;
//          i = (i + 1) % 8;
//          if (state == 4)
//            break; // at this point, i should point to first byte of size
//        } // end of while true
//        byte[] uintBytes = new byte[4];
//        for (int j = 0; j < 4; j++)
//        {
//          uintBytes[j] = circularByteBuf[i];
//          i = (i + 1) % 8;
//        }
//        qboxSize = BitConverter.ToUInt32(uintBytes, 0);
//        qboxSize = (uint)QBox.BE32((ulong)qboxSize);
//      }
//      catch (Exception ex)
//      {
//        if (ex is EndOfStreamException)
//          qboxSize = 0U;
//        else throw ex;
//      }
//      return qboxSize;
//    }

//    // Assume that Stream.Position is on the byte right after QBOX signature.
//    // Return byte count (from signature to mSampleCTS info).
//    private static int GetCTSAndTrackID(BinaryReader br, out ulong mSampleCTS, out int mSampleStreamId)
//    {
//      QBoxFlags mBoxFlags = new QBoxFlags();
//      ushort mSampleStreamType;
//      ulong mSampleFlags;
//      int mHeaderSize = 0;
//      ulong mIndex;
//      ulong mTimeStamp;
//      ulong mSampleDuration;

//      mSampleCTS = 0UL;
//      mSampleStreamId = ushort.MaxValue;

//      mBoxFlags.value = (uint)QBox.BE32(br.ReadUInt32());
//      mHeaderSize += 4;
//      mSampleStreamType = QBox.BE16(br.ReadUInt16());
//      mHeaderSize += 2;
//      mSampleStreamId = QBox.BE16(br.ReadUInt16());
//      mHeaderSize += 2; // replacement for all of commented lines below
//      mSampleFlags = QBox.BE32(br.ReadUInt32());
//      mHeaderSize += 4;

//      if ((mBoxFlags.flags & QBox.QBOX_FLAGS_BOXINDEX_PRESENT) != 0U)
//      {
//        mIndex = QBox.BE64(br.ReadUInt64());
//        mHeaderSize += 8; // this is necessary because assignment above does NOT increase mHeaderSize
//      }

//      if ((mBoxFlags.flags & QBox.QBOX_FLAGS_BOXTIMESTAMP_PRESENT) != 0U)
//      {
//        mTimeStamp = QBox.BE64(br.ReadUInt64());
//        mHeaderSize += 8; // this is necessary because assignment above does NOT increase mHeaderSize
//      }

//      if ((mBoxFlags.flags & QBox.QBOX_FLAGS_BOXDURATION_PRESENT) != 0U)
//      {
//        mSampleDuration = QBox.BE64(br.ReadUInt64());
//      }

//      // version 0 only uses a single int for the CTS, version 1 uses 64 bit high/low value
//      mSampleCTS = QBox.BE32(br.ReadUInt32());
//      mHeaderSize += 4;
//      if (mBoxFlags.version == 1)
//      {
//        ulong lowWord = QBox.BE32(br.ReadUInt32());
//        mSampleCTS = (mSampleCTS << 32) + lowWord;
//        mHeaderSize += 4;
//      }

//      // adjust time stamp if 120KHz bit is ON (doesn't matter which track)
//      if ((mSampleFlags & QBox.QBOX_SAMPLE_FLAGS_120HZ_CLOCK) != 0U)
//      {
//        mSampleCTS = (mSampleCTS * QBox.BASE_TIME_SCALE) / 120000U;
//      }

//      return mHeaderSize;
//    }

//    // Get size of next qbox, verify its QBOX signature.
//    private static uint GetSizeOfCurrentQBox(BinaryReader br)
//    {
//      uint size = (uint)QBox.BE32(br.ReadUInt32());
//      uint qboxType = (uint)QBox.BE32(br.ReadUInt32());
//      if (qboxType != QBox.QBOX_TYPE)
//        throw new Exception("Implement recovery from byte misalignment");
//      return size;
//    }
//  }
//}
