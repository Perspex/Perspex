// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "common.h"
#include "window.h"
#include "KeyTransform.h"
#include "cursor.h"
#include "menu.h"
#include <OpenGL/gl.h>

class SoftwareDrawingOperation
{
public:
    void* Data = 0;
    AvnFramebuffer Desc;
    void Alloc(NSView* view)
    {
        auto logicalSize = [view frame].size;
        auto pixelSize = [view convertSizeToBacking:logicalSize];
        int w = pixelSize.width;
        int h = pixelSize.height;
        int stride = w * 4;
        Data = malloc(h * stride);
        Desc = {
            .Data = Data,
            .Stride = stride,
            .Width = w,
            .Height = h,
            .PixelFormat = kAvnRgba8888,
            .Dpi = AvnVector { .X = w / logicalSize.width * 96, .Y = h / logicalSize.height * 96}
        };
    }
    
    void Dealloc()
    {
        if(Data != NULL)
        {
            free(Data);
            Data = NULL;
        }
    }
    
    ~SoftwareDrawingOperation()
    {
        Dealloc();
    }
};

class WindowBaseImpl : public virtual ComSingleObject<IAvnWindowBase, &IID_IAvnWindowBase>, public INSWindowHolder
{
private:
    NSCursor* cursor;

public:
    FORWARD_IUNKNOWN()
    virtual ~WindowBaseImpl()
    {
        View = NULL;
        Window = NULL;
    }
    AvnView* View;
    AvnWindow* Window;
    ComPtr<IAvnWindowBaseEvents> BaseEvents;
    SoftwareDrawingOperation CurrentSwDrawingOperation;
    AvnPoint lastPositionSet;
    NSString* _lastTitle;
    IAvnAppMenu* _mainMenu;
    
    WindowBaseImpl(IAvnWindowBaseEvents* events)
    {
        _mainMenu = nullptr;
        BaseEvents = events;
        View = [[AvnView alloc] initWithParent:this];

        Window = [[AvnWindow alloc] initWithParent:this];
        
        lastPositionSet.X = 100;
        lastPositionSet.Y = 100;
        _lastTitle = @"";
        
        [Window setStyleMask:NSWindowStyleMaskBorderless];
        [Window setBackingType:NSBackingStoreBuffered];
        [Window setContentView: View];
    }
    
    virtual AvnWindow* GetNSWindow() override
    {
        return Window;
    }
    
    virtual HRESULT Show() override
    {
        @autoreleasepool
        {
            SetPosition(lastPositionSet);
            UpdateStyle();
            
            [Window makeKeyAndOrderFront:Window];
            [NSApp activateIgnoringOtherApps:YES];
            
            [Window setTitle:_lastTitle];
            [Window setTitleVisibility:NSWindowTitleVisible];
        
            return S_OK;
        }
    }
    
    virtual HRESULT Hide () override
    {
        @autoreleasepool
        {
            if(Window != nullptr)
            {
                [Window orderOut:Window];
                [Window restoreParentWindow];
            }
            
            return S_OK;
        }
    }
    
    virtual HRESULT Activate () override
    {
        @autoreleasepool
        {
            if(Window != nullptr)
            {
                [Window makeKeyWindow];
                [NSApp activateIgnoringOtherApps:YES];
            }
        }
        
        return S_OK;
    }
    
    virtual HRESULT SetTopMost (bool value) override
    {
        @autoreleasepool
        {
            [Window setLevel: value ? NSFloatingWindowLevel : NSNormalWindowLevel];
            
            return S_OK;
        }
    }
    
    virtual HRESULT Close() override
    {
        @autoreleasepool
        {
            [Window close];
            return S_OK;
        }
    }
    
    virtual HRESULT GetClientSize(AvnSize* ret) override
    {
        @autoreleasepool
        {
            if(ret == nullptr)
                return E_POINTER;
            auto frame = [View frame];
            ret->Width = frame.size.width;
            ret->Height = frame.size.height;
            return S_OK;
        }
    }
    
    virtual HRESULT GetScaling (double* ret) override
    {
        @autoreleasepool
        {
            if(ret == nullptr)
                return E_POINTER;
            
            if(Window == nullptr)
            {
                *ret = 1;
                return S_OK;
            }
            
            *ret = [Window backingScaleFactor];
            return S_OK;
        }
    }
    
    virtual HRESULT SetMinMaxSize (AvnSize minSize, AvnSize maxSize) override
    {
        @autoreleasepool
        {
            [Window setMinSize: ToNSSize(minSize)];
            [Window setMaxSize: ToNSSize(maxSize)];
        
            return S_OK;
        }
    }
    
    virtual HRESULT Resize(double x, double y) override
    {
        @autoreleasepool
        {
            [Window setContentSize:NSSize{x, y}];
            
            return S_OK;
        }
    }
    
    virtual HRESULT Invalidate (AvnRect rect) override
    {
        @autoreleasepool
        {
            [View setNeedsDisplayInRect:[View frame]];
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetMainMenu(IAvnAppMenu* menu) override
    {
        _mainMenu = menu;
        
        auto nativeMenu = dynamic_cast<AvnAppMenu*>(menu);
        
        auto nsmenu = nativeMenu->GetNative();
        
        [Window applyMenu:nsmenu];
        
        return S_OK;
    }
    
    virtual HRESULT ObtainMainMenu(IAvnAppMenu** ret) override
    {
        if(ret == nullptr)
        {
            return E_POINTER;
        }
        
        *ret = _mainMenu;
        
        return S_OK;
    }
    
    virtual bool TryLock() override
    {
        @autoreleasepool
        {
            return [View lockFocusIfCanDraw] == YES;
        }
    }
    
    virtual void Unlock() override
    {
        @autoreleasepool
        {
            [View unlockFocus];
        }
    }
    
    virtual HRESULT BeginMoveDrag () override
    {
        @autoreleasepool
        {
            auto lastEvent = [View lastMouseDownEvent];
            
            if(lastEvent == nullptr)
            {
                return S_OK;
            }
            
            [Window performWindowDragWithEvent:lastEvent];
            
            return S_OK;
        }
    }
    
    virtual HRESULT GetPosition (AvnPoint* ret) override
    {
        @autoreleasepool
        {
            if(ret == nullptr)
            {
                return E_POINTER;
            }
            
            auto frame = [Window frame];
            
            ret->X = frame.origin.x;
            ret->Y = frame.origin.y + frame.size.height;
            
            *ret = ConvertPointY(*ret);
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetPosition (AvnPoint point) override
    {
        @autoreleasepool
        {
            lastPositionSet = point;
            [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(point))];
            
            return S_OK;
        }
    }
    
    virtual HRESULT PointToClient (AvnPoint point, AvnPoint* ret) override
    {
        @autoreleasepool
        {
            if(ret == nullptr)
            {
                return E_POINTER;
            }
            
            point = ConvertPointY(point);
            auto viewPoint = [Window convertScreenToBase:ToNSPoint(point)];
            
            *ret = [View translateLocalPoint:ToAvnPoint(viewPoint)];
            
            return S_OK;
        }
    }
    
    virtual HRESULT PointToScreen (AvnPoint point, AvnPoint* ret) override
    {
        @autoreleasepool
        {
            if(ret == nullptr)
            {
                return E_POINTER;
            }
            
            auto cocoaViewPoint =  ToNSPoint([View translateLocalPoint:point]);
            auto cocoaScreenPoint = [Window convertBaseToScreen:cocoaViewPoint];
            *ret = ConvertPointY(ToAvnPoint(cocoaScreenPoint));
            
            return S_OK;
        }
    }
    
    virtual HRESULT ThreadSafeSetSwRenderedFrame(AvnFramebuffer* fb, IUnknown* dispose) override
    {
        [View setSwRenderedFrame: fb dispose: dispose];
        return S_OK;
    }
    
    virtual HRESULT GetSoftwareFramebuffer(AvnFramebuffer*ret) override
    {
        if(![[NSThread currentThread] isMainThread])
            return E_FAIL;
        if(CurrentSwDrawingOperation.Data == NULL)
            CurrentSwDrawingOperation.Alloc(View);
        *ret = CurrentSwDrawingOperation.Desc;
        return S_OK;
    }
    
    virtual HRESULT SetCursor(IAvnCursor* cursor) override
    {
        @autoreleasepool
        {
            Cursor* avnCursor = dynamic_cast<Cursor*>(cursor);
            this->cursor = avnCursor->GetNative();
            UpdateCursor();
            
            if(avnCursor->IsHidden())
            {
                [NSCursor hide];
            }
            else
            {
                [NSCursor unhide];
            }
            
            return S_OK;
        }
    }

    virtual void UpdateCursor()
    {
        if (cursor != nil)
        {
            [cursor set];
        }
    }
    
    virtual HRESULT CreateGlRenderTarget(IAvnGlSurfaceRenderTarget** ppv) override
    {
        if(View == NULL)
            return E_FAIL;
        *ppv = ::CreateGlRenderTarget(Window, View);
        return S_OK;
    }

protected:
    virtual NSWindowStyleMask GetStyle()
    {
        return NSWindowStyleMaskBorderless;
    }
    
    void UpdateStyle()
    {
        [Window setStyleMask:GetStyle()];
    }
    
    virtual void OnResized ()
    {
        
    }
};

class WindowImpl : public virtual WindowBaseImpl, public virtual IAvnWindow, public IWindowStateChanged
{
private:
    bool _canResize = true;
    bool _hasDecorations = true;
    CGRect _lastUndecoratedFrame;
    AvnWindowState _lastWindowState;
    
    FORWARD_IUNKNOWN()
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    INTERFACE_MAP_ENTRY(IAvnWindow, IID_IAvnWindow)
    END_INTERFACE_MAP()
    virtual ~WindowImpl()
    {
    }
    
    ComPtr<IAvnWindowEvents> WindowEvents;
    WindowImpl(IAvnWindowEvents* events) : WindowBaseImpl(events)
    {
        WindowEvents = events;
        [Window setCanBecomeKeyAndMain];
        [Window disableCursorRects];
    }
    
    virtual HRESULT Show () override
    {
        @autoreleasepool
        {
            if([Window parentWindow] != nil)
                [[Window parentWindow] removeChildWindow:Window];
            WindowBaseImpl::Show();
            
            return SetWindowState(Normal);
        }
    }
    
    virtual HRESULT ShowDialog (IAvnWindow* parent) override
    {
        @autoreleasepool
        {
            if(parent == nullptr)
                return E_POINTER;

            auto cparent = dynamic_cast<WindowImpl*>(parent);
            if(cparent == nullptr)
                return E_INVALIDARG;
            
            [cparent->Window addChildWindow:Window ordered:NSWindowAbove];
            WindowBaseImpl::Show();
            
            return S_OK;
        }
    }
    
    void WindowStateChanged () override
    {
        AvnWindowState state;
        GetWindowState(&state);
        WindowEvents->WindowStateChanged(state);
    }
    
    bool UndecoratedIsMaximized ()
    {
        return CGRectEqualToRect([Window frame], [Window screen].visibleFrame);
    }
    
    bool IsZoomed ()
    {
        return _hasDecorations ? [Window isZoomed] : UndecoratedIsMaximized();
    }
    
    void DoZoom()
    {
        if (_hasDecorations)
        {
            [Window performZoom:Window];
        }
        else
        {
            if (!UndecoratedIsMaximized())
            {
                _lastUndecoratedFrame = [Window frame];
            }
            
            [Window zoom:Window];
        }
    }
    
    virtual HRESULT SetCanResize(bool value) override
    {
        @autoreleasepool
        {
            _canResize = value;
            UpdateStyle();
            return S_OK;
        }
    }
    
    virtual HRESULT SetHasDecorations(bool value) override
    {
        @autoreleasepool
        {
            _hasDecorations = value;
            UpdateStyle();
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetTitle (void* utf8title) override
    {
        @autoreleasepool
        {
            _lastTitle = [NSString stringWithUTF8String:(const char*)utf8title];
            [Window setTitle:_lastTitle];
            [Window setTitleVisibility:NSWindowTitleVisible];
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetTitleBarColor(AvnColor color) override
    {
        @autoreleasepool
        {
            float a = (float)color.Alpha / 255.0f;
            float r = (float)color.Red / 255.0f;
            float g = (float)color.Green / 255.0f;
            float b = (float)color.Blue / 255.0f;
            
            auto nscolor = [NSColor colorWithSRGBRed:r green:g blue:b alpha:a];
            
            // Based on the titlebar color we have to choose either light or dark
            // OSX doesnt let you set a foreground color for titlebar.
            if ((r*0.299 + g*0.587 + b*0.114) > 186.0f / 255.0f)
            {
                [Window setAppearance:[NSAppearance appearanceNamed:NSAppearanceNameVibrantLight]];
            }
            else
            {
                [Window setAppearance:[NSAppearance appearanceNamed:NSAppearanceNameVibrantDark]];
            }
            
            [Window setTitlebarAppearsTransparent:true];
            [Window setBackgroundColor:nscolor];
        }
        
        return S_OK;
    }
    
    virtual HRESULT GetWindowState (AvnWindowState*ret) override
    {
        @autoreleasepool
        {
            if(ret == nullptr)
            {
                return E_POINTER;
            }
            
            if([Window isMiniaturized])
            {
                *ret = Minimized;
                return S_OK;
            }
            
            if([Window isZoomed])
            {
                *ret = Maximized;
                return S_OK;
            }
            
            *ret = Normal;
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetWindowState (AvnWindowState state) override
    {
        @autoreleasepool
        {
            _lastWindowState = state;
            
            switch (state) {
                case Maximized:
                    lastPositionSet.X = 0;
                    lastPositionSet.Y = 0;
                    
                    if([Window isMiniaturized])
                    {
                        [Window deminiaturize:Window];
                    }
                    
                    if(!IsZoomed())
                    {
                        DoZoom();
                    }
                    break;
                    
                case Minimized:
                    [Window miniaturize:Window];
                    break;
                    
                default:
                    if([Window isMiniaturized])
                    {
                        [Window deminiaturize:Window];
                    }
                    
                    if(IsZoomed())
                    {
                        DoZoom();
                    }
                    break;
            }
            
            return S_OK;
        }
    }
    
protected:
    virtual void OnResized () override
    {
        auto windowState = [Window isMiniaturized] ? Minimized
        : (IsZoomed() ? Maximized : Normal);
        
        if (windowState != _lastWindowState)
        {
            _lastWindowState = windowState;
            
            WindowEvents->WindowStateChanged(windowState);
        }
    }
    
    virtual NSWindowStyleMask GetStyle() override
    {
        unsigned long s = NSWindowStyleMaskBorderless;
        if(_hasDecorations)
            s = s | NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskMiniaturizable;
        if(_canResize)
            s = s | NSWindowStyleMaskResizable;
        return s;
    }
};

NSArray* AllLoopModes = [NSArray arrayWithObjects: NSDefaultRunLoopMode, NSEventTrackingRunLoopMode, NSModalPanelRunLoopMode, NSRunLoopCommonModes, NSConnectionReplyMode, nil];

@implementation AvnView
{
    ComPtr<WindowBaseImpl> _parent;
    ComPtr<IUnknown> _swRenderedFrame;
    AvnFramebuffer _swRenderedFrameBuffer;
    bool _queuedDisplayFromThread;
    NSTrackingArea* _area;
    bool _isLeftPressed, _isMiddlePressed, _isRightPressed, _isMouseOver;
    NSEvent* _lastMouseDownEvent;
    bool _lastKeyHandled;
}

- (void)dealloc
{
}

- (void)onClosed
{
    _parent = NULL;
}

- (NSEvent*) lastMouseDownEvent
{
    return _lastMouseDownEvent;
}

-(AvnView*)  initWithParent: (WindowBaseImpl*) parent
{
    self = [super init];
    [self setWantsBestResolutionOpenGLSurface:true];
    _parent = parent;
    _area = nullptr;
    return self;
}

- (BOOL)isOpaque
{
    return YES;
}

- (BOOL)acceptsFirstResponder
{
    return true;
}

- (BOOL)acceptsFirstMouse:(NSEvent *)event
{
    return true;
}

- (BOOL)canBecomeKeyView
{
    return true;
}

-(void)setFrameSize:(NSSize)newSize
{
    [super setFrameSize:newSize];
    
    if(_area != nullptr)
    {
        [self removeTrackingArea:_area];
        _area = nullptr;
    }
    
    NSRect rect = NSZeroRect;
    rect.size = newSize;
    
    NSTrackingAreaOptions options = NSTrackingActiveAlways | NSTrackingMouseMoved | NSTrackingEnabledDuringMouseDrag;
    _area = [[NSTrackingArea alloc] initWithRect:rect options:options owner:self userInfo:nullptr];
    [self addTrackingArea:_area];
    
    _parent->UpdateCursor();

    _parent->BaseEvents->Resized(AvnSize{newSize.width, newSize.height});
}

- (void) drawFb: (AvnFramebuffer*) fb
{
    auto colorSpace = CGColorSpaceCreateDeviceRGB();
    auto dataProvider = CGDataProviderCreateWithData(NULL, fb->Data, fb->Height*fb->Stride, NULL);

    
    auto image = CGImageCreate(fb->Width, fb->Height, 8, 32, fb->Stride, colorSpace, kCGBitmapByteOrderDefault | kCGImageAlphaPremultipliedLast,
                               dataProvider, nullptr, false, kCGRenderingIntentDefault);
    
    auto ctx = [NSGraphicsContext currentContext];
    
    [ctx saveGraphicsState];
    auto cgc = [ctx CGContext];
    
    CGContextDrawImage(cgc, CGRect{0,0, fb->Width/(fb->Dpi.X/96), fb->Height/(fb->Dpi.Y/96)}, image);
    CGImageRelease(image);
    CGColorSpaceRelease(colorSpace);
    CGDataProviderRelease(dataProvider);
    
    [ctx restoreGraphicsState];

}

- (void)drawRect:(NSRect)dirtyRect
{
    _parent->BaseEvents->RunRenderPriorityJobs();
    @synchronized (self) {
        if(_swRenderedFrame != NULL)
        {
            [self drawFb: &_swRenderedFrameBuffer];
            return;
        }
    }
    
    auto swOp = &_parent->CurrentSwDrawingOperation;
    _parent->BaseEvents->Paint();
    if(swOp->Data != NULL)
        [self drawFb: &swOp->Desc];
    
    swOp->Dealloc();
    return;
}

-(void) redrawSelf
{
    @autoreleasepool
    {
        @synchronized(self)
        {
            if(!_queuedDisplayFromThread)
                return;
            _queuedDisplayFromThread = false;
        }
        [self setNeedsDisplayInRect:[self frame]];
        [self display];
        
    }
}

-(void) setSwRenderedFrame: (AvnFramebuffer*) fb dispose: (IUnknown*) dispose
{
    @autoreleasepool {
        @synchronized (self) {
            _swRenderedFrame = dispose;
            _swRenderedFrameBuffer = *fb;
            if(!_queuedDisplayFromThread)
            {
                _queuedDisplayFromThread = true;
                [self performSelector:@selector(redrawSelf) onThread:[NSThread mainThread] withObject:NULL waitUntilDone:false modes: AllLoopModes];
            }
        }
    }
}

- (AvnPoint) translateLocalPoint:(AvnPoint)pt
{
    pt.Y = [self bounds].size.height - pt.Y;
    return pt;
}

- (AvnPoint)toAvnPoint:(CGPoint)p
{
    AvnPoint result;
    
    result.X = p.x;
    result.Y = p.y;
    
    return result;
}

- (void) viewDidChangeBackingProperties
{
    _parent->BaseEvents->ScalingChanged([_parent->Window backingScaleFactor]);
    [super viewDidChangeBackingProperties];
}

- (bool) ignoreUserInput
{
    auto parentWindow = objc_cast<AvnWindow>([self window]);
    if(parentWindow == nil || ![parentWindow shouldTryToHandleEvents])
        return TRUE;
    return FALSE;
}

- (void)mouseEvent:(NSEvent *)event withType:(AvnRawMouseEventType) type
{
    if([self ignoreUserInput])
        return;
    
    [self becomeFirstResponder];
    auto localPoint = [self convertPoint:[event locationInWindow] toView:self];
    auto avnPoint = [self toAvnPoint:localPoint];
    auto point = [self translateLocalPoint:avnPoint];
    AvnVector delta;
    uint clickCount = 0;
    
    switch (type)
    {
        case Wheel:
        {
            auto speed = 5;
            
            if([event hasPreciseScrollingDeltas])
            {
                speed = 50;
            }
            
            delta.X = [event scrollingDeltaX] / speed;
            delta.Y = [event scrollingDeltaY] / speed;
            
            if(delta.X == 0 && delta.Y == 0)
            {
                return;
            }
            break;
        }
        
        case LeftButtonDown:
        case LeftButtonUp:
        case MiddleButtonDown:
        case MiddleButtonUp:
        case RightButtonDown:
        case RightButtonUp:
            clickCount = (uint)[event clickCount];
            break;
            
        default:
            break;
    }
    
    auto timestamp = [event timestamp] * 1000;
    auto modifiers = [self getModifiers:[event modifierFlags]];
    
    [self becomeFirstResponder];
    _parent->BaseEvents->RawMouseEvent(type, timestamp, modifiers, point, delta, clickCount);
    [super mouseMoved:event];
}

- (void)mouseMoved:(NSEvent *)event
{
    [self mouseEvent:event withType:Move];
}

- (void)mouseDown:(NSEvent *)event
{
    _isLeftPressed = true;
    _lastMouseDownEvent = event;
    [self mouseEvent:event withType:LeftButtonDown];
}

- (void)otherMouseDown:(NSEvent *)event
{
    _isMiddlePressed = true;
    _lastMouseDownEvent = event;
    [self mouseEvent:event withType:MiddleButtonDown];
}

- (void)rightMouseDown:(NSEvent *)event
{
    _isRightPressed = true;
    _lastMouseDownEvent = event;
    [self mouseEvent:event withType:RightButtonDown];
}

- (void)mouseUp:(NSEvent *)event
{
    _isLeftPressed = false;
    [self mouseEvent:event withType:LeftButtonUp];
}

- (void)otherMouseUp:(NSEvent *)event
{
    _isMiddlePressed = false;
    [self mouseEvent:event withType:MiddleButtonUp];
}

- (void)rightMouseUp:(NSEvent *)event
{
    _isRightPressed = false;
    [self mouseEvent:event withType:RightButtonUp];
}

- (void)mouseDragged:(NSEvent *)event
{
    [self mouseEvent:event withType:Move];
    [super mouseDragged:event];
}

- (void)otherMouseDragged:(NSEvent *)event
{
    [self mouseEvent:event withType:Move];
    [super otherMouseDragged:event];
}

- (void)rightMouseDragged:(NSEvent *)event
{
    [self mouseEvent:event withType:Move];
    [super rightMouseDragged:event];
}

- (void)scrollWheel:(NSEvent *)event
{
    [self mouseEvent:event withType:Wheel];
    [super scrollWheel:event];
}

- (void)mouseEntered:(NSEvent *)event
{
    _isMouseOver = true;
    [super mouseEntered:event];
}

- (void)mouseExited:(NSEvent *)event
{
    _isMouseOver = false;
    [self mouseEvent:event withType:LeaveWindow];
    [super mouseExited:event];
} 

- (void) keyboardEvent: (NSEvent *) event withType: (AvnRawKeyEventType)type
{
    if([self ignoreUserInput])
        return;
    auto key = s_KeyMap[[event keyCode]];
    
    auto timestamp = [event timestamp] * 1000;
    auto modifiers = [self getModifiers:[event modifierFlags]];
     
    _lastKeyHandled = _parent->BaseEvents->RawKeyEvent(type, timestamp, modifiers, key);
}

- (BOOL)performKeyEquivalent:(NSEvent *)event
{
    bool result = _lastKeyHandled;
    
    _lastKeyHandled = false;
    
    return result;
}

- (void)keyDown:(NSEvent *)event
{
    [self keyboardEvent:event withType:KeyDown];
    [[self inputContext] handleEvent:event];
    [super keyDown:event];
}

- (void)keyUp:(NSEvent *)event
{
    [self keyboardEvent:event withType:KeyUp];
    [super keyUp:event];
}

- (AvnInputModifiers)getModifiers:(NSEventModifierFlags)mod
{
    unsigned int rv = 0;
    
    if (mod & NSEventModifierFlagControl)
        rv |= Control;
    if (mod & NSEventModifierFlagShift)
        rv |= Shift;
    if (mod & NSEventModifierFlagOption)
        rv |= Alt;
    if (mod & NSEventModifierFlagCommand)
        rv |= Windows;
    
    if (_isLeftPressed)
        rv |= LeftMouseButton;
    if (_isMiddlePressed)
        rv |= MiddleMouseButton;
    if (_isRightPressed)
        rv |= RightMouseButton;
    
    return (AvnInputModifiers)rv;
}

- (BOOL)hasMarkedText
{
    return _lastKeyHandled;
}

- (NSRange)markedRange
{
    return NSMakeRange(NSNotFound, 0);
}

- (NSRange)selectedRange
{
    return NSMakeRange(NSNotFound, 0);
}

- (void)setMarkedText:(id)string selectedRange:(NSRange)selectedRange replacementRange:(NSRange)replacementRange
{
    
}

- (void)unmarkText
{
    
}

- (NSArray<NSString *> *)validAttributesForMarkedText
{
    return [NSArray new];
}

- (NSAttributedString *)attributedSubstringForProposedRange:(NSRange)range actualRange:(NSRangePointer)actualRange
{
    return [NSAttributedString new];
}

- (void)insertText:(id)string replacementRange:(NSRange)replacementRange
{
    if(!_lastKeyHandled)
    {
        _lastKeyHandled = _parent->BaseEvents->RawTextInputEvent(0, [string UTF8String]);
    }
}

- (NSUInteger)characterIndexForPoint:(NSPoint)point
{
    return 0;
}

- (NSRect)firstRectForCharacterRange:(NSRange)range actualRange:(NSRangePointer)actualRange
{
    CGRect result;
    
    return result;
}
@end


@implementation AvnWindow
{
    ComPtr<WindowBaseImpl> _parent;
    bool _canBecomeKeyAndMain;
    bool _closed;
    NSMenu* _menu;
    bool _isAppMenuApplied;
}

+(void)closeAll
{
    NSArray<NSWindow*>* windows = [NSArray arrayWithArray:[NSApp windows]];
    auto numWindows = [windows count];
    
    for(int i = 0; i < numWindows; i++)
    {
        [[windows objectAtIndex:i] performClose:nil];
    }
}

- (void)dealloc
{
}

- (void)pollModalSession:(nonnull NSModalSession)session
{
    auto response = [NSApp runModalSession:session];
    
    if(response == NSModalResponseContinue)
    {
        dispatch_async(dispatch_get_main_queue(), ^{
            [self pollModalSession:session];
        });
    }
    else if (!_closed)
    {
        [self orderOut:self];
        [NSApp endModalSession:session];
    }
}

-(void) applyMenu:(NSMenu *)menu
{
    if(menu == nullptr)
    {
        menu = [NSMenu new];
    }
    
    _menu = menu;
    
    if ([self isKeyWindow])
    {
        auto appMenu = ::GetAppMenuItem();
        
        if(appMenu != nullptr)
        {
            [[appMenu menu] removeItem:appMenu];
            
            [_menu insertItem:appMenu atIndex:0];
            
            _isAppMenuApplied = true;
        }
        
        [NSApp setMenu:menu];
    }
}

-(void) setCanBecomeKeyAndMain
{
    _canBecomeKeyAndMain = true;
}

-(AvnWindow*)  initWithParent: (WindowBaseImpl*) parent
{
    self = [super init];
    [self setReleasedWhenClosed:false];
    _parent = parent;
    [self setDelegate:self];
    return self;
}

- (BOOL)windowShouldClose:(NSWindow *)sender
{
    auto window = dynamic_cast<WindowImpl*>(_parent.getRaw());
    
    if(window != nullptr)
    {
        return !window->WindowEvents->Closing();
    }
    
    return true;
}

- (void)windowWillClose:(NSNotification *)notification
{
    _closed = true;
    if(_parent)
    {
        ComPtr<WindowBaseImpl> parent = _parent;
        _parent = NULL;
        [self restoreParentWindow];
        parent->BaseEvents->Closed();
        [parent->View onClosed];
        dispatch_async(dispatch_get_main_queue(), ^{
            [self setContentView: nil];
        });
    }
}

-(BOOL)canBecomeKeyWindow
{
    return _canBecomeKeyAndMain;
}

-(BOOL)canBecomeMainWindow
{
    return _canBecomeKeyAndMain;
}

-(bool) activateAppropriateChild: (bool)activating
{
    for(NSWindow* uch in [self childWindows])
    {
        auto ch = objc_cast<AvnWindow>(uch);
        if(ch == nil)
            continue;
        [ch activateAppropriateChild:false];
        return FALSE;
    }
    
    if(!activating)
        [self makeKeyAndOrderFront:self];
    return TRUE;
}

-(bool)shouldTryToHandleEvents
{
    for(NSWindow* uch in [self childWindows])
    {
        auto ch = objc_cast<AvnWindow>(uch);
        if(ch == nil)
            continue;
        return FALSE;
    }
    return TRUE;
}

-(void)makeKeyWindow
{
    if([self activateAppropriateChild: true])
    {
        [super makeKeyWindow];
    }
}

-(void)becomeKeyWindow
{
    if([self activateAppropriateChild: true])
    {
        if(_menu == nullptr)
        {
            _menu = [NSMenu new];
        }
        
        auto appMenu = ::GetAppMenuItem();
        
        if(appMenu != nullptr)
        {
            [[appMenu menu] removeItem:appMenu];
            
            [_menu insertItem:appMenu atIndex:0];
            
            _isAppMenuApplied = true;
        }
        
        [NSApp setMenu:_menu];
        
        _parent->BaseEvents->Activated();
        [super becomeKeyWindow];
    }
}

-(void) restoreParentWindow;
{
    auto parent = objc_cast<AvnWindow>([self parentWindow]);
    if(parent != nil)
    {
        [parent removeChildWindow:self];
        [parent activateAppropriateChild: false];
    }
}

- (void)windowDidMiniaturize:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}

- (void)windowDidDeminiaturize:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}

- (BOOL)windowShouldZoom:(NSWindow *)window toFrame:(NSRect)newFrame
{
    return true;
}

-(void)resignKeyWindow
{
    if(_parent)
        _parent->BaseEvents->Deactivated();
    
    auto appMenuItem = ::GetAppMenuItem();
    
    if(appMenuItem != nullptr)
    {
        auto appMenu = ::GetAppMenu();
        
        auto nativeAppMenu = dynamic_cast<AvnAppMenu*>(appMenu);
        
        [[appMenuItem menu] removeItem:appMenuItem];
        
        [nativeAppMenu->GetNative() addItem:appMenuItem];
        
        [NSApp setMenu:nativeAppMenu->GetNative()];
    }
    else
    {
        [NSApp setMenu:nullptr];
    }
    
    // remove window menu items from appmenu?
    
    [super resignKeyWindow];
}

- (void)windowDidMove:(NSNotification *)notification
{
    AvnPoint position;
    _parent->GetPosition(&position);
    _parent->BaseEvents->PositionChanged(position);
}

// TODO this breaks resizing.
/*- (void)windowDidResize:(NSNotification *)notification
{
    
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}*/
@end

class PopupImpl : public virtual WindowBaseImpl, public IAvnPopup
{
private:
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    INTERFACE_MAP_ENTRY(IAvnPopup, IID_IAvnPopup)
    END_INTERFACE_MAP()
    virtual ~PopupImpl(){}
    ComPtr<IAvnWindowEvents> WindowEvents;
    PopupImpl(IAvnWindowEvents* events) : WindowBaseImpl(events)
    {
        WindowEvents = events;
        [Window setLevel:NSPopUpMenuWindowLevel];
    }
    
protected:
    virtual NSWindowStyleMask GetStyle() override
    {
        return NSWindowStyleMaskBorderless;
    }
    
    virtual HRESULT Resize(double x, double y) override
    {
        @autoreleasepool
        {
            [Window setContentSize:NSSize{x, y}];
            
            [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(lastPositionSet))];
            return S_OK;
        }
    }
};

extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events)
{
    @autoreleasepool
    {
        IAvnPopup* ptr = dynamic_cast<IAvnPopup*>(new PopupImpl(events));
        return ptr;
    }
}

extern IAvnWindow* CreateAvnWindow(IAvnWindowEvents*events)
{
    @autoreleasepool
    {
        IAvnWindow* ptr = (IAvnWindow*)new WindowImpl(events);
        return ptr;
    }
}
