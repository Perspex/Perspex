using System;
using System.Collections.Generic;
using Avalonia.Data.Core;
using Avalonia.Utilities;

#nullable enable

// Don't need to override GetHashCode as the ISyntax objects will not be stored in a hash; the 
// only reason they have overridden Equals methods is for unit testing.
#pragma warning disable 659


namespace Avalonia.Markup.Parsers
{
#if !BUILDTASK
    public
#endif
    class PropertyPathGrammar
    {
        private enum State
        {
            Start,
            Next,
            AfterProperty,
            End
        }

        public static IEnumerable<ISyntax> Parse(string s)
        {
            var r = new CharacterReader(s.AsSpan());
            return Parse(ref r);
        }

        private static IEnumerable<ISyntax> Parse(ref CharacterReader r)
        {
            var state = State.Start;
            var parsed = new List<ISyntax>();
            while (state != State.End)
            {
                ISyntax? syntax = null;
                if (state == State.Start)
                    (state, syntax) = ParseStart(ref r);
                else if (state == State.Next)
                    (state, syntax) = ParseNext(ref r);
                else if (state == State.AfterProperty)
                    (state, syntax) = ParseAfterProperty(ref r);


                if (syntax != null)
                {
                    parsed.Add(syntax);
                }
            }

            if (state != State.End && r.End)
            {
                throw new ExpressionParseException(r.Position, "Unexpected end of property path");
            }

            return parsed;
        }
       
        private static (State, ISyntax?) ParseNext(ref CharacterReader r)
        {
            r.SkipWhitespace();
            if (r.End)
                return (State.End, null);

            return ParseStart(ref r);
        }

        private static (State, ISyntax) ParseStart(ref CharacterReader r)
        {
            if (TryParseCasts(ref r, out var rv))
                return rv;
            r.SkipWhitespace();

            if (r.TakeIf('('))
                return ParseTypeQualifiedProperty(ref r);

            return ParseProperty(ref r);
        }

        private static (State, ISyntax) ParseTypeQualifiedProperty(ref CharacterReader r)
        {
            r.SkipWhitespace();
            const string error =
                "Unable to parse qualified property name, expected `(ns:TypeName.PropertyName)` or `(TypeName.PropertyName)` after `(`";

            var typeName = ParseXamlIdentifier(ref r);


            if (!r.TakeIf('.'))
                throw new ExpressionParseException(r.Position, error);

            var propertyName = r.ParseIdentifier();
            if (propertyName.IsEmpty)
                throw new ExpressionParseException(r.Position, error);

            r.SkipWhitespace();
            if (!r.TakeIf(')'))
                throw new ExpressionParseException(r.Position,
                    "Expected ')' after qualified property name "
                    + typeName.ns + ':' + typeName.name +
                    "." + propertyName.ToString());

            return (State.AfterProperty,
                new TypeQualifiedPropertySyntax
                {
                    Name = propertyName.ToString(),
                    TypeName = typeName.name,
                    TypeNamespace = typeName.ns
                });
        }

        static (string? ns, string name) ParseXamlIdentifier(ref CharacterReader r)
        {
            var ident = r.ParseIdentifier();
            if (ident.IsEmpty)
                throw new ExpressionParseException(r.Position, "Expected identifier");
            if (r.TakeIf(':'))
            {
                var part2 = r.ParseIdentifier();
                if (part2.IsEmpty)
                    throw new ExpressionParseException(r.Position,
                        "Expected the rest of the identifier after " + ident.ToString() + ":");
                return (ident.ToString(), part2.ToString());
            }

            return (null, ident.ToString());
        }

        private static (State, ISyntax) ParseProperty(ref CharacterReader r)
        {
            r.SkipWhitespace();
            var prop = r.ParseIdentifier();
            if (prop.IsEmpty)
                throw new ExpressionParseException(r.Position, "Unable to parse property name");
            return (State.AfterProperty, new PropertySyntax {Name = prop.ToString()});
        }

        private static bool TryParseCasts(ref CharacterReader r, out (State, ISyntax) rv)
        {
            if (r.TakeIfKeyword(":="))
                rv = ParseEnsureType(ref r);
            else if (r.TakeIfKeyword(":>") || r.TakeIfKeyword("as "))
                rv = ParseCastType(ref r);
            else
            {
                rv = default;
                return false;
            }

            return true;
        }
        
        private static (State, ISyntax?) ParseAfterProperty(ref CharacterReader r)
        {
            if (TryParseCasts(ref r, out var rv))
                return rv;

            r.SkipWhitespace();
            if (r.End)
                return (State.End, null);
            if (r.TakeIf('.'))
                return (State.Next, ChildTraversalSyntax.Instance);



            throw new ExpressionParseException(r.Position, "Unexpected character " + r.Peek + " after property name");
        }

        private static (State, ISyntax) ParseEnsureType(ref CharacterReader r)
        {
            r.SkipWhitespace();
            var type = ParseXamlIdentifier(ref r);
            return (State.AfterProperty, new EnsureTypeSyntax {TypeName = type.name, TypeNamespace = type.ns});
        }

        private static (State, ISyntax) ParseCastType(ref CharacterReader r)
        {
            r.SkipWhitespace();
            var type = ParseXamlIdentifier(ref r);
            return (State.AfterProperty, new CastTypeSyntax {TypeName = type.name, TypeNamespace = type.ns});
        }

        public interface ISyntax
        {

        }

        public class PropertySyntax : ISyntax
        {
            public string Name { get; set; } = string.Empty;

            public override bool Equals(object? obj)
                => obj is PropertySyntax other
                   && other.Name == Name;
        }

        public class TypeQualifiedPropertySyntax : ISyntax
        {
            public string Name { get; set; } = string.Empty;
            public string TypeName { get; set; } = string.Empty;
            public string? TypeNamespace { get; set; }

            public override bool Equals(object? obj)
                => obj is TypeQualifiedPropertySyntax other
                   && other.Name == Name
                   && other.TypeName == TypeName
                   && other.TypeNamespace == TypeNamespace;
        }

        public class ChildTraversalSyntax : ISyntax
        {
            public static ChildTraversalSyntax Instance { get;  } = new ChildTraversalSyntax();
            public override bool Equals(object? obj) => obj is ChildTraversalSyntax;
        }

        public class EnsureTypeSyntax : ISyntax
        {
            public string TypeName { get; set; } = string.Empty;
            public string? TypeNamespace { get; set; }
            public override bool Equals(object? obj)
                => obj is EnsureTypeSyntax other
                   && other.TypeName == TypeName
                   && other.TypeNamespace == TypeNamespace;
        }

        public class CastTypeSyntax : ISyntax
        {
            public string TypeName { get; set; } = string.Empty;
            public string? TypeNamespace { get; set; }
            public override bool Equals(object? obj)
                => obj is CastTypeSyntax other
                   && other.TypeName == TypeName
                   && other.TypeNamespace == TypeNamespace;
        }
    }
}
