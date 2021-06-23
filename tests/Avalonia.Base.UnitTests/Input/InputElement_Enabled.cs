using Avalonia.Controls;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class InputElement_Enabled
    {
        [Fact]
        public void IsEffectivelyEnabled_Follows_IsEnabled()
        {
            var target = new Decorator();

            Assert.True(target.IsEnabled);
            Assert.True(target.IsEffectivelyEnabled);

            target.IsEnabled = false;

            Assert.False(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
        }

        [Fact]
        public void IsEffectivelyEnabled_Follows_Ancestor_IsEnabled()
        {
            Decorator child;
            Decorator grandchild;
            var target = new Decorator
            {
                Child = child = new Decorator
                {
                    Child = grandchild = new Decorator(),
                }
            };

            Assert.True(target.IsEnabled);
            Assert.True(target.IsEffectivelyEnabled);
            Assert.True(child.IsEnabled);
            Assert.True(child.IsEffectivelyEnabled);
            Assert.True(grandchild.IsEnabled);
            Assert.True(grandchild.IsEffectivelyEnabled);

            target.IsEnabled = false;

            Assert.False(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
            Assert.True(child.IsEnabled);
            Assert.False(child.IsEffectivelyEnabled);
            Assert.True(grandchild.IsEnabled);
            Assert.False(grandchild.IsEffectivelyEnabled);
        }

        [Fact]
        public void Disabled_Pseudoclass_Follows_IsEffectivelyEnabled()
        {
            Decorator child;
            var target = new Decorator
            {
                Child = child = new Decorator()
            };

            Assert.DoesNotContain(":disabled", child.Classes);

            target.IsEnabled = false;

            Assert.Contains(":disabled", child.Classes);
        }

        [Fact]
        public void IsEffectivelyEnabled_Respects_IsEnabledCore()
        {
            Decorator child;
            var target = new TestControl
            {
                Child = child = new Decorator()
            };

            target.ShouldEnable = false;

            Assert.True(target.IsEnabled);
            Assert.False(target.IsEffectivelyEnabled);
            Assert.True(child.IsEnabled);
            Assert.False(child.IsEffectivelyEnabled);
        }

        private class TestControl : Decorator
        {
            private bool _shouldEnable;

            public bool ShouldEnable
            {
                get => _shouldEnable;
                set { _shouldEnable = value; UpdateIsEffectivelyEnabled(); }
            }

            protected override bool IsEnabledCore => IsEnabled && _shouldEnable; 
        }
    }
}
