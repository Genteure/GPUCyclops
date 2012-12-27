using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Media.H264
{
  public class H264Sample
  {
    private int _totalSize;
    private SequenceParameterSet _sps;
    private PictureParameterSet _pps;

    public delegate void SampleDone(H264Sample sample);
    public SampleDone SampleDoneEvent;

    public H264Sample(SequenceParameterSet sps, PictureParameterSet pps, int size)
    {
      _sps = sps;
      _pps = pps;
      _totalSize = size;
    }

    public void ParseSample(byte[] sliceBytes)
    {
      //ThreadPool.QueueUserWorkItem(new WaitCallback(ParseThreadProc), sliceBytes);
      ParseThreadProc(sliceBytes);
    }

    void ParseThreadProc(object buffer)
    {
      byte[] sliceBytes = buffer as byte[];
      BitReader br = new BitReader(new MemoryStream(sliceBytes));
      int totalSize = _totalSize;
      int offset = 0;
      int state = 0;
      while (totalSize > 4)
      {
        int naluLen = (int)br.GetUIntFromNBits(32);
        if (naluLen > totalSize)
          throw new Exception("H264 parsing: wrong byte count encountered");
        if (naluLen > 0)
        {
          NALUnitType naluType = (NALUnitType)(br.PeekByte() & 0x1F);
          switch (state)
          {
            case 0:
              AccessUnitDelimiter aud = new AccessUnitDelimiter((uint)naluLen);
              aud.Read(br);
              state = 1;
              break;
            case 1:
              // either SEI or SPS (if it's an SEI, don't change state)
              if (naluType == NALUnitType.SupplementalEnhancementInfo)
              {
                SupplementatlEnhancementMessage sei = new SupplementatlEnhancementMessage((uint)naluLen);
                sei.Read(br);
              }
              else if (naluType == NALUnitType.IDRSlice)
              {
                CodedSliceIDR idr = new CodedSliceIDR(_sps, _pps, (uint)naluLen);
                idr.Read(br);
                state = 2; // next should be zero or more redundant coded pictures
              }
              break;
            case 2:
              // either coded picture or end of sequence
              break;
            default:
              break;
          }
          //ParseNalu(br, naluLen);
          offset += naluLen;
          totalSize -= (naluLen + 4);
        }
        else naluLen = 0; // debugging break point
      }

      if (SampleDoneEvent != null)
        SampleDoneEvent(this);
    }

    private void ParseNalu(BitReader br, int len)
    {
      byte firstByte = br.ReadByte();
      br.Position += (len - 1);
    }
  }
}
