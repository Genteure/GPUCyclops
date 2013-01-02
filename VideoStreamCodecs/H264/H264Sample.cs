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

    public SliceTypes SliceType { get; private set; }
    public uint FrameNum { get; private set; }

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
              else if (naluType == NALUnitType.SequenceParamSet)
              {
                // replace _sps
                _sps = new SequenceParameterSet((uint)naluLen);
                _sps.Read(br);
              }
              else if (naluType == NALUnitType.PictureParamSet)
              {
                // replace _pps
                _pps = new PictureParameterSet((uint)naluLen);
                _pps.Read(br);
              }
              else if (naluType == NALUnitType.IDRSlice)
              {
                CodedSliceIDR idr = new CodedSliceIDR(_sps, _pps, (uint)naluLen);
                idr.Read(br);
                SliceType = idr.Header.SliceType;
                FrameNum = idr.Header.FrameNum;
                state = 2; // next should be zero or more redundant coded pictures
              }
              else if (naluType == NALUnitType.NonIDRSlice)
              {
                CodedSliceNonIDR nonIdr = new CodedSliceNonIDR(_sps, _pps, (uint)naluLen);
                nonIdr.Read(br);
                SliceType = nonIdr.Header.SliceType;
                FrameNum = nonIdr.Header.FrameNum;
                state = 2; // next should be zero or more redundant coded pictures
              }
              else if (naluType == NALUnitType.SlicePartitionA)
              {
                CodedSlicePartitionA partA = new CodedSlicePartitionA(_sps, _pps, (uint)naluLen);
                partA.Read(br);
                SliceType = partA.Header.SliceType;
                FrameNum = partA.Header.FrameNum;
                state = 2; // next should be zero or more redundant coded pictures
              }
              else if ((naluType == NALUnitType.SlicePartitionB) || (naluType == NALUnitType.SlicePartitionC))
              {
                CodedSlicePartitionBorC partBC = new CodedSlicePartitionBorC(_sps, _pps, 0, naluType, (uint)naluLen);
                partBC.Read(br);
                // FIXME: check that SliceType and FrameNum are set at this point
                state = 2; // next should be zero or more redundant coded pictures
              }
              break;
            case 2:
              // either coded picture or end of sequence
              break;
            default:
              break;
          }
          offset += naluLen;
          totalSize -= (naluLen + 4);
        }
        else naluLen = 0; // debugging break point
      }

      if (SampleDoneEvent != null)
        SampleDoneEvent(this);
    }
  }
}
