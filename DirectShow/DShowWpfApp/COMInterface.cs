using System;
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Runtime.InteropServices;

namespace MFV.FlowVector
{
    public enum VMR9AspectRatioMode
    {
        None,
        LetterBox,
    }

    [Flags]
    public enum VMR9Mode
    {
        None        = 0,
        Windowed    = 1,
        Windowless  = 2,
        Renderless  = 4,
        Mask        = 7
    }

    [Flags]
    public enum VMR9RenderPrefs
    {
        None                = 0,
        DoNotRenderBorder   = 1,
        Mask                = 1,
    }


    [ComVisible(true), ComImport,
    Guid("5a804648-4f66-4867-9c43-4f5c822cf1b8"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVMRFilterConfig9
    {
        [PreserveSig]
        int SetImageCompositor([In] IntPtr lpVMRImgCompositor);  // unused, so IntPtr will do

        [PreserveSig]
        int SetNumberOfStreams([In] int dwMaxStreams);

        [PreserveSig]
        int GetNumberOfStreams([Out] out int pdwMaxStreams);

        [PreserveSig]
        int SetRenderingPrefs([In] VMR9RenderPrefs dwRenderFlags);

        [PreserveSig]
        int GetRenderingPrefs([Out] out VMR9RenderPrefs pdwRenderFlags);

        [PreserveSig]
        int SetRenderingMode([In] VMR9Mode Mode);

        [PreserveSig]
        int GetRenderingMode([Out] out VMR9Mode Mode);
    }

    [ComVisible(true), ComImport,
    Guid("8f537d09-f85e-4414-b23b-502e54c79927"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVMRWindowlessControl9
    {
        int GetNativeVideoSize(
            [Out] out int lpWidth,
            [Out] out int lpHeight,
            [Out] out int lpARWidth,
            [Out] out int lpARHeight
            );

        int GetMinIdealVideoSize(
            [Out] out int lpWidth,
            [Out] out int lpHeight
            );

        int GetMaxIdealVideoSize(
            [Out] out int lpWidth,
            [Out] out int lpHeight
            );

        int SetVideoPosition(
            [In] DShowNET.DsRECT lpSRCRect,
            [In] DShowNET.DsRECT lpDSTRect
            );

        int GetVideoPosition(
            [Out] DShowNET.DsRECT lpSRCRect,
           [Out] DShowNET.DsRECT lpDSTRect
            );

        int GetAspectRatioMode([Out] out VMR9AspectRatioMode lpAspectRatioMode);

        int SetAspectRatioMode([In] VMR9AspectRatioMode AspectRatioMode);

        int SetVideoClippingWindow([In] IntPtr hwnd);

        int RepaintVideo(
            [In] IntPtr hwnd,
            [In] IntPtr hdc
            );

        int DisplayModeChanged();

        int GetCurrentImage([Out] out IntPtr lpBytes);

        int SetBorderColor([In] int Color);

        int GetBorderColor([Out] out int lpColor);
    }

    [ComVisible(true), ComImport,
    Guid("00d96c29-bbde-4efc-9901-bb5036392146"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVMRAspectRatioControl9
    {
        [PreserveSig]
        int GetAspectRatioMode([Out] out VMR9AspectRatioMode lpdwARMode);

        [PreserveSig]
        int SetAspectRatioMode([In] VMR9AspectRatioMode lpdwARMode);
    }

    [Flags]
    public enum VMR9DeinterlacePrefs
    {
        None = 0,
        NextBest = 0x01,
        BOB = 0x02,
        Weave = 0x04,
        Mask = 0x07
    }

    public enum VMR9SampleFormat
    {
        None = 0,
        Reserved = 1,
        ProgressiveFrame = 2,
        FieldInterleavedEvenFirst = 3,
        FieldInterleavedOddFirst = 4,
        FieldSingleEven = 5,
        FieldSingleOdd = 6
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VMR9Frequency
    {
        public int dwNumerator;
        public int dwDenominator;
    }

    [Flags]
    public enum VMR9DeinterlaceTech
    {
        Unknown = 0x0000,
        BOBLineReplicate = 0x0001,
        BOBVerticalStretch = 0x0002,
        MedianFiltering = 0x0004,
        EdgeFiltering = 0x0010,
        FieldAdaptive = 0x0020,
        PixelAdaptive = 0x0040,
        MotionVectorSteered = 0x0080
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VMR9VideoDesc
    {
        public int dwSize;
        public int dwSampleWidth;
        public int dwSampleHeight;
        public VMR9SampleFormat SampleFormat;
        public int dwFourCC;
        public VMR9Frequency InputSampleFreq;
        public VMR9Frequency OutputFrameFreq;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VMR9DeinterlaceCaps
    {
        public int dwSize;
        public int dwNumPreviousOutputFrames;
        public int dwNumForwardRefSamples;
        public int dwNumBackwardRefSamples;
        public VMR9DeinterlaceTech DeinterlaceTechn;
    }

    [ComVisible(true), ComImport,
    Guid("a215fb8d-13c2-4f7f-993c-003d6271a459"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVMRDeinterlaceControl9
    {
        [PreserveSig]
        int GetNumberOfDeinterlaceModes(
            [In] ref VMR9VideoDesc lpVideoDescription,
            [In, Out] ref int lpdwNumDeinterlaceModes,
            [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.Struct)] Guid[] lpDeinterlaceModes
            );

        [PreserveSig]
        int GetDeinterlaceModeCaps(
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid lpDeinterlaceMode,
            [In] ref VMR9VideoDesc lpVideoDescription,
            [In, Out] ref VMR9DeinterlaceCaps lpDeinterlaceCaps
            );

        [PreserveSig]
        int GetDeinterlaceMode(
            [In] int dwStreamID,
            [Out] out Guid lpDeinterlaceMode
            );

        [PreserveSig]
        int SetDeinterlaceMode(
            [In] int dwStreamID,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid lpDeinterlaceMode
            );

        [PreserveSig]
        int GetDeinterlacePrefs([Out] out VMR9DeinterlacePrefs lpdwDeinterlacePrefs);

        [PreserveSig]
        int SetDeinterlacePrefs([In] VMR9DeinterlacePrefs lpdwDeinterlacePrefs);

        [PreserveSig]
        int GetActualDeinterlaceMode(
            [In] int dwStreamID,
            [Out] out Guid lpDeinterlaceMode
            );
    }

    [Flags]
    public enum VMR9SurfaceAllocationFlags
    {
        None = 0,
        ThreeDRenderTarget = 0x0001,
        DXVATarget = 0x0002,
        TextureSurface = 0x0004,
        OffscreenSurface = 0x0008,
        RGBDynamicSwitch = 0x0010,
        UsageReserved = 0x00e0,
        UsageMask = 0x00FF
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct VMR9AllocationInfo
    {
        public VMR9SurfaceAllocationFlags dwFlags;
        public int dwWidth;
        public int dwHeight;
        public int Format;
        public int Pool;
        public int MinBuffers;
        public Size szAspectRatio;
        public Size szNativeSize;
    }

    [ComVisible(true), ComImport,
    Guid("6de9a68a-a928-4522-bf57-655ae3866456"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVMRSurfaceAllocatorEx9 : IVMRSurfaceAllocator9
    {
        #region IVMRSurfaceAllocator9 Methods

        [PreserveSig]
        new int InitializeDevice(
            [In] UInt32 dwUserID,
            [In] ref VMR9AllocationInfo lpAllocInfo,
            [In, Out] ref int lpNumBuffers
            );

        [PreserveSig]
        new int TerminateDevice([In] IntPtr dwID);

        [PreserveSig]
        new int GetSurface(
            [In] UInt32 dwUserID,
            [In] int SurfaceIndex,
            [In] int SurfaceFlags,
            [Out] out IntPtr lplpSurface
            );

        [PreserveSig]
        new int AdviseNotify([In] IVMRSurfaceAllocatorNotify9 lpIVMRSurfAllocNotify);

        #endregion

        [PreserveSig]
        int GetSurfaceEx(
            [In] UInt32 dwUserID,
            [In] int SurfaceIndex,
            [In] int SurfaceFlags,
            [Out] out IntPtr lplpSurface,
            [Out] out DShowNET.DsRECT lprcDst
            );
    }

    [ComVisible(true), ComImport,
    Guid("dca3f5df-bb3a-4d03-bd81-84614bfbfa0c"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVMRSurfaceAllocatorNotify9
    {
        [PreserveSig]
        int AdviseSurfaceAllocator(
            [In] UInt32 dwUserID,
            [In] IVMRSurfaceAllocator9 lpIVRMSurfaceAllocator
            );

        [PreserveSig]
        int SetD3DDevice(
            [In] IntPtr lpD3DDevice,
            [In] IntPtr hMonitor
            );

        [PreserveSig]
        int ChangeD3DDevice(
            [In] IntPtr lpD3DDevice,
            [In] IntPtr hMonitor
            );

        [PreserveSig]
        int AllocateSurfaceHelper(
            [In] ref VMR9AllocationInfo lpAllocInfo,
            [In, Out] ref int lpNumBuffers,
            [Out, MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.SysInt)] IntPtr[] lplpSurface
            );

        [PreserveSig]
        int NotifyEvent(
            [In] int EventCode, // event code enum
            [In] IntPtr Param1,
            [In] IntPtr Param2
            );
    }

    [ComVisible(true), ComImport,
    Guid("8d5148ea-3f5d-46cf-9df1-d1b896eedb1f"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVMRSurfaceAllocator9
    {
        [PreserveSig]
        int InitializeDevice(
            [In] UInt32 dwUserID,
            [In] ref VMR9AllocationInfo lpAllocInfo,
            [In, Out] ref int lpNumBuffers
            );

        [PreserveSig]
        int TerminateDevice([In] IntPtr dwID);

        [PreserveSig]
        int GetSurface(
            [In] UInt32 dwUserID,
            [In] int SurfaceIndex,
            [In] int SurfaceFlags,
            [Out] out IntPtr lplpSurface
            );

        [PreserveSig]
        int AdviseNotify([In] IVMRSurfaceAllocatorNotify9 lpIVMRSurfAllocNotify);
    }

    [ComVisible(true), ComImport,
    Guid("E8F56BFD-6EE4-4ece-BE6B-D83455BC8804"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVectorGrabber
    {
        [PreserveSig]
        int SetCallback([In] IVectorGrabberCB pCallback);
    }



    [ComVisible(true), ComImport,
    Guid("62D4C758-2BF1-4917-9144-EF9FD2C7A0DD"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IVectorGrabberCB
    {
        [PreserveSig]
        int BufferFlowVectors([In, MarshalAs(UnmanagedType.LPStruct)] PIN_OUT pFlowVectors);
    }


    //// Flow Vector
    //// We use FLOAT because pixel distance can be sub-pixel.
    //typedef struct _FV2DVECTOR {
    //    union {
    //        FLOAT x;
    //        FLOAT dvX;
    //    };
    //    union {
    //        FLOAT y;
    //        FLOAT dvY;
    //    };
    //} FV2DVECTOR, *PFV2DVECTOR;
    //typedef struct _DS3DVECTOR {
    //    union {
    //        FLOAT x;
    //        FLOAT dvX;
    //    };
    //    union {
    //        FLOAT y;
    //        FLOAT dvY;
    //    };
    //    union {
    //        FLOAT z;
    //        FLOAT dvZ;
    //    };
    //} DS3DVECTOR, *PDS3DVECTOR;
    //typedef struct {
    //    FV2DVECTOR        AreaFrameOfRef;    // frame of reference for this area
    //    FV2DVECTOR        FocusOfExp;        // focus of expansion
    //    FV2DVECTOR        PrimaryFlow;       // primary flow vector
    //    FV2DVECTOR        Flow[4];           // secondary flow vectors (moving objects)
    //    FLOAT             TimeToCollision;   // time to collision
    //    int               AreaID;            // which area in the frame this data refers to
    //} FLOW_VECTOR, *PFLOW_VECTOR;
    //// calculated position, velocity, and camera angle
    //typedef struct {
    //    DS3DVECTOR  Position;
    //    DS3DVECTOR  Velocity;          // must be unit vector
    //    DS3DVECTOR  CameraDirection;   // must be unit vector
    //} CALC_COORD, *PCALC_COORD; // calculated camera coordinates
    //// array of flow vectors
    //typedef struct {
    //    int					Count;  // count of vectors
    //    REFERENCE_TIME		FrameTime;         // which frame this data refers to
    //    CALC_COORD			CameraPos;
    //    FLOW_VECTOR			FlowVectors[1];
    //} PIN_OUT, *PPIN_OUT;
    [StructLayout(LayoutKind.Sequential), ComVisible(false)]
    [DataContract]
    public class PIN_OUT		// pin out
    {
        [DataMember]
        public int Count;
        [DataMember]
        public Int64 FrameTime;
        //public CALC_COORD CameraPos;
        public float CameraPos_x;
        public float CameraPos_y;
        public float CameraPos_z;
        public float CameraVel_x;
        public float CameraVel_y;
        public float CameraVel_z;
        public float CameraDir_x;
        public float CameraDir_y;
        public float CameraDir_z;

        //public FLOW_VECTOR[] FlowVectors;
        [DataMember]
        public float AreaFrame_x0;
        [DataMember]
        public float AreaFrame_y0;
        public float FocusOfExp_x0;
        public float FocusOfExp_y0;
        [DataMember]
        public float PrimaryFlow_x0;
        [DataMember]
        public float PrimaryFlow_y0;
        [DataMember]
        public float Flow0_x0;
        [DataMember]
        public float Flow0_y0;
        [DataMember]
        public float Flow1_x0;
        [DataMember]
        public float Flow1_y0;
        [DataMember]
        public float Flow2_x0;
        [DataMember]
        public float Flow2_y0;
        [DataMember]
        public float Flow3_x0;
        [DataMember]
        public float Flow3_y0;
        public float TimeToCollision0;
        [DataMember]
        public int AreaID0;

        [DataMember]
        public float AreaFrame_x1;
        [DataMember]
        public float AreaFrame_y1;
        public float FocusOfExp_x1;
        public float FocusOfExp_y1;
        [DataMember]
        public float PrimaryFlow_x1;
        [DataMember]
        public float PrimaryFlow_y1;
        [DataMember]
        public float Flow0_x1;
        [DataMember]
        public float Flow0_y1;
        [DataMember]
        public float Flow1_x1;
        [DataMember]
        public float Flow1_y1;
        [DataMember]
        public float Flow2_x1;
        [DataMember]
        public float Flow2_y1;
        [DataMember]
        public float Flow3_x1;
        [DataMember]
        public float Flow3_y1;
        public float TimeToCollision1;
        [DataMember]
        public int AreaID1;

        [DataMember]
        public float AreaFrame_x2;
        [DataMember]
        public float AreaFrame_y2;
        public float FocusOfExp_x2;
        public float FocusOfExp_y2;
        [DataMember]
        public float PrimaryFlow_x2;
        [DataMember]
        public float PrimaryFlow_y2;
        [DataMember]
        public float Flow0_x2;
        [DataMember]
        public float Flow0_y2;
        [DataMember]
        public float Flow1_x2;
        [DataMember]
        public float Flow1_y2;
        [DataMember]
        public float Flow2_x2;
        [DataMember]
        public float Flow2_y2;
        [DataMember]
        public float Flow3_x2;
        [DataMember]
        public float Flow3_y2;
        public float TimeToCollision2;
        [DataMember]
        public int AreaID2;

        [DataMember]
        public float AreaFrame_x3;
        [DataMember]
        public float AreaFrame_y3;
        public float FocusOfExp_x3;
        public float FocusOfExp_y3;
        [DataMember]
        public float PrimaryFlow_x3;
        [DataMember]
        public float PrimaryFlow_y3;
        [DataMember]
        public float Flow0_x3;
        [DataMember]
        public float Flow0_y3;
        [DataMember]
        public float Flow1_x3;
        [DataMember]
        public float Flow1_y3;
        [DataMember]
        public float Flow2_x3;
        [DataMember]
        public float Flow2_y3;
        [DataMember]
        public float Flow3_x3;
        [DataMember]
        public float Flow3_y3;
        public float TimeToCollision3;
        [DataMember]
        public int AreaID3;

        [DataMember]
        public float AreaFrame_x4;
        [DataMember]
        public float AreaFrame_y4;
        public float FocusOfExp_x4;
        public float FocusOfExp_y4;
        [DataMember]
        public float PrimaryFlow_x4;
        [DataMember]
        public float PrimaryFlow_y4;
        [DataMember]
        public float Flow0_x4;
        [DataMember]
        public float Flow0_y4;
        [DataMember]
        public float Flow1_x4;
        [DataMember]
        public float Flow1_y4;
        [DataMember]
        public float Flow2_x4;
        [DataMember]
        public float Flow2_y4;
        [DataMember]
        public float Flow3_x4;
        [DataMember]
        public float Flow3_y4;
        public float TimeToCollision4;
        [DataMember]
        public int AreaID4;

        [DataMember]
        public float AreaFrame_x5;
        [DataMember]
        public float AreaFrame_y5;
        public float FocusOfExp_x5;
        public float FocusOfExp_y5;
        [DataMember]
        public float PrimaryFlow_x5;
        [DataMember]
        public float PrimaryFlow_y5;
        [DataMember]
        public float Flow0_x5;
        [DataMember]
        public float Flow0_y5;
        [DataMember]
        public float Flow1_x5;
        [DataMember]
        public float Flow1_y5;
        [DataMember]
        public float Flow2_x5;
        [DataMember]
        public float Flow2_y5;
        [DataMember]
        public float Flow3_x5;
        [DataMember]
        public float Flow3_y5;
        public float TimeToCollision5;
        [DataMember]
        public int AreaID5;

        public PIN_OUT()
        {
        }

        //public PIN_OUT(PIN_OUT marshalFromCOM)
        //{
        //    this.Count = marshalFromCOM.Count;
        //    this.FrameTime = marshalFromCOM.FrameTime;
        ////public CALC_COORD CameraPos = marshalFromCOM.;
        //    this.CameraPos_x = marshalFromCOM.CameraPos_x;
        //    this.CameraPos_y = marshalFromCOM.CameraPos_y;
        //    this.CameraPos_z = marshalFromCOM.CameraPos_z;
        //    this.CameraVel_x = marshalFromCOM.CameraVel_x;
        //    this.CameraVel_y = marshalFromCOM.CameraVel_y;
        //    this.CameraVel_z = marshalFromCOM.CameraVel_z;
        //    this.CameraDir_x = marshalFromCOM.CameraDir_x;
        //    this.CameraDir_y = marshalFromCOM.CameraDir_y;
        //    this.CameraDir_z = marshalFromCOM.CameraDir_z;

        ////public FLOW_VECTOR[] FlowVectors = marshalFromCOM.;
        //    this.AreaFrame_x0 = marshalFromCOM.AreaFrame_x0;
        //    this.AreaFrame_y0 = marshalFromCOM.AreaFrame_y0;
        //    this.FocusOfExp_x0 = marshalFromCOM.FocusOfExp_x0;
        //    this.FocusOfExp_y0 = marshalFromCOM.FocusOfExp_y0;
        //    this.PrimaryFlow_x0 = marshalFromCOM.PrimaryFlow_x0;
        //    this.PrimaryFlow_y0 = marshalFromCOM.PrimaryFlow_y0;
        //    this.Flow0_x0 = marshalFromCOM.Flow0_x0;
        //    this.Flow0_y0 = marshalFromCOM.Flow0_y0;
        //    this.Flow1_x0 = marshalFromCOM.Flow1_x0;
        //    this.Flow1_y0 = marshalFromCOM.Flow1_y0;
        //    this.Flow2_x0 = marshalFromCOM.Flow2_x0;
        //    this.Flow2_y0 = marshalFromCOM.Flow2_y0;
        //    this.Flow3_x0 = marshalFromCOM.Flow3_x0;
        //    this.Flow3_y0 = marshalFromCOM.Flow3_y0;
        //    this.TimeToCollision0 = marshalFromCOM.TimeToCollision0;
        //    this.AreaID0 = marshalFromCOM.AreaID0;

        //    this.AreaFrame_x1 = marshalFromCOM.AreaFrame_x1;
        //    this.AreaFrame_y1 = marshalFromCOM.AreaFrame_y1;
        //    this.FocusOfExp_x1 = marshalFromCOM.FocusOfExp_x1;
        //    this.FocusOfExp_y1 = marshalFromCOM.FocusOfExp_y1;
        //    this.PrimaryFlow_x1 = marshalFromCOM.PrimaryFlow_x1;
        //    this.PrimaryFlow_y1 = marshalFromCOM.PrimaryFlow_y1;
        //    this.Flow0_x1 = marshalFromCOM.Flow0_x1;
        //    this.Flow0_y1 = marshalFromCOM.Flow0_y1;
        //    this.Flow1_x1 = marshalFromCOM.Flow1_x1;
        //    this.Flow1_y1 = marshalFromCOM.Flow1_y1;
        //    this.Flow2_x1 = marshalFromCOM.Flow2_x1;
        //    this.Flow2_y1 = marshalFromCOM.Flow2_y1;
        //    this.Flow3_x1 = marshalFromCOM.Flow3_x1;
        //    this.Flow3_y1 = marshalFromCOM.Flow3_y1;
        //    this.TimeToCollision1 = marshalFromCOM.TimeToCollision1;
        //    this.AreaID1 = marshalFromCOM.AreaID1;

        //    this.AreaFrame_x2 = marshalFromCOM.AreaFrame_x2;
        //    this.AreaFrame_y2 = marshalFromCOM.AreaFrame_y2;
        //    this.FocusOfExp_x2 = marshalFromCOM.FocusOfExp_x2;
        //    this.FocusOfExp_y2 = marshalFromCOM.FocusOfExp_y2;
        //    this.PrimaryFlow_x2 = marshalFromCOM.PrimaryFlow_x2;
        //    this.PrimaryFlow_y2 = marshalFromCOM.PrimaryFlow_y2;
        //    this.Flow0_x2 = marshalFromCOM.Flow0_x2;
        //    this.Flow0_y2 = marshalFromCOM.Flow0_y2;
        //    this.Flow1_x2 = marshalFromCOM.Flow1_x2;
        //    this.Flow1_y2 = marshalFromCOM.Flow1_y2;
        //    this.Flow2_x2 = marshalFromCOM.Flow2_x2;
        //    this.Flow2_y2 = marshalFromCOM.Flow2_y2;
        //    this.Flow3_x2 = marshalFromCOM.Flow3_x2;
        //    this.Flow3_y2 = marshalFromCOM.Flow3_y2;
        //    this.TimeToCollision2 = marshalFromCOM.TimeToCollision2;
        //    this.AreaID2 = marshalFromCOM.AreaID2;

        //    this.AreaFrame_x3 = marshalFromCOM.AreaFrame_x3;
        //    this.AreaFrame_y3 = marshalFromCOM.AreaFrame_y3;
        //    this.FocusOfExp_x3 = marshalFromCOM.FocusOfExp_x3;
        //    this.FocusOfExp_y3 = marshalFromCOM.FocusOfExp_y3;
        //    this.PrimaryFlow_x3 = marshalFromCOM.PrimaryFlow_x3;
        //    this.PrimaryFlow_y3 = marshalFromCOM.PrimaryFlow_y3;
        //    this.Flow0_x3 = marshalFromCOM.Flow0_x3;
        //    this.Flow0_y3 = marshalFromCOM.Flow0_y3;
        //    this.Flow1_x3 = marshalFromCOM.Flow1_x3;
        //    this.Flow1_y3 = marshalFromCOM.Flow1_y3;
        //    this.Flow2_x3 = marshalFromCOM.Flow2_x3;
        //    this.Flow2_y3 = marshalFromCOM.Flow2_y3;
        //    this.Flow3_x3 = marshalFromCOM.Flow3_x3;
        //    this.Flow3_y3 = marshalFromCOM.Flow3_y3;
        //    this.TimeToCollision3 = marshalFromCOM.TimeToCollision3;
        //    this.AreaID3 = marshalFromCOM.AreaID3;

        //    this.AreaFrame_x4 = marshalFromCOM.AreaFrame_x4;
        //    this.AreaFrame_y4 = marshalFromCOM.AreaFrame_y4;
        //    this.FocusOfExp_x4 = marshalFromCOM.FocusOfExp_x4;
        //    this.FocusOfExp_y4 = marshalFromCOM.FocusOfExp_y4;
        //    this.PrimaryFlow_x4 = marshalFromCOM.PrimaryFlow_x4;
        //    this.PrimaryFlow_y4 = marshalFromCOM.PrimaryFlow_y4;
        //    this.Flow0_x4 = marshalFromCOM.Flow0_x4;
        //    this.Flow0_y4 = marshalFromCOM.Flow0_y4;
        //    this.Flow1_x4 = marshalFromCOM.Flow1_x4;
        //    this.Flow1_y4 = marshalFromCOM.Flow1_y4;
        //    this.Flow2_x4 = marshalFromCOM.Flow2_x4;
        //    this.Flow2_y4 = marshalFromCOM.Flow2_y4;
        //    this.Flow3_x4 = marshalFromCOM.Flow3_x4;
        //    this.Flow3_y4 = marshalFromCOM.Flow3_y4;
        //    this.TimeToCollision4 = marshalFromCOM.TimeToCollision4;
        //    this.AreaID4 = marshalFromCOM.AreaID4;

        //    this.AreaFrame_x5 = marshalFromCOM.AreaFrame_x5;
        //    this.AreaFrame_y5 = marshalFromCOM.AreaFrame_y5;
        //    this.FocusOfExp_x5 = marshalFromCOM.FocusOfExp_x5;
        //    this.FocusOfExp_y5 = marshalFromCOM.FocusOfExp_y5;
        //    this.PrimaryFlow_x5 = marshalFromCOM.PrimaryFlow_x5;
        //    this.PrimaryFlow_y5 = marshalFromCOM.PrimaryFlow_y5;
        //    this.Flow0_x5 = marshalFromCOM.Flow0_x5;
        //    this.Flow0_y5 = marshalFromCOM.Flow0_y5;
        //    this.Flow1_x5 = marshalFromCOM.Flow1_x5;
        //    this.Flow1_y5 = marshalFromCOM.Flow1_y5;
        //    this.Flow2_x5 = marshalFromCOM.Flow2_x5;
        //    this.Flow2_y5 = marshalFromCOM.Flow2_y5;
        //    this.Flow3_x5 = marshalFromCOM.Flow3_x5;
        //    this.Flow3_y5 = marshalFromCOM.Flow3_y5;
        //    this.TimeToCollision5 = marshalFromCOM.TimeToCollision5;
        //    this.AreaID5 = marshalFromCOM.AreaID5;
        //}
    }
}
