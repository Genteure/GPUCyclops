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
using System.Runtime.InteropServices;

namespace ImageCrossFade
{

    public partial class Form1 : Form
    {
        bool Initialized = false;

        Bitmap InputBitmap0;
        Bitmap InputBitmap1;
        BitmapData InputBitmapData0;
        BitmapData InputBitmapData1;
        Bitmap OutputBitmap;

        OpenCLManager OCLMan;
        OpenCLNet.Program OCLProgram;
        Mem InputBuffer0;
        Mem InputBuffer1;
        Kernel CrossFadeKernel;
        IntPtr[] CrossFadeGlobalWorkSize = new IntPtr[2];

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                InitializeOpenCL();
                SetupBitmaps();
                Initialized = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show( ex.Message, "Initiation failed...");
                Application.Exit();
            }
        }

        /// <summary>
        /// Loads two bitmaps and locks them for the duration of the program.
        /// Also creates two OpenCL buffers that map to the locked images
        /// </summary>
        private void SetupBitmaps()
        {
            InputBitmap0 = (Bitmap)Bitmap.FromFile(@"Input0.png");
            InputBitmap1 = (Bitmap)Bitmap.FromFile(@"Input1.png");
            if (InputBitmap1.Size != InputBitmap0.Size)
                InputBitmap1 = new Bitmap(InputBitmap1, InputBitmap0.Size);
            OutputBitmap = new Bitmap(InputBitmap0);

            InputBitmapData0 = InputBitmap0.LockBits(new Rectangle(0, 0, InputBitmap0.Width, InputBitmap0.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            InputBitmapData1 = InputBitmap1.LockBits(new Rectangle(0, 0, InputBitmap1.Width, InputBitmap1.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            InputBuffer0 = OCLMan.Context.CreateBuffer(MemFlags.USE_HOST_PTR, InputBitmapData0.Stride * InputBitmapData0.Height, InputBitmapData0.Scan0);
            InputBuffer1 = OCLMan.Context.CreateBuffer(MemFlags.USE_HOST_PTR, InputBitmapData1.Stride * InputBitmapData1.Height, InputBitmapData1.Scan0);
        }

        /// <summary>
        /// Create a OpenCLManager, configure it, and then create a context using all devices in platform 0,
        /// Once the context is up and running we compile our source file "OpenCLFunctions.cl"
        /// The Helper automatically compiles and creates kernels.
        /// We can then extract named kernels using the GetKernel method.
        /// 
        /// For more advanced scenarios, one might use the functions in the Platform class
        /// to query devices, create contexts etc. Platforms can be enumerated using
        /// for( int i=0; i<OpenCL.NumberofPlatforms; i++ )
        ///     Platform p = OpenCL.GetPlatform(i);
        /// </summary>
        private void InitializeOpenCL()
        {
            if (OpenCL.NumberOfPlatforms == 0)
            {
                MessageBox.Show("OpenCL not available");
                Application.Exit();
            }

            OCLMan = new OpenCLManager();
            // Attempt to save binaries after compilation, as well as load precompiled binaries
            // to avoid compilation. Usually you'll want this to be true. 
            OCLMan.AttemptUseBinaries = true;
            // Attempt to compile sources. This should probably be true for almost all projects.
            // Setting it to false means that when you attempt to compile "mysource.cl", it will
            // only scan the precompiled binary directory for a binary corresponding to a source
            // with that name. There's a further restriction that the compiled binary also has to
            // use the same Defines and BuildOptions
            OCLMan.AttemptUseSource = true;
            // Binary and source paths
            // This is where we store our sources and where compiled binaries are placed
            OCLMan.BinaryPath = @"OpenCL\bin";
            OCLMan.SourcePath = @"OpenCL\src";
            // If true, RequireImageSupport will filter out any devices without image support
            // In this project we don't need image support though, so we set it to false
            OCLMan.RequireImageSupport = false;
            // The Defines string gets prepended to any and all sources that are compiled
            // and serve as a convenient way to pass configuration information to the compilation process
            OCLMan.Defines = "#define MyCompany_MyProject_Define 1";
            // The BuildOptions string is passed directly to clBuild and can be used to do debug builds etc
            OCLMan.BuildOptions = "";

            OCLMan.CreateDefaultContext(0, DeviceType.ALL);

            OCLProgram = OCLMan.CompileFile("OpenCLFunctions.cl");

            for (int i = 0; i < OCLMan.Context.Devices.Length; i++)
                comboBoxDeviceSelector.Items.Add(OCLMan.Context.Devices[i].Vendor+":"+OCLMan.Context.Devices[i].Name);
            comboBoxDeviceSelector.SelectedIndex = 0;

            CrossFadeKernel = OCLProgram.CreateKernel("CrossFade");
        }

        /// <summary>
        /// Launch the CrossFadeKernel
        /// 
        /// First we set its arguments, 
        /// then we enqueue the kernel using EnqueueNDRangeKernel,
        /// and finally, map the buffer for reading to make sure
        /// there aren't any cache issues when OpenCL completes.
        /// </summary>
        /// <param name="ratio"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="input0"></param>
        /// <param name="inputStride0"></param>
        /// <param name="input1"></param>
        /// <param name="inputStride1"></param>
        /// <param name="output"></param>
        private void DoCrossFade( float ratio,
                                  int width, int height,
                                  Mem input0, int inputStride0,
                                  Mem input1, int inputStride1,
                                  BitmapData output )
        {
            Mem outputBuffer;

            int deviceIndex = comboBoxDeviceSelector.SelectedIndex;

            outputBuffer = OCLMan.Context.CreateBuffer(MemFlags.USE_HOST_PTR, output.Stride * output.Height, output.Scan0);

            CrossFadeGlobalWorkSize[0] = (IntPtr)width;
            CrossFadeGlobalWorkSize[1] = (IntPtr)height;
            CrossFadeKernel.SetArg(0, ratio);
            CrossFadeKernel.SetArg(1, width);
            CrossFadeKernel.SetArg(2, height);
            CrossFadeKernel.SetArg(3, input0);
            CrossFadeKernel.SetArg(4, inputStride0);
            CrossFadeKernel.SetArg(5, input1);
            CrossFadeKernel.SetArg(6, inputStride1);
            CrossFadeKernel.SetArg(7, outputBuffer);
            CrossFadeKernel.SetArg(8, output.Stride);

            OCLMan.CQ[deviceIndex].EnqueueNDRangeKernel(CrossFadeKernel, 2, null, CrossFadeGlobalWorkSize, null);
            OCLMan.CQ[deviceIndex].EnqueueBarrier();
            IntPtr p = OCLMan.CQ[deviceIndex].EnqueueMapBuffer(outputBuffer, true, MapFlags.READ, IntPtr.Zero, (IntPtr)(output.Stride * output.Height));
            OCLMan.CQ[deviceIndex].EnqueueUnmapMemObject(outputBuffer, p);
            OCLMan.CQ[deviceIndex].Finish();
            outputBuffer.Dispose();
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            if (!Initialized)
            {
                g.FillRectangle(new SolidBrush(BackColor), 0, 0, Width, Height);
                return;
            }

            g.FillRectangle(new SolidBrush(BackColor), 0, 0, Width, Height);
            g.DrawImageUnscaled(OutputBitmap, 0, 50);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        private void hScrollBarRatio_ValueChanged(object sender, EventArgs e)
        {
            if (!Initialized)
                return;

            BitmapData bd = OutputBitmap.LockBits(new Rectangle(0, 0, OutputBitmap.Width, OutputBitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            DoCrossFade(hScrollBarRatio.Value*0.01f,
                InputBitmapData0.Width, InputBitmapData0.Height,
                InputBuffer0, InputBitmapData0.Stride,
                InputBuffer1, InputBitmapData1.Stride,
                bd);
            OutputBitmap.UnlockBits(bd);
            Refresh();
        }

        private void comboBoxDeviceSelector_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
