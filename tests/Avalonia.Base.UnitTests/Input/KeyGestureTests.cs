using System.Collections.Generic;
using Avalonia.Input;
using Xunit;

namespace Avalonia.Base.UnitTests.Input
{
    public class KeyGestureTests
    {
        public static readonly IEnumerable<object[]> ParseData = new object[][]
        {
            new object[]{"Ctrl+A", new KeyGesture(Key.A, KeyModifiers.Control)},
            new object[]{"  \tShift\t+Alt +B", new KeyGesture(Key.B, KeyModifiers.Shift | KeyModifiers.Alt) },
            new object[]{"Control++", new KeyGesture(Key.OemPlus, KeyModifiers.Control) },
            new object[]{ "Shift+⌘+A", new KeyGesture(Key.A, KeyModifiers.Meta | KeyModifiers.Shift) },
            new object[]{ "Shift+Cmd+A", new KeyGesture(Key.A, KeyModifiers.Meta | KeyModifiers.Shift) },
        };

        public static readonly IEnumerable<object[]> ToStringData = new object[][]
        {
            new object[]{new KeyGesture(Key.A), "A"},
            new object[]{new KeyGesture(Key.A, KeyModifiers.Control), "Ctrl+A"},
            new object[]{new KeyGesture(Key.A, KeyModifiers.Control | KeyModifiers.Shift), "Ctrl+Shift+A"},
            new object[]{new KeyGesture(Key.A, KeyModifiers.Alt | KeyModifiers.Shift), "Shift+Alt+A"},
            new object[]{new KeyGesture(Key.A, KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.Shift), "Ctrl+Shift+Alt+A"},
            new object[]{new KeyGesture(Key.A, KeyModifiers.Meta | KeyModifiers.Shift), "Shift+Cmd+A"},
        };

        [Theory]
        [MemberData(nameof(ParseData))]
        public void Key_Gesture_Is_Able_To_Parse_Sample_Data(string text, KeyGesture gesture)
        {
            Assert.Equal(gesture, KeyGesture.Parse(text));
        }

        [Theory]
        [InlineData(Key.OemMinus, Key.Subtract)]
        [InlineData(Key.OemPlus, Key.Add)]
        [InlineData(Key.OemPeriod, Key.Decimal)]
        public void Key_Gesture_Matches_NumPad_To_Regular_Digit(Key gestureKey, Key pressedKey)
        {
            var keyGesture = new KeyGesture(gestureKey);

            Assert.True(keyGesture.Matches(new KeyEventArgs
            {
                Key = pressedKey
            }));
        }

        [Theory]
        [MemberData(nameof(ToStringData))]
        public void ToString_Produces_Correct_Results(KeyGesture gesture, string expected)
        {
            Assert.Equal(expected, gesture.ToString());
        }
    }
}
