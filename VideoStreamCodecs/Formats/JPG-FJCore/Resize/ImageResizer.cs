﻿#if !MONO
using System;

namespace Media.Formats.JPGFJCore
{
    public class ResizeNotNeededException : Exception {  }
    public class ResizeProgressChangedEventArgs : EventArgs { public double Progress; }

    public class ImageResizer
    {
        private ResizeProgressChangedEventArgs progress = new ResizeProgressChangedEventArgs();
        public event EventHandler<ResizeProgressChangedEventArgs> ProgressChanged;

				private FJImage _input;

				public ImageResizer(FJImage input)
        {
            _input = input;
        }

				public static bool ResizeNeeded(FJImage image, int maxEdgeLength)
        {
            double scale = (image.Width > image.Height) ? 
                (double)maxEdgeLength / image.Width : 
                (double)maxEdgeLength / image.Height;

            return scale < 1.0; // true if we must downscale
        }

				public FJImage Resize(int maxEdgeLength, ResamplingFilters technique)
        {
            double scale = 0;

            if (_input.Width > _input.Height)
                scale = (double)maxEdgeLength / _input.Width;
            else
                scale = (double)maxEdgeLength / _input.Height;

            if (scale >= 1.0)
                throw new ResizeNotNeededException();
            else
                return Resize(scale, technique);
        }

				public FJImage Resize(int maxWidth, int maxHeight, ResamplingFilters technique)
        {
            double wFrac = (double)maxWidth / _input.Width;
            double hFrac = (double)maxHeight / _input.Height;
            double scale = 0;

            // Make the image as large as possible, while 
            // fitting in the supplied box and
            // obeying the aspect ratio

            if (wFrac < hFrac) { scale = wFrac; }
            else { scale = hFrac; }

            if (scale >= 1.0)
                throw new ResizeNotNeededException();
            else
                return Resize(scale, technique);
        }

				public FJImage Resize(double scale, ResamplingFilters technique)
        {
            int height = (int)(scale * _input.Height);
            int width = (int)(scale * _input.Width);

            Filter resizeFilter;

            switch (technique)
            {
                case ResamplingFilters.NearestNeighbor:
                    resizeFilter = new NNResize();
                    break;
                case ResamplingFilters.LowpassAntiAlias:
                    resizeFilter = new LowpassResize();
                    break;
                default:
                    throw new NotSupportedException();
            }

						return new FJImage(_input.ColorModel, resizeFilter.Apply(_input.Raster, width, height));
        }

        void ResizeProgressChanged(object sender, FilterProgressEventArgs e)
        {
            progress.Progress = e.Progress;
            if (ProgressChanged != null) ProgressChanged(this, progress);
        }

    }
}
#endif
