using System;

namespace Avalonia.Utilities
{
    public static class UriUtilities
    {
        internal static void RegisterResUriParsers()
        {
            if (!UriParser.IsKnownScheme("avares"))
                UriParser.Register(new GenericUriParser(
                    GenericUriParserOptions.GenericAuthority |
                    GenericUriParserOptions.NoUserInfo |
                    GenericUriParserOptions.NoPort |
                    GenericUriParserOptions.NoQuery |
                    GenericUriParserOptions.NoFragment), "avares", -1);
        }
    }
}
