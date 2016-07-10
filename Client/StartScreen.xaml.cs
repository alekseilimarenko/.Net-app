using System;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using BaldaServer;
using System.Windows.Input;

namespace Balda
{
    /// <summary>
    /// стартовый экран
    /// </summary>
    public partial class StartScreen : Window
    {
        public Thread _clearTextBox;
        private ChannelFactory<ServiceGame> gameFactory;
        bool exit = false;

        //конструктор стартового окна
        public StartScreen()
        {
            InitializeComponent();
        }

        //загрузка компонентов стартового окна
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //грид ошибок
            ErrorGrid.Visibility = Visibility.Hidden;

            //грид регистрации
            RegGrid.Visibility = Visibility.Hidden;

            gameFactory = new ChannelFactory<ServiceGame>("EndPoint");
            
            //грид стартового экрана
            StartGrid.Visibility = Visibility.Visible;
        }

        //изменение размеров кнопки при нажатии
        private void NewGame_MouseDown(object sender, MouseButtonEventArgs e)
        {
            NewGame.Margin = new Thickness(96, 213, 582, 172);
        }

        //кнопка выхода из игры
        private void ExitBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Close();
            }
            catch (Exception)
            {
                Close();
            }
        }

        //кнопка входа в игру
        private void NewGame_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                App.Proxy = gameFactory.CreateChannel();
                
                NewGame.Margin = new Thickness(96, 212, 582, 173);

                if (LoginBox.Text == "" || PassBox.Password == "")
                {
                    ShowMessage("Введите логин или пароль", 0);
                    _clearTextBox = new Thread(BeginClear) { IsBackground = true };
                    _clearTextBox.Start();
                    return;
                }

                int choice = App.Proxy.Login(LoginBox.Text, PassBox.Password);
                switch (choice)
                {
                    case 0:
                        ShowMessage("Указанный логин не найден, зарегистрироваться?", 1);
                        break;
                    case 1:
                        ShowMessage("Игрок с таким логином уже играет, введите другой логин или зарегистрируйтесь", 0);
                        _clearTextBox = new Thread(BeginClear) { IsBackground = true };
                        _clearTextBox.Start();
                        break;
                    case 2:
                        ShowMessage("Вы ввели неверный пароль", 0);
                        _clearTextBox = new Thread(BeginClear) { IsBackground = true };
                        _clearTextBox.Start();
                        break;
                    case 3:
                        App.login = LoginBox.Text;
                        MainWindow mw = new MainWindow();
                        App.myWindows.Add(mw);
                        Close();
                        mw.Show();
                        break;
                }
            }
            catch (CommunicationException)
            {
                ShowMessage("Нет связи с сервером", 0);
                _clearTextBox = new Thread(BeginClear) { IsBackground = true };
                _clearTextBox.Start();
            }
            catch (Exception ex)
            {
                
            }
        }

        //кнопка подтверждения выбора
        private void OkButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (exit)
            {
                ErrorGrid.Visibility = Visibility.Hidden;
                RegGrid.Visibility = Visibility.Hidden;
                StartGrid.Visibility = Visibility.Visible;
            }
            else
            {
                RegBtn_MouseUp(null, null);
            }
        }

        //кнопка отмены выбора
        private void NoButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            StateBox.Content = "";
            LoginBox.Text = "";
            PassBox.Clear();
            StateBox.Dispatcher.Invoke(new Action(() => StateBox.Content = ""));
            ErrorGrid.Visibility = Visibility.Hidden;
        }

        //кнопка регистрации
        private void RegBtn_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                App.Proxy = gameFactory.CreateChannel();

                if (App.Proxy.Connect())
                {
                    ErrorGrid.Visibility = Visibility.Hidden;
                    StartGrid.Visibility = Visibility.Hidden;
                    RegGrid.Visibility = Visibility.Visible;
                    for (int i = 0; i < LogoGrid.Children.Count; i++)
                    {
                        LogoGrid.Children[i].Opacity = 1;
                    }
                    LBox.Text = "";
                    PBox.Password = "";
                    NikBox.Text = "";
                    Userlogo = "";
                    LogoGrid.IsEnabled = true;
                }
            }
            catch(CommunicationException com)
            {
                ShowMessage("Нет связи с сервером", 0);
                _clearTextBox = new Thread(BeginClear) { IsBackground = true };
                _clearTextBox.Start();
            }
        }

        //вывод сообщений об ошибках
        public void ShowMessage(string mes, int state) 
        {
            ErrorGrid.Visibility = Visibility.Visible;
            StateBox.Content = mes;

            switch (state)
            {
                case 0:
                    OkButton.Visibility = Visibility.Hidden;
                    NoButton.Visibility = Visibility.Hidden;
                    break;
                case 1:
                    OkButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    break;
            }
        }

        //очистка информационного лэйбла
        public void BeginClear()
        {
            try
            {
                Thread.Sleep(2000);
                Dispatcher.Invoke(delegate
                {
                    ErrorGrid.Visibility = Visibility.Hidden;
                    StateBox.Content = "";
                });
            }
            catch (Exception) { }
        }
    }
}