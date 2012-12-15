#if !ANDROID
namespace Media.Formats.JPGFJCore
{
    using System;

    public enum ResamplingFilters
    {
        NearestNeighbor,
        LowpassAntiAlias
        //Bicubic
    }

    public class FilterProgressEventArgs : EventArgs { public double Progress; }

    internal abstract class Filter
    {
        protected int _newWidth, _newHeight;
        protected byte[][,] _sourceData, _destinationData;
        protected bool _color;

        public event EventHandler<FilterProgressEventArgs> ProgressChanged;
        FilterProgressEventArgs progressArgs = new FilterProgressEventArgs();

        protected void UpdateProgress(double progress)
        {
            progressArgs.Progress = progress;
            if (ProgressChanged != null) ProgressChanged(this, progressArgs);
        }

        public byte[][,] Apply( byte[][,] imageData, int newWidth, int newHeight )
        {
            _newHeight = newHeight;
            _newWidth = newWidth;
            _color = !(imageData.Length == 1);
			_destinationData = FJImage.CreateRaster(newWidth, newHeight, imageData.Length);
            _sourceData = imageData;

            ApplyFilter();

            return _destinationData;
        }

        protected abstract void ApplyFilter();
    }
}
#endif