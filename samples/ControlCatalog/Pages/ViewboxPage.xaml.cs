using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace ControlCatalog.Pages
{
    public class ViewboxPage : UserControl
    {
        public ViewboxPage()
        {
            InitializeComponent();

            var stretchSelector = this.FindControl<ComboBox>("StretchSelector");

            stretchSelector.Items = new[]
            {
                Stretch.Uniform, Stretch.UniformToFill, Stretch.Fill, Stretch.None
            };

            stretchSelector.SelectedIndex = 0;

            var stretchDirectionSelector = this.FindControl<ComboBox>("StretchDirectionSelector");

            stretchDirectionSelector.Items = new[]
            {
                StretchDirection.Both, StretchDirection.DownOnly, StretchDirection.UpOnly
            };

            stretchDirectionSelector.SelectedIndex = 0;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
