// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Description: Invoke pattern provider interface

using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("54fcb24b-e18e-47a2-b4d3-eccbe77599a2")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInvokeProvider
    {
        void Invoke();
    }
}
