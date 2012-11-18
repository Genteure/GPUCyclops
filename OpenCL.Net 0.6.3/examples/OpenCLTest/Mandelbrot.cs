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
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Drawing.Imaging;
using OpenCLNet;

namespace OpenCLTest
{

    public class Mandelbrot
    {
        public float Left { get; set; }
        public float Top { get; set; }
        public float Right { get; set; }
        public float Bottom { get; set; }

        public int BitmapWidth { get; set; }
        public int BitmapHeight { get; set; }

        public Bitmap Bitmap { get; protected set; }

        Platform openCLPlatform;
        Device[] openCLDevices;
        Context openCLContext;
        CommandQueue openCLCQ;
        Program mandelBrotProgram;
        Kernel mandelbrotKernel;
        Mem mandelbrotMemBuffer;

        public Mandelbrot( Platform platform, int width, int height )
        {
            openCLPlatform = platform;
            openCLDevices = openCLPlatform.QueryDevices(DeviceType.ALL);
            openCLContext = openCLPlatform.CreateDefaultContext();
            openCLCQ = openCLContext.CreateCommandQueue(openCLDevices[0], CommandQueueProperties.PROFILING_ENABLE);
            mandelBrotProgram = openCLContext.CreateProgramWithSource(File.ReadAllText("Mandelbrot.cl"));
            try
            {
                mandelBrotProgram.Build();
            }
            catch (OpenCLException)
            {
                string buildLog = mandelBrotProgram.GetBuildLog(openCLDevices[0]);
                MessageBox.Show(buildLog,"Build error(64 bit debug sessions in vs2008 always fail like this - debug in 32 bit or use vs2010)");
                Application.Exit();
            }
            mandelbrotKernel = mandelBrotProgram.CreateKernel("Mandelbrot");

            Left = -2.0f;
            Top = 2.0f;
            Right = 2.0f;
            Bottom = -2.0f;
            BitmapWidth = width;
            BitmapHeight = height;

            mandelbrotMemBuffer = openCLContext.CreateBuffer((MemFlags)((long)MemFlags.WRITE_ONLY), width*height*4, IntPtr.Zero);
        }

        public void AllocBuffers()
        {
            Bitmap = new Bitmap( BitmapWidth, BitmapHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb );
        }

        public void Calculate()
        {
            BitmapData bd = Bitmap.LockBits(new Rectangle(0, 0, Bitmap.Width, Bitmap.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            int bitmapSize = bd.Width * bd.Height;

            mandelbrotKernel.SetArg( 0, Left );
            mandelbrotKernel.SetArg( 1, Top );
            mandelbrotKernel.SetArg( 2, Right );
            mandelbrotKernel.SetArg( 3, Bottom );
            mandelbrotKernel.SetArg( 4, BitmapWidth );
            mandelbrotKernel.SetArg( 5, mandelbrotMemBuffer );

            Event calculationStart;
            Event calculationEnd;

            openCLCQ.EnqueueMarker(out calculationStart);

            IntPtr[] globalWorkSize = new IntPtr[2];

            globalWorkSize[0] = (IntPtr)BitmapWidth;
            globalWorkSize[1] = (IntPtr)BitmapHeight;
            openCLCQ.EnqueueNDRangeKernel(mandelbrotKernel, 2, null, globalWorkSize, null);

            for (int y = 0; y < BitmapHeight; y++)
                openCLCQ.EnqueueReadBuffer(mandelbrotMemBuffer, true, (IntPtr)(BitmapWidth*4*y), (IntPtr)(BitmapWidth*4), (IntPtr)(bd.Scan0.ToInt64()+y*bd.Stride));
            openCLCQ.Finish();
            openCLCQ.EnqueueMarker(out calculationEnd);
            openCLCQ.Finish();

            ulong start = 0;
            ulong end = 0;
            try
            {
                calculationStart.GetEventProfilingInfo(ProfilingInfo.QUEUED, out start);
                calculationEnd.GetEventProfilingInfo(ProfilingInfo.END, out end);
            }
            catch (OpenCLException)
            {
            }
            finally
            {
                CalculationTimeMS = (end - start) / 1000000;
                calculationStart.Dispose();
                calculationEnd.Dispose();
            }
            Bitmap.UnlockBits(bd);
        }

        public ulong CalculationTimeMS;
    }
}
