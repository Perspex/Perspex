using System;
using System.Globalization;
using Avalonia.Animation.Animators;
using Avalonia.Utilities;

namespace Avalonia.Media
{
    public struct BoxShadow
    {
        public double OffsetX { get; set; }
        public double OffsetY { get; set; }
        public double Blur { get; set; }
        public double Spread { get; set; }
        public Color Color { get; set; }
        public bool IsInset { get; set; }

        static BoxShadow()
        {
            Animation.Animation.RegisterAnimator<BoxShadowAnimator>(prop =>
                typeof(BoxShadow).IsAssignableFrom(prop.PropertyType));
        }
        
        public bool Equals(in BoxShadow other)
        {
            return OffsetX.Equals(other.OffsetX) && OffsetY.Equals(other.OffsetY) && Blur.Equals(other.Blur) && Spread.Equals(other.Spread) && Color.Equals(other.Color);
        }

        public override bool Equals(object obj)
        {
            return obj is BoxShadow other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = OffsetX.GetHashCode();
                hashCode = (hashCode * 397) ^ OffsetY.GetHashCode();
                hashCode = (hashCode * 397) ^ Blur.GetHashCode();
                hashCode = (hashCode * 397) ^ Spread.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                return hashCode;
            }
        }

        public bool IsEmpty => OffsetX == 0 && OffsetY == 0 && Blur == 0 && Spread == 0;

        private readonly static char[] s_Separator = new char[] { ' ', '\t' };

        struct ArrayReader
        {
            private int _index;
            private string[] _arr;

            public ArrayReader(string[] arr)
            {
                _arr = arr;
                _index = 0;
            }

            public bool TryReadString(out string s)
            {
                s = null;
                if (_index >= _arr.Length)
                    return false;
                s = _arr[_index];
                _index++;
                return true;
            }

            public string ReadString()
            {
                if(!TryReadString(out var rv))
                    throw new FormatException();
                return rv;
            }
        }
        public static unsafe BoxShadow Parse(string s)
        {
            if(s == null)
                throw new ArgumentNullException();
            if (s.Length == 0)
                throw new FormatException();

            var p = s.Split(s_Separator, StringSplitOptions.RemoveEmptyEntries);
            if (p.Length == 1 && p[0] == "none")
                return default;
            
            if (p.Length < 3 || p.Length > 6)
                throw new FormatException();
            
            bool inset = false;

            var tokenizer = new ArrayReader(p);

            string firstToken = tokenizer.ReadString();
            if (firstToken == "inset")
            {
                inset = true;
                firstToken = tokenizer.ReadString();
            }

            var offsetX = double.Parse(firstToken, CultureInfo.InvariantCulture);
            var offsetY = double.Parse(tokenizer.ReadString(), CultureInfo.InvariantCulture);
            double blur = 0;
            double spread = 0;
            

            tokenizer.TryReadString(out var token3);
            tokenizer.TryReadString(out var token4);
            tokenizer.TryReadString(out var token5);

            if (token4 != null) 
                blur = double.Parse(token3, CultureInfo.InvariantCulture);
            if (token5 != null)
                spread = double.Parse(token4, CultureInfo.InvariantCulture);

            var color = Color.Parse(token5 ?? token4 ?? token3);
            return new BoxShadow
            {
                IsInset = inset,
                OffsetX = offsetX,
                OffsetY = offsetY,
                Blur = blur,
                Spread = spread,
                Color = color
            };
        }

        public Rect TransformBounds(in Rect rect)
            => IsInset ? rect : rect.Translate(new Vector(OffsetX, OffsetY)).Inflate(Spread + Blur);
    }
}
