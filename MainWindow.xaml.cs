using System.Diagnostics;
using System.Net;
using System.Windows;

namespace MapyCZforTS_CS
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var px = new Proxy();
            px.Start();

        }

        private void enableCheckbox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void portInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {

        }

        private void loggingCheckbox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void cachingCheckbox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void mapsetInput_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {

        }
    }
}
