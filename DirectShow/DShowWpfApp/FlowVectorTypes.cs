using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml;


namespace MFV.FlowVector
{
  public class LogGroups
  {
    public static XmlQualifiedName Console = new XmlQualifiedName("System:Console");  // FIXME: fake for now
  }

  public delegate void Log(XmlQualifiedName cat, string s);



    /// <summary>
    /// FlowVector Contract class
    /// </summary>
    public sealed class Contract
    {
        /// <summary>
        /// The Dss Service contract
        /// </summary>
        public const String Identifier = "http://3dexistence.robotics.schemas.centerus.com/2007/05/flowvector.html";
    }

    /// <summary>
    /// The FlowVector State
    /// </summary>
    [DataContract]
    public class FlowVectorState
    {
      /// <summary>
        /// Use a struct, PIN_OUT, defined in DirectShow interface.
        /// </summary>
        private PIN_OUT _rawFlowVectors;

        [DataMember]
        public PIN_OUT RawFlowVectors
        {
            get { return _rawFlowVectors; }
            set { _rawFlowVectors = value; }
        }

        public FlowVectorState()
        {
            _rawFlowVectors = new PIN_OUT();
        }

    }

    [DataContract]
    public class CameraFrameBits
    {
        private int frameIndex;

        [DataMember]
        public int FrameIndex
        {
            get { return frameIndex; }
            set { frameIndex = value; }
        }

    }

    [DataContract]
    public class FilterGraphState
    {
        private FilterState filterState;

        [DataMember]
        public FilterState FilterState
        {
            get { return filterState; }
            set { filterState = value; }
        }
    }

    [DataContract]
    public class RunRequest { }

    [DataContract]
    public class PauseRequest { }

    [DataContract]
    public class StopRequest { }



}
