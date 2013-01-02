﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Media.H264
{
  class CodedSlicePartitionA : CodedSliceBase
  {
    public uint SliceID;

    public CodedSlicePartitionA(SequenceParameterSet sps, PictureParameterSet pps, uint size) 
      : base(sps, pps, (byte)0, NALUnitType.SlicePartitionA, size)
    {
    }

    public override void Read(BitReader bitReader)
    {
      base.Read(bitReader);
      Header.Read(bitReader);
      SliceID = bitReader.DecodeUnsignedExpGolomb(); // this is not in base class
      Data.Read(bitReader);

      // FIXME: SliceData above is not complete, so for now let's just skip the rest of data
      Nalu.SkipToEndOfNALU(bitReader);

      Nalu.CheckSize(bitReader);
    }
  }
}