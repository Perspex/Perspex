#import <Cocoa/Cocoa.h>

NS_ASSUME_NONNULL_BEGIN

class IAvnAutomationPeer;

@interface AvnAutomationNode : NSAccessibilityElement
- (AvnAutomationNode *)initWithPeer:(IAvnAutomationPeer *)peer;
@end

struct INSAccessibilityHolder
{
    virtual NSObject* _Nonnull GetNSAccessibility () = 0;
};

NS_ASSUME_NONNULL_END
