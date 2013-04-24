using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using PewPew.Server;

namespace PewPew
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Communicator comm;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            comm = new Communicator();
            lblConnectionStatus.Content = comm.GetState();
            StartSocket();
            lblConnectionStatus.Content = comm.GetState();
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            lblConnectionStatus.Content = comm.GetState();
            StopSocket();
            lblConnectionStatus.Content = comm.GetState();
        }

        private void StartSocket()
        {
            comm = new Communicator();
            lblConnectionStatus.Content = comm.GetState();
            comm.BeginListening();
            lblConnectionStatus.Content = comm.GetState();
            comm.WaitForConnection();
            lblConnectionStatus.Content = comm.GetState();
        }

        private void StopSocket()
        {
            lblConnectionStatus.Content = comm.GetState();
            comm.Close();
            lblConnectionStatus.Content = comm.GetState();
        }
    }
}
