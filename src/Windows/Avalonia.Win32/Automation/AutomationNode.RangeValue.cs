using Avalonia.Automation.Provider;
using UIA = Avalonia.Win32.Interop.Automation;

#nullable enable

namespace Avalonia.Win32.Automation
{
    internal partial class AutomationNode : UIA.IRangeValueProvider
    {
        double UIA.IRangeValueProvider.Value => InvokeSync<IRangeValueProvider, double>(x => x.Value);
        public bool IsReadOnly => InvokeSync<IRangeValueProvider, bool>(x => x.IsReadOnly);
        public double Maximum => InvokeSync<IRangeValueProvider, double>(x => x.Maximum);
        public double Minimum => InvokeSync<IRangeValueProvider, double>(x => x.Minimum);
        public double LargeChange => 1;
        public double SmallChange => 1;

        public void SetValue(double value) => InvokeSync<IRangeValueProvider>(x => x.SetValue(value));
    }
}
