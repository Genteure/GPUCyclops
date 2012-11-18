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
    public partial class Form1 : Form
    {
        Mandelbrot Mandelbrot;
        int FromSetPtr = 0;
        int ToSetPtr = 0;
        float[,] Sets = new float[2, 4]
            {
                {-2.0f, 2.0f, 2.0f, -2.0f},
                {-0.17225f, 0.66116f, -0.17115f, 0.660347f},
            };

        TimeSpan ZoomTime = TimeSpan.FromSeconds(30);
        DateTime ZoomStart;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load( object sender, EventArgs e )
        {
            try
            {
                Platform platform = OpenCL.GetPlatform(0);
                Mandelbrot = new Mandelbrot(platform, Width, Height);
                Mandelbrot.AllocBuffers();
                UpdateMandelbrot();
            }
            catch (Exception oex)
            {
                MessageBox.Show( oex.ToString(), "OpenCL Initialization failed" );
                Application.Exit();
            }
        }

        protected int Align(int i, int align)
        {
            return (i + align - 1) / align * align;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            UpdateMandelbrot();
        }

        public void ZoomMandelbrot()
        {
            float zoomFactor = (float)((DateTime.Now - ZoomStart).TotalMilliseconds / ZoomTime.TotalMilliseconds);
            Mandelbrot.Left = Sets[FromSetPtr, 0]+(float)((Sets[ToSetPtr, 0] - Sets[FromSetPtr, 0]) * zoomFactor);
            Mandelbrot.Right = Sets[FromSetPtr, 2] + (float)((Sets[ToSetPtr, 2] - Sets[FromSetPtr, 2]) * zoomFactor);
            Mandelbrot.Top = Sets[FromSetPtr, 1] + (float)((Sets[ToSetPtr, 1] - Sets[FromSetPtr, 1]) * zoomFactor);
            Mandelbrot.Bottom = Sets[FromSetPtr, 3] + (float)((Sets[ToSetPtr, 3] - Sets[FromSetPtr, 3]) * zoomFactor);
            UpdateMandelbrot();
        }

        private void UpdateMandelbrot()
        {
            Mandelbrot.Calculate();

            using (Graphics gfx = this.CreateGraphics())
            {
                gfx.DrawImageUnscaled(Mandelbrot.Bitmap, 0, 0);
                gfx.DrawString("ms per frame="+Mandelbrot.CalculationTimeMS, Font, Brushes.Yellow, new PointF(50.0f, 50.0f));
            }
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startToolStripMenuItem.Enabled = false;
            stopToolStripMenuItem.Enabled = true;
            ToSetPtr = 1;
            ZoomStart = DateTime.Now;
            timer.Start();
        }

        private void stopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            startToolStripMenuItem.Enabled = true;
            stopToolStripMenuItem.Enabled = false;
            ToSetPtr = 0;
            ZoomStart = DateTime.Now;
            timer.Start();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            TimeSpan dt = DateTime.Now - ZoomStart;
            if (dt >= ZoomTime)
            {
                timer.Stop();
                FromSetPtr = ToSetPtr;
            }
            ZoomMandelbrot();
        }
    }
}

#if false
            ErrorCode result;
            uint numPlatforms;
            IntPtr[] platformIDs;

            // Get number of platforms
            result = cl.GetPlatformIDs( 0, null, out numPlatforms );
            if( result!=ErrorCode.SUCCESS )
                throw new Exception( "GetPlatformIDs failed with ErrorCode."+result );
            Debug.WriteLine( "Number of platforms: "+numPlatforms );
            if( numPlatforms==0 )
                throw new Exception( "No openCL platforms available." );

            // Create an array of platform IDs
            platformIDs = new IntPtr[numPlatforms];
            result = cl.GetPlatformIDs( numPlatforms, platformIDs, out numPlatforms );
            if( result!=ErrorCode.SUCCESS )
                throw new Exception( "GetPlatformIDs failed with ErrorCode."+result );
            Debug.WriteLine("");


        int[] globSize = new int[3];
        int[] globID = new int[3];
        float left;
        float top;
        float right;
        float bottom;
        AlignedArrayFloat aaf;

        private void KernelIterator2d()
        {
            int activeIndex = globSize.Length-1;

            while( activeIndex>=0 )
            {
                MandelBrotKernel( left, top, right, bottom, aaf );
                while( activeIndex>=0 )
                {
                    globID[activeIndex]++;
                    if( globID[activeIndex]>=globSize[activeIndex] )
                    {
                        globID[activeIndex] = 0;
                        activeIndex--;
                    }
                    else
                    {
                        activeIndex = globSize.Length-1;
                        break;
                    }
                }
            }
        }

        private int get_global_size( int dimension ) { return globSize[dimension]; }
        private int get_global_id( int dimension ) { return globID[dimension]; }
        private void MandelBrotKernel( float left, float top, float right, float bottom, AlignedArrayFloat af )
        {
            int width = get_global_size(0);
            int height = get_global_size(1);
            int cx = get_global_id(0);
            int cy = get_global_id(1);
            float dx = (right-left)/(float)width;
            float dy = (bottom-top)/(float)height;

            float x0 = left+dx*(float)cx;
            float y0 = top+dy*(float)cy;
            float x = 0.0f;
            float y = 0.0f;
            int iteration = 0;
            int max_iteration = 1000;

            while( x*x-y*y<=(2*2) && iteration<max_iteration )
            {
                float xtemp = x*x-y*y+x0;
                y = 2*x*y+y0;
                x = xtemp;
                iteration++;
            }
            float color;
            color = iteration==max_iteration?0.0f: (float)iteration/(float)max_iteration;
            af[width*4*cy+cx*4+0] = 1.0f;
            af[width*4*cy+cx*4+1] = color;
            af[width*4*cy+cx*4+2] = color;
            af[width*4*cy+cx*4+3] = color;
        }

#endif
