namespace Media.Formats.MP4
{
    public class InvalidBoxException : Fmp4ParserException
    {
        public InvalidBoxException(long position, string message) : base(message)
        {
            this.Position = position;
        }

        public InvalidBoxException(BoxType type, long position, string message) : base(message)
        {
            this.BoxType = type;
            this.Position = position;
        }

        public BoxType BoxType { get; private set; }

        public long Position { get; private set; }
    }
}
