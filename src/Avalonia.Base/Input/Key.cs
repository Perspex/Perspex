#if AVALONIA_REMOTE_PROTOCOL
namespace Avalonia.Remote.Protocol.Input
#else
namespace Avalonia.Input
#endif
{
    /// <summary>
    /// Defines the keys available on a keyboard.
    /// </summary>
    public enum Key
    {
        /// <summary>
        /// No key pressed.
        /// </summary>
        None = 0,

        /// <summary>
        /// The Cancel key.
        /// </summary>
        Cancel = 1,

        /// <summary>
        /// The Back key.
        /// </summary>
        Back = 2,

        /// <summary>
        /// The Tab key.
        /// </summary>
        Tab = 3,

        /// <summary>
        /// The Linefeed key.
        /// </summary>
        LineFeed = 4,

        /// <summary>
        /// The Clear key.
        /// </summary>
        Clear = 5,

        /// <summary>
        /// The Return key.
        /// </summary>
        Return = 6,

        /// <summary>
        /// The Enter key.
        /// </summary>
        Enter = 6,

        /// <summary>
        /// The Pause key.
        /// </summary>
        Pause = 7,

        /// <summary>
        /// The Caps Lock key.
        /// </summary>
        CapsLock = 8,

        /// <summary>
        /// The Caps Lock key.
        /// </summary>
        Capital = 8,

        /// <summary>
        /// The IME Hangul mode key.
        /// </summary>
        HangulMode = 9,

        /// <summary>
        /// The IME Kana mode key.
        /// </summary>
        KanaMode = 9,

        /// <summary>
        /// The IME Junja mode key.
        /// </summary>
        JunjaMode = 10,

        /// <summary>
        /// The IME Final mode key.
        /// </summary>
        FinalMode = 11,

        /// <summary>
        /// The IME Kanji mode key.
        /// </summary>
        KanjiMode = 12,

        /// <summary>
        /// The IME Hanja mode key.
        /// </summary>
        HanjaMode = 12,

        /// <summary>
        /// The Escape key.
        /// </summary>
        Escape = 13,

        /// <summary>
        /// The IME Convert key.
        /// </summary>
        ImeConvert = 14,

        /// <summary>
        /// The IME NonConvert key.
        /// </summary>
        ImeNonConvert = 15,

        /// <summary>
        /// The IME Accept key.
        /// </summary>
        ImeAccept = 16,

        /// <summary>
        /// The IME Mode change key.
        /// </summary>
        ImeModeChange = 17,

        /// <summary>
        /// The space bar.
        /// </summary>
        Space = 18,

        /// <summary>
        /// The Page Up key.
        /// </summary>
        PageUp = 19,

        /// <summary>
        /// The Page Up key.
        /// </summary>
        Prior = 19,

        /// <summary>
        /// The Page Down key.
        /// </summary>
        PageDown = 20,

        /// <summary>
        /// The Page Down key.
        /// </summary>
        Next = 20,

        /// <summary>
        /// The End key.
        /// </summary>
        End = 21,

        /// <summary>
        /// The Home key.
        /// </summary>
        Home = 22,

        /// <summary>
        /// The Left arrow key.
        /// </summary>
        Left = 23,

        /// <summary>
        /// The Up arrow key.
        /// </summary>
        Up = 24,

        /// <summary>
        /// The Right arrow key.
        /// </summary>
        Right = 25,

        /// <summary>
        /// The Down arrow key.
        /// </summary>
        Down = 26,

        /// <summary>
        /// The Select key.
        /// </summary>
        Select = 27,

        /// <summary>
        /// The Print key.
        /// </summary>
        Print = 28,

        /// <summary>
        /// The Execute key.
        /// </summary>
        Execute = 29,

        /// <summary>
        /// The Print Screen key.
        /// </summary>
        Snapshot = 30,

        /// <summary>
        /// The Print Screen key.
        /// </summary>
        PrintScreen = 30,

        /// <summary>
        /// The Insert key.
        /// </summary>
        Insert = 31,

        /// <summary>
        /// The Delete key.
        /// </summary>
        Delete = 32,

        /// <summary>
        /// The Help key.
        /// </summary>
        Help = 33,

        /// <summary>
        /// The 0 key.
        /// </summary>
        D0 = 34,

        /// <summary>
        /// The 1 key.
        /// </summary>
        D1 = 35,

        /// <summary>
        /// The 2 key.
        /// </summary>
        D2 = 36,

        /// <summary>
        /// The 3 key.
        /// </summary>
        D3 = 37,

        /// <summary>
        /// The 4 key.
        /// </summary>
        D4 = 38,

        /// <summary>
        /// The 5 key.
        /// </summary>
        D5 = 39,

        /// <summary>
        /// The 6 key.
        /// </summary>
        D6 = 40,

        /// <summary>
        /// The 7 key.
        /// </summary>
        D7 = 41,

        /// <summary>
        /// The 8 key.
        /// </summary>
        D8 = 42,

        /// <summary>
        /// The 9 key.
        /// </summary>
        D9 = 43,

        /// <summary>
        /// The A key.
        /// </summary>
        A = 44,

        /// <summary>
        /// The B key.
        /// </summary>
        B = 45,

        /// <summary>
        /// The C key.
        /// </summary>
        C = 46,

        /// <summary>
        /// The D key.
        /// </summary>
        D = 47,

        /// <summary>
        /// The E key.
        /// </summary>
        E = 48,

        /// <summary>
        /// The F key.
        /// </summary>
        F = 49,

        /// <summary>
        /// The G key.
        /// </summary>
        G = 50,

        /// <summary>
        /// The H key.
        /// </summary>
        H = 51,

        /// <summary>
        /// The I key.
        /// </summary>
        I = 52,

        /// <summary>
        /// The J key.
        /// </summary>
        J = 53,

        /// <summary>
        /// The K key.
        /// </summary>
        K = 54,

        /// <summary>
        /// The L key.
        /// </summary>
        L = 55,

        /// <summary>
        /// The M key.
        /// </summary>
        M = 56,

        /// <summary>
        /// The N key.
        /// </summary>
        N = 57,

        /// <summary>
        /// The O key.
        /// </summary>
        O = 58,

        /// <summary>
        /// The P key.
        /// </summary>
        P = 59,

        /// <summary>
        /// The Q key.
        /// </summary>
        Q = 60,

        /// <summary>
        /// The R key.
        /// </summary>
        R = 61,

        /// <summary>
        /// The S key.
        /// </summary>
        S = 62,

        /// <summary>
        /// The T key.
        /// </summary>
        T = 63,

        /// <summary>
        /// The U key.
        /// </summary>
        U = 64,

        /// <summary>
        /// The V key.
        /// </summary>
        V = 65,

        /// <summary>
        /// The W key.
        /// </summary>
        W = 66,

        /// <summary>
        /// The X key.
        /// </summary>
        X = 67,

        /// <summary>
        /// The Y key.
        /// </summary>
        Y = 68,

        /// <summary>
        /// The Z key.
        /// </summary>
        Z = 69,

        /// <summary>
        /// The left Windows key.
        /// </summary>
        LWin = 70,

        /// <summary>
        /// The right Windows key.
        /// </summary>
        RWin = 71,

        /// <summary>
        /// The Application key.
        /// </summary>
        Apps = 72,

        /// <summary>
        /// The Sleep key.
        /// </summary>
        Sleep = 73,

        /// <summary>
        /// The 0 key on the numeric keypad.
        /// </summary>
        NumPad0 = 74,

        /// <summary>
        /// The 1 key on the numeric keypad.
        /// </summary>
        NumPad1 = 75,

        /// <summary>
        /// The 2 key on the numeric keypad.
        /// </summary>
        NumPad2 = 76,

        /// <summary>
        /// The 3 key on the numeric keypad.
        /// </summary>
        NumPad3 = 77,

        /// <summary>
        /// The 4 key on the numeric keypad.
        /// </summary>
        NumPad4 = 78,

        /// <summary>
        /// The 5 key on the numeric keypad.
        /// </summary>
        NumPad5 = 79,

        /// <summary>
        /// The 6 key on the numeric keypad.
        /// </summary>
        NumPad6 = 80,

        /// <summary>
        /// The 7 key on the numeric keypad.
        /// </summary>
        NumPad7 = 81,

        /// <summary>
        /// The 8 key on the numeric keypad.
        /// </summary>
        NumPad8 = 82,

        /// <summary>
        /// The 9 key on the numeric keypad.
        /// </summary>
        NumPad9 = 83,

        /// <summary>
        /// The Multiply key.
        /// </summary>
        Multiply = 84,

        /// <summary>
        /// The Add key.
        /// </summary>
        Add = 85,

        /// <summary>
        /// The Separator key.
        /// </summary>
        Separator = 86,

        /// <summary>
        /// The Subtract key.
        /// </summary>
        Subtract = 87,

        /// <summary>
        /// The Decimal key.
        /// </summary>
        Decimal = 88,

        /// <summary>
        /// The Divide key.
        /// </summary>
        Divide = 89,

        /// <summary>
        /// The F1 key.
        /// </summary>
        F1 = 90,

        /// <summary>
        /// The F2 key.
        /// </summary>
        F2 = 91,

        /// <summary>
        /// The F3 key.
        /// </summary>
        F3 = 92,

        /// <summary>
        /// The F4 key.
        /// </summary>
        F4 = 93,

        /// <summary>
        /// The F5 key.
        /// </summary>
        F5 = 94,

        /// <summary>
        /// The F6 key.
        /// </summary>
        F6 = 95,

        /// <summary>
        /// The F7 key.
        /// </summary>
        F7 = 96,

        /// <summary>
        /// The F8 key.
        /// </summary>
        F8 = 97,

        /// <summary>
        /// The F9 key.
        /// </summary>
        F9 = 98,

        /// <summary>
        /// The F10 key.
        /// </summary>
        F10 = 99,

        /// <summary>
        /// The F11 key.
        /// </summary>
        F11 = 100,

        /// <summary>
        /// The F12 key.
        /// </summary>
        F12 = 101,

        /// <summary>
        /// The F13 key.
        /// </summary>
        F13 = 102,

        /// <summary>
        /// The F14 key.
        /// </summary>
        F14 = 103,

        /// <summary>
        /// The F15 key.
        /// </summary>
        F15 = 104,

        /// <summary>
        /// The F16 key.
        /// </summary>
        F16 = 105,

        /// <summary>
        /// The F17 key.
        /// </summary>
        F17 = 106,

        /// <summary>
        /// The F18 key.
        /// </summary>
        F18 = 107,

        /// <summary>
        /// The F19 key.
        /// </summary>
        F19 = 108,

        /// <summary>
        /// The F20 key.
        /// </summary>
        F20 = 109,

        /// <summary>
        /// The F21 key.
        /// </summary>
        F21 = 110,

        /// <summary>
        /// The F22 key.
        /// </summary>
        F22 = 111,

        /// <summary>
        /// The F23 key.
        /// </summary>
        F23 = 112,

        /// <summary>
        /// The F24 key.
        /// </summary>
        F24 = 113,

        /// <summary>
        /// The Numlock key.
        /// </summary>
        NumLock = 114,

        /// <summary>
        /// The Scroll key.
        /// </summary>
        Scroll = 115,

        /// <summary>
        /// The left Shift key.
        /// </summary>
        LeftShift = 116,

        /// <summary>
        /// The right Shift key.
        /// </summary>
        RightShift = 117,

        /// <summary>
        /// The left Ctrl key.
        /// </summary>
        LeftCtrl = 118,

        /// <summary>
        /// The right Ctrl key.
        /// </summary>
        RightCtrl = 119,

        /// <summary>
        /// The left Alt key.
        /// </summary>
        LeftAlt = 120,

        /// <summary>
        /// The right Alt key.
        /// </summary>
        RightAlt = 121,

        /// <summary>
        /// The browser Back key.
        /// </summary>
        BrowserBack = 122,

        /// <summary>
        /// The browser Forward key.
        /// </summary>
        BrowserForward = 123,

        /// <summary>
        /// The browser Refresh key.
        /// </summary>
        BrowserRefresh = 124,

        /// <summary>
        /// The browser Stop key.
        /// </summary>
        BrowserStop = 125,

        /// <summary>
        /// The browser Search key.
        /// </summary>
        BrowserSearch = 126,

        /// <summary>
        /// The browser Favorites key.
        /// </summary>
        BrowserFavorites = 127,

        /// <summary>
        /// The browser Home key.
        /// </summary>
        BrowserHome = 128,

        /// <summary>
        /// The Volume Mute key.
        /// </summary>
        VolumeMute = 129,

        /// <summary>
        /// The Volume Down key.
        /// </summary>
        VolumeDown = 130,

        /// <summary>
        /// The Volume Up key.
        /// </summary>
        VolumeUp = 131,

        /// <summary>
        /// The media Next Track key.
        /// </summary>
        MediaNextTrack = 132,

        /// <summary>
        /// The media Previous Track key.
        /// </summary>
        MediaPreviousTrack = 133,

        /// <summary>
        /// The media Stop key.
        /// </summary>
        MediaStop = 134,

        /// <summary>
        /// The media Play/Pause key.
        /// </summary>
        MediaPlayPause = 135,

        /// <summary>
        /// The Launch Mail key.
        /// </summary>
        LaunchMail = 136,

        /// <summary>
        /// The Select Media key.
        /// </summary>
        SelectMedia = 137,

        /// <summary>
        /// The Launch Application 1 key.
        /// </summary>
        LaunchApplication1 = 138,

        /// <summary>
        /// The Launch Application 2 key.
        /// </summary>
        LaunchApplication2 = 139,

        /// <summary>
        /// The OEM Semicolon key.
        /// </summary>
        OemSemicolon = 140,

        /// <summary>
        /// The OEM 1 key.
        /// </summary>
        Oem1 = 140,

        /// <summary>
        /// The OEM Plus key.
        /// </summary>
        OemPlus = 141,

        /// <summary>
        /// The OEM Comma key.
        /// </summary>
        OemComma = 142,

        /// <summary>
        /// The OEM Minus key.
        /// </summary>
        OemMinus = 143,

        /// <summary>
        /// The OEM Period key.
        /// </summary>
        OemPeriod = 144,

        /// <summary>
        /// The OEM Question Mark key.
        /// </summary>
        OemQuestion = 145,

        /// <summary>
        /// The OEM 2 key.
        /// </summary>
        Oem2 = 145,

        /// <summary>
        /// The OEM Tilde key.
        /// </summary>
        OemTilde = 146,

        /// <summary>
        /// The OEM 3 key.
        /// </summary>
        Oem3 = 146,

        /// <summary>
        /// The ABNT_C1 (Brazilian) key.
        /// </summary>
        AbntC1 = 147,

        /// <summary>
        /// The ABNT_C2 (Brazilian) key.
        /// </summary>
        AbntC2 = 148,

        /// <summary>
        /// The OEM Open Brackets key.
        /// </summary>
        OemOpenBrackets = 149,

        /// <summary>
        /// The OEM 4 key.
        /// </summary>
        Oem4 = 149,

        /// <summary>
        /// The OEM Pipe key.
        /// </summary>
        OemPipe = 150,

        /// <summary>
        /// The OEM 5 key.
        /// </summary>
        Oem5 = 150,

        /// <summary>
        /// The OEM Close Brackets key.
        /// </summary>
        OemCloseBrackets = 151,

        /// <summary>
        /// The OEM 6 key.
        /// </summary>
        Oem6 = 151,

        /// <summary>
        /// The OEM Quotes key.
        /// </summary>
        OemQuotes = 152,

        /// <summary>
        /// The OEM 7 key.
        /// </summary>
        Oem7 = 152,

        /// <summary>
        /// The OEM 8 key.
        /// </summary>
        Oem8 = 153,

        /// <summary>
        /// The OEM Backslash key.
        /// </summary>
        OemBackslash = 154,

        /// <summary>
        /// The OEM 3 key.
        /// </summary>
        Oem102 = 154,

        /// <summary>
        /// A special key masking the real key being processed by an IME.
        /// </summary>
        ImeProcessed = 155,

        /// <summary>
        /// A special key masking the real key being processed as a system key.
        /// </summary>
        System = 156,

        /// <summary>
        /// The OEM ATTN key.
        /// </summary>
        OemAttn = 157,

        /// <summary>
        /// The DBE_ALPHANUMERIC key.
        /// </summary>
        DbeAlphanumeric = 157,

        /// <summary>
        /// The OEM Finish key.
        /// </summary>
        OemFinish = 158,

        /// <summary>
        /// The DBE_KATAKANA key.
        /// </summary>
        DbeKatakana = 158,

        /// <summary>
        /// The DBE_HIRAGANA key.
        /// </summary>
        DbeHiragana = 159,

        /// <summary>
        /// The OEM Copy key.
        /// </summary>
        OemCopy = 159,

        /// <summary>
        /// The DBE_SBCSCHAR key.
        /// </summary>
        DbeSbcsChar = 160,

        /// <summary>
        /// The OEM Auto key.
        /// </summary>
        OemAuto = 160,

        /// <summary>
        /// The DBE_DBCSCHAR key.
        /// </summary>
        DbeDbcsChar = 161,

        /// <summary>
        /// The OEM ENLW key.
        /// </summary>
        OemEnlw = 161,

        /// <summary>
        /// The OEM BackTab key.
        /// </summary>
        OemBackTab = 162,

        /// <summary>
        /// The DBE_ROMAN key.
        /// </summary>
        DbeRoman = 162,

        /// <summary>
        /// The DBE_NOROMAN key.
        /// </summary>
        DbeNoRoman = 163,

        /// <summary>
        /// The ATTN key.
        /// </summary>
        Attn = 163,

        /// <summary>
        /// The CRSEL key.
        /// </summary>
        CrSel = 164,

        /// <summary>
        /// The DBE_ENTERWORDREGISTERMODE key.
        /// </summary>
        DbeEnterWordRegisterMode = 164,

        /// <summary>
        /// The EXSEL key.
        /// </summary>
        ExSel = 165,

        /// <summary>
        /// The DBE_ENTERIMECONFIGMODE key.
        /// </summary>
        DbeEnterImeConfigureMode = 165,

        /// <summary>
        /// The ERASE EOF Key.
        /// </summary>
        EraseEof = 166,

        /// <summary>
        /// The DBE_FLUSHSTRING key.
        /// </summary>
        DbeFlushString = 166,

        /// <summary>
        /// The Play key.
        /// </summary>
        Play = 167,

        /// <summary>
        /// The DBE_CODEINPUT key.
        /// </summary>
        DbeCodeInput = 167,

        /// <summary>
        /// The DBE_NOCODEINPUT key.
        /// </summary>
        DbeNoCodeInput = 168,

        /// <summary>
        /// The Zoom key.
        /// </summary>
        Zoom = 168,

        /// <summary>
        /// Reserved for future use.
        /// </summary>
        NoName = 169,

        /// <summary>
        /// The DBE_DETERMINESTRING key.
        /// </summary>
        DbeDetermineString = 169,

        /// <summary>
        /// The DBE_ENTERDLGCONVERSIONMODE key.
        /// </summary>
        DbeEnterDialogConversionMode = 170,

        /// <summary>
        /// The PA1 key.
        /// </summary>
        Pa1 = 170,

        /// <summary>
        /// The OEM Clear key.
        /// </summary>
        OemClear = 171,

        /// <summary>
        /// The key is used with another key to create a single combined character.
        /// </summary>
        DeadCharProcessed = 172,
        
        
        /// <summary>
        /// OSX Platform-specific Fn+Left key
        /// </summary>
        FnLeftArrow = 10001,
        /// <summary>
        /// OSX Platform-specific Fn+Right key
        /// </summary>
        FnRightArrow = 10002,
        /// <summary>
        /// OSX Platform-specific Fn+Up key
        /// </summary>
        FnUpArrow = 10003,
        /// <summary>
        /// OSX Platform-specific Fn+Down key
        /// </summary>
        FnDownArrow = 10004,
    }
}
