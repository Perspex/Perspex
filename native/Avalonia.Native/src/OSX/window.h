#ifndef window_h
#define window_h

class WindowBaseImpl;

@interface AvnView : NSView<NSTextInputClient, NSDraggingDestination>
-(AvnView* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent;
-(NSEvent* _Nonnull) lastMouseDownEvent;
-(AvnPoint) translateLocalPoint:(AvnPoint)pt;
-(void) setSwRenderedFrame: (AvnFramebuffer* _Nonnull) fb dispose: (IUnknown* _Nonnull) dispose;
-(void) onClosed;
-(AvnPixelSize) getPixelSize;
-(AvnPlatformResizeReason) getResizeReason;
-(void) setResizeReason:(AvnPlatformResizeReason)reason;
@end

@interface AutoFitContentView : NSView
-(AutoFitContentView* _Nonnull) initWithContent: (NSView* _Nonnull) content;
-(void) ShowTitleBar: (bool) show;
-(void) SetTitleBarHeightHint: (double) height;
-(void) SetContent: (NSView* _Nonnull) content;
-(void) ShowBlur: (bool) show;
@end

@interface AvnWindow : NSWindow <NSWindowDelegate>
+(void) closeAll;
-(AvnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent;
-(void) setCanBecomeKeyAndMain;
-(void) pollModalSession: (NSModalSession _Nonnull) session;
-(void) restoreParentWindow;
-(bool) shouldTryToHandleEvents;
-(void) setEnabled: (bool) enable;
-(void) showAppMenuOnly;
-(void) showWindowMenuWithAppMenu;
-(void) applyMenu:(NSMenu* _Nullable)menu;
-(double) getScaling;
-(double) getExtendedTitleBarHeight;
-(void) setIsExtended:(bool)value;
@end

struct INSWindowHolder
{
    virtual AvnWindow* _Nonnull GetNSWindow () = 0;
};

struct IWindowStateChanged
{
    virtual void WindowStateChanged () = 0;
    virtual void StartStateTransition () = 0;
    virtual void EndStateTransition () = 0;
    virtual SystemDecorations Decorations () = 0;
    virtual AvnWindowState WindowState () = 0;
};

class ResizeScope
{
public:
    ResizeScope(AvnView* _Nonnull view, AvnPlatformResizeReason reason)
    {
        _view = view;
        _restore = [view getResizeReason];
        [view setResizeReason:reason];
    }
    
    ~ResizeScope()
    {
        [_view setResizeReason:_restore];
    }
private:
    AvnView* _Nonnull _view;
    AvnPlatformResizeReason _restore;
};

#endif /* window_h */
