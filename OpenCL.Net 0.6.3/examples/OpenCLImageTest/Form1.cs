/*
 * Copyright (c) 2009 Olav Kalgraf(olav.kalgraf@gmail.com)
 *
 * Permission is hereby granted, free of charge, to any person
 * obtaining a copy of this software and associated documentation
 * files (the "Software"), to deal in the Software without
 * restriction, including without limitation the rights to use,
 * copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following
 * conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 * OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
 * HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 * FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using OpenCLNet;
using CL = OpenCLNet;

namespace OpenCLImageTest
{
    public partial class Form1 : Form
    {
        // Configuration constants
        const PixelFormat MyPixelFormat = PixelFormat.Format32bppArgb;
        private CL.ImageFormat MyImageFormat = CL.ImageFormat.ARGB8U;


        string OpenCLSource;
        Bitmap TestImage;
        Bitmap TestImageOutput;

        // Currently selected platform
        Platform oclPlatform;
        // All devices in the currently selected platform
        Device[] oclDevices;
        // Currently selected device
        Device   oclDevice;
        // Currently active context
        Context  oclContext;
        // Command queue to selected device
        CommandQueue oclCQ;

        // Current program+data
        CL.Program oclProgram;
        CL.Image OCLInputImage;
        CL.Image OCLOutputImage;
        Sampler OCLSampler;
        Kernel FilterKernel;

        bool oclFullyInitialized = false;
        ContextNotify oclContextNotify;
        List<string> CallBackEventList = new List<string>();

        public Form1()
        {
            InitializeComponent();
            oclContextNotify = new ContextNotify(OpenCLContextNotifyCallBack);
        }

        public void SetupOpenCL()
        {
            if (OpenCL.NumberOfPlatforms == 0)
            {
                MessageBox.Show("OpenCL not available");
                Application.Exit();
            }
        }

        public void PopulateOCLPlatformsComboBox()
        {
            comboBoxOpenCLPlatforms.Items.Clear();
            for (int platformID = 0; platformID < OpenCL.NumberOfPlatforms; platformID++)
            {
                Platform p = OpenCL.GetPlatform(platformID);

                comboBoxOpenCLPlatforms.Items.Add(p.Vendor+":"+p.Name+" "+p.Version);
            }
        }

        public void PopulateOCLDevicesComboBox(Platform p, DeviceType deviceType)
        {
            Device[] devices = p.QueryDevices(deviceType);
            comboBoxOpenCLDevices.Items.Clear();
            foreach (Device d in devices)
            {
                comboBoxOpenCLDevices.Items.Add(d.Vendor + " " + d.Name);
            }
        }

        public void CreateContext( Platform platform, Device device )
        {
            IntPtr[] contextProperties = new IntPtr[]
            {
                (IntPtr)ContextProperties.PLATFORM, platform.PlatformID,
                IntPtr.Zero, IntPtr.Zero
            };

            Device[] devices = new Device[]
            {
                device
            };

            oclContext = platform.CreateContext(contextProperties, devices, oclContextNotify, IntPtr.Zero);
            oclCQ = oclContext.CreateCommandQueue(device, CommandQueueProperties.PROFILING_ENABLE);
            CL.ImageFormat[] imageFormats = oclContext.GetSupportedImageFormats(MemFlags.WRITE_ONLY | MemFlags.ALLOC_HOST_PTR, MemObjectType.IMAGE2D);
            foreach (CL.ImageFormat f in imageFormats)
            {
              if (f.Equals(MyImageFormat))
              {
                MyImageFormat = new CL.ImageFormat(f.ChannelOrder, f.ChannelType);
                return;
              }
            }
            MyImageFormat = new CL.ImageFormat();
        }

        public void OpenCLContextNotifyCallBack(string errInfo, byte[] privateInfo, IntPtr cb, IntPtr userData)
        {
            CallBackEventList.Add( errInfo );
            textBoxCallBackEvents.Lines = CallBackEventList.ToArray();
        }

        public void BuildOCLSource(string source)
        {
            oclProgram = oclContext.CreateProgramWithSource(source);
            oclProgram.Build();
            FilterKernel = oclProgram.CreateKernel("FilterImage");
        }

        public void CreateOCLImages(Context context)
        {
            OCLInputImage = CreateOCLImageFromWindowsBitmap(TestImage);
            OCLOutputImage = oclContext.CreateImage2D(MemFlags.WRITE_ONLY, MyImageFormat, 256, 256, 0, IntPtr.Zero); // panelScaled.Width, panelScaled.Height, 0, IntPtr.Zero);
            OCLSampler = oclContext.CreateSampler(true, AddressingMode.CLAMP_TO_EDGE, FilterMode.LINEAR);
        }

        public void ReleaseDeviceResources()
        {
            oclFullyInitialized = false;
            if (OCLSampler != null)
            {
                OCLSampler.Dispose();
                OCLSampler = null;
            }
            if (OCLInputImage != null)
            {
                OCLInputImage.Dispose();
                OCLInputImage = null;
            }

            if (OCLOutputImage != null)
            {
                OCLOutputImage.Dispose();
                OCLOutputImage = null;
            }

            if (FilterKernel != null)
            {
                FilterKernel.Dispose();
                FilterKernel = null;
            }

            if (oclProgram != null)
            {
                oclProgram.Dispose();
                oclProgram = null;
            }

            if (oclCQ != null)
            {
                oclCQ.Dispose();
                oclCQ = null;
            }

            if (oclContext != null)
            {
                oclContext.Dispose();
                oclContext = null;
            }
        }

        public void Setup()
        {
            TestImage = (Bitmap)Bitmap.FromFile(@"Input0.png");
            TestImage = new Bitmap(TestImage, 256, 256);
            TestImageOutput = new Bitmap(256, 256, MyPixelFormat); // panelScaled.Width, panelScaled.Height, MyPixelFormat); // PixelFormat.Format32bppArgb);

            if (OpenCL.NumberOfPlatforms <= 0)
            {
                MessageBox.Show("OpenCL not available");
                Application.Exit();
            }

            PopulateOCLPlatformsComboBox();
            oclPlatform = OpenCL.GetPlatform(0);
            comboBoxOpenCLPlatforms.SelectedIndex = 0;
        }

        public CL.Image CreateOCLImageFromWindowsBitmap(Bitmap bitmap)
        {
            CL.Image oclImage;

            BitmapData bd = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, MyPixelFormat); // PixelFormat.Format32bppArgb);
            MemFlags flags = (MemFlags)((long)MemFlags.READ_ONLY | (long)MemFlags.COPY_HOST_PTR);
            oclImage = oclContext.CreateImage2D(flags, MyImageFormat, bd.Width, bd.Height, bd.Stride, bd.Scan0);
            bitmap.UnlockBits(bd);
            return oclImage;
        }

        public unsafe void CopyOCLImageToWindowsBitmap(Mem ocl_Image, ref Bitmap windowsBitmap)
        {
            IntPtr[] origin = new IntPtr[3];
            IntPtr[] region = new IntPtr[3];
            //CL.Image ocl_Out_Image;
            Mem buffer;

            BitmapData bd = windowsBitmap.LockBits(new Rectangle(0, 0, windowsBitmap.Width, windowsBitmap.Height), ImageLockMode.WriteOnly, MyPixelFormat); // PixelFormat.Format32bppRgb);
            //ocl_Out_Image = oclContext.CreateImage2D(MemFlags.WRITE_ONLY | MemFlags.USE_HOST_PTR, MyImageFormat, bd.Width, bd.Height, bd.Stride, bd.Scan0);
            //MemFlags flags = MemFlags.WRITE_ONLY | MemFlags.ALLOC_HOST_PTR;
            MemFlags flags = MemFlags.WRITE_ONLY | MemFlags.USE_HOST_PTR;
            //ocl_Out_Image = oclContext.CreateImage2D(flags, MyImageFormat, bd.Width, bd.Height);
            buffer = oclContext.CreateBuffer(flags, bd.Stride * bd.Height, bd.Scan0);
            //windowsBitmap.Dispose();

            origin[0] = (IntPtr)0;
            origin[1] = (IntPtr)0;
            origin[2] = (IntPtr)0;
            region[0] = (IntPtr)bd.Width;
            region[1] = (IntPtr)bd.Height;
            region[2] = (IntPtr)1;

            // CCT (11/23/2012): this call fails with MEM_OBJECT_ALLOCATION_FAILURE on NVidia GLX 580 if flags = MemFlags.USE_HOST_PTR
            oclCQ.EnqueueCopyImageToBuffer(ocl_Image, buffer, origin, region, IntPtr.Zero);

            // CCT (11/23/2012): this call fails with OUT_OF_RESOURCES on NVidia GLX 580 if flags = MemFlags.USE_HOST_PTR
            //oclCQ.EnqueueCopyImage(ocl_Image, ocl_Out_Image, origin, origin, region);

            oclCQ.EnqueueBarrier();

            // CCT (11/23/2012): this call fails with MEM_OBJECT_ALLOCATION_FAILURE on NVidia GLX 580
            IntPtr p = oclCQ.EnqueueMapBuffer(buffer, true, MapFlags.READ, IntPtr.Zero, (IntPtr)(bd.Height * bd.Stride));

            //windowsBitmap = new Bitmap(bd.Width, bd.Height, bd.Stride, MyPixelFormat, p);
            oclCQ.EnqueueUnmapMemObject(buffer, p);

            //IntPtr imageRowPitch;
            //IntPtr imageSlicePitch;
            // CCT (11/23/2012): this call fails with OUT_OF_RESOURCES on NVidia GLX 580
            //IntPtr p = oclCQ.EnqueueMapImage(ocl_Out_Image, true, MapFlags.READ, origin, region, out imageRowPitch, out imageSlicePitch);
            //oclCQ.EnqueueUnmapMemObject(ocl_Out_Image, p);
            oclCQ.Finish();
            windowsBitmap.UnlockBits(bd);
            //ocl_Out_Image.Dispose();
            buffer.Dispose();
        }

        public void ScaleImage()
        {
            IntPtr[] globalWorkSize = new IntPtr[3];

            globalWorkSize[0] = (IntPtr)TestImageOutput.Width;
            globalWorkSize[1] = (IntPtr)TestImageOutput.Height;
            FilterKernel.SetArg(0, 0.0f);
            FilterKernel.SetArg(1, 0.0f);
            FilterKernel.SetArg(2, 1.0f);
            FilterKernel.SetArg(3, 1.0f);
            FilterKernel.SetArg(4, 0.0f);
            FilterKernel.SetArg(5, 0.0f);
            FilterKernel.SetArg(6, 1.0f);
            FilterKernel.SetArg(7, 1.0f);
            FilterKernel.SetArg(8, OCLInputImage);
            FilterKernel.SetArg(9, OCLOutputImage);
            FilterKernel.SetArg(10, OCLSampler);
            oclCQ.EnqueueNDRangeKernel(FilterKernel, 2, null, globalWorkSize, null);
            oclCQ.EnqueueBarrier();

          // Note: the following call to CopyOCLImageToWindowsBitmap has a first parameter CL.Image being cast to a Mem,
          // but in OpenCL an Image object is distinguished or is different from a Buffer object.
            CopyOCLImageToWindowsBitmap(OCLOutputImage, ref TestImageOutput);
            oclCQ.Finish();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                Setup();
            }
            catch (OpenCLException ocle)
            {
                MessageBox.Show(ocle.Message);
                Application.Exit();
            }
        }

        private void buttonScaleImage_Click(object sender, EventArgs e)
        {
            if (oclFullyInitialized)
            {
                ScaleImage();
            }
            groupBoxScaled.Refresh();
        }

        private void panelOriginal_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (TestImage != null)
                g.DrawImageUnscaled(TestImage, 0, 0);
        }

        private void panelScaled_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (oclFullyInitialized)
            {
                if (TestImageOutput != null)
                    g.DrawImageUnscaled(TestImageOutput, 0, 0);
            }
        }

        private void comboBoxOpenCLDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool supportsImages;
            bool supportsImageFormat;

            try
            {
                ReleaseDeviceResources();
                panelScaled.Refresh();

                oclDevice = oclDevices[comboBoxOpenCLDevices.SelectedIndex];
                CreateContext(oclPlatform, oclDevice);
                supportsImages = oclDevice.ImageSupport;
                supportsImageFormat = oclContext.SupportsImageFormat(MemFlags.READ_WRITE, MemObjectType.IMAGE2D, ChannelOrder.RGBA, ChannelType.UNSIGNED_INT8);
                if (oclDevice.ImageSupport && supportsImageFormat)
                {
                    buttonScaleImage.Enabled = true;
                    labelImageSupportIndicator.Text = "Yes";
                    OpenCLSource = File.ReadAllText(@"OpenCLFunctions.cl");
                    BuildOCLSource(OpenCLSource);
                    CreateOCLImages(oclContext);
                    oclFullyInitialized = true;
                }
                else
                {
                    buttonScaleImage.Enabled = false;
                    labelImageSupportIndicator.Text = "No " + (supportsImageFormat ? "(No Image support at all)" : "(Images supported, but no support for RGBA8888)");
                    oclContext = null;
                }
            }
            catch (OpenCLBuildException oclbe)
            {
                MessageBox.Show(this, oclbe.BuildLogs[0], "OpenCL build error");
            }
            catch (OpenCLException ocle)
            {
                MessageBox.Show(this, ocle.Message, "OpenCL exception");
            }
        }

        private void comboBoxOpenCLPlatforms_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                ReleaseDeviceResources();

                oclPlatform = OpenCL.GetPlatform(comboBoxOpenCLPlatforms.SelectedIndex);
                oclDevices = oclPlatform.QueryDevices(DeviceType.ALL);
                PopulateOCLDevicesComboBox(oclPlatform, DeviceType.ALL);
                if (comboBoxOpenCLDevices.Items.Count > 0)
                {
                    comboBoxOpenCLDevices.SelectedIndex = 0;
                }
                else
                {
                    oclDevice = null;
                }
            }
            catch (OpenCLException ocle)
            {
                MessageBox.Show(this, ocle.Message, "OpenCL exception");
            }
        }
    }
}
