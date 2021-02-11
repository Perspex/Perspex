using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Media;

namespace Avalonia.Documents.Internal
{
    public class IdentityTransform : Transform
    {
        // TODO: This replaces Transform.Identity. Is this okay, or should this be moved into Avalonia?
        public static readonly IdentityTransform Instance = new ();

        public override Matrix Value => Matrix.Identity;
    }
}
