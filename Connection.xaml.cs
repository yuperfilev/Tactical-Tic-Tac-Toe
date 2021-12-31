using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace Strategic_Tic_Tac_Toe
{
    /// <summary>
    /// Логика взаимодействия для Connection.xaml
    /// </summary>
    public partial class Connection : Window
    {
        public Connection()
        {
            InitializeComponent();        
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow game = new MainWindow(false, int.Parse(port.Text), IP.Text);
                try
                {
                    Visibility = Visibility.Collapsed;
                    game.ShowDialog();
                    Close();
                }
                catch (Exception ex)
                {
                    Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неверный формат IP или порт!\n" + ex.Message);
            }
        }

        private void CreateConn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MainWindow game = new MainWindow(true, int.Parse(port.Text));
                Visibility = Visibility.Collapsed;
                game.ShowDialog();
                Close();
            }
            catch(Exception ex)
            {
                MessageBox.Show("Неверный формат IP или порт!\n" + ex.Message);
            }
        }
    }
}
