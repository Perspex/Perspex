namespace Avalonia.Media.TextFormatting.Unicode
{
    public enum LineBreakClass
    {
        OpenPunctuation, //OP
        ClosePunctuation, //CL
        CloseParenthesis, //CP
        Quotation, //QU
        Glue, //GL
        Nonstarter, //NS
        Exclamation, //EX
        BreakSymbols, //SY
        InfixNumeric, //IS
        PrefixNumeric, //PR
        PostfixNumeric, //PO
        Numeric, //NU
        Alphabetic, //AL
        HebrewLetter, //HL
        Ideographic, //ID
        Inseparable, //IN
        Hyphen, //HY
        BreakAfter, //BA
        BreakBefore, //BB
        BreakBoth, //B2
        ZWSpace, //ZW
        CombiningMark, //CM
        WordJoiner, //WJ
        H2, //H2
        H3, //H3
        JL, //JL
        JV, //JV
        JT, //JT
        RegionalIndicator, //RI
        EBase, //EB
        EModifier, //EM
        ZWJ, //ZWJ
        ContingentBreak, //CB

        Unknown, //XX
        Ambiguous, //AI
        MandatoryBreak, //BK
        ConditionalJapaneseStarter, //CJ
        CarriageReturn, //CR
        LineFeed, //LF
        NextLine, //NL
        ComplexContext, //SA
        Surrogate, //SG
        Space, //SP
    }
}
