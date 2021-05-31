using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    internal static class UiaCoreTypesApi
    {
        private const string StartListeningExportName = "SynchronizedInputPattern_StartListening";

        internal enum AutomationIdType
        {
            Property,
            Pattern,
            Event,
            ControlType,
            TextAttribute
        }

        internal const int UIA_E_ELEMENTNOTENABLED = unchecked((int)0x80040200);
        internal const int UIA_E_ELEMENTNOTAVAILABLE = unchecked((int)0x80040201);
        internal const int UIA_E_NOCLICKABLEPOINT = unchecked((int)0x80040202);
        internal const int UIA_E_PROXYASSEMBLYNOTLOADED = unchecked((int)0x80040203);

        internal static int UiaLookupId(AutomationIdType type, ref Guid guid)
        {   
            return RawUiaLookupId( type, ref guid );
        }

        internal static object UiaGetReservedNotSupportedValue()
        {
            object notSupportedValue;
            CheckError(RawUiaGetReservedNotSupportedValue(out notSupportedValue));
            return notSupportedValue;
        }

        internal static object UiaGetReservedMixedAttributeValue()
        {
            object mixedAttributeValue;
            CheckError(RawUiaGetReservedMixedAttributeValue(out mixedAttributeValue));
            return mixedAttributeValue;
        }

        private static void CheckError(int hr)
        {
            if (hr >= 0)
            {
                return;
            }

            Marshal.ThrowExceptionForHR(hr, (IntPtr)(-1));
        }

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaLookupId", CharSet = CharSet.Unicode)]
        private static extern int RawUiaLookupId(AutomationIdType type, ref Guid guid);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaGetReservedNotSupportedValue", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetReservedNotSupportedValue([MarshalAs(UnmanagedType.IUnknown)] out object notSupportedValue);

        [DllImport("UIAutomationCore.dll", EntryPoint = "UiaGetReservedMixedAttributeValue", CharSet = CharSet.Unicode)]
        private static extern int RawUiaGetReservedMixedAttributeValue([MarshalAs(UnmanagedType.IUnknown)] out object mixedAttributeValue);
    }
}
