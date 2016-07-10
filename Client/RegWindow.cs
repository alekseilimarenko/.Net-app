using System;
using System.ServiceModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Balda
{
    /// <summary>
    /// экран регистрации
    /// </summary>
    public partial class StartScreen : Window
    {
        private string Userlogo { get; set; }
        private Image img;

        //кнопка подтверждения регистрации
        private void ConfermButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            try
            {
                App.Proxy = gameFactory.CreateChannel();

                if (LBox.Text == "" || PassBox.Password == "" || NikBox.Text == "")
                {
                    ShowMessage("Вы заполнили не все поля", 0);
                    _clearTextBox = new Thread(BeginClear) { IsBackground = true };
                    _clearTextBox.Start();
                    return;
                }

                if (Userlogo == "")
                {
                    ShowMessage("Вы не выбрали аватар", 0);
                    _clearTextBox = new Thread(BeginClear) { IsBackground = true };
                    _clearTextBox.Start();
                    return;
                }

                if (App.Proxy.RegUser(LBox.Text, PassBox.Password, NikBox.Text, Userlogo))
                {
                    MainWindow mw = new MainWindow();
                    App.login = LBox.Text;
                    Close();
                    mw.Show();
                }
            }
            catch (CommunicationException)
            {
                Dispatcher.Invoke(delegate
                {
                    StartGrid.Visibility = Visibility.Visible;
                    RegGrid.Visibility = Visibility.Hidden;
                    ShowMessage("Связь с сервером отсутствует", 0);
                    _clearTextBox = new Thread(BeginClear) { IsBackground = true };
                    _clearTextBox.Start();
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(delegate
                {
                    StateLabel.Foreground = Brushes.Red;
                    StateLabel.Content = ex.Message;
                });
            }
        }

        //кнопка отмены выбора
        private void DenyButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (LBox.Text == "" || PassBox.Password == "" || NikBox.Text == "" || Userlogo == "")
            {
                ShowMessage("Регистрация не завершена, вернуться на стартовый экран?", 1);
                exit = true;
            }
        }

        //обработчик события клика по имэджу
        private void Image_MouseUp(object sender, MouseButtonEventArgs e)
        {
            img = sender as Image;
            if (img == null)
            {
                return;
            }
            img.Opacity = 0.5;
            Userlogo = img.Source.ToString();
            LogoGrid.IsEnabled = false;
        }

        //обработчик события переключения на ввод пароля
        private void PassBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (LBox.Text != "")
            {
                Thread trylogin = new Thread(TestLogin) { IsBackground = true };
                trylogin.Start();
            }
        }

        //проверка логина в базе данных
        private void TestLogin()
        {
            string temp = null;
            App.Proxy = gameFactory.CreateChannel();
            Dispatcher.Invoke(delegate { temp = LBox.Text; });

            try
            {
                int choice = App.Proxy.Login(temp, null);
                switch (choice)
                {
                    case 0:
                        Dispatcher.Invoke(delegate
                        {
                            StateLabel.Foreground = Brushes.Green;
                            ConfermButton.IsEnabled = true;
                            StateLabel.Content = "Логин не занят";
                        });
                        break;
                    case 1:
                    case 2:
                        Dispatcher.Invoke(delegate
                        {
                            StateLabel.Foreground = Brushes.Red;
                            StateLabel.Content = "Логин занят";
                            ConfermButton.IsEnabled = false;
                        });
                        break;
                }
            }
            catch (CommunicationException)
            {
                Dispatcher.Invoke(delegate
                {
                    StartGrid.Visibility = Visibility.Visible;
                    RegGrid.Visibility = Visibility.Hidden;
                    ShowMessage("Связь с сервером отсутствует", 0);
                    _clearTextBox = new Thread(BeginClear) { IsBackground = true };
                    _clearTextBox.Start();
                });
            }
            catch (Exception)
            {

            }
        }
    }
}