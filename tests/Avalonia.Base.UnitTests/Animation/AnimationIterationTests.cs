using System;
using System.Threading.Tasks;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class AnimationIterationTests
    {
        [Fact]
        public void Check_KeyTime_Correctly_Converted_To_Cue()
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 100d),
                },
                KeyTime = TimeSpan.FromSeconds(0.5)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 0d),
                },
                KeyTime = TimeSpan.FromSeconds(0)
            };

            var animation = new Avalonia.Animation.Animation()
            {
                Duration = TimeSpan.FromSeconds(1),
                Children =
                {
                    keyframe2,
                    keyframe1
                }
            };

            var border = new Border()
            {
                Height = 100d,
                Width = 100d
            };

            var clock = new TestClock();
            var animationRun = animation.RunAsync(border, clock);

            clock.Step(TimeSpan.Zero); 
            Assert.Equal(border.Width, 0d);

            clock.Step(TimeSpan.FromSeconds(1)); 
            Assert.Equal(border.Width, 100d);
 
        }


        [Fact]
        public void Check_Initial_Inter_and_Trailing_Delay_Values()
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 200d),
                },
                Cue = new Cue(1d)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 100d),
                },
                Cue = new Cue(0d)
            };

            var animation = new Avalonia.Animation.Animation()
            {
                Duration = TimeSpan.FromSeconds(3),
                Delay = TimeSpan.FromSeconds(3),
                DelayBetweenIterations = TimeSpan.FromSeconds(3),
                IterationCount = new IterationCount(2),
                Children =
                {
                    keyframe2,
                    keyframe1
                }
            };

            var border = new Border()
            {
                Height = 100d,
                Width = 100d
            };

            var clock = new TestClock();
            var animationRun = animation.RunAsync(border, clock);

            clock.Step(TimeSpan.Zero);

            // Initial Delay.
            clock.Step(TimeSpan.FromSeconds(1));
            Assert.Equal(border.Width, 0d);

            clock.Step(TimeSpan.FromSeconds(6));

            // First Inter-Iteration delay.
            clock.Step(TimeSpan.FromSeconds(8));
            Assert.Equal(border.Width, 200d);

            // Trailing Delay should be non-existent.
            clock.Step(TimeSpan.FromSeconds(14));
            Assert.True(animationRun.Status == TaskStatus.RanToCompletion);
            Assert.Equal(border.Width, 100d);
        }

        [Fact]
        public void Check_FillModes_Start_and_End_Values_if_Retained()
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 0d),
                },
                Cue = new Cue(0.0d)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(Border.WidthProperty, 300d),
                },
                Cue = new Cue(1.0d)
            };

            var animation = new Avalonia.Animation.Animation()
            {
                Duration = TimeSpan.FromSeconds(0.05d),
                Delay = TimeSpan.FromSeconds(0.05d),
                Easing = new SineEaseInOut(),
                FillMode = FillMode.Both,
                Children =
                {
                    keyframe1,
                    keyframe2
                }
            };

            var border = new Border()
            {
                Height = 100d,
                Width = 100d,
            };

            var clock = new TestClock();
            var animationRun = animation.RunAsync(border, clock);

            clock.Step(TimeSpan.FromSeconds(0d));
            Assert.Equal(border.Width, 0d);

            clock.Step(TimeSpan.FromSeconds(0.050d));
            Assert.Equal(border.Width, 0d);

            clock.Step(TimeSpan.FromSeconds(0.100d));
            Assert.Equal(border.Width, 300d);
        }
    }
}
