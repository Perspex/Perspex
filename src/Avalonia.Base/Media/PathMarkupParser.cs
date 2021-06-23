using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Parses a path markup string.
    /// </summary>
    public class PathMarkupParser : IDisposable
    {
        private static readonly Dictionary<char, Command> s_commands =
            new Dictionary<char, Command>
                {
                    { 'F', Command.FillRule },
                    { 'M', Command.Move },
                    { 'L', Command.Line },
                    { 'H', Command.HorizontalLine },
                    { 'V', Command.VerticalLine },
                    { 'Q', Command.QuadraticBezierCurve },
                    { 'T', Command.SmoothQuadraticBezierCurve },
                    { 'C', Command.CubicBezierCurve },
                    { 'S', Command.SmoothCubicBezierCurve },
                    { 'A', Command.Arc },
                    { 'Z', Command.Close },
                };

        private IGeometryContext _geometryContext;
        private Point _currentPoint;
        private Point? _beginFigurePoint;
        private Point? _previousControlPoint;
        private bool _isOpen;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathMarkupParser"/> class.
        /// </summary>
        /// <param name="geometryContext">The geometry context.</param>
        /// <exception cref="ArgumentNullException">geometryContext</exception>
        public PathMarkupParser(IGeometryContext geometryContext)
        {
            if (geometryContext == null)
            {
                throw new ArgumentNullException(nameof(geometryContext));
            }

            _geometryContext = geometryContext;
        }

        private enum Command
        {
            None,
            FillRule,
            Move,
            Line,
            HorizontalLine,
            VerticalLine,
            CubicBezierCurve,
            QuadraticBezierCurve,
            SmoothCubicBezierCurve,
            SmoothQuadraticBezierCurve,
            Arc,
            Close
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _geometryContext = null;
            }

            _isDisposed = true;
        }

        private static Point MirrorControlPoint(Point controlPoint, Point center)
        {
            var dir = controlPoint - center;

            return center + -dir;
        }

        /// <summary>
        /// Parses the specified path data and writes the result to the geometryContext of this instance.
        /// </summary>
        /// <param name="pathData">The path data.</param>
        public void Parse(string pathData)
        {
            var span = pathData.AsSpan();
            _currentPoint = new Point();

            while(!span.IsEmpty)
            {
                if(!ReadCommand(ref span, out var command, out var relative))
                {
                    break;
                }

                bool initialCommand = true;
                
                do
                {
                    if (!initialCommand)
                    {
                        span = ReadSeparator(span);
                    }

                    switch (command)
                    {
                        case Command.None:
                            break;
                        case Command.FillRule:
                            SetFillRule(ref span);
                            break;
                        case Command.Move:
                            AddMove(ref span, relative);
                            break;
                        case Command.Line:
                            AddLine(ref span, relative);
                            break;
                        case Command.HorizontalLine:
                            AddHorizontalLine(ref span, relative);
                            break;
                        case Command.VerticalLine:
                            AddVerticalLine(ref span, relative);
                            break;
                        case Command.CubicBezierCurve:
                            AddCubicBezierCurve(ref span, relative);
                            break;
                        case Command.QuadraticBezierCurve:
                            AddQuadraticBezierCurve(ref span, relative);
                            break;
                        case Command.SmoothCubicBezierCurve:
                            AddSmoothCubicBezierCurve(ref span, relative);
                            break;
                        case Command.SmoothQuadraticBezierCurve:
                            AddSmoothQuadraticBezierCurve(ref span, relative);
                            break;
                        case Command.Arc:
                            AddArc(ref span, relative);
                            break;
                        case Command.Close:
                            CloseFigure();
                            break;
                        default:
                            throw new NotSupportedException("Unsupported command");
                    }

                    initialCommand = false;
                } while (PeekArgument(span));
                
            }

            if (_isOpen)
            {
                _geometryContext.EndFigure(false);
            }
        }

        private void CreateFigure()
        {
            if (_isOpen)
            {
                _geometryContext.EndFigure(false);
            }

            _geometryContext.BeginFigure(_currentPoint);

            _beginFigurePoint = _currentPoint;

            _isOpen = true;
        }

        private void SetFillRule(ref ReadOnlySpan<char> span)
        {
            if (!ReadArgument(ref span, out var fillRule) || fillRule.Length != 1)
            {
                throw new InvalidDataException("Invalid fill rule.");
            }

            FillRule rule;

            switch (fillRule[0])
            {
                case '0':
                    rule = FillRule.EvenOdd;
                    break;
                case '1':
                    rule = FillRule.NonZero;
                    break;
                default:
                    throw new InvalidDataException("Invalid fill rule");
            }

            _geometryContext.SetFillRule(rule);
        }

        private void CloseFigure()
        {
            if (_isOpen)
            {
                _geometryContext.EndFigure(true);

                if (_beginFigurePoint != null)
                {
                    _currentPoint = _beginFigurePoint.Value;
                    _beginFigurePoint = null;
                }
            }

            _previousControlPoint = null;

            _isOpen = false;
        }

        private void AddMove(ref ReadOnlySpan<char> span, bool relative)
        {
            var currentPoint = relative
                                ? ReadRelativePoint(ref span, _currentPoint)
                                : ReadPoint(ref span);

            _currentPoint = currentPoint;

            CreateFigure();

            while (PeekArgument(span))
            {
                span = ReadSeparator(span);
                AddLine(ref span, relative);
            }
        }

        private void AddLine(ref ReadOnlySpan<char> span, bool relative)
        {
            _currentPoint = relative
                                ? ReadRelativePoint(ref span, _currentPoint)
                                : ReadPoint(ref span);

            if (!_isOpen)
            {
                CreateFigure();
            }

            _geometryContext.LineTo(_currentPoint);
        }

        private void AddHorizontalLine(ref ReadOnlySpan<char> span, bool relative)
        {
            _currentPoint = relative
                                ? new Point(_currentPoint.X + ReadDouble(ref span), _currentPoint.Y)
                                : _currentPoint.WithX(ReadDouble(ref span));

            if (!_isOpen)
            {
                CreateFigure();
            }

            _geometryContext.LineTo(_currentPoint);
        }

        private void AddVerticalLine(ref ReadOnlySpan<char> span, bool relative)
        {
            _currentPoint = relative
                                ? new Point(_currentPoint.X, _currentPoint.Y + ReadDouble(ref span))
                                : _currentPoint.WithY(ReadDouble(ref span));

            if (!_isOpen)
            {
                CreateFigure();
            }

            _geometryContext.LineTo(_currentPoint);
        }

        private void AddCubicBezierCurve(ref ReadOnlySpan<char> span, bool relative)
        {
            var point1 = relative
                    ? ReadRelativePoint(ref span, _currentPoint)
                    : ReadPoint(ref span);

            span = ReadSeparator(span);

            var point2 = relative
                    ? ReadRelativePoint(ref span, _currentPoint)
                    : ReadPoint(ref span);

            _previousControlPoint = point2;

            span = ReadSeparator(span);

            var point3 = relative
                    ? ReadRelativePoint(ref span, _currentPoint)
                    : ReadPoint(ref span);

            if (!_isOpen)
            {
                CreateFigure();
            }

            _geometryContext.CubicBezierTo(point1, point2, point3);

            _currentPoint = point3;
        }

        private void AddQuadraticBezierCurve(ref ReadOnlySpan<char> span, bool relative)
        {
            var start = relative
                    ? ReadRelativePoint(ref span, _currentPoint)
                    : ReadPoint(ref span);

            _previousControlPoint = start;

            span = ReadSeparator(span);

            var end = relative
                    ? ReadRelativePoint(ref span, _currentPoint)
                    : ReadPoint(ref span);

            if (!_isOpen)
            {
                CreateFigure();
            }

            _geometryContext.QuadraticBezierTo(start, end);

            _currentPoint = end;
        }

        private void AddSmoothCubicBezierCurve(ref ReadOnlySpan<char> span, bool relative)
        {
            var point2 = relative
                    ? ReadRelativePoint(ref span, _currentPoint)
                    : ReadPoint(ref span);

            span = ReadSeparator(span);

            var end = relative
                    ? ReadRelativePoint(ref span, _currentPoint)
                    : ReadPoint(ref span);

            if (_previousControlPoint != null)
            {
                _previousControlPoint = MirrorControlPoint((Point)_previousControlPoint, _currentPoint);
            }

            if (!_isOpen)
            {
                CreateFigure();
            }

            _geometryContext.CubicBezierTo(_previousControlPoint ?? _currentPoint, point2, end);

            _previousControlPoint = point2;

            _currentPoint = end;
        }

        private void AddSmoothQuadraticBezierCurve(ref ReadOnlySpan<char> span, bool relative)
        {
            var end = relative
                    ? ReadRelativePoint(ref span, _currentPoint)
                    : ReadPoint(ref span);

            if (_previousControlPoint != null)
            {
                _previousControlPoint = MirrorControlPoint((Point)_previousControlPoint, _currentPoint);
            }

            if (!_isOpen)
            {
                CreateFigure();
            }

            _geometryContext.QuadraticBezierTo(_previousControlPoint ?? _currentPoint, end);

            _currentPoint = end;
        }

        private void AddArc(ref ReadOnlySpan<char> span, bool relative)
        {
            var size = ReadSize(ref span);

            span = ReadSeparator(span);

            var rotationAngle = ReadDouble(ref span);
            span = ReadSeparator(span);
            var isLargeArc = ReadBool(ref span);

            span = ReadSeparator(span);

            var sweepDirection = ReadBool(ref span) ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;
            
            span = ReadSeparator(span);

            var end = relative
                    ? ReadRelativePoint(ref span, _currentPoint)
                    : ReadPoint(ref span);

            if (!_isOpen)
            {
                CreateFigure();
            }

            _geometryContext.ArcTo(end, size, rotationAngle, isLargeArc, sweepDirection);

            _currentPoint = end;

            _previousControlPoint = null;
        }

        private static bool PeekArgument(ReadOnlySpan<char> span)
        {
            span = SkipWhitespace(span);

            return !span.IsEmpty && (span[0] == ',' || span[0] == '-' || span[0] == '.' || char.IsDigit(span[0]));
        }

        private static bool ReadArgument(ref ReadOnlySpan<char> remaining, out ReadOnlySpan<char> argument)
        {
            remaining = SkipWhitespace(remaining);
            if (remaining.IsEmpty)
            {
                argument = ReadOnlySpan<char>.Empty;
                return false;
            }

            var valid = false;
            int i = 0;
            if (remaining[i] == '-')
            {
                i++;
            }
            for (; i < remaining.Length && char.IsNumber(remaining[i]); i++) valid = true;

            if (i < remaining.Length && remaining[i] == '.')
            {
                valid = false;
                i++;
            }
            for (; i < remaining.Length && char.IsNumber(remaining[i]); i++) valid = true;

            if (i < remaining.Length)
            {
                // scientific notation
                if (remaining[i] == 'E' || remaining[i] == 'e')
                {
                    valid = false;
                    i++;
                    if (remaining[i] == '-' || remaining[i] == '+')
                    {
                        i++;
                        for (; i < remaining.Length && char.IsNumber(remaining[i]); i++) valid = true;
                    }                  
                }               
            }          

            if (!valid)
            {
                argument = ReadOnlySpan<char>.Empty;
                return false;
            }
            argument = remaining.Slice(0, i);
            remaining = remaining.Slice(i);
            return true;
        }


        private static ReadOnlySpan<char> ReadSeparator(ReadOnlySpan<char> span)
        {
            span = SkipWhitespace(span);
            if (!span.IsEmpty && span[0] == ',')
            {
                span = span.Slice(1);
            }
            return span;
        }

        private static ReadOnlySpan<char> SkipWhitespace(ReadOnlySpan<char> span)
        {
            int i = 0;
            for (; i < span.Length && char.IsWhiteSpace(span[i]); i++) ;
            return span.Slice(i);
        }

        private bool ReadBool(ref ReadOnlySpan<char> span)
        {
            if (!ReadArgument(ref span, out var boolValue) || boolValue.Length != 1)
            {
                throw new InvalidDataException("Invalid bool rule.");
            }
            
            switch (boolValue[0])
            {
                case '0':
                    return false;
                case '1':
                    return true;
                default:
                    throw new InvalidDataException("Invalid bool rule");
            }
        }

        private double ReadDouble(ref ReadOnlySpan<char> span)
        {
            if (!ReadArgument(ref span, out var doubleValue))
            {
                throw new InvalidDataException("Invalid double value");
            }

            return double.Parse(doubleValue.ToString(), CultureInfo.InvariantCulture);
        }

        private Size ReadSize(ref ReadOnlySpan<char> span)
        {
            var width = ReadDouble(ref span);
            span = ReadSeparator(span);
            var height = ReadDouble(ref span);
            return new Size(width, height);
        }

        private Point ReadPoint(ref ReadOnlySpan<char> span)
        {
            var x = ReadDouble(ref span);
            span = ReadSeparator(span);
            var y = ReadDouble(ref span);
            return new Point(x, y);
        }

        private Point ReadRelativePoint(ref ReadOnlySpan<char> span, Point origin)
        {
            var x = ReadDouble(ref span);
            span = ReadSeparator(span);
            var y = ReadDouble(ref span);
            return new Point(origin.X + x, origin.Y + y);
        }

        private bool ReadCommand(ref ReadOnlySpan<char> span, out Command command, out bool relative)
        {
            span = SkipWhitespace(span);
            if (span.IsEmpty)
            {
                command = default;
                relative = false;
                return false;
            }
            var c = span[0];
            if (!s_commands.TryGetValue(char.ToUpperInvariant(c), out command))
            {
                throw new InvalidDataException("Unexpected path command '" + c + "'.");
            }
            relative = char.IsLower(c);
            span = span.Slice(1);
            return true;
        }
    }
}
