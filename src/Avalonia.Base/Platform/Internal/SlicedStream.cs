using System;
using System.IO;

namespace Avalonia.Platform.Internal
{
    internal class SlicedStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly int _from;

        public SlicedStream(Stream baseStream, int from, int length)
        {
            Length = length;
            _baseStream = baseStream;
            _from = from;
            _baseStream.Position = from;
        }
        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _baseStream.Read(buffer, offset, (int)Math.Min(count, Length - Position));
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
                Position = offset;
            if (origin == SeekOrigin.End)
                Position = _from + Length + offset;
            if (origin == SeekOrigin.Current)
                Position = Position + offset;
            return Position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override bool CanRead => true;
        public override bool CanSeek => _baseStream.CanRead;
        public override bool CanWrite => false;
        public override long Length { get; }
        public override long Position
        {
            get => _baseStream.Position - _from;
            set => _baseStream.Position = value + _from;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                _baseStream.Dispose();
        }

        public override void Close() => _baseStream.Close();
    }
}
