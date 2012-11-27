using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices.ComTypes;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Configuration;

using DShowNET;
using DShowNET.Device;
using SharedMemory;

namespace MFV.FlowVector
{
    [DataContract]
    public enum FilterState
    {
        Stopped = 0,
        Paused,
        Running
    }

    class DShowMFVGraph : IDisposable, ISampleGrabberCB, IVectorGrabberCB
    {
        private readonly string ATICrossbar = @"@device:pnp:\\?\PCI#VEN_1002&DEV_4D52&SUBSYS_A3461002&REV_00#4&13699180&0&3048#{a799a801-a46d-11d0-a18c-00a02401dcd4}\{39309da7-b1c0-43a3-aac3-6d6bfcbb75a9}";
        private readonly string ATICapture  = @"@device:pnp:\\?\PCI#VEN_1002&DEV_4D52&SUBSYS_A3461002&REV_00#4&13699180&0&3048#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\{bc187864-4183-4dc5-9fe0-679a7a039975}";
        private readonly string TeeSplitter = @"@device:pnp:\\?\Root#SYSTEM#0000#{0a4252a0-7e70-11d0-a5d6-28db04c10000}\{cfd669f1-9bc2-11d0-8299-0000f822fe8a}&{0A4252A0-7E70-11D0-A5D6-28DB04C10000}";
#if DEBUG
        private int rotCookie = 0;
#endif

#if false // Use of VMR9 abandoned for now
        private UInt32 userID = 0x1FEED;
        private IVMRSurfaceAllocator9 vmrAllocator;
#endif

        /// <summary> event interface. </summary>
        private IMediaEvent mediaEvt;

        private IBaseFilter videoRenderer;

        /// <summary> base filter of the actually used video devices. </summary>
        private IBaseFilter capFilter;

        /// <summary> graph builder interface. </summary>
        private IGraphBuilder graphBuilder;

        /// <summary> tee splitter for feeding video frames to both flowVector and VMR9 filters. </summary>
        private IBaseFilter teeSplitter;

        /// <summary> capture graph builder interface. </summary>
        private ICaptureGraphBuilder2 capGraph;
        private IVectorGrabber vectorGrabber;

        /// <summary> control interface. </summary>
        private IMediaControl mediaCtrl;

        private IAMCrossbar crossBar;

        /// <summary> list of installed video devices. </summary>
        private ArrayList capDevices;

        /// <summary> flag to detect first Form appearance </summary>
        private bool firstActive;

        /// <summary> grabber filter interface. </summary>
        private IBaseFilter baseGrabFlt;
        private IBaseFilter sampleGrabber;
        private ISampleGrabber grabberConfig;
        private IBaseFilter atiCrossbar;
        private IBaseFilter motionVector;
        private IBaseFilter wmVideoDecoder;
        private IBaseFilter stretchVideo;
        private IBaseFilter colorConverter;
        private DsDevice dev;
        private IBaseFilter modFrameRate;

        private IPin captureDevOutPin;

        /// <summary> structure describing the bitmap to grab. </summary>
        private VideoInfoHeader videoInfoHeader;
        private bool captured = true;
        private int bufferedSize;
        private IntPtr videoFrame;

        private bool atiTVCardFound = false;

        private string videoSource;
        private string filePath;
        private bool videoPreview;

        private Log LogInfo;

        private const int WM_GRAPHNOTIFY = 0x00008001;	// message from graph

        private const int WS_CHILD = 0x40000000;	// attributes for video window
        private const int WS_CLIPCHILDREN = 0x02000000;
        private const int WS_CLIPSIBLINGS = 0x04000000;

        // ctor
        public DShowMFVGraph(Log logInfo)
        {
            LogInfo = logInfo;

            AppSettingsReader appConfig = new AppSettingsReader();
            videoSource = (string)appConfig.GetValue("VideoSource", typeof(string));
            filePath = (string)appConfig.GetValue("FilePath", typeof(string));
            videoPreview = (bool)appConfig.GetValue("VideoPreview", typeof(bool));

            videoFrame = GlobalMemClass.CreateSharedMem("globalmem");

            InitializeInternal();
        }

        /// <summary> Clean up any resources being used. </summary>
        public void Dispose() // Dispose()
        {
            CloseInterfaces();
            GlobalMemClass.Cleanup();
            LogInfo(LogGroups.Console, "DShowMFVGraph disposed.");
        }

        private void InitializeInternal()
        {
            System.Threading.ApartmentState state =
            System.Threading.Thread.CurrentThread.GetApartmentState();
            LogInfo(LogGroups.Console, state.ToString() + "\n");
        }

        /// <summary> detect first form appearance, start grabber. </summary>
        /// <fixme> There are three paths to activation: 
        /// One opens a media file whose name is specified in the dsshost.exe.config file,
        /// another assumes that a particular video card is present,
        /// and the third asks the user to select a video input source. 
        /// This module really only works for the first two paths.
        /// </fixme>
        public void Activate()
        {
            IMoniker moniker;
            IMoniker devmoniker;

            if (firstActive)
                return;
            firstActive = true;

            if (!DsUtils.IsCorrectDirectXVersion())
            {
                LogInfo(LogGroups.Console, "DirectX 8.1 NOT installed!");
                //this.Close(); 
                return;
            }

            try
            {
                if (videoSource.Equals("File", StringComparison.CurrentCultureIgnoreCase))
                {
                    LogInfo(LogGroups.Console, "File to play : " + filePath);
                    //capFilter = (IBaseFilter)DsDev.CreateFromCLSID(Clsid.WM_ASF_Reader);
                }
                else if (videoSource.Equals("Analog", StringComparison.CurrentCultureIgnoreCase))
                {
                    atiCrossbar = (IBaseFilter)DsDev.CreateFromMoniker(ATICrossbar);
                    capFilter = (IBaseFilter)DsDev.CreateFromMoniker(ATICapture);
                    atiTVCardFound = true;
                }
                else // digital camera, may be a webcam
                {
                    if (!DsDev.GetDevicesOfCat(FilterCategory.VideoInputDevice, out capDevices))
                    {
                        LogInfo(LogGroups.Console, "No video capture devices found!");
                        return;
                    }

                    if (capDevices.Count == 1)
                        dev = capDevices[0] as DsDevice;
                    else
                    {
                        //DeviceSelector selector = new DeviceSelector(capDevices);
                        //selector.ShowDialog();
                        //dev = selector.SelectedDevice;
                        dev = capDevices[0] as DsDevice;
                    }

                    if (dev == null)
                    {
                        LogInfo(LogGroups.Console, "Cannot use first capture device found.");
                        return;
                    }

                    CreateCaptureDevice(dev.Mon);
                }
                StartupVideo();
            }
            catch (Exception ex)
            {
                LogInfo(LogGroups.Console, "Filter Graph can't start : " + ex.Message);
            }
        }


        /// <summary> start all the interfaces, graphs and preview window. </summary>
        bool StartupVideo()
        {
            int hr;
            try
            {
                GetInterfaces();

                SetupGraph();

#if DEBUG
                DsROT.AddGraphToRot(graphBuilder, out rotCookie);		// graphBuilder capGraph
#endif

                return true;
            }
            catch (Exception ee)
            {
                LogInfo(LogGroups.Console, "Could not start video stream\r\n" + ee.Message);
                return false;
            }
        }

        public FilterState GetStatus()
        {
            int state = 0;
            int hr = mediaCtrl.GetState(100, out state);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
            return (FilterState)state;
        }

        public void Run()
        {
            int hr = mediaCtrl.Run();
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        public void Pause()
        {
            int hr = mediaCtrl.Pause();
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        public void Stop()
        {
            int hr = mediaCtrl.Stop();
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
        }


        /// <summary>
        /// This was used to retrieve all pin names. Now it is not being used.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="PinDir"></param>
        /// <returns></returns>
        void DisplayPinNames(IBaseFilter filter)
        {
            IEnumPins enumPins = null;
            IPin[] localPins = new IPin[2];
            string pinName;
            int hr;

            hr = filter.EnumPins(out enumPins);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            try
            {
                int count = 0;
                while (enumPins.Next(1, localPins, out count) == 0)
                {
                    PinDirection PinDirThis;
                    hr = localPins[0].QueryDirection(out PinDirThis);
                    if (hr < 0)
                    {
                        Marshal.ReleaseComObject(localPins[0]);
                        Marshal.ThrowExceptionForHR(hr);
                    }
                    localPins[0].QueryId(out pinName);
                    LogInfo(LogGroups.Console, pinName);
                    // Release the pin for the next time through the loop.
                    Marshal.ReleaseComObject(localPins[0]);
                }
                // No more pins. We did not find a match.
                //throw new Exception("pin not found");
                LogInfo(LogGroups.Console, "\n");
                return; // null;
            }
            finally
            {
                Marshal.ReleaseComObject(enumPins);
            }
        }

        void AddFilter(IBaseFilter filter, string name)
        {
            int hr = graphBuilder.AddFilter(filter, name);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        void ConnectPins(IBaseFilter upFilter, string upName, IBaseFilter downFilter, string downName)
        {
            int hr;
            IPin pin1, pin2;
            PinDirection PinDirThis;
            if (upName == "CapturePin")
            {
                pin1 = captureDevOutPin;
            }
            else
            {
                hr = upFilter.FindPin(upName, out pin1);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
                hr = pin1.QueryDirection(out PinDirThis);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
                if (PinDirThis != PinDirection.Output)
                    throw new Exception("Wrong upstream pin");
            }
            //pin1 = GetPin(upFilter, PinDirection.Output);
            hr = downFilter.FindPin(downName, out pin2);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
            hr = pin2.QueryDirection(out PinDirThis);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
            if (PinDirThis != PinDirection.Input)
                throw new Exception("Wrong downstream pin");
            //pin2 = GetPin(downFilter, PinDirection.Input);
            hr = graphBuilder.Connect(pin1, pin2);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        void SetupVideoCrossbar()
        {
            int hr;
            crossBar = (IAMCrossbar)atiCrossbar;
            hr = crossBar.Route(0, 2); // input pin 2 is S-Video
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        /// <summary> build the capture graph for grabber. </summary>
        bool SetupGraph()
        {
            int hr;
            IPin pin1, pin2;
            try
            {
                hr = capGraph.SetFiltergraph(graphBuilder);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);

                if (atiTVCardFound)
                {
                    SetupVideoCrossbar();
                    AddFilter(atiCrossbar, "ATI Crossbar");
                    AddFilter(capFilter, "Analog Capture Device");
                    AddFilter(wmVideoDecoder, "AVI Decompressor");
                    AddFilter(stretchVideo, "Stretch Video");
                    AddFilter(colorConverter, "Color Space Converter");
                }
                else if (videoSource.Equals("File"))
                {
                    graphBuilder.AddSourceFilter(filePath, "WM ASF Reader", out capFilter);
                    AddFilter(modFrameRate, "Modify Frame Rate");
                    AddFilter(stretchVideo, "Stretch Video");
                    AddFilter(colorConverter, "Color Space Converter");
                }
                else
                {
                    int state;
                    if (capFilter.GetState(100, out state) == 0)
                    {
                        AddFilter(capFilter, "Capture Filter");
                    }
                }

                AddFilter(sampleGrabber, "Sample Grabber"); // make sure samples grabbed have 32 bits per pixel to work with Ge Force 7900

                AddFilter(baseGrabFlt, "Vector Grabber");
                AddFilter(motionVector, "Motion Flow Vector Filter");

                if (videoPreview)
                {
                    AddFilter(teeSplitter, "Smart Tee Splitter");
                    AddFilter(colorConverter, "Color Space Converter");
                    AddFilter(videoRenderer, "Video Renderer");
                }

#if false // Attempt to use VMR9 abandoned for now
                IVMRFilterConfig9 vmrConfig = videoRenderer as IVMRFilterConfig9;
                hr = vmrConfig.SetRenderingMode(VMR9Mode.Renderless);
                if (hr < 0)
                    Marshal.ThrowExceptionForHR(hr);
                IVMRSurfaceAllocatorNotify9 vmrAllocNotify = videoRenderer as IVMRSurfaceAllocatorNotify9;
                vmrAllocNotify.AdviseSurfaceAllocator(userID, vmrAllocator);
                vmrAllocator.AdviseNotify(vmrAllocNotify);
#endif

                // connect the pins
                if (videoSource.Equals("File"))
                {
                    ConnectPins(capFilter, "Raw Video 1", modFrameRate, "In");
                    ConnectPins(modFrameRate, "Out", stretchVideo, "In");
                    //ConnectPins(wmVideoDecoder, "out0", stretchVideo, "In");
                    ConnectPins(stretchVideo, "Out", colorConverter, "In");
                    ConnectPins(colorConverter, "Out", sampleGrabber, "In");
                }
                else
                {
                    if (atiTVCardFound)
                    {
                        ConnectPins(atiCrossbar, "0: Video Decoder Out", capFilter, "0");
                        ConnectPins(capFilter, "2", wmVideoDecoder, "In");
                        ConnectPins(wmVideoDecoder, "Out", stretchVideo, "In");
                        ConnectPins(stretchVideo, "Out", colorConverter, "In");
                        ConnectPins(colorConverter, "Out", sampleGrabber, "In");
                    }
                    else // webcam case
                    {
                        //ConnectPins(capFilter, "CapturePin", stretchVideo, "In");
                        ConnectPins(capFilter, "CapturePin", sampleGrabber, "In");
                    }
                }


                if (videoPreview)
                {
                    ConnectPins(sampleGrabber, "Out", teeSplitter, "Input");
                    //ConnectPins(teeSplitter, "0", videoRenderer, "In");
                    ConnectPins(teeSplitter, "Preview", colorConverter, "In");
                    ConnectPins(colorConverter, "Out", videoRenderer, "VMR Input0");
                    ConnectPins(teeSplitter, "Capture", motionVector, "In");
                }
                else
                {
                    ConnectPins(sampleGrabber, "Out", motionVector, "In");
                }
                ConnectPins(motionVector, "Out", baseGrabFlt, "In");

                // check that all filters are accounted for
                // there must be a total of 7 filters if source is "File"
                IEnumFilters enumFilters;
                graphBuilder.EnumFilters(out enumFilters);
                enumFilters.Reset();
                IBaseFilter[] filters = new IBaseFilter[1];
                int count = 0;
                int total = 0;
                while (0 == (hr = enumFilters.Next(1, filters, out count)))
                {
                    FilterInfo info = new FilterInfo();
                    hr = filters[0].QueryFilterInfo(info);
                    if (hr < 0)
                        Marshal.ThrowExceptionForHR(hr);
                    LogInfo(LogGroups.Console, info.achName);
                    IPin[] pins = new IPin[1];
                    IEnumPins enumPins;
                    filters[0].EnumPins(out enumPins);
                    while (0 == (hr = enumPins.Next(1, pins, out count)))
                    {
                        IPin pin;
                        hr = pins[0].ConnectedTo(out pin);
                        if (pin != null)
                        {
                            string pinID;
                            hr = pin.QueryId(out pinID);
                            LogInfo(LogGroups.Console, pinID);
                        }
                    }
                    Marshal.ReleaseComObject(filters[0]);
                    total++;
                }
                Marshal.ReleaseComObject(enumFilters);

                SetupVideoGrabber();

                SetupVectorGrabber();

                return true;
            }
            catch (Exception ee)
            {
                LogInfo(LogGroups.Console, "Could not setup graph\r\n" + ee.Message);
                return false;
            }
        }

        void SetupVideoGrabber()
        {
            AMMediaType media = new AMMediaType();
            int hr = grabberConfig.GetConnectedMediaType(media);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
            if (((!media.formatType.Equals(FormatType.VideoInfo)) && 
                (!media.formatType.Equals(FormatType.WaveEx))) ||
                (media.formatPtr == IntPtr.Zero))
                throw new NotSupportedException("Unknown Grabber Media Format");

            videoInfoHeader = (VideoInfoHeader)Marshal.PtrToStructure(media.formatPtr, typeof(VideoInfoHeader));
            Marshal.FreeCoTaskMem(media.formatPtr); media.formatPtr = IntPtr.Zero;

            hr = grabberConfig.SetBufferSamples(false);
            if (hr == 0)
                hr = grabberConfig.SetOneShot(false);
            if (hr == 0)
                hr = grabberConfig.SetCallback(null, 0);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);

            grabberConfig.SetCallback(this, 1);

        }

        void SetupVectorGrabber()
        {
            int hr = vectorGrabber.SetCallback(this);
            if (hr < 0)
                Marshal.ThrowExceptionForHR(hr);
        }

        // FIXME: complete this routine to catch the end of video properly.
        // Use CCR to assign the task.
        void AssignTaskToWaitForCompletion(IMediaEvent mediaEvent)
        {
        }

        /// <summary> create the used COM components and get the interfaces. </summary>
        bool GetInterfaces()
        {
            Type comType = null;
            object comObj = null;
            try
            {
                graphBuilder = (IGraphBuilder)DsDev.CreateFromCLSID(Clsid.FilterGraph);
                mediaEvt = (IMediaEvent)graphBuilder;
                AssignTaskToWaitForCompletion(mediaEvt);

                Guid clsid = Clsid.CaptureGraphBuilder2;
                Guid riid = typeof(ICaptureGraphBuilder2).GUID;
                capGraph = (ICaptureGraphBuilder2)DsBugWO.CreateDsInstance(ref clsid, ref riid);

                if (atiTVCardFound)
                {
                    wmVideoDecoder = (IBaseFilter)DsDev.CreateFromCLSID(Clsid.AVI_Decompressor);
                }

                stretchVideo = (IBaseFilter)DsDev.CreateFromCLSID(Clsid.Stretch_Video);
                colorConverter = (IBaseFilter)DsDev.CreateFromCLSID(Clsid.ColorSpaceConverter);
                modFrameRate = (IBaseFilter)DsDev.CreateFromCLSID(Clsid.ModifyFrameRate);

                motionVector = (IBaseFilter)DsDev.CreateFromCLSID(Clsid.MotionVector);
                // Vector Grabber
                vectorGrabber = (IVectorGrabber)DsDev.CreateFromCLSID(Clsid.FlowVectorGrabber);

                sampleGrabber = (IBaseFilter)DsDev.CreateFromCLSID(Clsid.SampleGrabber);
                grabberConfig = sampleGrabber as ISampleGrabber;

                mediaCtrl = (IMediaControl)graphBuilder;

                if (videoPreview)
                {
                    teeSplitter = (IBaseFilter)DsDev.CreateFromCLSID(Clsid.SmartTee); //.CreateFromMoniker(TeeSplitter);
                    videoRenderer = (IBaseFilter)DsDev.CreateFromCLSID(Clsid.VideoRenderer); // (Clsid.VMR9);
                }

                // Attemp to use VMR9 abandoned for now
                //vmrAllocator = (IVMRSurfaceAllocator9)DsDev.CreateFromCLSID(Clsid.VMR9Allocator);
                baseGrabFlt = (IBaseFilter)vectorGrabber;
                return true;
            }
            catch (Exception ee)
            {
                LogInfo(LogGroups.Console, "Could not get interfaces\r\n" + ee.Message);
                return false;
            }
            finally
            {
                if (comObj != null)
                    Marshal.ReleaseComObject(comObj); comObj = null;
            }
        }

        /// <summary> create the user selected capture device. </summary>
        bool CreateCaptureDevice(IMoniker mon)
        {
            int hr;
            object capObj = null;
            try
            {
                Guid gbf = typeof(IBaseFilter).GUID;
                Guid classID;

                mon.BindToObject(null, null, ref gbf, out capObj);
                capFilter = (IBaseFilter)capObj;

                IPin[] pin = new IPin[2];
                int count;
                PinDirection pinDir;
                IEnumPins enumPins;
                if (capFilter.EnumPins(out enumPins) == 0)
                {
                    if (((hr = enumPins.Next(1, pin, out count)) == 0))
                    {
                        pin[0].QueryDirection(out pinDir);
                        if (pinDir == PinDirection.Output)
                            captureDevOutPin = pin[0];
                        if (count > 1)
                            Marshal.ReleaseComObject(pin[1]);
                    }
                    Marshal.ReleaseComObject(enumPins);
                }
                if (captureDevOutPin == null)
                    throw new Exception("Cannot find output pin for capture device");
                return true;
            }
            catch (Exception ee)
            {
                LogInfo(LogGroups.Console, "Could not create capture device\r\n" + ee.Message);
                return false;
            }
            //finally
            //{
            //    if (capObj != null)
            //    {
            //        Marshal.ReleaseComObject(capObj);
            //        capObj = null;
            //    }
            //}

        }



        /// <summary> do cleanup and release DirectShow. </summary>
        void CloseInterfaces()
        {
            int hr;
#if DEBUG
            if (rotCookie != 0)
                DsROT.RemoveGraphFromRot(ref rotCookie);
#endif

            if (mediaCtrl != null)
            {
                hr = mediaCtrl.Stop();
                Marshal.ReleaseComObject(mediaCtrl);
                mediaCtrl = null;
            }

            if (mediaEvt != null)
            {
                Marshal.ReleaseComObject(mediaEvt);
                mediaEvt = null;
            }

            if (baseGrabFlt != null)
            {
                Marshal.ReleaseComObject(baseGrabFlt);
                baseGrabFlt = null;
            }

            if (sampleGrabber != null)
            {
                Marshal.ReleaseComObject(sampleGrabber);
                sampleGrabber = null;
            }

            if (grabberConfig != null)
            {
                Marshal.ReleaseComObject(grabberConfig);
                grabberConfig = null;
            }

            if (capGraph != null)
            {
                Marshal.ReleaseComObject(capGraph);
                capGraph = null;
            }

            if (graphBuilder != null)
            {
                Marshal.ReleaseComObject(graphBuilder);
                graphBuilder = null;
            }

            if (dev != null)
            {
                dev.Dispose();
                dev = null;
            }

            if (wmVideoDecoder != null)
            {
                Marshal.ReleaseComObject(wmVideoDecoder);
                wmVideoDecoder = null;
            }

            if (capFilter != null)
            {
                Marshal.ReleaseComObject(capFilter);
                capFilter = null;
            }

            if (capDevices != null)
            {
                foreach (DsDevice d in capDevices)
                    d.Dispose();
                capDevices = null;
            }

            if (atiCrossbar != null)
            {
                Marshal.ReleaseComObject(atiCrossbar);
                atiCrossbar = null;
            }

            if (motionVector != null)
            {
                Marshal.ReleaseComObject(motionVector);
                motionVector = null;
            }

            if (teeSplitter != null)
            {
                Marshal.ReleaseComObject(teeSplitter);
                teeSplitter = null;
            }

            if (videoRenderer != null)
            {
                Marshal.ReleaseComObject(videoRenderer);
                videoRenderer = null;
            }

            if (vectorGrabber != null)
            {
                //vectorGrabber.SetCallback(null); // FIXME: sometimes, this causes an exception
                Marshal.ReleaseComObject(vectorGrabber);
                vectorGrabber = null;
            }

            if (crossBar != null)
            {
                Marshal.ReleaseComObject(crossBar);
                crossBar = null;
            }
        }


        /// <summary> sample callback, NOT USED. </summary>
        int ISampleGrabberCB.SampleCB(double SampleTime, IMediaSample pSample)
        {
            LogInfo(LogGroups.Console, "ISampleGrabberCB.SampleCB called.");
            return 0;
        }

        /// <summary> buffer callback, COULD BE FROM FOREIGN THREAD. </summary>
        int ISampleGrabberCB.BufferCB(double SampleTime, IntPtr pBuffer, int BufferLen)
        {
            bufferedSize = BufferLen;
            if ((pBuffer != IntPtr.Zero) && (BufferLen > 1000))
            {
              // FIXME: just use SharedMemory to send both image and data
                //ReplaceFrame sendFrame = new ReplaceFrame();
                RtlMoveMemory(videoFrame, pBuffer, (ulong)BufferLen); // shoot video frame through 32/64-bit barrier
                //_mainPort.Post(sendFrame); // signal XNA game that video frame is available
            }
            else
            {
                LogInfo(LogGroups.Console, "ISampleGrabberCB.BufferCB buffer param invalid.");
            }
            return 0;
        }

        /// <summary>
        /// Callback method: warning this may be called from a different thread than the rest of this class.
        /// </summary>
        /// <param name="pFlowVectors">PIN_OUT: See the file COMInterface.cs for the struct definition.</param>
        /// <returns>HRESULT</returns>
        int IVectorGrabberCB.BufferFlowVectors([In, MarshalAs(UnmanagedType.LPStruct)] PIN_OUT pFlowVectors)
        {
            try
            {
              // FIXME: use SharedMemory to communicate with XNA game
                //Replace flowvectors = new Replace();
                //flowvectors.Body.RawFlowVectors = pFlowVectors; // new PIN_OUT(pFlowVectors);
                //_mainPort.Post(flowvectors);
            }
            catch (Exception ee)
            {
                LogInfo(LogGroups.Console, "BufferFlowVectors Exception: " + ee.Message);
            }
            return 0;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern void RtlMoveMemory(
            IntPtr destination,
            IntPtr source,
            UInt64 length);

    }
}
