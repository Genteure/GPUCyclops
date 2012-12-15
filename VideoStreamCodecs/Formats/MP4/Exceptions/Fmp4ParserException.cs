namespace Media.Formats.MP4
{
    using System;
    using System.Runtime.Serialization;

//    [Serializable]
    public class Fmp4ParserException : Exception
    {
        public Fmp4ParserException()
        {
        }

        public Fmp4ParserException(string value) : base(value)
        {
        }

        public Fmp4ParserException(string value, Exception exception) : base(value, exception)
        {
        }
    }
}
