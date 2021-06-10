using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;

namespace Avalonia.Diagnostics.Controls
{
    internal class ThicknessEditor : ContentControl
    {
        public static readonly DirectProperty<ThicknessEditor, Thickness> ThicknessProperty =
            AvaloniaProperty.RegisterDirect<ThicknessEditor, Thickness>(nameof(Thickness), o => o.Thickness,
                (o, v) => o.Thickness = v, defaultBindingMode: BindingMode.TwoWay);

        public static readonly DirectProperty<ThicknessEditor, string?> HeaderProperty =
            AvaloniaProperty.RegisterDirect<ThicknessEditor, string?>(nameof(Header), o => o.Header,
                (o, v) => o.Header = v);

        public static readonly DirectProperty<ThicknessEditor, bool> IsPresentProperty =
            AvaloniaProperty.RegisterDirect<ThicknessEditor, bool>(nameof(Header), o => o.IsPresent,
                (o, v) => o.IsPresent = v);

        public static readonly DirectProperty<ThicknessEditor, double> LeftProperty =
            AvaloniaProperty.RegisterDirect<ThicknessEditor, double>(nameof(Left), o => o.Left, (o, v) => o.Left = v);

        public static readonly DirectProperty<ThicknessEditor, double> TopProperty =
            AvaloniaProperty.RegisterDirect<ThicknessEditor, double>(nameof(Top), o => o.Top, (o, v) => o.Top = v);

        public static readonly DirectProperty<ThicknessEditor, double> RightProperty =
            AvaloniaProperty.RegisterDirect<ThicknessEditor, double>(nameof(Right), o => o.Right,
                (o, v) => o.Right = v);

        public static readonly DirectProperty<ThicknessEditor, double> BottomProperty =
            AvaloniaProperty.RegisterDirect<ThicknessEditor, double>(nameof(Bottom), o => o.Bottom,
                (o, v) => o.Bottom = v);

        public static readonly StyledProperty<IBrush> HighlightProperty =
            AvaloniaProperty.Register<ThicknessEditor, IBrush>(nameof(Highlight));

        private Thickness _thickness;
        private string? _header;
        private bool _isPresent = true;
        private double _left;
        private double _top;
        private double _right;
        private double _bottom;
        private bool _isUpdatingThickness;

        public Thickness Thickness
        {
            get => _thickness;
            set => SetAndRaise(ThicknessProperty, ref _thickness, value);
        }

        public string? Header
        {
            get => _header;
            set => SetAndRaise(HeaderProperty, ref _header, value);
        }

        public bool IsPresent
        {
            get => _isPresent;
            set => SetAndRaise(IsPresentProperty, ref _isPresent, value);
        }

        public double Left
        {
            get => _left;
            set => SetAndRaise(LeftProperty, ref _left, value);
        }

        public double Top
        {
            get => _top;
            set => SetAndRaise(TopProperty, ref _top, value);
        }

        public double Right
        {
            get => _right;
            set => SetAndRaise(RightProperty, ref _right, value);
        }

        public double Bottom
        {
            get => _bottom;
            set => SetAndRaise(BottomProperty, ref _bottom, value);
        }

        public IBrush Highlight
        {
            get => GetValue(HighlightProperty);
            set => SetValue(HighlightProperty, value);
        }

        protected override void OnPropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == ThicknessProperty)
            {
                try
                {
                    _isUpdatingThickness = true;

                    var value = change.NewValue.GetValueOrDefault<Thickness>();

                    Left = value.Left;
                    Top = value.Top;
                    Right = value.Right;
                    Bottom = value.Bottom;
                }
                finally
                {
                    _isUpdatingThickness = false;
                }
            }
            else if (!_isUpdatingThickness &&
                     (change.Property == LeftProperty || change.Property == TopProperty ||
                      change.Property == RightProperty || change.Property == BottomProperty))
            {
                Thickness = new Thickness(Left, Top, Right, Bottom);
            }
        }
    }
}
