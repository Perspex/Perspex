using System;
using System.Runtime.InteropServices;

#nullable enable

namespace Avalonia.Win32.Interop.Automation
{
    [ComVisible(true)]
    [Guid("670c3006-bf4c-428b-8534-e1848f645122")]
    public enum NavigateDirection
    {
        Parent,
        NextSibling,
        PreviousSibling,
        FirstChild,
        LastChild,
    }

    // NOTE: This interface needs to be public otherwise Navigate is never called. I have no idea
    // why given that IRawElementProviderSimple and IRawElementProviderFragmentRoot seem to get
    // called fine when they're internal, but I lost a couple of days to this.
    [ComVisible(true)]
    [Guid("f7063da8-8359-439c-9297-bbc5299a7d87")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IRawElementProviderFragment : IRawElementProviderSimple
    {
        IRawElementProviderFragment? Navigate(NavigateDirection direction);
        int[]? GetRuntimeId();
        Rect BoundingRectangle { get; }
        IRawElementProviderSimple[]? GetEmbeddedFragmentRoots();
        void SetFocus();
        IRawElementProviderFragmentRoot? FragmentRoot { get; }
    }
}
