using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Avalonia.Native.Interop
{
    partial interface IAvnString
    {
        public string String { get; }
        public byte[] Bytes { get; }
    }

    partial interface IAvnStringArray
    {
        string[] ToStringArray();
    }

    internal class AvnString : CallbackBase, IAvnString
    {
        private IntPtr _native;
        private int _nativeLen;

        public AvnString(string s) => String = s;

        public string String { get; }
        public byte[] Bytes => Encoding.UTF8.GetBytes(String);
        
        public unsafe void* Pointer()
        {
            EnsureNative();
            return _native.ToPointer();
        }

        public int Length()
        {
            EnsureNative();
            return _nativeLen;
        }

        protected override void Destroyed()
        {
            if (_native != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_native);
                _native = IntPtr.Zero;
            }
        }
        
        private unsafe void EnsureNative()
        {
            if (string.IsNullOrEmpty(String))
                return;
            if (_native == IntPtr.Zero)
            {
                _nativeLen = Encoding.UTF8.GetByteCount(String);
                _native = Marshal.AllocHGlobal(_nativeLen + 1);
                var ptr = (byte*)_native.ToPointer();
                fixed (char* chars = String)
                    Encoding.UTF8.GetBytes(chars, String.Length, ptr, _nativeLen);
                ptr[_nativeLen] = 0;
            }
        }
    }
}
namespace Avalonia.Native.Interop.Impl
{
    unsafe partial class __MicroComIAvnStringProxy
    {
        private string _managed;
        private byte[] _bytes;

        public string String
        {
            get
            {
                if (_managed == null)
                {
                    var ptr = Pointer();
                    if (ptr == null)
                        return null;
                    _managed = System.Text.Encoding.UTF8.GetString((byte*)ptr, Length());
                }

                return _managed;
            }
        }

        public byte[] Bytes
        {
            get
            {
                if (_bytes == null)
                {
                    _bytes = new byte[Length()];
                    Marshal.Copy(new IntPtr(Pointer()), _bytes, 0, _bytes.Length);
                }

                return _bytes;
            }
        }

        public override string ToString() => String;
    }
    
    partial class __MicroComIAvnStringArrayProxy
    {
        public string[] ToStringArray()
        {
            var arr = new string[Count];
            for(uint c = 0; c<arr.Length;c++)
                using (var s = Get(c))
                    arr[c] = s.String;
            return arr;
        }
    }
}
