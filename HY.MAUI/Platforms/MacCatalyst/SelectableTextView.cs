using Foundation;
using UIKit;

namespace HY.MAUI.Platforms.MacCatalyst;

public class SelectableTextView : UITextView
{
    public override bool CanBecomeFirstResponder => true;

    public SelectableTextView()
    {

    }

    public override bool CanPerform(ObjCRuntime.Selector action, NSObject? withSender)
    {
        base.CanPerform(action, withSender);

        // 只允许复制操作
        if (action.Name is "copy:")
            return true;

        return false;
    }

}