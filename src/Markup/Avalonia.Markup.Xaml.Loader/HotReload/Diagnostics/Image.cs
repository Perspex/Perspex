//
// Image.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2009 - 2010 Novell, Inc. (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;

namespace Mono.Reflection {

	public sealed class Image : IDisposable {

		long position;
		Stream stream;

		Image (Stream stream)
		{
			this.stream = stream;
			this.position = stream.Position;
			this.stream.Position = 0;
		}

		bool Advance (int length)
		{
			if (stream.Position + length >= stream.Length)
				return false;

			stream.Seek (length, SeekOrigin.Current);
			return true;
		}

		bool MoveTo (uint position)
		{
			if (position >= stream.Length)
				return false;

			stream.Position = position;
			return true;
		}

		void IDisposable.Dispose ()
		{
			stream.Position = position;
		}

		ushort ReadUInt16 ()
		{
			return (ushort) (stream.ReadByte ()
				| (stream.ReadByte () << 8));
		}

		uint ReadUInt32 ()
		{
			return (uint) (stream.ReadByte ()
				| (stream.ReadByte () << 8)
				| (stream.ReadByte () << 16)
				| (stream.ReadByte () << 24));
		}

		bool IsManagedAssembly ()
		{
			if (stream.Length < 318)
				return false;
			if (ReadUInt16 () != 0x5a4d)
				return false;
			if (!Advance (58))
				return false;
			if (!MoveTo (ReadUInt32 ()))
				return false;
			if (ReadUInt32 () != 0x00004550)
				return false;
			if (!Advance (20))
				return false;
			if (!Advance (ReadUInt16 () == 0x20b ? 222 : 206))
				return false;

			return ReadUInt32 () != 0;
		}

		public static bool IsAssembly (string file)
		{
			if (file == null)
				throw new ArgumentNullException ("file");

			using (var stream = new FileStream (file, FileMode.Open, FileAccess.Read, FileShare.Read))
				return IsAssembly (stream);
		}

		public static bool IsAssembly (Stream stream)
		{
			if (stream == null)
				throw new ArgumentNullException ("stream");
			if (!stream.CanRead)
				throw new ArgumentException ("Can not read from stream");
			if (!stream.CanSeek)
				throw new ArgumentException ("Can not seek in stream");

			using (var image = new Image (stream))
				return image.IsManagedAssembly ();
		}
	}
}
	