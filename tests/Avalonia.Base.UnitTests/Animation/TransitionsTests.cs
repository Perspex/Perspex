using System;
using Avalonia.Animation;
using Avalonia.Controls;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class TransitionsTests
    {
        [Fact]
        public void Check_Transitions_Interpolation_Negative_Bounds_Clamp()
        {
            var clock = new TestClock();

            var border = new Border
            {
                Transitions = new Transitions
                    {
                        new DoubleTransition
                        {
                            Duration = TimeSpan.FromSeconds(1),
                            Property = Border.OpacityProperty,
                        }
                    }
            };

            border.Opacity = 0;

            clock.Pulse(TimeSpan.FromSeconds(0));
            clock.Pulse(TimeSpan.FromSeconds(-0.5));

            Assert.Equal(0, border.Opacity);

        }

        [Fact]
        public void Check_Transitions_Interpolation_Positive_Bounds_Clamp()
        {
            var clock = new TestClock();

            var border = new Border
            {
                Transitions = new Transitions
                    {
                        new DoubleTransition
                        {
                            Duration = TimeSpan.FromSeconds(1),
                            Property = Border.OpacityProperty,
                        }
                    }
            };

            border.Opacity = 0;

            clock.Pulse(TimeSpan.FromSeconds(0));
            clock.Pulse(TimeSpan.FromMilliseconds(1001));

            Assert.Equal(0, border.Opacity);

        }

        [Fact]
        public void TransitionInstance_With_Zero_Duration_Is_Completed_On_First_Tick()
        {
            var clock = new TestClock();

            int i = 0;
            var inst = new TransitionInstance(clock, TimeSpan.Zero, TimeSpan.Zero).Subscribe(nextValue =>
            {
                switch (i++)
                {
                    case 0: Assert.Equal(0, nextValue); break;
                    case 1: Assert.Equal(1d, nextValue); break;
                }
            });

            clock.Pulse(TimeSpan.FromMilliseconds(10));

        }

        [Fact]
        public void TransitionInstance_Properly_Calculates_Delay_And_Duration_Values()
        {
            var clock = new TestClock();

            int i = -1;
            var inst = new TransitionInstance(clock, TimeSpan.FromMilliseconds(30), TimeSpan.FromMilliseconds(70)).Subscribe(nextValue =>
            {
                switch (i++)
                {
                    case 0: Assert.Equal(0, nextValue); break;
                    case 1: Assert.Equal(0, nextValue); break;
                    case 2: Assert.Equal(0, nextValue); break;
                    case 3: Assert.Equal(0, nextValue); break;
                    case 4: Assert.Equal(Math.Round(10d / 70d, 4), Math.Round(nextValue, 4)); break;
                    case 5: Assert.Equal(Math.Round(20d / 70d, 4), Math.Round(nextValue, 4)); break;
                    case 6: Assert.Equal(Math.Round(30d / 70d, 4), Math.Round(nextValue, 4)); break;
                    case 7: Assert.Equal(Math.Round(40d / 70d, 4), Math.Round(nextValue, 4)); break;
                    case 8: Assert.Equal(Math.Round(50d / 70d, 4), Math.Round(nextValue, 4)); break;
                    case 9: Assert.Equal(Math.Round(60d / 70d, 4), Math.Round(nextValue, 4)); break;
                    case 10: Assert.Equal(1d, nextValue); break;
                }
            });

            for (int z = 0; z <= 10; z++)
            {
                clock.Pulse(TimeSpan.FromMilliseconds(10));
            }
        }
    }
}
