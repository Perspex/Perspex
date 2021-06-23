namespace Avalonia.Media.TextFormatting.Unicode
{
    public enum GeneralCategory
    {
        Other, //C# Cc | Cf | Cn | Co | Cs
        Control, //Cc
        Format, //Cf
        Unassigned, //Cn
        PrivateUse, //Co
        Surrogate, //Cs
        Letter, //L# Ll | Lm | Lo | Lt | Lu
        CasedLetter, //LC# Ll | Lt | Lu
        LowercaseLetter, //Ll
        ModifierLetter, //Lm
        OtherLetter, //Lo
        TitlecaseLetter, //Lt
        UppercaseLetter, //Lu
        Mark, //M
        SpacingMark, //Mc
        EnclosingMark, //Me
        NonspacingMark, //Mn
        Number, //N# Nd | Nl | No
        DecimalNumber, //Nd
        LetterNumber, //Nl
        OtherNumber, //No
        Punctuation, //P
        ConnectorPunctuation, //Pc
        DashPunctuation, //Pd
        ClosePunctuation, //Pe
        FinalPunctuation, //Pf
        InitialPunctuation, //Pi
        OtherPunctuation, //Po
        OpenPunctuation, //Ps
        Symbol, //S# Sc | Sk | Sm | So
        CurrencySymbol, //Sc
        ModifierSymbol, //Sk
        MathSymbol, //Sm
        OtherSymbol, //So
        Separator, //Z# Zl | Zp | Zs
        LineSeparator, //Zl
        ParagraphSeparator, //Zp
        SpaceSeparator, //Zs
    }
}
