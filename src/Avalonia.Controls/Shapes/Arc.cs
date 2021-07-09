using System;
using Avalonia.Media;

namespace Avalonia.Controls.Shapes
{
    public class Arc : Shape
    {
        ArcSegment _arcSegment;
        PathFigure _arcFigure;
        PathGeometry _arcGeometry;

        PathGeometry _emptyGeometry = new PathGeometry()
        {
            Figures = 
            {
                new PathFigure()
                {
                    StartPoint = new Point(0, 0)
                }
            }
        };

        public static readonly StyledProperty<double> StartAngleProperty =
            AvaloniaProperty.Register<Arc, double>(nameof(StartAngle), 0.0);

        public static readonly StyledProperty<double> EndAngleProperty =
            AvaloniaProperty.Register<Arc, double>(nameof(EndAngle), 0.0);

        static Arc()
        {
            StrokeThicknessProperty.OverrideDefaultValue<Arc>(1);
            AffectsGeometry<Arc>(BoundsProperty, StrokeThicknessProperty, StartAngleProperty, EndAngleProperty);
        }

        public Arc() : base()
        {
            _arcSegment = new ArcSegment()
            {
                SweepDirection = SweepDirection.Clockwise
            };

            _arcFigure = new PathFigure()
            {
                Segments = 
                {
                    _arcSegment
                },
                IsClosed = false
            };

            _arcGeometry = new PathGeometry()
            {
                Figures = 
                {
                    _arcFigure
                }
            };
        }

        public double StartAngle
        {
            get { return GetValue(StartAngleProperty); }
            set { SetValue(StartAngleProperty, value); }
        }

        public double EndAngle
        {
            get { return GetValue(EndAngleProperty); }
            set { SetValue(EndAngleProperty, value); }
        }
        
        protected override Geometry CreateDefiningGeometry()
        {
            double angle1 = DegreesToRad(StartAngle);
            double angle2 = DegreesToRad(EndAngle);
            
            double startAngle = Math.Min(angle1, angle2);
            double endAngle = Math.Max(angle1, angle2);

            double normStart = RadToNormRad(startAngle);
            double normEnd = RadToNormRad(endAngle);

            var rect = new Rect(Bounds.Size);

            if ((normStart == normEnd) && (startAngle != endAngle)) //complete ring
            {
                return new EllipseGeometry(rect.Deflate(StrokeThickness / 2));
            }
            else if ((normStart == normEnd) && (startAngle == endAngle)) //empty
            {
                return _emptyGeometry;
            }
            else //partial ring
            {
                var deflatedRect = rect.Deflate(StrokeThickness / 2);

                double centerX = rect.Center.X;
                double centerY = rect.Center.Y;

                double radiusX = deflatedRect.Width / 2;
                double radiusY = deflatedRect.Height / 2;
                
                double angleGap = RadToNormRad(endAngle - startAngle);

                Point startPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, startAngle);
                Point endPoint = GetRingPoint(radiusX, radiusY, centerX, centerY, endAngle);
                
                _arcFigure.StartPoint = startPoint;
                
                _arcSegment.Point = endPoint;
                _arcSegment.IsLargeArc = angleGap >= HALF_TAU;
                _arcSegment.Size = new Size(radiusX, radiusY);
                
                return _arcGeometry;
            }
        }

        public override void Render(DrawingContext ctx)
        {
            double angle1 = DegreesToRad(StartAngle);
            double angle2 = DegreesToRad(EndAngle);
            
            double startAngle = Math.Min(angle1, angle2);
            double endAngle = Math.Max(angle1, angle2);

            double normStart = RadToNormRad(startAngle);
            double normEnd = RadToNormRad(endAngle);


            if ((normStart == normEnd) && (startAngle == endAngle)) //empty
            {

            }
            else //not empty
            {
                base.Render(ctx);
            }
        }

        const double TAU = 6.2831853071795862; //Math.Tau doesn't exist pre-.NET 5 :(
        const double HALF_TAU = TAU / 2.0;

        static double DegreesToRad(double inAngle) =>
            inAngle * Math.PI / 180;
        
        static double RadToNormRad(double inAngle) =>
            (0 + (inAngle % TAU) + TAU) % TAU;


        static Point GetRingPoint(double radiusX, double radiusY, double centerX, double centerY, double angle) =>
            new Point((radiusX * Math.Cos(angle)) + centerX, (radiusY * Math.Sin(angle)) + centerY);
    }
}
