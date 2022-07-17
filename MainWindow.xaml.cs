using System;
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
            var px = new ProxyServer(5001);
            px.Start();

            //Image img = new(16.1110678, 50.1822833, 4096, 4096, 1, 20);
            //string fname = img.Get();
            //Console.WriteLine(fname);
        }
    }
}
