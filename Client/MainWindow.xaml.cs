using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Threading;
using System.Windows.Media;
using Binding = System.Windows.Data.Binding;
using BaldaServer;
using System.IO;

namespace Balda
{
    /// <summary>
    /// грид выбора игры
    /// </summary>
    public partial class MainWindow : Window
    {
        private Game gm;
        public ClientList Gamer, Client;
        private Thread newGameThread, update, onLineThread;
        private List<ClientList> Lists = new List<ClientList>();
        private bool inGame = false;

        //иннициализация элементов окна
        public MainWindow()
        {
            InitializeComponent();
        }

        //загрузка окна
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                NewGame.Visibility = Visibility.Visible;
                CancelNewGame.Visibility = Visibility.Hidden;
                
                GetGamerInfo();
                GamerLogo.Source = new BitmapImage(new Uri(Gamer.UserLogo));

                EntryInfo.Content = "Добро пожаловать!";

                GridView gridView = new GridView();
                MyView.View = gridView;
                gridView.Columns.Add(new GridViewColumn
                {
                    Header = "Ник",
                    DisplayMemberBinding = new Binding("Nik"),
                    Width = 200
                });
                gridView.Columns.Add(new GridViewColumn
                {
                    Header = "Всего игр",
                    DisplayMemberBinding = new Binding("TotalGame"),
                    Width = 100
                });
                gridView.Columns.Add(new GridViewColumn
                {
                    Header = "Выиграно игр",
                    DisplayMemberBinding = new Binding("WinGame"),
                    Width = 100
                });

                update = new Thread(UpdateListGamers) {IsBackground = true};
                update.Start();

                onLineThread = new Thread(OnLineStatus) {IsBackground = true};
                onLineThread.Start();

                ChooseGameGrid.Visibility = Visibility.Visible;
                GameFieldGrid.Visibility = Visibility.Hidden;
                Spinner.Visibility = Visibility.Hidden;
            }
            catch (CommunicationException)
            {
                LostConnection();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => EntryInfo.Content = ex.Message);
            }
        }

        //получение данных об игроке вошедшем в игру
        private void GetGamerInfo()
        {
            Gamer = App.Proxy.GetGamerInfo(App.login);
            GamerNik.Content = Gamer.UserNik;
            GCountLbl.Content = Gamer.GameCount;
            WinCountLbl.Content = Gamer.WinCount;
        }

        //проверка соединения с сервером
        private void OnLineStatus()
        {
            try
            {
                while (true)
                {
                    bool tryConnect = App.Proxy.IamOnLine(App.login);

                    Thread.Sleep(1000);
                }
            }
            catch (CommunicationException)
            {
                LostConnection();
            }
            catch (Exception) { }
        }

        //обновление списка игроков создавших игру
        private void UpdateListGamers()
        {
            try
            {
                while (true)
                {
                    Dispatcher.Invoke(() => MyView.Items.Clear());
                    if (!inGame)
                    {
                        Lists = App.Proxy.GetGamers(App.login);

                        if (Lists.Count == 0)
                        {
                            Dispatcher.Invoke(() => MyView.Items.Add(new MyItem
                            {
                                Nik = "Свободных игр нет"
                            }));
                        }
                        else
                        {
                            foreach (ClientList gamer in Lists.Where(gamer => gamer.UserLogin != Gamer.UserLogin))
                            {
                                Dispatcher.Invoke(() => MyView.Items.Add(new MyItem
                                {
                                    Nik = gamer.UserNik,
                                    TotalGame = gamer.GameCount,
                                    WinGame = gamer.WinCount
                                }));
                            }
                        } 
                    }
                    Thread.Sleep(10000);
                }
            }
            catch (CommunicationException)
            {
                LostConnection();
            }
            catch (Exception) { }
        }

        //обработчик нажатия на кнопку создания новой игры
        private void NewGame_MouseUp(object sender, MouseButtonEventArgs e)
        {
           NewGameConnect(true);
        }

        //обработчик нажатия на кнопку отмены создания новой игры
        private void CancelNewGame_MouseUp(object sender, MouseButtonEventArgs e)
        {
           NewGameConnect(false);
        }

        //создание или удаление новой игры
        private void NewGameConnect(bool state)
        {
            if (state)
            {
                NewGame.Visibility = Visibility.Hidden;
                CancelNewGame.Visibility = Visibility.Visible;
                EntryGame.IsEnabled = false;
                MyView.IsEnabled = false;
                newGameThread = new Thread(NewGameInfo) { IsBackground = true };
                newGameThread.Start();
            }
            else
            {
                NewGame.Visibility = Visibility.Visible;
                CancelNewGame.Visibility = Visibility.Hidden;
                EntryGame.IsEnabled = true;
                MyView.IsEnabled = true;
                newGameThread.Abort();
                Thread cancelThread = new Thread(CancelGame) { IsBackground = true };
                cancelThread.Start();
            }
        }

        //поток создания новой игры
        private void NewGameInfo()
        {
            App.IsNewGame = true;
            try
            {
                App.Proxy.CreateNewGame(Gamer.UserLogin);
                while (true)
                {
                    gm = App.Proxy.GetGame(Gamer.UserLogin);
                    switch (gm.GameState)
                    {
                        case 1:
                            Dispatcher.Invoke(delegate
                            {
                                Spinner.Visibility = Visibility.Visible;
                                EntryInfo.Content = "Ожидайте второго соперника";
                            });
                            break;
                        case 2:
                            goto GStart;
                    }
                    Thread.Sleep(1000);
                }
            GStart:
                StartGame();
            }
            catch (CommunicationException)
            {
                LostConnection();
            }
            catch (Exception)
            {
                if (gm == null)
                {
                    LostConnection();
                }
            }
        }

        //поток отмены создания новой игры
        private void CancelGame()
        {
            try
            {
                if (App.Proxy.CancelNewGame(App.login))
                {
                    Dispatcher.Invoke(delegate
                    {
                        Spinner.Visibility = Visibility.Hidden;
                        EntryInfo.Content = "Вы удалили созданную игру"; 
                    });
                }
            }
            catch (CommunicationException)
            {
                LostConnection();
            }
            catch (Exception) { }
        }

        //подключение к созданной игре
        private void EntryGame_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Lists.Count == 0)
            {
                Dispatcher.Invoke(() => EntryInfo.Content = "Свободных игр нет");
                NewGame.IsEnabled = true;
                return;
            }
            if (Client == null || Client.UserNik == "")
            {
                Dispatcher.Invoke(() => EntryInfo.Content = "Вы не выбрали соперника");
                return;
            }
            Thread connectToGameThread = new Thread(ConnectToGameInfo) {IsBackground = true};
            connectToGameThread.Start();
        }

        //поток подключения к созданной игре
        private void ConnectToGameInfo()
        {
            try
            {
                App.Proxy.ConnectToGame(Client.UserLogin, Gamer.UserLogin);
                gm = App.Proxy.GetGame(Client.UserLogin);

                if (gm != null && gm.GameState == 2)
                {
                    StartGame();
                }
            }
            catch (CommunicationException)
            {
                LostConnection();
            }
            catch (Exception) {}
        }

        //обработчик клика по листвью выбора игрока
        private void listView_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                MyItem item = (MyItem) MyView.SelectedItem;
                Client = Lists.Find(x => x.UserNik == item.Nik);
            }
            catch (Exception) {}
        }

        //начало игры
        private void StartGame()
        {
            Dispatcher.Invoke(delegate
            {
                ChooseGameGrid.Visibility = Visibility.Hidden;
                Spinner.Visibility = Visibility.Hidden;
                GameFieldGrid.Visibility = Visibility.Visible;
                EntryInfo.Content = "";
                inGame = true;
                NewGame.Visibility = Visibility.Visible;
                NewGame.IsEnabled = true;
                CancelNewGame.Visibility = Visibility.Hidden;
                EntryGame.IsEnabled = true;
                MyView.Items.Clear();
                MyView.IsEnabled = true;

                if (gm.ListGamer[0].UserLogin == App.login)
                {
                    Gamer1Nik.Content = gm.ListGamer[0].UserNik;
                    Gamer1Logo.Source = new BitmapImage(new Uri(gm.ListGamer[0].UserLogo));
                    Gamer2Nik.Content = gm.ListGamer[1].UserNik;
                    Gamer2Logo.Source = new BitmapImage(new Uri(gm.ListGamer[1].UserLogo));
                }
                else
                {
                    Gamer1Nik.Content = gm.ListGamer[1].UserNik;
                    Gamer1Logo.Source = new BitmapImage(new Uri(gm.ListGamer[1].UserLogo));
                    Gamer2Nik.Content = gm.ListGamer[0].UserNik;
                    Gamer2Logo.Source = new BitmapImage(new Uri(gm.ListGamer[0].UserLogo));
                }

                Conferm.Visibility = Visibility.Hidden;
                Abort.Visibility = Visibility.Hidden;
                HintListBox.Visibility = Visibility.Hidden;
                HintBox.IsEnabled = false;

                for (int i = 0; i < GameField.Children.Count; i++)
                {
                    if (GameField.Children[i] as baldaGrid != null)
                    {
                        (GameField.Children[i] as baldaGrid).bykva = gm.GameField[i];
                    }
                }

                GridView grid1View = new GridView();
                Gamer1View.View = grid1View;
                grid1View.Columns.Add(new GridViewColumn
                {
                    Header = "Слово",
                    DisplayMemberBinding = new Binding("Word"),
                    Width = 130
                });
                grid1View.Columns.Add(new GridViewColumn
                {
                    Header = "Счет",
                    DisplayMemberBinding = new Binding("Score"),
                    Width = 45
                });

                GridView grid2View = new GridView();
                Gamer2View.View = grid2View;
                grid2View.Columns.Add(new GridViewColumn
                {
                    Header = "Слово",
                    DisplayMemberBinding = new Binding("Word"),
                    Width = 130
                });
                grid2View.Columns.Add(new GridViewColumn
                {
                    Header = "Счет",
                    DisplayMemberBinding = new Binding("Score"),
                    Width = 45
                });
            });
            
            App.Proxy.SaveRecord(App.login, "enter");
            
            Thread takeLetterThread = new Thread(GetGameInfo) { IsBackground = true };
            takeLetterThread.Start();

            Thread threadDict = new Thread(GetDictionary) { IsBackground = true };
            threadDict.Start();
        }

        //обработчик клика по кнопке выхода из игры
        private void Exit_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Thread exitThread = new Thread(GameClosing) {IsBackground = true};
                exitThread.Start();
                Close();
            }
            catch (Exception)
            {
            }
        }

        //поток выхода из игры
        private void GameClosing()
        {
            try
            {
                App.Proxy.GameExit(App.login);
            }
            catch (CommunicationException)
            {
                LostConnection();
            }
            catch (Exception)
            {
            }
        }

        //выход из игры при обрыве соединения с сервером
        private void LostConnection()
        {
            Dispatcher.Invoke(delegate
            {
                 if (App.stScreen == null || (App.stScreen != null && !App.stScreen.IsActive))
                {
                    App.stScreen = new StartScreen();
                    App.stScreen.ErrorGrid.Visibility = Visibility.Visible;
                    App.stScreen.ShowMessage("Связь с сервером прервана",0);
                    App.stScreen.StateBox.Foreground = Brushes.Red;
                    App.stScreen._clearTextBox = new Thread(App.stScreen.BeginClear) {IsBackground = true};
                    App.stScreen._clearTextBox.Start();
                    Close();
                    App.stScreen.Show();
                 }
            });
        }
    }
    
    //класс для отрисовки данных в листвью 
    public class MyItem
    {
        public string Nik { get; set; }
        public int TotalGame { get; set; }
        public int WinGame { get; set; }
    }
}