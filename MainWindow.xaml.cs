using System;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using System.Text;

namespace Strategic_Tic_Tac_Toe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constructor
        public MainWindow(bool isHost, int port, string ip = null)
        {
            InitializeComponent();
            MessageReceiver.DoWork += MessageReceiver_DoWork;         
            NewGame();
            if (isHost)
            {
                xPlayer = true;
                //System.Net.IPAddress iP = System.Net.IPAddress.Any;
                System.Net.IPAddress[] iP = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName());
                MessageBox.Show("IP-адрес сервера: " + iP[1]);
                server = new TcpListener(iP[1], port);
                server.Start();
                sock = server.AcceptSocket();
                Msg.Foreground = Brushes.Green;
                Msg.Text = "Ваш ход";
            }
            else
            {
                xPlayer = false;
                try
                {
                    client = new TcpClient(ip, port);
                    sock = client.Client;
                    MessageReceiver.RunWorkerAsync();
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    Close();
                }
            }
            
        }      
        #endregion

        #region Private Members

        private GameStatus[] mResult;
        private MarkType[,] mOperResult;
        private bool xPlayer;
        private bool mGameEnded;
        private bool freezboard;     
        private int nNextField;
        private int nOperField;
        private Socket sock;
        private BackgroundWorker MessageReceiver = new BackgroundWorker();
        private TcpClient client;
        private TcpListener server = null;
        private Help h;
        #endregion
        private void NewGame()
        {
            mResult = new GameStatus[9];
            mOperResult = new MarkType[9,9];            
            
            for(var i=0; i<mResult.Length; i++)
            {
                mResult[i] = GameStatus.Game;
                for (var j = 0; j < mResult.Length; j++)
                    mOperResult[i, j] = MarkType.Free;
            }         
            nNextField = -1;

            Container.Children.OfType<Button>().ToList().ForEach(button =>
            {
                if (button.Name != "btnNewG" && button.Name != "btnEndG" && button.Name !="btnH")
                {
                    button.Content = string.Empty;
                    button.Background = Brushes.White;
                    button.Foreground = Brushes.Blue;
                }
            });
            mGameEnded = false;
        }
        private void MessageReceiver_DoWork(object sender, DoWorkEventArgs e)
        {
            freezboard = true;
            CheckOperWinner();
            if (CheckTactWinner()) return;
            Dispatcher.Invoke(() => { Msg.Text = "Ход оппонента"; Msg.Foreground = Brushes.Green;});
            ReceiveMove();
            xPlayer = !xPlayer;
            CheckOperWinner();
            if (!CheckTactWinner())
            {
                Dispatcher.Invoke(() => { Msg.Text = "Ваш ход";});
                freezboard = false;
                Dispatcher.Invoke(() => {
                   Container.Children.OfType<Button>().ToList().ForEach(button =>
                    {
                    if (button.Name != "btnNewG" && button.Name != "btnEndG" && button.Name != "btnH")
                    {
                        int row_check = int.Parse(button.Name[6].ToString());
                        int col_check = int.Parse(button.Name[8].ToString());
                        int field = row_check / 3 * 3 + col_check / 3;
                        button.Background = Brushes.White;
                        if (field == nNextField)
                        {
                            button.Background = Brushes.LightGreen;
                        }
                    }
                    });
                });

            }
            xPlayer = !xPlayer;
        }
        private bool CheckButton(object sender,string btn_name)
        { 
            object btn;
            Button button;
            int column, row;
            if (sender != null)
            {
                btn = sender;
                button = (Button)btn;
                column = Grid.GetColumn(button);
                row = Grid.GetRow(button);
            }
            else
            {
                btn = Dispatcher.Invoke(() => { return Container.FindName(btn_name); });
                button = (Button)btn;
                column = Dispatcher.Invoke(() => { return Grid.GetColumn(button);});
                row = Dispatcher.Invoke(() => { return Grid.GetRow(button); });
            }
            
            nOperField = row / 3 * 3 + column / 3;

            var index = row % 3 * 3 + column % 3;

            if (mResult[nOperField] != GameStatus.Game)
            {
                Msg.Text = "Неверный ход! Оперативное поле уже захвачено";
                Msg.Foreground = Brushes.Red;
                return false;
            }
            if (nNextField != -1 && nOperField != nNextField && mResult[nNextField] == GameStatus.Game)
            {
                Msg.Text = "Неверный ход! Ход должен быть сделан в зеленое поле: " + Convert.ToString(nNextField + 1);
                Msg.Foreground = Brushes.Red;
                return false;
            }
            if (xPlayer)
            {
                mOperResult[nOperField, index] = MarkType.Cross;
                Dispatcher.Invoke(() => { button.Content = "X"; });
            }
            else
            {
                mOperResult[nOperField, index] = MarkType.Nought;
                Dispatcher.Invoke(() =>
                {
                    button.Foreground = Brushes.Red;
                    button.Content = "O";
                });
            }
            nNextField = index;
            return true;
        }
        private void Button_click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            Msg.Text = "";
            if (freezboard)
            {
                MessageBox.Show("Ход оппонента");
                return;
            }
                
            else if (!CheckButton(sender,null))
                    return;
            byte[] buffer;
            string btn_name = button.Name;
            buffer = Encoding.UTF8.GetBytes(btn_name);
            try
            {
                sock.Send(buffer);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Оппонент вышел");
                ClkOnNewG(null, null);
            }
            MessageReceiver.RunWorkerAsync();
        }

        private void CheckOperWinner()
        {

            #region checking by rows

            // row 0
            if (mOperResult[nOperField, 0] != MarkType.Free && (mOperResult[nOperField, 0] & mOperResult[nOperField, 1] & mOperResult[nOperField, 2]) == mOperResult[nOperField, 0])
            {
                if (xPlayer)
                    mResult[nOperField] = GameStatus.WinO;
                else
                    mResult[nOperField] = GameStatus.WinX;
            }
            
            //row 1
            if (mOperResult[nOperField, 3] != MarkType.Free && (mOperResult[nOperField, 3] & mOperResult[nOperField, 4] & mOperResult[nOperField, 5]) == mOperResult[nOperField, 3])
            {
                if (xPlayer)
                    mResult[nOperField] = GameStatus.WinO;
                else
                    mResult[nOperField] = GameStatus.WinX;
            }

            //row 2
            if (mOperResult[nOperField, 6] != MarkType.Free && (mOperResult[nOperField, 6] & mOperResult[nOperField, 7] & mOperResult[nOperField, 8]) == mOperResult[nOperField, 6])
            {
                if (xPlayer)
                    mResult[nOperField] = GameStatus.WinO;
                else
                    mResult[nOperField] = GameStatus.WinX;
            }
            #endregion

            #region checking by columns

            //column 0
            if (mOperResult[nOperField, 0] != MarkType.Free && (mOperResult[nOperField, 0] & mOperResult[nOperField, 3] & mOperResult[nOperField, 6]) == mOperResult[nOperField, 0])
            {
                if (xPlayer)
                    mResult[nOperField] = GameStatus.WinO;
                else
                    mResult[nOperField] = GameStatus.WinX;
            }

            //column 1
            if (mOperResult[nOperField, 1] != MarkType.Free && (mOperResult[nOperField, 1] & mOperResult[nOperField, 4] & mOperResult[nOperField, 7]) == mOperResult[nOperField, 1])
            {
                if (xPlayer)
                    mResult[nOperField] = GameStatus.WinO;
                else
                    mResult[nOperField] = GameStatus.WinX;
            }

            //column 2
            if (mOperResult[nOperField, 2] != MarkType.Free && (mOperResult[nOperField, 2] & mOperResult[nOperField, 5] & mOperResult[nOperField, 8]) == mOperResult[nOperField, 2])
            {
                if (xPlayer)
                    mResult[nOperField] = GameStatus.WinO;
                else
                    mResult[nOperField] = GameStatus.WinX;
            }

            #endregion

            #region checking by diagonals

            if (mOperResult[nOperField, 0] != MarkType.Free && (mOperResult[nOperField, 0] & mOperResult[nOperField, 4] & mOperResult[nOperField, 8]) == mOperResult[nOperField, 0])
            {
                if (xPlayer)
                    mResult[nOperField] = GameStatus.WinO;
                else
                    mResult[nOperField] = GameStatus.WinX;
            }

            if (mOperResult[nOperField, 2] != MarkType.Free && (mOperResult[nOperField, 2] & mOperResult[nOperField, 4] & mOperResult[nOperField, 6]) == mOperResult[nOperField, 2])
            {
                if (xPlayer)
                    mResult[nOperField] = GameStatus.WinO;
                else
                    mResult[nOperField] = GameStatus.WinX;
            }

            #endregion

            #region No Winners
            bool draw = true;
            for (var i = 0; i < 9; i++)
                if (mOperResult[nOperField, i] == MarkType.Free)
                    draw = false;
            if (draw)
                mResult[nOperField] = GameStatus.Draw;
            #endregion
        }

        private bool CheckTactWinner()
        {
            #region Horizontal Win
            // row 0
            if (mResult[0] != GameStatus.Game && mResult[0] != GameStatus.Draw && (mResult[0] & mResult[1] & mResult[2]) == mResult[0])
            { 
                mGameEnded = true;
                goto Finish;
            }             

            //row 1
            if (mResult[3]!= GameStatus.Game && mResult[3] != GameStatus.Draw && (mResult[3] & mResult[4] & mResult[5]) == mResult[3])
            {
                mGameEnded = true;
                goto Finish;
            }
            //row 2
            if (mResult[6]!= GameStatus.Game && mResult[6] != GameStatus.Draw && (mResult[6] & mResult[7] & mResult[8]) == mResult[6])
            {
                mGameEnded = true;
                goto Finish;
            }
            #endregion

            #region Vertical Win
            // column 0
            if (mResult[0] != GameStatus.Game && mResult[0] != GameStatus.Draw && (mResult[0] & mResult[3] & mResult[6]) == mResult[0])
            {
                mGameEnded = true;
                goto Finish;
            }


            //column 1
            if (mResult[1] != GameStatus.Game && mResult[1] != GameStatus.Draw && (mResult[1] & mResult[4] & mResult[7]) == mResult[1])
            {
                mGameEnded = true;
                goto Finish;
            }
            //column 2
            if (mResult[2] != GameStatus.Game && mResult[2] != GameStatus.Draw && (mResult[2] & mResult[5] & mResult[8]) == mResult[2])
            {
                mGameEnded = true;
                goto Finish;
            }
            #endregion

            #region Diagonal Win

            if (mResult[0] != GameStatus.Game && mResult[0] != GameStatus.Draw && (mResult[0] & mResult[4] & mResult[8]) == mResult[0])
            {
                mGameEnded = true;
                goto Finish;
            }

            if (mResult[2] != GameStatus.Game && mResult[2] != GameStatus.Draw && (mResult[2] & mResult[4] & mResult[6]) == mResult[2])
            {
                mGameEnded = true;
                goto Finish;
            }

        #endregion

            #region No Winners
            if (!mResult.Any(result => result==GameStatus.Game))
            {
                mGameEnded = true;
                Msg.Text = "Конец игры! Ничья";
                Msg.Foreground = Brushes.Gray;
                Container.Children.Cast<Button>().ToList().ForEach(button =>
                {
                    button.Background = Brushes.Orange;
                });
                return true;
            }
        #endregion

        Finish:
            if (mGameEnded == true)
            {
                if (xPlayer)
                    MessageBox.Show("Конец игры! Победил X");                
                else
                    MessageBox.Show("Конец игры! Победил O");
                return true;
            }
            return false;
        }
        
        private void ClkOnNewG(object sender, RoutedEventArgs e)
        {
            byte[] buffer = new byte[1];
            buffer[0] = 1;
            try
            {
                sock.Send(buffer);
            }
            catch (Exception ex) { }
            Close();
            Connection conn = new Connection();
            conn.Show();          
        }

        private void ClkOnEndG(object sender, RoutedEventArgs e)
        {
            byte[] buffer = new byte[1];
            buffer[0] = 1;
            try
            {
                sock.Send(buffer);
            }
            catch (Exception ex)
            {
            }
            MessageReceiver.WorkerSupportsCancellation = true;
            MessageReceiver.CancelAsync();
            if (server != null)
                server.Stop();
            Close();
            
        }

        private void ReceiveMove()
        {
            byte[] buffer = new byte[9];
            try { 
                sock.Receive(buffer);
                if (buffer[0] == 1)
                {
                    MessageBox.Show("Оппонент вышел");
                    Dispatcher.Invoke(() => ClkOnNewG(null, null));
                    Close();
                }
                else
                {
                    string btn_name = Encoding.UTF8.GetString(buffer);
                   xPlayer = !xPlayer;
                    if (CheckButton(null, btn_name)) ;
                    else
                    {
                        xPlayer = !xPlayer;
                        ReceiveMove();
                    }
                    xPlayer = !xPlayer;
                }
            }
            catch (Exception ex)
            {               
            }
        }
        private void Help_listShow(object sender, RoutedEventArgs e)
        {
            h = new Help();
            h.Show();
        }
        private void Window_Closing(object sender, CancelEventArgs e)
        {
            MessageReceiver.WorkerSupportsCancellation = true;
            MessageReceiver.CancelAsync();
            if (server != null)
                server.Stop();
        }
    }
}
