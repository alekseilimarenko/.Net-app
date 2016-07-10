using System;
using System.Collections.Generic;
using System.IO;
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

namespace Balda
{
    public partial class MainWindow : Window
    {
        //игра
        private Game _gm;
        private ClientList _creator, _chosen;
        private int _index, _num, _prevIndex, _nextIndex, countSkip;
        string _symbol = "";
        string _word = "";
        List<int> wordindex = new List<int>();
        private bool _exit, _finish, _win, _ni4ya, skipMove;

        //подготовка и загрузка словаря в листбокс
        private void GetDictionary()
        {
            string[] myDict = null;
            try
            {
                if (File.Exists("dictionary.txt"))
                {
                    myDict = File.ReadAllLines("dictionary.txt");
                }
                if (myDict != null)
                {
                    for (int i = 0; i < myDict.Count(); i++)
                    {
                        Dispatcher.Invoke(() => HintListBox.Items.Add(myDict[i]));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //получение с сервера информации об игре
        public void GetGameInfo()
        {
            bool firstMove = false, secondMove = false;
            try
            {
                while (true)
                {
                    _gm = App.Proxy.GetGame(App.login);

                    switch (_gm.GameState)
                    {
                        //игрок оключился от игры
                        case -1:
                             Dispatcher.Invoke(delegate
                            {
                                WarningLabel.Content = "Противник отключился вернуться на окно выбора игры?";
                                GameFildGrid.IsEnabled = false;
                                SymbolGrid.IsEnabled = false;
                                Conferm.Visibility = Visibility.Visible;
                                _finish = true;

                            });
                            return;

                        //ходит создатель игры
                        case 2:
                            if (firstMove) break;
                            if (_gm.CurGamer == App.login)
                            {
                                Dispatcher.Invoke(delegate
                                {
                                    StateLabel.Content = "Ваш ход";
                                    GameFildGrid.IsEnabled = true;
                                    SymbolGrid.IsEnabled = true;
                                    Gamer1Grid.IsEnabled = true;
                                });
                            }
                            else
                            {
                                Dispatcher.Invoke(delegate
                                {
                                    StateLabel.Content = "Ход противника";
                                    GameFildGrid.IsEnabled = false;
                                    SymbolGrid.IsEnabled = false;
                                    Gamer1Grid.IsEnabled = false;
                                });
                            }

                            firstMove = true;
                            secondMove = false;

                            Dispatcher.Invoke(delegate
                            {
                                for (int i = 0; i < GameFildGrid.Children.Count; i++)
                                {
                                    if (GameFildGrid.Children[i] as baldaGrid != null)
                                    {
                                        (GameFildGrid.Children[i] as baldaGrid).bykva = _gm.GameField[i];
                                    }
                                }
                            });

                            if (_gm.CurGamer == App.login)
                            {
                                FillGamerListView(true);
                            }
                            else
                            {
                                FillGamerListView(false);
                            }
                            break;

                        //ходит подключившийся к игре
                        case 3:
                            if (secondMove) break;

                            if (_gm.CurGamer == App.login)
                            {
                                Dispatcher.Invoke(delegate
                                {
                                    StateLabel.Content = "Ваш ход";
                                    GameFildGrid.IsEnabled = true;
                                    SymbolGrid.IsEnabled = true;
                                    Gamer1Grid.IsEnabled = true;
                                });
                            }
                            else
                            {
                                Dispatcher.Invoke(delegate
                                {
                                    StateLabel.Content = "Ход противника";
                                    GameFildGrid.IsEnabled = false;
                                    SymbolGrid.IsEnabled = false;
                                    Gamer1Grid.IsEnabled = false;
                                });
                            }
                            firstMove = false;
                            secondMove = true;

                            Dispatcher.Invoke(delegate
                            {
                                for (int i = 0; i < GameFildGrid.Children.Count; i++)
                                {
                                    if (GameFildGrid.Children[i] as baldaGrid != null)
                                    {
                                        (GameFildGrid.Children[i] as baldaGrid).bykva = _gm.GameField[i];
                                    }
                                }
                            });

                            if (_gm.CurGamer == App.login)
                            {
                                FillGamerListView(false);
                            }
                            else
                            {
                                FillGamerListView(true);
                            }
                            break;
                        
                        //выигрыш
                        case 4:
                        case 5:
                                if (_gm.CurGamer == App.login)
                                {
                                    Dispatcher.Invoke(delegate
                                    {
                                        WarningLabel.Content = "Поздравляем с победой, хотите сыграть еще раз?";
                                        GameFildGrid.IsEnabled = false;
                                        SymbolGrid.IsEnabled = false;
                                        Gamer1Grid.IsEnabled = false;
                                        Conferm.Visibility = Visibility.Visible;
                                        Abort.Visibility = Visibility.Visible;
                                        _win = true;

                                    });
                                    return;
                                }
                                else
                                {
                                    Dispatcher.Invoke(delegate
                                    {
                                        WarningLabel.Content = "К сожалению Вы проиграли, хотите сыграть еще раз?";
                                        GameFildGrid.IsEnabled = false;
                                        SymbolGrid.IsEnabled = false;
                                        Gamer1Grid.IsEnabled = false;
                                        Conferm.Visibility = Visibility.Visible;
                                        Abort.Visibility = Visibility.Visible;
                                        _win = true;
                                    });
                                    return;
                                }
                        
                        //ничья
                        case 6:
                            Dispatcher.Invoke(delegate
                            {
                                WarningLabel.Content = "Ничья, хотите сыграть еще раз?";
                                GameFildGrid.IsEnabled = false;
                                SymbolGrid.IsEnabled = false;
                                Gamer1Grid.IsEnabled = false;
                                Conferm.Visibility = Visibility.Visible;
                                Abort.Visibility = Visibility.Visible;
                                _ni4ya = true;
                            });
                            return;
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (CommunicationException)
            {
                //StartScreen sc = new StartScreen();
                //Close();
                //sc.Show();
            }
            catch (Exception)
            {

            }
        }

        //заполнение листвью составленными словами
        private void FillGamerListView(bool choise)
        {
            if (choise)
            {
                Dispatcher.Invoke(() => Gamer1View.Items.Clear());
                foreach (string t in _gm.WordsGamer1.Where(t => t != null))
                {
                    Dispatcher.Invoke(() => Gamer1View.Items.Add(new ScoreView
                    {
                        Word = t,
                        Score = t.Count()
                    }));
                }

                Dispatcher.Invoke(() => Gamer2View.Items.Clear());
                foreach (string t in _gm.WordsGamer2.Where(t => t != null))
                {
                    Dispatcher.Invoke(() => Gamer2View.Items.Add(new ScoreView
                    {
                        Word = t,
                        Score = t.Count()
                    }));
                }
            }

            if (!choise)
            {
                Dispatcher.Invoke(() => Gamer1View.Items.Clear());
                foreach (string t in _gm.WordsGamer2.Where(t => t != null))
                {
                    Dispatcher.Invoke(() => Gamer1View.Items.Add(new ScoreView
                    {
                        Word = t,
                        Score = t.Count()
                    }));
                }

                Dispatcher.Invoke(() => Gamer2View.Items.Clear());
                foreach (string t in _gm.WordsGamer1.Where(t => t != null))
                {
                    Dispatcher.Invoke(() => Gamer2View.Items.Add(new ScoreView
                    {
                        Word = t,
                        Score = t.Count()
                    }));
                }
            }
        }

        //обработчик нажатия левой кнопки мыши
        private void baldaGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WarningLabel.Content = "";
            try
            {
                baldaGrid temp = sender as baldaGrid;
                int tag = (Int32.Parse(temp.Tag.ToString()));

                if (tag < 32)
                {
                    _num = tag;
                    temp.Margin = new Thickness(-2, -2, -2, -2);
                    _symbol = temp.bykva;
                    SymbolGrid.IsEnabled = false;
                }

                if (tag >= 50)
                {
                    _index = tag - 50;
                    
                    if ((GameFildGrid.Children[_index] as baldaGrid).bykva == "")
                    {
                        (GameFildGrid.Children[_index] as baldaGrid).bykva = _symbol;
                        ((baldaGrid)SymbolGrid.Children[_num]).Margin = new Thickness(0);
                        //_symbol = "";
                        _num = 0;
                    }
                    else
                    {
                        Dispatcher.Invoke(() => WarningLabel.Content = "Данная ячейка занята");
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        //обработчик нажатия правой кнопки мыши
        private void baldaGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            WarningLabel.Content = "";
            try
            {
                baldaGrid temp = sender as baldaGrid;
                int tag = (Int32.Parse(temp.Tag.ToString()));

                if (tag < 50)
                {
                    WarningLabel.Content = "При составлении слова использутся только ячейки игрового поля";
                    return;
                }

                if (((baldaGrid) GameFildGrid.Children[tag - 50]).bykva == "")
                {
                    WarningLabel.Content = "При составлении слова нельзя использовать пустые ячейки";
                    return;
                }

                if (wordindex.Contains(tag - 50))
                {
                    WarningLabel.Content = "Вы уже использовали данную букву";
                    return;
                }

                if (!wordindex.Any())
                {
                    _prevIndex = tag - 50;
                    _word = _word + ((baldaGrid)GameFildGrid.Children[_prevIndex] ).bykva;
                    MBox.Content = _word;
                    wordindex.Add(_prevIndex);
                    ((baldaGrid)GameFildGrid.Children[_prevIndex]).MyBrushSource = Brushes.GreenYellow;
                }
                else
                {
                    _nextIndex = tag - 50;
                    if (_nextIndex == _prevIndex - 1 || _nextIndex == _prevIndex + 1 || _nextIndex == _prevIndex - 5 || _nextIndex == _prevIndex + 5)
                    {
                        _word = _word + ((baldaGrid)GameFildGrid.Children[_nextIndex]).bykva;
                        MBox.Content = _word;
                        wordindex.Add(_nextIndex);
                        ((baldaGrid)GameFildGrid.Children[_nextIndex]).MyBrushSource = Brushes.GreenYellow;
                        _prevIndex = _nextIndex;
                    }
                    else
                    {
                        WarningLabel.Content = "Нельзя использовать буквы расположенные по диагонали или пропускать буквы";
                    }
                }
            }
            catch (Exception) { }
        }

        //обработчик нажатия кнопки хода игрока
        private void GamerMove_Click(object sender, RoutedEventArgs e)
        {
            if (_word == "")
            {
                WarningLabel.Content = "Вы не составили слово";
                return;
            }

            if (!wordindex.Contains(_index))
            {
                WarningLabel.Content = "Вы не использовали новую букву";
            }
            else
            {
                try
                {
                    int mes = App.Proxy.SendWord(App.login, _index, _word, _symbol);
                    HintListBox.Visibility = Visibility.Hidden;
                    switch (mes)
                    {
                        case 0:
                            WarningLabel.Content = "В словаре нет такого слова, добавить в словарь?";
                            Conferm.Visibility = Visibility.Visible;
                            Abort.Visibility = Visibility.Visible;
                            break;
                        case 1:
                            for (int i = 0; i < GameFildGrid.Children.Count; i++)
                            {
                                baldaGrid grid = GameFildGrid.Children[i] as baldaGrid;
                                if (grid != null)
                                {
                                    grid.MyBrushSource = new SolidColorBrush(Color.FromRgb(153, 180, 209));
                                }
                            }
                            ClearValue();
                            break;
                        case 2:
                            WarningLabel.Content = "Введенное слово уже использовано в игре";
                            break;
                        case 3:
                            break;
                    }
                }
                catch (CommunicationException)
                {
                    StartScreen sc = new StartScreen();
                    Close();
                    sc.Show();
                }
                catch (Exception)
                {

                }
            }
        }

        //подтверждение выбора игрока
        private void Conferm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //игрок пропускает ход
                if (skipMove)
                {
                    if (countSkip == 0)
                    {
                        countSkip++;
                        App.Proxy.SendWord(App.login, 0, "", "");
                        skipMove = false;
                        return;
                    }
                    else
                    {
                        App.Proxy.FinishGame(App.login);
                        return;
                    }
                }

                //ничья
                if (_ni4ya)
                {

                }

                //победа
                if (_win)
                {
                    //переход на окно выбора игры
                }

                //выход из партии
                if (_finish)
                {
                    App.Proxy.FinishGame(App.login);
                    Owner.Show();
                    Close();
                    return;
                }

                //выход из игры
                if (_exit)
                {
                    App.Proxy.GameExit(App.login);
                    Application.Current.Shutdown();
                    return;
                }

                //отправка слова на сервер
                App.Proxy.SendWord(App.login, _index, _word, _symbol, 1);
                for (int i = 0; i < GameFildGrid.Children.Count; i++)
                {
                    baldaGrid grid = GameFildGrid.Children[i] as baldaGrid;
                    if (grid != null)
                    {
                        grid.MyBrushSource = new SolidColorBrush(Color.FromRgb(153, 180, 209));
                    }
                }
                ClearValue();
                Conferm.Visibility = Visibility.Hidden;
                Abort.Visibility = Visibility.Hidden;
            }
            catch (CommunicationException com) { }
            catch (Exception ex) { }
        }

        //очистка выбранных значений
        private void ClearValue()
        {
            _symbol = "";
            _index = 0;
            MBox.Content = "";
            _word = "";
            wordindex.Clear();
            WarningLabel.Content = "";
        }

        //отмена выбора игрока
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            //пропуск хода
            if (skipMove)
            {
                skipMove = false;
                WarningLabel.Content = "";
                return;
            }

            //победа
            if (_win)
            {
                //переход на стартовое окно
            }

            //смена цвета на базовый
            for (int i = 0; i < wordindex.Count; i++)
            {
                baldaGrid grid = GameFildGrid.Children[wordindex[i]] as baldaGrid;
                if (grid != null)
                {
                    grid.MyBrushSource = new SolidColorBrush(Color.FromRgb(153, 180, 209));
                }
            }

            SymbolGrid.IsEnabled = true;
            ((baldaGrid)SymbolGrid.Children[_num]).Margin = new Thickness(0);
            ((baldaGrid)GameFildGrid.Children[_index]).bykva = "";
            ClearValue();
        }

        //обработчик нажатия кнопки завершения партии
        private void FinishGame_Click(object sender, RoutedEventArgs e)
        {
            if (_gm.GameField.Contains(""))
            {
                WarningLabel.Content = "Игра не окончена, вы действительно хотите выйти";
                Conferm.Visibility = Visibility.Visible;
                Abort.Visibility = Visibility.Visible;
                _finish = true;
            }
        }

        //обработчик нажатия кнопки выхода из игры
        private void ExitGame_Click(object sender, RoutedEventArgs e)
        {
            if (_gm.GameField.Contains(""))
            {
                WarningLabel.Content = "Игра не окончена, вы действительно хотите выйти";
                Conferm.Visibility = Visibility.Visible;
                Abort.Visibility = Visibility.Visible;
                _exit = true;
            }
        }

        //отмена выбранных данных
        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            //выход из партии и игры
            if (_exit || _finish)
            {
                Conferm.Visibility = Visibility.Hidden;
                Abort.Visibility = Visibility.Hidden;
                WarningLabel.Content = "";
                _exit = false;
                _finish = false;
            }
            else
            {
                Cancel_Click(null, null);
                Conferm.Visibility = Visibility.Hidden;
                Abort.Visibility = Visibility.Hidden;
                WarningLabel.Content = "";
            }
        }

        //обработчик кнопки подсказка
        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            HintListBox.Visibility = Visibility.Visible;
        }

        //кнопка пропуска хода
        private void SkipMoveButton_Click(object sender, RoutedEventArgs e)
        {
            skipMove = true;
            WarningLabel.Content = "Вы пропускаете ход?";
            Conferm.Visibility = Visibility.Hidden;
            Abort.Visibility = Visibility.Hidden;
        }
    }
    
    //класс для отрисовки данных игрока
    public class ScoreView
    {
        public string Word { get; set; }
        public int Score { get; set; }
    }
}
