using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace NicksTestApp
{
    public partial class MainWindow : Window
    {
        Button _button;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
