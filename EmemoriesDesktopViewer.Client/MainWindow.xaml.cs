using System.Drawing;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace EmemoriesDesktopViewer.Client
{
    /// <summary>
    /// Logica di interazione per MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnShare_Click(object sender, RoutedEventArgs e)
        {
            new ShareScreenWindow().Show();
        }

        private void BtnSee_Click(object sender, RoutedEventArgs e)
        {
            new SeeScreenWindow().Show();
        }
    }
}
