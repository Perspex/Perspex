using System;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using Avalonia.Styling;
using Xunit;

namespace Avalonia.Base.UnitTests.Animation
{
    public class KeySplineTests
    {
        [Theory]
        [InlineData("1,2 3,4")]
        [InlineData("1 2 3 4")]
        [InlineData("1 2,3 4")]
        [InlineData("1,2,3,4")]
        public void Can_Parse_KeySpline_Via_TypeConverter(string input)
        {
            var conv = new KeySplineTypeConverter();

            var keySpline = (KeySpline)conv.ConvertFrom(input);

            Assert.Equal(1, keySpline.ControlPointX1);
            Assert.Equal(2, keySpline.ControlPointY1);
            Assert.Equal(3, keySpline.ControlPointX2);
            Assert.Equal(4, keySpline.ControlPointY2);
        }

        [Theory]
        [InlineData("1,2F,3,4")] 
        [InlineData("Foo,Bar,Fee,Buzz")] 
        public void Can_Handle_Invalid_String_KeySpline_Via_TypeConverter(string input)
        {
            var conv = new KeySplineTypeConverter();

            Assert.ThrowsAny<Exception>(() => (KeySpline)conv.ConvertFrom(input));
        }

        [Theory]
        [InlineData(0.00)]
        [InlineData(0.50)]
        [InlineData(1.00)]
        public void KeySpline_X_Values_In_Range_Do_Not_Throw(double input)
        {
            var keySpline = new KeySpline();
            keySpline.ControlPointX1 = input; // no exception will be thrown -- test will fail if exception thrown
            keySpline.ControlPointX2 = input; // no exception will be thrown -- test will fail if exception thrown
        }

        [Theory]
        [InlineData(-0.01)]
        [InlineData(1.01)]
        public void KeySpline_X_Values_Cannot_Be_Out_Of_Range(double input)
        {
            var keySpline = new KeySpline();
            Assert.Throws<ArgumentException>(() => keySpline.ControlPointX1 = input);
            Assert.Throws<ArgumentException>(() => keySpline.ControlPointX2 = input);
        }

        [Fact]
        public void SplineEasing_Can_Be_Mutated()
        {
            var easing = new SplineEasing();

            Assert.Equal(0, easing.Ease(0));
            Assert.Equal(1, easing.Ease(1));

            easing.X1 = 0.25;
            easing.Y1 = 0.5;
            easing.X2 = 0.75;
            easing.Y2 = 1.0;

            Assert.NotEqual(0.5, easing.Ease(0.5));
        }

        /*
          To get the test values for the KeySpline test, you can:
          1) Grab the WPF sample for KeySpline animations from https://github.com/microsoft/WPF-Samples/tree/master/Animation/KeySplineAnimations
          2) Add the following xaml somewhere:
            <Button Content="Capture"
                    Click="Button_Click"/>
            <ScrollViewer VerticalScrollBarVisibility="Visible">
                <TextBlock Name="CaptureData"
                           Text="---"
                           TextWrapping="Wrap" />
            </ScrollViewer>
          3) Add the following code to the code behind:
            private void Button_Click(object sender, RoutedEventArgs e)
            {
                CaptureData.Text += string.Format("\n{0} | {1}", myTranslateTransform3D.OffsetX, (TimeSpan)ExampleStoryboard.GetCurrentTime(this));
                CaptureData.Text +=
                    "\nKeySpline=\"" + mySplineKeyFrame.KeySpline.ControlPoint1.X.ToString() + "," +
                    mySplineKeyFrame.KeySpline.ControlPoint1.Y.ToString() + " " +
                    mySplineKeyFrame.KeySpline.ControlPoint2.X.ToString() + "," +
                    mySplineKeyFrame.KeySpline.ControlPoint2.Y.ToString() + "\"";
                CaptureData.Text += "\n-----";
            }
          4) Run the app, mess with the slider values, then click the button to capture output values
         **/

        [Fact]
        public void Check_KeySpline_Handled_properly()
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(RotateTransform.AngleProperty, -2.5d),
                },
                KeyTime = TimeSpan.FromSeconds(0)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(RotateTransform.AngleProperty, 2.5d),
                },
                KeyTime = TimeSpan.FromSeconds(5),
                KeySpline = new KeySpline(0.1123555056179775,
                                          0.657303370786517,
                                          0.8370786516853934,
                                          0.499999999999999999)
            };

            var animation = new Avalonia.Animation.Animation()
            {
                Duration = TimeSpan.FromSeconds(5),
                Children =
                {
                    keyframe1,
                    keyframe2
                },
                IterationCount = new IterationCount(5),
                PlaybackDirection = PlaybackDirection.Alternate
            };

            var rotateTransform = new RotateTransform(-2.5);
            var rect = new Rectangle()
            {
                RenderTransform = rotateTransform
            };

            var clock = new TestClock();
            var animationRun = animation.RunAsync(rect, clock);

            // position is what you'd expect at end and beginning
            clock.Step(TimeSpan.Zero);
            Assert.Equal(rotateTransform.Angle, -2.5);
            clock.Step(TimeSpan.FromSeconds(5));
            Assert.Equal(rotateTransform.Angle, 2.5);

            // test some points in between end and beginning
            var tolerance = 0.01;
            clock.Step(TimeSpan.Parse("00:00:10.0153932"));
            var expected = -2.4122350198982545;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

            clock.Step(TimeSpan.Parse("00:00:11.2655407"));
            expected = -0.37153223002125113;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

            clock.Step(TimeSpan.Parse("00:00:12.6158773"));
            expected = 0.3967885416786294;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

            clock.Step(TimeSpan.Parse("00:00:14.6495256"));
            expected = 1.8016358493761722;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);
        }
        
        [Fact]
        public void Check_KeySpline_Parsing_Is_Correct()
        {
            var keyframe1 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(RotateTransform.AngleProperty, -2.5d),
                },
                KeyTime = TimeSpan.FromSeconds(0)
            };

            var keyframe2 = new KeyFrame()
            {
                Setters =
                {
                    new Setter(RotateTransform.AngleProperty, 2.5d),
                },
                KeyTime = TimeSpan.FromSeconds(5),
            };

            var animation = new Avalonia.Animation.Animation()
            {
                Duration = TimeSpan.FromSeconds(5),
                Children =
                {
                    keyframe1,
                    keyframe2
                },
                IterationCount = new IterationCount(5),
                PlaybackDirection = PlaybackDirection.Alternate,
                Easing = Easing.Parse("0.1123555056179775,0.657303370786517,0.8370786516853934,0.499999999999999999")
            };

            var rotateTransform = new RotateTransform(-2.5);
            var rect = new Rectangle()
            {
                RenderTransform = rotateTransform
            };

            var clock = new TestClock();
            var animationRun = animation.RunAsync(rect, clock);

            // position is what you'd expect at end and beginning
            clock.Step(TimeSpan.Zero);
            Assert.Equal(rotateTransform.Angle, -2.5);
            clock.Step(TimeSpan.FromSeconds(5));
            Assert.Equal(rotateTransform.Angle, 2.5);

            // test some points in between end and beginning
            var tolerance = 0.01;
            clock.Step(TimeSpan.Parse("00:00:10.0153932"));
            var expected = -2.4122350198982545;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

            clock.Step(TimeSpan.Parse("00:00:11.2655407"));
            expected = -0.37153223002125113;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

            clock.Step(TimeSpan.Parse("00:00:12.6158773"));
            expected = 0.3967885416786294;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);

            clock.Step(TimeSpan.Parse("00:00:14.6495256"));
            expected = 1.8016358493761722;
            Assert.True(Math.Abs(rotateTransform.Angle - expected) <= tolerance);
        }
    }
}
