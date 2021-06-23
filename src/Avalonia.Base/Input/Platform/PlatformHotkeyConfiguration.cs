using System.Collections.Generic;

namespace Avalonia.Input.Platform
{
    public class PlatformHotkeyConfiguration
    {
        public PlatformHotkeyConfiguration() : this(KeyModifiers.Control)
        {
            
        }
        
        public PlatformHotkeyConfiguration(KeyModifiers commandModifiers,
            KeyModifiers selectionModifiers = KeyModifiers.Shift,
            KeyModifiers wholeWordTextActionModifiers = KeyModifiers.Control)
        {
            CommandModifiers = commandModifiers;
            SelectionModifiers = selectionModifiers;
            WholeWordTextActionModifiers = wholeWordTextActionModifiers;
            Copy = new List<KeyGesture>
            {
                new KeyGesture(Key.C, commandModifiers)
            };
            Cut = new List<KeyGesture>
            {
                new KeyGesture(Key.X, commandModifiers)
            };
            Paste = new List<KeyGesture>
            {
                new KeyGesture(Key.V, commandModifiers)
            };
            Undo = new List<KeyGesture>
            {
                new KeyGesture(Key.Z, commandModifiers)
            };
            Redo = new List<KeyGesture>
            {
                new KeyGesture(Key.Y, commandModifiers),
                new KeyGesture(Key.Z, commandModifiers | selectionModifiers)
            };
            SelectAll = new List<KeyGesture>
            {
                new KeyGesture(Key.A, commandModifiers)
            };
            MoveCursorToTheStartOfLine = new List<KeyGesture>
            {
                new KeyGesture(Key.Home)
            };
            MoveCursorToTheEndOfLine = new List<KeyGesture>
            {
                new KeyGesture(Key.End)
            };
            MoveCursorToTheStartOfDocument = new List<KeyGesture>
            {
                new KeyGesture(Key.Home, commandModifiers)
            };
            MoveCursorToTheEndOfDocument = new List<KeyGesture>
            {
                new KeyGesture(Key.End, commandModifiers)
            };
            MoveCursorToTheStartOfLineWithSelection = new List<KeyGesture>
            {
                new KeyGesture(Key.Home, selectionModifiers)
            };
            MoveCursorToTheEndOfLineWithSelection = new List<KeyGesture>
            {
                new KeyGesture(Key.End, selectionModifiers)
            };
            MoveCursorToTheStartOfDocumentWithSelection = new List<KeyGesture>
            {
                new KeyGesture(Key.Home, commandModifiers | selectionModifiers)
            };
            MoveCursorToTheEndOfDocumentWithSelection = new List<KeyGesture>
            {
                new KeyGesture(Key.End, commandModifiers | selectionModifiers)
            };
        }
        
        public KeyModifiers CommandModifiers { get; set; }
        public KeyModifiers WholeWordTextActionModifiers { get; set; }
        public KeyModifiers SelectionModifiers { get; set; }
        public List<KeyGesture> Copy { get; set; }
        public List<KeyGesture> Cut { get; set; }
        public List<KeyGesture> Paste { get; set; }
        public List<KeyGesture> Undo { get; set; }
        public List<KeyGesture> Redo { get; set; }
        public List<KeyGesture> SelectAll { get; set; }
        public List<KeyGesture> MoveCursorToTheStartOfLine { get; set; }
        public List<KeyGesture> MoveCursorToTheEndOfLine { get; set; }
        public List<KeyGesture> MoveCursorToTheStartOfDocument { get; set; }
        public List<KeyGesture> MoveCursorToTheEndOfDocument { get; set; }
        public List<KeyGesture> MoveCursorToTheStartOfLineWithSelection { get; set; }
        public List<KeyGesture> MoveCursorToTheEndOfLineWithSelection { get; set; }
        public List<KeyGesture> MoveCursorToTheStartOfDocumentWithSelection { get; set; }
        public List<KeyGesture> MoveCursorToTheEndOfDocumentWithSelection { get; set; }
        
        
    }
}
