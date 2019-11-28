// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "com.h"
#include "key.h"

#define AVNCOM(name, id) COMINTERFACE(name, 2e2cda0a, 9ae5, 4f1b, 8e, 20, 08, 1a, 04, 27, 9f, id)

struct IAvnWindowEvents;
struct IAvnWindow;
struct IAvnPopup;
struct IAvnMacOptions;
struct IAvnPlatformThreadingInterface;
struct IAvnSystemDialogEvents;
struct IAvnSystemDialogs;
struct IAvnScreens;
struct IAvnClipboard;
struct IAvnCursor;
struct IAvnCursorFactory;
struct IAvnGlFeature;
struct IAvnGlContext;
struct IAvnGlDisplay;
struct IAvnGlSurfaceRenderTarget;
struct IAvnGlSurfaceRenderingSession;
struct IAvnAppMenu;
struct IAvnAppMenuItem;

struct AvnSize
{
    double Width, Height;
};

struct AvnPixelSize
{
    int Width, Height;
};

struct AvnRect
{
    double X, Y, Width, Height;
};

struct AvnVector
{
    double X, Y;
};

struct AvnPoint
{
    double X, Y;
};

struct AvnScreen
{
    AvnRect Bounds;
    AvnRect WorkingArea;
    float PixelDensity;
    bool Primary;
};

enum AvnPixelFormat
{
    kAvnRgb565,
    kAvnRgba8888,
    kAvnBgra8888
};

struct AvnFramebuffer
{
    void* Data;
    int Width;
    int Height;
    int Stride;
    AvnVector Dpi;
    AvnPixelFormat PixelFormat;
};

struct AvnColor
{
    unsigned char Alpha;
    unsigned char Red;
    unsigned char Green;
    unsigned char Blue;
};

enum AvnRawMouseEventType
{
    LeaveWindow,
    LeftButtonDown,
    LeftButtonUp,
    RightButtonDown,
    RightButtonUp,
    MiddleButtonDown,
    MiddleButtonUp,
    Move,
    Wheel,
    NonClientLeftButtonDown
};

enum AvnRawKeyEventType
{
    KeyDown,
    KeyUp
};

enum AvnInputModifiers
{
    AvnInputModifiersNone = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
    LeftMouseButton = 16,
    RightMouseButton = 32,
    MiddleMouseButton = 64
};

enum AvnWindowState
{
    Normal,
    Minimized,
    Maximized,
};

enum AvnStandardCursorType
{
    CursorArrow,
    CursorIbeam,
    CursorWait,
    CursorCross,
    CursorUpArrow,
    CursorSizeWestEast,
    CursorSizeNorthSouth,
    CursorSizeAll,
    CursorNo,
    CursorHand,
    CursorAppStarting,
    CursorHelp,
    CursorTopSide,
    CursorBottomSize,
    CursorLeftSide,
    CursorRightSide,
    CursorTopLeftCorner,
    CursorTopRightCorner,
    CursorBottomLeftCorner,
    CursorBottomRightCorner,
    CursorDragMove,
    CursorDragCopy,
    CursorDragLink,
    CursorNone
};

enum AvnWindowEdge
{
    WindowEdgeNorthWest,
    WindowEdgeNorth,
    WindowEdgeNorthEast,
    WindowEdgeWest,
    WindowEdgeEast,
    WindowEdgeSouthWest,
    WindowEdgeSouth,
    WindowEdgeSouthEast
};

AVNCOM(IAvaloniaNativeFactory, 01) : IUnknown
{
public:
    virtual HRESULT Initialize() = 0;
    virtual IAvnMacOptions* GetMacOptions() = 0;
    virtual HRESULT CreateWindow(IAvnWindowEvents* cb, IAvnWindow** ppv) = 0;
    virtual HRESULT CreatePopup (IAvnWindowEvents* cb, IAvnPopup** ppv) = 0;
    virtual HRESULT CreatePlatformThreadingInterface(IAvnPlatformThreadingInterface** ppv) = 0;
    virtual HRESULT CreateSystemDialogs (IAvnSystemDialogs** ppv) = 0;
    virtual HRESULT CreateScreens (IAvnScreens** ppv) = 0;
    virtual HRESULT CreateClipboard(IAvnClipboard** ppv) = 0;
    virtual HRESULT CreateCursorFactory(IAvnCursorFactory** ppv) = 0;
    virtual HRESULT ObtainGlFeature(IAvnGlFeature** ppv) = 0;
    virtual HRESULT ObtainAppMenu(IAvnAppMenu** retOut) = 0;
    virtual HRESULT SetAppMenu(IAvnAppMenu* menu) = 0;
    virtual HRESULT CreateMenu (IAvnAppMenu** ppv) = 0;
    virtual HRESULT CreateMenuItem (IAvnAppMenuItem** ppv) = 0;
    virtual HRESULT CreateMenuItemSeperator (IAvnAppMenuItem** ppv) = 0;
};

AVNCOM(IAvnString, 17) : IUnknown
{
    virtual HRESULT Pointer(void**retOut) = 0;
    virtual HRESULT Length(int*ret) = 0;
};

AVNCOM(IAvnWindowBase, 02) : IUnknown
{
    virtual HRESULT Show() = 0;
    virtual HRESULT Hide () = 0;
    virtual HRESULT Close() = 0;
    virtual HRESULT Activate () = 0;
    virtual HRESULT GetClientSize(AvnSize*ret) = 0;
    virtual HRESULT GetScaling(double*ret)=0;
    virtual HRESULT SetMinMaxSize(AvnSize minSize, AvnSize maxSize) = 0;
    virtual HRESULT Resize(double width, double height) = 0;
    virtual HRESULT Invalidate (AvnRect rect) = 0;
    virtual HRESULT BeginMoveDrag () = 0;
    virtual HRESULT GetPosition (AvnPoint*ret) = 0;
    virtual HRESULT SetPosition (AvnPoint point) = 0;
    virtual HRESULT PointToClient (AvnPoint point, AvnPoint*ret) = 0;
    virtual HRESULT PointToScreen (AvnPoint point, AvnPoint*ret) = 0;
    virtual HRESULT ThreadSafeSetSwRenderedFrame(AvnFramebuffer* fb, IUnknown* dispose) = 0;
    virtual HRESULT SetTopMost (bool value) = 0;
    virtual HRESULT SetCursor(IAvnCursor* cursor) = 0;
    virtual HRESULT CreateGlRenderTarget(IAvnGlSurfaceRenderTarget** ret) = 0;
    virtual HRESULT GetSoftwareFramebuffer(AvnFramebuffer*ret) = 0;
    virtual HRESULT SetMainMenu(IAvnAppMenu* menu) = 0;
    virtual HRESULT ObtainMainMenu(IAvnAppMenu** retOut) = 0;
    virtual bool TryLock() = 0;
    virtual void Unlock() = 0;
};

AVNCOM(IAvnPopup, 03) : virtual IAvnWindowBase
{
    
};

AVNCOM(IAvnWindow, 04) : virtual IAvnWindowBase
{
    virtual HRESULT ShowDialog (IAvnWindow* parent) = 0;
    virtual HRESULT SetCanResize(bool value) = 0;
    virtual HRESULT SetHasDecorations(bool value) = 0;
    virtual HRESULT SetTitle (void* utf8Title) = 0;
    virtual HRESULT SetTitleBarColor (AvnColor color) = 0;
    virtual HRESULT SetWindowState(AvnWindowState state) = 0;
    virtual HRESULT GetWindowState(AvnWindowState*ret) = 0;
};

AVNCOM(IAvnWindowBaseEvents, 05) : IUnknown
{
    virtual HRESULT Paint() = 0;
    virtual void Closed() = 0;
    virtual void Activated() = 0;
    virtual void Deactivated() = 0;
    virtual void Resized(const AvnSize& size) = 0;
    virtual void PositionChanged (AvnPoint position) = 0;
    virtual void RawMouseEvent (AvnRawMouseEventType type,
                                unsigned int timeStamp,
                                AvnInputModifiers modifiers,
                                AvnPoint point,
                                AvnVector delta,
                                unsigned int clickCount) = 0;
    virtual bool RawKeyEvent (AvnRawKeyEventType type, unsigned int timeStamp, AvnInputModifiers modifiers, unsigned int key) = 0;
    virtual bool RawTextInputEvent (unsigned int timeStamp, const char* text) = 0;
    virtual void ScalingChanged(double scaling) = 0;
    virtual void RunRenderPriorityJobs() = 0;
};


AVNCOM(IAvnWindowEvents, 06) : IAvnWindowBaseEvents
{
    /**
     * Closing Event
     * Called when the user presses the OS window close button.
     * return true to allow the close, return false to prevent close.
     */
    virtual bool Closing () = 0;
    
    virtual void WindowStateChanged (AvnWindowState state) = 0;
};

AVNCOM(IAvnMacOptions, 07) : IUnknown
{
    virtual HRESULT SetShowInDock(int show) = 0;
    virtual HRESULT SetApplicationTitle (void* utf8string) = 0;
};

AVNCOM(IAvnActionCallback, 08) : IUnknown
{
    virtual void Run() = 0;
};

AVNCOM(IAvnSignaledCallback, 09) : IUnknown
{
    virtual void Signaled(int priority, bool priorityContainsMeaningfulValue) = 0;
};

AVNCOM(IAvnLoopCancellation, 0a) : IUnknown
{
    virtual void Cancel() = 0;
};

AVNCOM(IAvnPlatformThreadingInterface, 0b) : IUnknown
{
    virtual bool GetCurrentThreadIsLoopThread() = 0;
    virtual void SetSignaledCallback(IAvnSignaledCallback* cb) = 0;
    virtual IAvnLoopCancellation* CreateLoopCancellation() = 0;
    virtual HRESULT RunLoop(IAvnLoopCancellation* cancel) = 0;
    // Can't pass int* to sharpgentools for some reason
    virtual void Signal(int priority) = 0;
    virtual IUnknown* StartTimer(int priority, int ms, IAvnActionCallback* callback) = 0;
};

AVNCOM(IAvnSystemDialogEvents, 0c) : IUnknown
{
    virtual void OnCompleted (int numResults, void* ptrFirstResult) = 0;
};

AVNCOM(IAvnSystemDialogs, 0d) : IUnknown
{
    virtual void SelectFolderDialog (IAvnWindow* parentWindowHandle,
                                     IAvnSystemDialogEvents* events,
                                     const char* title,
                                     const char* initialPath) = 0;
    
    virtual void OpenFileDialog (IAvnWindow* parentWindowHandle,
                                 IAvnSystemDialogEvents* events,
                                 bool allowMultiple,
                                 const char* title,
                                 const char* initialDirectory,
                                 const char* initialFile,
                                 const char* filters) = 0;
    
    virtual void SaveFileDialog (IAvnWindow* parentWindowHandle,
                                 IAvnSystemDialogEvents* events,
                                 const char* title,
                                 const char* initialDirectory,
                                 const char* initialFile,
                                 const char* filters) = 0;
};

AVNCOM(IAvnScreens, 0e) : IUnknown
{
    virtual HRESULT GetScreenCount (int* ret) = 0;
    virtual HRESULT GetScreen (int index, AvnScreen* ret) = 0;
};

AVNCOM(IAvnClipboard, 0f) : IUnknown
{
    virtual HRESULT GetText (IAvnString**ppv) = 0;
    virtual HRESULT SetText (void* utf8Text) = 0;
    virtual HRESULT Clear() = 0;
};

AVNCOM(IAvnCursor, 10) : IUnknown
{
};

AVNCOM(IAvnCursorFactory, 11) : IUnknown
{
    virtual HRESULT GetCursor (AvnStandardCursorType cursorType, IAvnCursor** retOut) = 0;
};


AVNCOM(IAvnGlFeature, 12) : IUnknown
{
    virtual HRESULT ObtainDisplay(IAvnGlDisplay**retOut) = 0;
    virtual HRESULT ObtainImmediateContext(IAvnGlContext**retOut) = 0;
};

AVNCOM(IAvnGlDisplay, 13) : IUnknown
{
    virtual HRESULT GetSampleCount(int* ret) = 0;
    virtual HRESULT GetStencilSize(int* ret) = 0;
    virtual HRESULT ClearContext() = 0;
    virtual void* GetProcAddress(char* proc) = 0;
};

AVNCOM(IAvnGlContext, 14) : IUnknown
{
    virtual HRESULT MakeCurrent() = 0;
};

AVNCOM(IAvnGlSurfaceRenderTarget, 15) : IUnknown
{
    virtual HRESULT BeginDrawing(IAvnGlSurfaceRenderingSession** ret) = 0;
};

AVNCOM(IAvnGlSurfaceRenderingSession, 16) : IUnknown
{
    virtual HRESULT GetPixelSize(AvnPixelSize* ret) = 0;
    virtual HRESULT GetScaling(double* ret) = 0;
};

AVNCOM(IAvnAppMenu, 17) : IUnknown
{
    virtual HRESULT AddItem (IAvnAppMenuItem* item) = 0;
    virtual HRESULT RemoveItem (IAvnAppMenuItem* item) = 0;
    virtual HRESULT SetTitle (void* utf8String) = 0;
    virtual HRESULT Clear () = 0;
};

AVNCOM(IAvnPredicateCallback, 18) : IUnknown
{
    virtual bool Evaluate() = 0;
};

AVNCOM(IAvnAppMenuItem, 19) : IUnknown
{
    virtual HRESULT SetSubMenu (IAvnAppMenu* menu) = 0;
    virtual HRESULT SetTitle (void* utf8String) = 0;
    virtual HRESULT SetGesture (void* utf8String, AvnInputModifiers modifiers) = 0;
    virtual HRESULT SetAction (IAvnPredicateCallback* predicate, IAvnActionCallback* callback) = 0;
};

extern "C" IAvaloniaNativeFactory* CreateAvaloniaNative();
