#include "common.h"
#include "window.h"
#include "KeyTransform.h"
#include "cursor.h"
#include "menu.h"
#include <OpenGL/gl.h>
#include "rendertarget.h"

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
    AutoFitContentView* StandardContainer;
    AvnView* View;
    AvnWindow* Window;
    ComPtr<IAvnWindowBaseEvents> BaseEvents;
    ComPtr<IAvnGlContext> _glContext;
    NSObject<IRenderTarget>* renderTarget;
    AvnPoint lastPositionSet;
    NSString* _lastTitle;
    IAvnMenu* _mainMenu;
    
    bool _shown;
    bool _inResize;
    
    WindowBaseImpl(IAvnWindowBaseEvents* events, IAvnGlContext* gl)
    {
        _shown = false;
        _inResize = false;
        _mainMenu = nullptr;
        BaseEvents = events;
        _glContext = gl;
        renderTarget = [[IOSurfaceRenderTarget alloc] initWithOpenGlContext: gl];
        View = [[AvnView alloc] initWithParent:this];
        StandardContainer = [[AutoFitContentView new] initWithContent:View];

        Window = [[AvnWindow alloc] initWithParent:this];
        
        lastPositionSet.X = 100;
        lastPositionSet.Y = 100;
        _lastTitle = @"";
        
        [Window setStyleMask:NSWindowStyleMaskBorderless];
        [Window setBackingType:NSBackingStoreBuffered];
        
        [Window setOpaque:false];
    }
    
    virtual HRESULT ObtainNSWindowHandle(void** ret) override
    {
        START_COM_CALL;
        
        if (ret == nullptr)
        {
            return E_POINTER;
        }
        
        *ret =  (__bridge void*)Window;
        
        return S_OK;
    }
    
    virtual HRESULT ObtainNSWindowHandleRetained(void** ret) override
    {
        START_COM_CALL;
        
        if (ret == nullptr)
        {
            return E_POINTER;
        }
        
        *ret =  (__bridge_retained void*)Window;
        
        return S_OK;
    }
    
    virtual HRESULT ObtainNSViewHandle(void** ret) override
    {
        START_COM_CALL;
        
        if (ret == nullptr)
        {
            return E_POINTER;
        }
        
        *ret =  (__bridge void*)View;
        
        return S_OK;
    }
    
    virtual HRESULT ObtainNSViewHandleRetained(void** ret) override
    {
        START_COM_CALL;
        
        if (ret == nullptr)
        {
            return E_POINTER;
        }
        
        *ret =  (__bridge_retained void*)View;
        
        return S_OK;
    }
    
    virtual AvnWindow* GetNSWindow() override
    {
        return Window;
    }
    
    virtual HRESULT Show(bool activate) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            SetPosition(lastPositionSet);
            UpdateStyle();
            
            [Window setContentView: StandardContainer];
            [Window setTitle:_lastTitle];
            
            if(ShouldTakeFocusOnShow() && activate)
            {
                [Window orderFront: Window];
                [Window makeKeyAndOrderFront:Window];
                [Window makeFirstResponder:View];
                [NSApp activateIgnoringOtherApps:YES];
            }
            else
            {
                [Window orderFront: Window];
            }
            
            _shown = true;
            
            return S_OK;
        }
    }
    
    virtual bool ShouldTakeFocusOnShow()
    {
        return true;
    }
    
    virtual HRESULT Hide () override
    {
        START_COM_CALL;
        
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
        START_COM_CALL;
        
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
        START_COM_CALL;
        
        @autoreleasepool
        {
            [Window setLevel: value ? NSFloatingWindowLevel : NSNormalWindowLevel];
            
            return S_OK;
        }
    }
    
    virtual HRESULT Close() override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if (Window != nullptr)
            {
                auto window = Window;
                Window = nullptr;
                
                [window close];
            }
            
            return S_OK;
        }
    }
    
    virtual HRESULT GetClientSize(AvnSize* ret) override
    {
        START_COM_CALL;
        
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
    
    virtual HRESULT GetFrameSize(AvnSize* ret) override
    {
        @autoreleasepool
        {
            if(ret == nullptr)
                return E_POINTER;
            
            auto frame = [Window frame];
            ret->Width = frame.size.width;
            ret->Height = frame.size.height;
            
            return S_OK;
        }
    }
    
    virtual HRESULT GetScaling (double* ret) override
    {
        START_COM_CALL;
        
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
        START_COM_CALL;
        
        @autoreleasepool
        {
            [Window setMinSize: ToNSSize(minSize)];
            [Window setMaxSize: ToNSSize(maxSize)];
        
            return S_OK;
        }
    }
    
    virtual HRESULT Resize(double x, double y) override
    {
        if(_inResize)
        {
            return S_OK;
        }
        
        _inResize = true;
        
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto maxSize = [Window maxSize];
            auto minSize = [Window minSize];
            
            if (x < minSize.width)
            {
                x = minSize.width;
            }
            
            if (y < minSize.height)
            {
                y = minSize.height;
            }
            
            if (x > maxSize.width)
            {
                x = maxSize.width;
            }
            
            if (y > maxSize.height)
            {
                y = maxSize.height;
            }
            
            @try
            {
                if(!_shown)
                {
                    BaseEvents->Resized(AvnSize{x,y});
                }
                
                [Window setContentSize:NSSize{x, y}];
            }
            @finally
            {
                _inResize = false;
            }
            
            return S_OK;
        }
    }
    
    virtual HRESULT Invalidate (AvnRect rect) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            [View setNeedsDisplayInRect:[View frame]];
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetMainMenu(IAvnMenu* menu) override
    {
        START_COM_CALL;
        
        _mainMenu = menu;
        
        auto nativeMenu = dynamic_cast<AvnAppMenu*>(menu);
        
        auto nsmenu = nativeMenu->GetNative();
        
        [Window applyMenu:nsmenu];
        
        if ([Window isKeyWindow])
        {
            [Window showWindowMenuWithAppMenu];
        }
        
        return S_OK;
    }
    
    virtual HRESULT BeginMoveDrag () override
    {
        START_COM_CALL;
        
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
    
    virtual HRESULT BeginResizeDrag (AvnWindowEdge edge) override
    {
        START_COM_CALL;
        
        return S_OK;
    }
    
    virtual HRESULT GetPosition (AvnPoint* ret) override
    {
        START_COM_CALL;
        
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
        START_COM_CALL;
        
        @autoreleasepool
        {
            lastPositionSet = point;
            [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(point))];
            
            return S_OK;
        }
    }
    
    virtual HRESULT PointToClient (AvnPoint point, AvnPoint* ret) override
    {
        START_COM_CALL;
        
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
        START_COM_CALL;
        
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
        START_COM_CALL;
        
        [View setSwRenderedFrame: fb dispose: dispose];
        return S_OK;
    }
    
    virtual HRESULT SetCursor(IAvnCursor* cursor) override
    {
        START_COM_CALL;
        
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
        START_COM_CALL;
        
        if(View == NULL)
            return E_FAIL;
        *ppv = [renderTarget createSurfaceRenderTarget];
        return *ppv == nil ? E_FAIL : S_OK;
    }
    
    virtual HRESULT CreateNativeControlHost(IAvnNativeControlHost** retOut) override
    {
        START_COM_CALL;
        
        if(View == NULL)
            return E_FAIL;
        *retOut = ::CreateNativeControlHost(View);
        return S_OK;
    }
    
    virtual HRESULT SetBlurEnabled (bool enable) override
    {
        START_COM_CALL;
        
        [StandardContainer ShowBlur:enable];
        
        return S_OK;
    }
    
    virtual HRESULT BeginDragAndDropOperation(AvnDragDropEffects effects, AvnPoint point,
                                              IAvnClipboard* clipboard, IAvnDndResultCallback* cb,
                                              void* sourceHandle) override
    {
        START_COM_CALL;
        
        auto item = TryGetPasteboardItem(clipboard);
        [item setString:@"" forType:GetAvnCustomDataType()];
        if(item == nil)
            return E_INVALIDARG;
        if(View == NULL)
            return E_FAIL;
        
        auto nsevent = [NSApp currentEvent];
        auto nseventType = [nsevent type];
        
        // If current event isn't a mouse one (probably due to malfunctioning user app)
        // attempt to forge a new one
        if(!((nseventType >= NSEventTypeLeftMouseDown && nseventType <= NSEventTypeMouseExited)
           || (nseventType >= NSEventTypeOtherMouseDown && nseventType <= NSEventTypeOtherMouseDragged)))
        {
            auto nspoint = [Window convertBaseToScreen: ToNSPoint(point)];
            CGPoint cgpoint = NSPointToCGPoint(nspoint);
            auto cgevent = CGEventCreateMouseEvent(NULL, kCGEventLeftMouseDown, cgpoint, kCGMouseButtonLeft);
            nsevent = [NSEvent eventWithCGEvent: cgevent];
            CFRelease(cgevent);
        }
        
        auto dragItem = [[NSDraggingItem alloc] initWithPasteboardWriter: item];
        
        auto dragItemImage = [NSImage imageNamed:NSImageNameMultipleDocuments];
        NSRect dragItemRect = {(float)point.X, (float)point.Y, [dragItemImage size].width, [dragItemImage size].height};
        [dragItem setDraggingFrame: dragItemRect contents: dragItemImage];
        
        int op = 0; int ieffects = (int)effects;
        if((ieffects & (int)AvnDragDropEffects::Copy) != 0)
            op |= NSDragOperationCopy;
        if((ieffects & (int)AvnDragDropEffects::Link) != 0)
            op |= NSDragOperationLink;
        if((ieffects & (int)AvnDragDropEffects::Move) != 0)
            op |= NSDragOperationMove;
        [View beginDraggingSessionWithItems: @[dragItem] event: nsevent
                                     source: CreateDraggingSource((NSDragOperation) op, cb, sourceHandle)];
        return S_OK;
    }

protected:
    virtual NSWindowStyleMask GetStyle()
    {
        return NSWindowStyleMaskBorderless;
    }
    
    void UpdateStyle()
    {
        [Window setStyleMask: GetStyle()];
    }
    
public:
    virtual void OnResized ()
    {
        
    }
};

class WindowImpl : public virtual WindowBaseImpl, public virtual IAvnWindow, public IWindowStateChanged
{
private:
    bool _canResize;
    bool _fullScreenActive;
    SystemDecorations _decorations;
    AvnWindowState _lastWindowState;
    AvnWindowState _actualWindowState;
    bool _inSetWindowState;
    NSRect _preZoomSize;
    bool _transitioningWindowState;
    bool _isClientAreaExtended;
    AvnExtendClientAreaChromeHints _extendClientHints;
    
    FORWARD_IUNKNOWN()
    BEGIN_INTERFACE_MAP()
    INHERIT_INTERFACE_MAP(WindowBaseImpl)
    INTERFACE_MAP_ENTRY(IAvnWindow, IID_IAvnWindow)
    END_INTERFACE_MAP()
    virtual ~WindowImpl()
    {
    }
    
    ComPtr<IAvnWindowEvents> WindowEvents;
    WindowImpl(IAvnWindowEvents* events, IAvnGlContext* gl) : WindowBaseImpl(events, gl)
    {
        _isClientAreaExtended = false;
        _extendClientHints = AvnDefaultChrome;
        _fullScreenActive = false;
        _canResize = true;
        _decorations = SystemDecorationsFull;
        _transitioningWindowState = false;
        _inSetWindowState = false;
        _lastWindowState = Normal;
        _actualWindowState = Normal;
        WindowEvents = events;
        [Window setCanBecomeKeyAndMain];
        [Window disableCursorRects];
        [Window setTabbingMode:NSWindowTabbingModeDisallowed];
    }
    
    void HideOrShowTrafficLights ()
    {
        if (Window == nil)
        {
            return;
        }
        
        for (id subview in Window.contentView.superview.subviews) {
            if ([subview isKindOfClass:NSClassFromString(@"NSTitlebarContainerView")]) {
                NSView *titlebarView = [subview subviews][0];
                for (id button in titlebarView.subviews) {
                    if ([button isKindOfClass:[NSButton class]])
                    {
                        if(_isClientAreaExtended)
                        {
                            auto wantsChrome = (_extendClientHints & AvnSystemChrome) || (_extendClientHints & AvnPreferSystemChrome);
                            
                            [button setHidden: !wantsChrome];
                        }
                        else
                        {
                            [button setHidden: (_decorations != SystemDecorationsFull)];
                        }
                        
                        [button setWantsLayer:true];
                    }
                }
            }
        }
    }
    
    virtual HRESULT Show (bool activate) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            WindowBaseImpl::Show(activate);
            
            HideOrShowTrafficLights();
            
            return SetWindowState(_lastWindowState);
        }
    }
    
    virtual HRESULT SetEnabled (bool enable) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            [Window setEnabled:enable];
            return S_OK;
        }
    }
    
    virtual HRESULT SetParent (IAvnWindow* parent) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(parent == nullptr)
                return E_POINTER;

            auto cparent = dynamic_cast<WindowImpl*>(parent);
            if(cparent == nullptr)
                return E_INVALIDARG;
            
            [cparent->Window addChildWindow:Window ordered:NSWindowAbove];
            
            UpdateStyle();
            
            return S_OK;
        }
    }
    
    void StartStateTransition () override
    {
        _transitioningWindowState = true;
    }
    
    void EndStateTransition () override
    {
        _transitioningWindowState = false;
    }
    
    SystemDecorations Decorations () override
    {
        return _decorations;
    }
    
    AvnWindowState WindowState () override
    {
        return _lastWindowState;
    }
    
    void WindowStateChanged () override
    {
        if(_shown && !_inSetWindowState && !_transitioningWindowState)
        {
            AvnWindowState state;
            GetWindowState(&state);
            
            if(_lastWindowState != state)
            {
                if(_isClientAreaExtended)
                {
                    if(_lastWindowState == FullScreen)
                    {
                        // we exited fs.
                       if(_extendClientHints & AvnOSXThickTitleBar)
                       {
                          Window.toolbar = [NSToolbar new];
                          Window.toolbar.showsBaselineSeparator = false;
                       }

                       [Window setTitlebarAppearsTransparent:true];

                       [StandardContainer setFrameSize: StandardContainer.frame.size];
                    }
                    else if(state == FullScreen)
                    {
                        // we entered fs.
                        if(_extendClientHints & AvnOSXThickTitleBar)
                        {
                            Window.toolbar = nullptr;
                        }
                       
                        [Window setTitlebarAppearsTransparent:false];
                        
                        [StandardContainer setFrameSize: StandardContainer.frame.size];
                    }
                }
                
                _lastWindowState = state;
                _actualWindowState = state;
                WindowEvents->WindowStateChanged(state);
            }
        }
    }
    
    bool UndecoratedIsMaximized ()
    {
        auto windowSize = [Window frame];
        auto available = [Window screen].visibleFrame;
        return CGRectEqualToRect(windowSize, available);
    }
    
    bool IsZoomed ()
    {
        return _decorations == SystemDecorationsFull ? [Window isZoomed] : UndecoratedIsMaximized();
    }
    
    void DoZoom()
    {
        switch (_decorations)
        {
            case SystemDecorationsNone:
            case SystemDecorationsBorderOnly:
                [Window setFrame:[Window screen].visibleFrame display:true];
                break;

            
            case SystemDecorationsFull:
                [Window performZoom:Window];
                break;
        }
    }
    
    virtual HRESULT SetCanResize(bool value) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            _canResize = value;
            UpdateStyle();
            return S_OK;
        }
    }
    
    virtual HRESULT SetDecorations(SystemDecorations value) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            auto currentWindowState = _lastWindowState;
            _decorations = value;
            
            if(_fullScreenActive)
            {
                return S_OK;
            }
            
            UpdateStyle();
            
            HideOrShowTrafficLights();

            switch (_decorations)
            {
                case SystemDecorationsNone:
                    [Window setHasShadow:NO];
                    [Window setTitleVisibility:NSWindowTitleHidden];
                    [Window setTitlebarAppearsTransparent:YES];
                    
                    if(currentWindowState == Maximized)
                    {
                        if(!UndecoratedIsMaximized())
                        {
                            DoZoom();
                        }
                    }
                    break;

                case SystemDecorationsBorderOnly:
                    [Window setHasShadow:YES];
                    [Window setTitleVisibility:NSWindowTitleHidden];
                    [Window setTitlebarAppearsTransparent:YES];
                    
                    if(currentWindowState == Maximized)
                    {
                        if(!UndecoratedIsMaximized())
                        {
                            DoZoom();
                        }
                    }
                    break;

                case SystemDecorationsFull:
                    [Window setHasShadow:YES];
                    [Window setTitleVisibility:NSWindowTitleVisible];
                    [Window setTitlebarAppearsTransparent:NO];
                    [Window setTitle:_lastTitle];
                    
                    if(currentWindowState == Maximized)
                    {
                        auto newFrame = [Window contentRectForFrameRect:[Window frame]].size;
                        
                        [View setFrameSize:newFrame];
                    }
                    break;
            }

            return S_OK;
        }
    }
    
    virtual HRESULT SetTitle (char* utf8title) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            _lastTitle = [NSString stringWithUTF8String:(const char*)utf8title];
            [Window setTitle:_lastTitle];
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetTitleBarColor(AvnColor color) override
    {
        START_COM_CALL;
        
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
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(ret == nullptr)
            {
                return E_POINTER;
            }
            
            if(([Window styleMask] & NSWindowStyleMaskFullScreen) == NSWindowStyleMaskFullScreen)
            {
                *ret = FullScreen;
                return S_OK;
            }
            
            if([Window isMiniaturized])
            {
                *ret = Minimized;
                return S_OK;
            }
            
            if(IsZoomed())
            {
                *ret = Maximized;
                return S_OK;
            }
            
            *ret = Normal;
            
            return S_OK;
        }
    }
    
    virtual HRESULT TakeFocusFromChildren () override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(Window == nil)
                return S_OK;
            if([Window isKeyWindow])
                [Window makeFirstResponder: View];
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetExtendClientArea (bool enable) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            _isClientAreaExtended = enable;
            
            if(enable)
            {
                Window.titleVisibility = NSWindowTitleHidden;
                
                [Window setTitlebarAppearsTransparent:true];
                
                auto wantsTitleBar = (_extendClientHints & AvnSystemChrome) || (_extendClientHints & AvnPreferSystemChrome);
                
                if (wantsTitleBar)
                {
                    [StandardContainer ShowTitleBar:true];
                }
                else
                {
                    [StandardContainer ShowTitleBar:false];
                }
                
                if(_extendClientHints & AvnOSXThickTitleBar)
                {
                    Window.toolbar = [NSToolbar new];
                    Window.toolbar.showsBaselineSeparator = false;
                }
                else
                {
                    Window.toolbar = nullptr;
                }
            }
            else
            {
                Window.titleVisibility = NSWindowTitleVisible;
                Window.toolbar = nullptr;
                [Window setTitlebarAppearsTransparent:false];
                View.layer.zPosition = 0;
            }
            
            [Window setIsExtended:enable];
            
            HideOrShowTrafficLights();
            
            UpdateStyle();
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetExtendClientAreaHints (AvnExtendClientAreaChromeHints hints) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            _extendClientHints = hints;
            
            SetExtendClientArea(_isClientAreaExtended);
            return S_OK;
        }
    }
    
    virtual HRESULT GetExtendTitleBarHeight (double*ret) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(ret == nullptr)
            {
                return E_POINTER;
            }
            
            *ret = [Window getExtendedTitleBarHeight];
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetExtendTitleBarHeight (double value) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            [StandardContainer SetTitleBarHeightHint:value];
            return S_OK;
        }
    }
    
    void EnterFullScreenMode ()
    {
        _fullScreenActive = true;
        
        [Window setHasShadow:YES];
        [Window setTitleVisibility:NSWindowTitleVisible];
        [Window setTitlebarAppearsTransparent:NO];
        [Window setTitle:_lastTitle];
        
        Window.styleMask = Window.styleMask | NSWindowStyleMaskTitled | NSWindowStyleMaskResizable;
        Window.styleMask = Window.styleMask & ~NSWindowStyleMaskFullSizeContentView;
    
        [Window toggleFullScreen:nullptr];
    }
    
    void ExitFullScreenMode ()
    {
        [Window toggleFullScreen:nullptr];
        
        _fullScreenActive = false;
        
        SetDecorations(_decorations);
    }
    
    virtual HRESULT SetWindowState (AvnWindowState state) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(Window == nullptr)
            {
                return  S_OK;
            }
            
            if(_actualWindowState == state)
            {
                return S_OK;
            }
            
            _inSetWindowState = true;
            
            auto currentState = _actualWindowState;
            _lastWindowState = state;
            
            if(currentState == Normal)
            {
                _preZoomSize = [Window frame];
            }
            
            if(_shown)
            {
                switch (state) {
                    case Maximized:
                        if(currentState == FullScreen)
                        {
                            ExitFullScreenMode();
                        }
                        
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
                        if(currentState == FullScreen)
                        {
                            ExitFullScreenMode();
                        }
                        else
                        {
                            [Window miniaturize:Window];
                        }
                        break;
                        
                    case FullScreen:
                        if([Window isMiniaturized])
                        {
                            [Window deminiaturize:Window];
                        }
                        
                        EnterFullScreenMode();
                        break;
                        
                    case Normal:
                        if([Window isMiniaturized])
                        {
                            [Window deminiaturize:Window];
                        }
                        
                        if(currentState == FullScreen)
                        {
                            ExitFullScreenMode();
                        }
                        
                        if(IsZoomed())
                        {
                            if(_decorations == SystemDecorationsFull)
                            {
                                DoZoom();
                            }
                            else
                            {
                                [Window setFrame:_preZoomSize display:true];
                                auto newFrame = [Window contentRectForFrameRect:[Window frame]].size;
                                
                                [View setFrameSize:newFrame];
                            }
                            
                        }
                        break;
                }
                
                _actualWindowState = _lastWindowState;
            }
            
            
            _inSetWindowState = false;
            
            return S_OK;
        }
    }

    virtual void OnResized () override
    {
        if(_shown && !_inSetWindowState && !_transitioningWindowState)
        {
            WindowStateChanged();
        }
    }
    
protected:
    virtual NSWindowStyleMask GetStyle() override
    {
        unsigned long s = NSWindowStyleMaskBorderless;

        switch (_decorations)
        {
            case SystemDecorationsNone:
                s = s | NSWindowStyleMaskFullSizeContentView;
                break;

            case SystemDecorationsBorderOnly:
                s = s | NSWindowStyleMaskTitled | NSWindowStyleMaskFullSizeContentView;
                break;

            case SystemDecorationsFull:
                s = s | NSWindowStyleMaskTitled | NSWindowStyleMaskClosable | NSWindowStyleMaskBorderless;
                
                if(_canResize)
                {
                    s = s | NSWindowStyleMaskResizable;
                }
                break;
        }

        if([Window parentWindow] == nullptr)
        {
            s |= NSWindowStyleMaskMiniaturizable;
        }
        
        if(_isClientAreaExtended)
        {
            s |= NSWindowStyleMaskFullSizeContentView | NSWindowStyleMaskTexturedBackground;
        }
        return s;
    }
};

NSArray* AllLoopModes = [NSArray arrayWithObjects: NSDefaultRunLoopMode, NSEventTrackingRunLoopMode, NSModalPanelRunLoopMode, NSRunLoopCommonModes, NSConnectionReplyMode, nil];

@implementation AutoFitContentView
{
    NSVisualEffectView* _titleBarMaterial;
    NSBox* _titleBarUnderline;
    NSView* _content;
    NSVisualEffectView* _blurBehind;
    double _titleBarHeightHint;
    bool _settingSize;
}

-(AutoFitContentView* _Nonnull) initWithContent:(NSView *)content
{
    _titleBarHeightHint = -1;
    _content = content;
    _settingSize = false;

    [self setAutoresizesSubviews:true];
    [self setWantsLayer:true];
    
    _titleBarMaterial = [NSVisualEffectView new];
    [_titleBarMaterial setBlendingMode:NSVisualEffectBlendingModeWithinWindow];
    [_titleBarMaterial setMaterial:NSVisualEffectMaterialTitlebar];
    [_titleBarMaterial setWantsLayer:true];
    _titleBarMaterial.hidden = true;
    
    _titleBarUnderline = [NSBox new];
    _titleBarUnderline.boxType = NSBoxSeparator;
    _titleBarUnderline.fillColor = [NSColor underPageBackgroundColor];
    _titleBarUnderline.hidden = true;
    
    [self addSubview:_titleBarMaterial];
    [self addSubview:_titleBarUnderline];
    
    _blurBehind = [NSVisualEffectView new];
    [_blurBehind setBlendingMode:NSVisualEffectBlendingModeBehindWindow];
    [_blurBehind setMaterial:NSVisualEffectMaterialLight];
    [_blurBehind setWantsLayer:true];
    _blurBehind.hidden = true;
    
    [_blurBehind setAutoresizingMask:NSViewWidthSizable | NSViewHeightSizable];
    [_content setAutoresizingMask:NSViewWidthSizable | NSViewHeightSizable];
    
    [self addSubview:_blurBehind];
    [self addSubview:_content];
    
    [self setWantsLayer:true];
    return self;
}

-(void) ShowBlur:(bool)show
{
    _blurBehind.hidden = !show;
}

-(void) ShowTitleBar: (bool) show
{
    _titleBarMaterial.hidden = !show;
    _titleBarUnderline.hidden = !show;
}

-(void) SetTitleBarHeightHint: (double) height
{
    _titleBarHeightHint = height;
    
    [self setFrameSize:self.frame.size];
}

-(void)setFrameSize:(NSSize)newSize
{
    if(_settingSize)
    {
        return;
    }
    
    _settingSize = true;
    [super setFrameSize:newSize];
    
    auto window = objc_cast<AvnWindow>([self window]);
    
    // TODO get actual titlebar size
    
    double height = _titleBarHeightHint == -1 ? [window getExtendedTitleBarHeight] : _titleBarHeightHint;
    
    NSRect tbar;
    tbar.origin.x = 0;
    tbar.origin.y = newSize.height - height;
    tbar.size.width = newSize.width;
    tbar.size.height = height;
    
    [_titleBarMaterial setFrame:tbar];
    tbar.size.height = height < 1 ? 0 : 1;
    [_titleBarUnderline setFrame:tbar];

    _settingSize = false;
}

-(void) SetContent: (NSView* _Nonnull) content
{
    if(content != nullptr)
    {
        [content removeFromSuperview];
        [self addSubview:content];
        _content = content;
    }
}
@end

@implementation AvnView
{
    ComPtr<WindowBaseImpl> _parent;
    ComPtr<IUnknown> _swRenderedFrame;
    AvnFramebuffer _swRenderedFrameBuffer;
    bool _queuedDisplayFromThread;
    NSTrackingArea* _area;
    bool _isLeftPressed, _isMiddlePressed, _isRightPressed, _isXButton1Pressed, _isXButton2Pressed, _isMouseOver;
    AvnInputModifiers _modifierState;
    NSEvent* _lastMouseDownEvent;
    bool _lastKeyHandled;
    AvnPixelSize _lastPixelSize;
    NSObject<IRenderTarget>* _renderTarget;
}

- (void)onClosed
{
    @synchronized (self)
    {
        _parent = nullptr;
    }
}

-(AvnPixelSize) getPixelSize
{
    return _lastPixelSize;
}

- (NSEvent*) lastMouseDownEvent
{
    return _lastMouseDownEvent;
}

- (void) updateRenderTarget
{
    [_renderTarget resize:_lastPixelSize withScale: [[self window] backingScaleFactor]];
    [self setNeedsDisplayInRect:[self frame]];
}

-(AvnView*)  initWithParent: (WindowBaseImpl*) parent
{
    self = [super init];
    _renderTarget = parent->renderTarget;
    [self setWantsLayer:YES];
    [self setLayerContentsRedrawPolicy: NSViewLayerContentsRedrawDuringViewResize];
    
    _parent = parent;
    _area = nullptr;
    _lastPixelSize.Height = 100;
    _lastPixelSize.Width = 100;
    [self registerForDraggedTypes: @[@"public.data", GetAvnCustomDataType()]];
    
    _modifierState = AvnInputModifiersNone;
    return self;
}

- (BOOL)isFlipped
{
    return YES;
}

- (BOOL)wantsUpdateLayer
{
    return YES;
}

- (void)setLayer:(CALayer *)layer
{
    [_renderTarget setNewLayer: layer];
    [super setLayer: layer];
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

    if (_parent == nullptr)
    {
        return;
    }

    NSRect rect = NSZeroRect;
    rect.size = newSize;
    
    NSTrackingAreaOptions options = NSTrackingActiveAlways | NSTrackingMouseMoved | NSTrackingEnabledDuringMouseDrag;
    _area = [[NSTrackingArea alloc] initWithRect:rect options:options owner:self userInfo:nullptr];
    [self addTrackingArea:_area];
    
    _parent->UpdateCursor();
    
    auto fsize = [self convertSizeToBacking: [self frame].size];
    
    if(_lastPixelSize.Width != (int)fsize.width || _lastPixelSize.Height != (int)fsize.height)
    {
        _lastPixelSize.Width = (int)fsize.width;
        _lastPixelSize.Height = (int)fsize.height;
        [self updateRenderTarget];
    
        _parent->BaseEvents->Resized(AvnSize{newSize.width, newSize.height});
    }
}

- (void)updateLayer
{
    AvnInsidePotentialDeadlock deadlock;
    if (_parent == nullptr)
    {
        return;
    }
    
    _parent->BaseEvents->RunRenderPriorityJobs();
    
    if (_parent == nullptr)
    {
        return;
    }
        
    _parent->BaseEvents->Paint();
}

- (void)drawRect:(NSRect)dirtyRect
{
    return;
}

-(void) setSwRenderedFrame: (AvnFramebuffer*) fb dispose: (IUnknown*) dispose
{
    @autoreleasepool {
        [_renderTarget setSwFrame:fb];
        dispose->Release();
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
    auto fsize = [self convertSizeToBacking: [self frame].size];
    _lastPixelSize.Width = (int)fsize.width;
    _lastPixelSize.Height = (int)fsize.height;
    [self updateRenderTarget];
    
    if(_parent != nullptr)
    {
        _parent->BaseEvents->ScalingChanged([_parent->Window backingScaleFactor]);
    }
    
    [super viewDidChangeBackingProperties];
}

- (bool) ignoreUserInput:(bool)trigerInputWhenDisabled
{
    auto parentWindow = objc_cast<AvnWindow>([self window]);
    
    if(parentWindow == nil || ![parentWindow shouldTryToHandleEvents])
    {
        if(trigerInputWhenDisabled)
        {
            auto window = dynamic_cast<WindowImpl*>(_parent.getRaw());
            
            if(window != nullptr)
            {
                window->WindowEvents->GotInputWhenDisabled();
            }
        }
        
        return TRUE;
    }
    
    return FALSE;
}

- (void)mouseEvent:(NSEvent *)event withType:(AvnRawMouseEventType) type
{
    bool triggerInputWhenDisabled = type != Move;
    
    if([self ignoreUserInput: triggerInputWhenDisabled])
    {
        return;
    }
    
    auto localPoint = [self convertPoint:[event locationInWindow] toView:self];
    auto avnPoint = [self toAvnPoint:localPoint];
    auto point = [self translateLocalPoint:avnPoint];
    AvnVector delta;
    
    if(type == Wheel)
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
    }
    
    auto timestamp = [event timestamp] * 1000;
    auto modifiers = [self getModifiers:[event modifierFlags]];
    
    if(type != AvnRawMouseEventType::Move ||
       (
           [self window] != nil &&
           (
                [[self window] firstResponder] == nil
                || ![[[self window] firstResponder] isKindOfClass: [NSView class]]
           )
       )
    )
        [self becomeFirstResponder];
    
    if(_parent != nullptr)
    {
        _parent->BaseEvents->RawMouseEvent(type, timestamp, modifiers, point, delta);
    }
    
    [super mouseMoved:event];
}

- (BOOL) resignFirstResponder
{
    _parent->BaseEvents->LostFocus();
    return YES;
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
    _lastMouseDownEvent = event;

    switch(event.buttonNumber)
    {
        case 3:
            _isMiddlePressed = true;
            [self mouseEvent:event withType:MiddleButtonDown];
            break;
        case 4:
            _isXButton1Pressed = true;
            [self mouseEvent:event withType:XButton1Down];
            break;
        case 5:
            _isXButton2Pressed = true;
            [self mouseEvent:event withType:XButton2Down];
            break;
    }
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
    switch(event.buttonNumber)
    {
        case 3:
            _isMiddlePressed = false;
            [self mouseEvent:event withType:MiddleButtonUp];
            break;
        case 4:
            _isXButton1Pressed = false;
            [self mouseEvent:event withType:XButton1Up];
            break;
        case 5:
            _isXButton2Pressed = false;
            [self mouseEvent:event withType:XButton2Up];
            break;
    }
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
    if([self ignoreUserInput: false])
    {
        return;
    }
    
    auto key = s_KeyMap[[event keyCode]];
    
    auto timestamp = [event timestamp] * 1000;
    auto modifiers = [self getModifiers:[event modifierFlags]];
     
    if(_parent != nullptr)
    {
        auto handled = _parent->BaseEvents->RawKeyEvent(type, timestamp, modifiers, key);
        if (key != LeftCtrl && key != RightCtrl) {
          _lastKeyHandled = handled;
        } else {
          _lastKeyHandled = false;
        }
    }
}

- (BOOL)performKeyEquivalent:(NSEvent *)event
{
    bool result = _lastKeyHandled;
    
    _lastKeyHandled = false;
    
    return result;
}

- (void)flagsChanged:(NSEvent *)event
{
    auto newModifierState = [self getModifiers:[event modifierFlags]];
    
    bool isAltCurrentlyPressed = (_modifierState & Alt) == Alt;
    bool isControlCurrentlyPressed = (_modifierState & Control) == Control;
    bool isShiftCurrentlyPressed = (_modifierState & Shift) == Shift;
    bool isCommandCurrentlyPressed = (_modifierState & Windows) == Windows;
    
    bool isAltPressed = (newModifierState & Alt) == Alt;
    bool isControlPressed = (newModifierState & Control) == Control;
    bool isShiftPressed = (newModifierState & Shift) == Shift;
    bool isCommandPressed = (newModifierState & Windows) == Windows;
    
    
    if (isAltPressed && !isAltCurrentlyPressed)
    {
        [self keyboardEvent:event withType:KeyDown];
    }
    else if (isAltCurrentlyPressed && !isAltPressed)
    {
        [self keyboardEvent:event withType:KeyUp];
    }
    
    if (isControlPressed && !isControlCurrentlyPressed)
    {
        [self keyboardEvent:event withType:KeyDown];
    }
    else if (isControlCurrentlyPressed && !isControlPressed)
    {
        [self keyboardEvent:event withType:KeyUp];
    }
    
    if (isShiftPressed && !isShiftCurrentlyPressed)
    {
        [self keyboardEvent:event withType:KeyDown];
    }
    else if(isShiftCurrentlyPressed && !isShiftPressed)
    {
        [self keyboardEvent:event withType:KeyUp];
    }
    
    if(isCommandPressed && !isCommandCurrentlyPressed)
    {
        [self keyboardEvent:event withType:KeyDown];
    }
    else if(isCommandCurrentlyPressed && ! isCommandPressed)
    {
        [self keyboardEvent:event withType:KeyUp];
    }
    
    _modifierState = newModifierState;
    
    [[self inputContext] handleEvent:event];
    [super flagsChanged:event];
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
    if (_isXButton1Pressed)
        rv |= XButton1MouseButton;
    if (_isXButton2Pressed)
        rv |= XButton2MouseButton;
    
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
        if(_parent != nullptr)
        {
            _lastKeyHandled = _parent->BaseEvents->RawTextInputEvent(0, [string UTF8String]);
        }
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

- (NSDragOperation)triggerAvnDragEvent: (AvnDragEventType) type info: (id <NSDraggingInfo>)info
{
    auto localPoint = [self convertPoint:[info draggingLocation] toView:self];
    auto avnPoint = [self toAvnPoint:localPoint];
    auto point = [self translateLocalPoint:avnPoint];
    auto modifiers = [self getModifiers:[[NSApp currentEvent] modifierFlags]];
    NSDragOperation nsop = [info draggingSourceOperationMask];
   
        auto effects = ConvertDragDropEffects(nsop);
    int reffects = (int)_parent->BaseEvents
    ->DragEvent(type, point, modifiers, effects,
                CreateClipboard([info draggingPasteboard], nil),
                GetAvnDataObjectHandleFromDraggingInfo(info));
    
    NSDragOperation ret = 0;
    
    // Ensure that the managed part didn't add any new effects
    reffects = (int)effects & (int)reffects;
    
    // OSX requires exactly one operation
    if((reffects & (int)AvnDragDropEffects::Copy) != 0)
        ret = NSDragOperationCopy;
    else if((reffects & (int)AvnDragDropEffects::Move) != 0)
        ret = NSDragOperationMove;
    else if((reffects & (int)AvnDragDropEffects::Link) != 0)
        ret = NSDragOperationLink;
    if(ret == 0)
        ret = NSDragOperationNone;
    return ret;
}

- (NSDragOperation)draggingEntered:(id <NSDraggingInfo>)sender
{
    return [self triggerAvnDragEvent: AvnDragEventType::Enter info:sender];
}

- (NSDragOperation)draggingUpdated:(id <NSDraggingInfo>)sender
{
    return [self triggerAvnDragEvent: AvnDragEventType::Over info:sender];
}

- (void)draggingExited:(id <NSDraggingInfo>)sender
{
    [self triggerAvnDragEvent: AvnDragEventType::Leave info:sender];
}

- (BOOL)prepareForDragOperation:(id <NSDraggingInfo>)sender
{
    return [self triggerAvnDragEvent: AvnDragEventType::Over info:sender] != NSDragOperationNone;
}

- (BOOL)performDragOperation:(id <NSDraggingInfo>)sender
{
    return [self triggerAvnDragEvent: AvnDragEventType::Drop info:sender] != NSDragOperationNone;
}

- (void)concludeDragOperation:(nullable id <NSDraggingInfo>)sender
{
    
}

@end


@implementation AvnWindow
{
    ComPtr<WindowBaseImpl> _parent;
    bool _canBecomeKeyAndMain;
    bool _closed;
    bool _isEnabled;
    bool _isExtended;
    AvnMenu* _menu;
    double _lastScaling;
}

-(void) setIsExtended:(bool)value;
{
    _isExtended = value;
}

-(double) getScaling
{
    return _lastScaling;
}

-(double) getExtendedTitleBarHeight
{
    if(_isExtended)
    {
        for (id subview in self.contentView.superview.subviews)
        {
            if ([subview isKindOfClass:NSClassFromString(@"NSTitlebarContainerView")])
            {
                NSView *titlebarView = [subview subviews][0];

                return (double)titlebarView.frame.size.height;
            }
        }

        return -1;
    }
    else
    {
        return 0;
    }
}

+(void)closeAll
{
    NSArray<NSWindow*>* windows = [NSArray arrayWithArray:[NSApp windows]];
    auto numWindows = [windows count];
    
    for(int i = 0; i < numWindows; i++)
    {
        auto window = (AvnWindow*)[windows objectAtIndex:i];
        
        if([window parentWindow] == nullptr) // Avalonia will handle the child windows.
        {
            [window performClose:nil];
        }
    }
}

- (void)performClose:(id)sender
{
    if([[self delegate] respondsToSelector:@selector(windowShouldClose:)])
    {
        if(![[self delegate] windowShouldClose:self]) return;
    }
    else if([self respondsToSelector:@selector(windowShouldClose:)])
    {
        if(![self windowShouldClose:self]) return;
    }

    [self close];
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

-(void) showWindowMenuWithAppMenu
{
    if(_menu != nullptr)
    {
        auto appMenuItem = ::GetAppMenuItem();
        
        if(appMenuItem != nullptr)
        {
            auto appMenu = [appMenuItem menu];
            
            [appMenu removeItem:appMenuItem];
            
            [_menu insertItem:appMenuItem atIndex:0];
            
            [_menu setHasGlobalMenuItem:true];
        }
        
        [NSApp setMenu:_menu];
    }
    else
    {
        [self showAppMenuOnly];
    }
}

-(void) showAppMenuOnly
{
    auto appMenuItem = ::GetAppMenuItem();
    
    if(appMenuItem != nullptr)
    {
        auto appMenu = ::GetAppMenu();
        
        auto nativeAppMenu = dynamic_cast<AvnAppMenu*>(appMenu);
        
        [[appMenuItem menu] removeItem:appMenuItem];
        
        if(_menu != nullptr)
        {
            [_menu setHasGlobalMenuItem:false];
        }
        
        [nativeAppMenu->GetNative() addItem:appMenuItem];
        
        [NSApp setMenu:nativeAppMenu->GetNative()];
    }
    else
    {
        [NSApp setMenu:nullptr];
    }
}

-(void) applyMenu:(AvnMenu *)menu
{
    if(menu == nullptr)
    {
        menu = [AvnMenu new];
    }
    
    _menu = menu;
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
    _closed = false;
    _isEnabled = true;
    
    _lastScaling = [self backingScaleFactor];
    [self setOpaque:NO];
    [self setBackgroundColor: [NSColor clearColor]];
    _isExtended = false;
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

- (void)windowDidChangeBackingProperties:(NSNotification *)notification
{
    _lastScaling = [self backingScaleFactor];
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
    return _isEnabled;
}

-(void) setEnabled:(bool)enable
{
    _isEnabled = enable;
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
    [self showWindowMenuWithAppMenu];
    
    if([self activateAppropriateChild: true])
    {
        if(_parent != nullptr)
        {
            _parent->BaseEvents->Activated();
        }
    }
    
    [super becomeKeyWindow];
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

- (void)windowDidResize:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->WindowStateChanged();
    }
}

- (void)windowWillExitFullScreen:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->StartStateTransition();
    }
}

- (void)windowDidExitFullScreen:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->EndStateTransition();
        
        if(parent->Decorations() != SystemDecorationsFull && parent->WindowState() == Maximized)
        {
            NSRect screenRect = [[self screen] visibleFrame];
            [self setFrame:screenRect display:YES];
        }
        
        if(parent->WindowState() == Minimized)
        {
            [self miniaturize:nullptr];
        }
        
        parent->WindowStateChanged();
    }
}

- (void)windowWillEnterFullScreen:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->StartStateTransition();
    }
}

- (void)windowDidEnterFullScreen:(NSNotification *)notification
{
    auto parent = dynamic_cast<IWindowStateChanged*>(_parent.operator->());
    
    if(parent != nullptr)
    {
        parent->EndStateTransition();
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
    
    [self showAppMenuOnly];
    
    [super resignKeyWindow];
}

- (void)windowDidMove:(NSNotification *)notification
{
    AvnPoint position;
    
    if(_parent != nullptr)
    {
        _parent->GetPosition(&position);
        _parent->BaseEvents->PositionChanged(position);
    }
}
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
    PopupImpl(IAvnWindowEvents* events, IAvnGlContext* gl) : WindowBaseImpl(events, gl)
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
        START_COM_CALL;
        
        @autoreleasepool
        {
            if (Window != nullptr)
            {
                [Window setContentSize:NSSize{x, y}];
            
                [Window setFrameTopLeftPoint:ToNSPoint(ConvertPointY(lastPositionSet))];
            }
            
            return S_OK;
        }
    }
public:
    virtual bool ShouldTakeFocusOnShow() override
    {
        return false;
    }
};

extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events, IAvnGlContext* gl)
{
    @autoreleasepool
    {
        IAvnPopup* ptr = dynamic_cast<IAvnPopup*>(new PopupImpl(events, gl));
        return ptr;
    }
}

extern IAvnWindow* CreateAvnWindow(IAvnWindowEvents*events, IAvnGlContext* gl)
{
    @autoreleasepool
    {
        IAvnWindow* ptr = (IAvnWindow*)new WindowImpl(events, gl);
        return ptr;
    }
}
