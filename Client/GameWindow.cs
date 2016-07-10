using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Windows;
using System.Windows.Input;
using System.Threading;
using System.Windows.Media;
using BaldaServer;
using System.Windows.Controls;

namespace Balda
{
    /// <summary>
    /// грид игрового поля
    /// </summary>
    public partial class MainWindow : Window
    {
        private ClientList _creator, _chosen;
        private int _index = -1, _num = -1, _prevIndex, _nextIndex, countSkip;
        string _symbol = "";
        string _word = "";
        List<int> wordindex = new List<int>();
        private bool _exit, _finish, _win, _ni4ya, skipMove, _disconnect, AddSymbol = true, AddWord = false;
        private string[] WordArray;
        private string lt;
        Thread search;
        
        //подготовка и загрузка словаря в листбокс
        private void GetDictionary()
        {
            try
            {
                if (File.Exists("dictionary.txt"))
                {
                    WordArray = File.ReadAllLines("dictionary.txt");
                }
                if (WordArray != null)
                {
                    for (int i = 0; i < WordArray.Count(); i++)
                    {
                        Dispatcher.Invoke(() => HintListBox.Items.Add(WordArray[i]));
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
                    gm = App.Proxy.GetGame(App.login);

                    switch (gm.GameState)
                    {
                        //игрок оключился от игры
                        case -1:
                             Dispatcher.Invoke(delegate
                            {
                                WarningLabel.Content = "Противник отключился вернуться на окно выбора игры?";
                                GameField.IsEnabled = false;
                                SymbolGrid.IsEnabled = false;
                                Conferm.Visibility = Visibility.Visible;
                                _disconnect = true;
                            });
                            App.Proxy.SaveRecord(App.login, "exit");
                            return;

                        //ходит создатель игры
                        case 2:
                            if (firstMove) break;
                            if (gm.CurGamer == App.login)
                            {
                                Dispatcher.Invoke(delegate
                                {
                                    StateLabel.Content = "Ваш ход";
                                    GameField.IsEnabled = false;
                                    SymbolGrid.IsEnabled = true;
                                    Gamer1Grid.IsEnabled = true;
                                });
                            }
                            else
                            {
                                Dispatcher.Invoke(delegate
                                {
                                    StateLabel.Content = "Ход противника";
                                    GameField.IsEnabled = false;
                                    SymbolGrid.IsEnabled = false;
                                    Gamer1Grid.IsEnabled = false;
                                });
                            }

                            firstMove = true;
                            secondMove = false;

                            Dispatcher.Invoke(delegate
                            {
                                for (int i = 0; i < GameField.Children.Count; i++)
                                {
                                    if (GameField.Children[i] as baldaGrid != null)
                                    {
                                        (GameField.Children[i] as baldaGrid).bykva = gm.GameField[i];
                                    }
                                }
                            });

                            if (gm.CurGamer == App.login)
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

                            if (gm.CurGamer == App.login)
                            {
                                Dispatcher.Invoke(delegate
                                {
                                    StateLabel.Content = "Ваш ход";
                                    GameField.IsEnabled = false;
                                    SymbolGrid.IsEnabled = true;
                                    Gamer1Grid.IsEnabled = true;
                                });
                            }
                            else
                            {
                                Dispatcher.Invoke(delegate
                                {
                                    StateLabel.Content = "Ход противника";
                                    GameField.IsEnabled = false;
                                    SymbolGrid.IsEnabled = false;
                                    Gamer1Grid.IsEnabled = false;
                                });
                            }
                            firstMove = false;
                            secondMove = true;

                            Dispatcher.Invoke(delegate
                            {
                                for (int i = 0; i < GameField.Children.Count; i++)
                                {
                                    if (GameField.Children[i] as baldaGrid != null)
                                    {
                                        (GameField.Children[i] as baldaGrid).bykva = gm.GameField[i];
                                    }
                                }
                            });

                            if (gm.CurGamer == App.login)
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
                                if (gm.CurGamer == App.login)
                                {
                                    Dispatcher.Invoke(delegate
                                    {
                                        WarningLabel.Content = "Поздравляем с победой, хотите сыграть еще раз?";
                                        GameField.IsEnabled = false;
                                        SymbolGrid.IsEnabled = false;
                                        Gamer1Grid.IsEnabled = false;
                                        Conferm.Visibility = Visibility.Visible;
                                        Abort.Visibility = Visibility.Visible;
                                        _win = true;

                                    });
                                    App.Proxy.SaveRecord(App.login, "win");
                                    return;
                                }
                                else
                                {
                                    Dispatcher.Invoke(delegate
                                    {
                                        WarningLabel.Content = "К сожалению Вы проиграли, хотите сыграть еще раз?";
                                        GameField.IsEnabled = false;
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
                                GameField.IsEnabled = false;
                                SymbolGrid.IsEnabled = false;
                                Gamer1Grid.IsEnabled = false;
                                Conferm.Visibility = Visibility.Visible;
                                _ni4ya = true;
                            });
                            return;
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (CommunicationException)
            {
                LostConnection();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => WarningLabel.Content = ex.Message);
            }
        }

        //заполнение листвью составленными словами
        private void FillGamerListView(bool choise)
        {
            if (choise)
            {
                Dispatcher.Invoke(() => Gamer1View.Items.Clear());
                foreach (string t in gm.WordsGamer1.Where(t => t != null))
                {
                    Dispatcher.Invoke(() => Gamer1View.Items.Add(new ScoreView
                    {
                        Word = t,
                        Score = t.Count()
                    }));
                }

                Dispatcher.Invoke(() => Gamer2View.Items.Clear());
                foreach (string t in gm.WordsGamer2.Where(t => t != null))
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
                foreach (string t in gm.WordsGamer2.Where(t => t != null))
                {
                    Dispatcher.Invoke(() => Gamer1View.Items.Add(new ScoreView
                    {
                        Word = t,
                        Score = t.Count()
                    }));
                }

                Dispatcher.Invoke(() => Gamer2View.Items.Clear());
                foreach (string t in gm.WordsGamer1.Where(t => t != null))
                {
                    Dispatcher.Invoke(() => Gamer2View.Items.Add(new ScoreView
                    {
                        Word = t,
                        Score = t.Count()
                    }));
                }
            }
        }

        //добавление буквы на игровое поле
        private void baldaGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            WarningLabel.Content = "";
            try
            {
                if (AddSymbol)
                {
                    baldaGrid temp = sender as baldaGrid;
                    int tag = (Int32.Parse(temp.Tag.ToString()));

                    if (tag < 32)
                    {
                        _num = tag;

                        temp.Margin = new Thickness(-2, -2, -2, -2);
                        _symbol = temp.bykva;
                        ((baldaGrid)SymbolGrid.Children[_num]).MyBrushSource = Brushes.Aquamarine;

                        GameField.IsEnabled = true;
                        SymbolGrid.IsEnabled = false;
                    }

                    if (tag >= 50)
                    {
                        if ((GameField.Children[tag - 50] as baldaGrid).bykva == "")
                        {
                            _index = tag - 50;
                            (GameField.Children[_index] as baldaGrid).bykva = _symbol;
                            ((baldaGrid)SymbolGrid.Children[_num]).Margin = new Thickness(0);
                            ((baldaGrid)SymbolGrid.Children[_num]).MyBrushSource = new SolidColorBrush(Color.FromRgb(153, 180, 209));
                            _num = -1;
                            AddSymbol = false;
                            AddWord = true;
                        }
                    }
                }
                else
                {
                    WarningLabel.Content = "Для составления слова используйте правую кнопку мыши";
                }
            }
            catch (Exception ex)
            {
                WarningLabel.Content = ex.Message;
            }
        }

        //составление слова
        private void baldaGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            WarningLabel.Content = "";
            try
            {
                if (AddWord)
                {
                    baldaGrid temp = sender as baldaGrid;
                    int tag = (Int32.Parse(temp.Tag.ToString()));

                    if (tag < 50)
                    {
                        WarningLabel.Content = "При составлении слова использовать ячейки игрового поля";
                        return;
                    }

                    if (((baldaGrid)GameField.Children[tag - 50]).bykva == "")
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
                        _word = _word + ((baldaGrid)GameField.Children[_prevIndex]).bykva;
                        MBox.Content = _word;
                        wordindex.Add(_prevIndex);
                        ((baldaGrid)GameField.Children[_prevIndex]).MyBrushSource = Brushes.GreenYellow;
                    }
                    else
                    {
                        _nextIndex = tag - 50;
                        if (_nextIndex == _prevIndex - 1 || _nextIndex == _prevIndex + 1 || _nextIndex == _prevIndex - 5 || _nextIndex == _prevIndex + 5)
                        {
                            _word = _word + ((baldaGrid)GameField.Children[_nextIndex]).bykva;
                            MBox.Content = _word;
                            wordindex.Add(_nextIndex);
                            ((baldaGrid)GameField.Children[_nextIndex]).MyBrushSource = Brushes.GreenYellow;
                            
                            _prevIndex = _nextIndex;
                        }
                        else
                        {
                            WarningLabel.Content = "Использовать занятые ячейки расположенные по прямой";
                        }
                    }
                }
                else
                {
                    WarningLabel.Content = "Вы не добавили новую букву";
                }
            }
            catch (Exception ex) 
            {
                WarningLabel.Content = ex.Message;
            }
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
                        case -1:
                            WarningLabel.Content = "В словаре нет такого слова, добавить в словарь?";
                            Conferm.Visibility = Visibility.Visible;
                            Abort.Visibility = Visibility.Visible;
                            break;
                        case 1:
                            for (int i = 0; i < wordindex.Count; i++)
                            {
                                baldaGrid grid = GameField.Children[wordindex[i]] as baldaGrid;
                                if (grid != null)
                                {
                                    grid.MyBrushSource = new SolidColorBrush(Color.FromRgb(153, 180, 209));
                                }
                            }
                            ClearValue();
                            HintBox.Text = "";
                            HintBox.IsEnabled = false;
                            HintListBox.Items.Clear();
                            AddSymbol = true;
                            AddWord = false;
                            break;
                        case 2:
                            WarningLabel.Content = "Введенное слово уже использовано в игре";
                            break;
                        case 3:
                            WarningLabel.Content = "Ваш соперник пропускает ход";
                            break;
                    }
                }
                catch (CommunicationException)
                {
                    LostConnection();
                }
                catch (Exception ex)
                {
                    WarningLabel.Content = ex.Message;
                }
            }
        }
        
        //отмена выбора игрока
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            WarningLabel.Content = "";
            if (_num > 0)
            {
                SymbolGrid.IsEnabled = true;
                GameField.IsEnabled = false;
                ((baldaGrid)SymbolGrid.Children[_num]).Margin = new Thickness(0);
                ((baldaGrid)SymbolGrid.Children[_num]).MyBrushSource = new SolidColorBrush(Color.FromRgb(153, 180, 209));
                _num = -1;
                _symbol = "";
            }

            if (_index > 0)
            {
                //смена цвета на базовый
                for (int i = 0; i < wordindex.Count; i++)
                {
                    baldaGrid grid = GameField.Children[wordindex[i]] as baldaGrid;
                    if (grid != null)
                    {
                        grid.MyBrushSource = new SolidColorBrush(Color.FromRgb(153, 180, 209));
                    }
                }

                SymbolGrid.IsEnabled = true;
                ((baldaGrid)GameField.Children[_index]).bykva = "";
                AddSymbol = true;
                AddWord = false;
                GameField.IsEnabled = false;
                ClearValue();
            }
        }

        //обработчик нажатия кнопки завершения партии
        private void FinishGame_Click(object sender, RoutedEventArgs e)
        {
            if (gm == null)
            {
                LostConnection();
                return;
            }

            if (gm.GameField.Contains(""))
            {
                WarningLabel.Content = "Игра не окончена, вы действительно хотите выйти?";
                Conferm.Visibility = Visibility.Visible;
                Abort.Visibility = Visibility.Visible;
                _finish = true;
                inGame = false;
            }
        }

        //обработчик нажатия кнопки выхода из игры
        private void ExitGame_Click(object sender, RoutedEventArgs e)
        {
            if (gm == null)
            {
                LostConnection();
                return;
            }

            if (gm.GameField.Contains(""))
            {
                WarningLabel.Content = "Игра не окончена, вы действительно хотите выйти?";
                Conferm.Visibility = Visibility.Visible;
                Abort.Visibility = Visibility.Visible;
                _exit = true;
            }
        }

        //подтверждение выбора игрока
        private void Conferm_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //игрок отключился от сервера
                if (_disconnect)
                {
                    GameFieldGrid.Visibility = Visibility.Hidden;
                    ChooseGameGrid.Visibility = Visibility.Visible;
                    ClearValue();
                    inGame = false;
                    return;
                }
                //игрок пропускает ход
                if (skipMove)
                {
                    if (countSkip == 0)
                    {
                        countSkip++;
                        App.Proxy.SendWord(App.login, 0, "", "");
                        skipMove = false;
                        ClearValue();
                        Conferm.Visibility = Visibility.Hidden;
                        HintBox.Text = "";
                        HintBox.IsEnabled = false;
                        HintListBox.Items.Clear();
                        Abort.Visibility = Visibility.Hidden;
                        return;
                    }
                    else
                    {
                        App.Proxy.FinishGame(App.login);
                        GameFieldGrid.Visibility = Visibility.Hidden;
                        ChooseGameGrid.Visibility = Visibility.Visible;
                        GetGamerInfo();
                        ClearValue();
                        HintBox.Text = "";
                        HintBox.IsEnabled = false;
                        HintListBox.Items.Clear();
                        inGame = false;
                        return;
                    }
                }

                //ничья
                if (_ni4ya)
                {
                    GameFieldGrid.Visibility = Visibility.Hidden;
                    ChooseGameGrid.Visibility = Visibility.Visible;
                    GetGamerInfo();
                    ClearValue();
                    inGame = false;
                    return;
                }

                //победа
                if (_win)
                {
                    GameFieldGrid.Visibility = Visibility.Hidden;
                    ChooseGameGrid.Visibility = Visibility.Visible;
                    ClearValue();
                    GetGamerInfo();
                    inGame = false;
                    return;
                }

                //выход из партии
                if (_finish)
                {
                    App.Proxy.FinishGame(App.login);
                    GameFieldGrid.Visibility = Visibility.Hidden;
                    ChooseGameGrid.Visibility = Visibility.Visible;
                    ClearValue();
                    GetGamerInfo();
                    inGame = false;
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

                for (int i = 0; i < wordindex.Count; i++)
                {
                    baldaGrid grid = GameField.Children[wordindex[i]] as baldaGrid;
                    if (grid != null)
                    {
                        grid.MyBrushSource = new SolidColorBrush(Color.FromRgb(153, 180, 209));
                    }
                }
                ClearValue();
                AddSymbol = true;
                AddWord = false;
                HintBox.Text = "";
                HintBox.IsEnabled = false;
                HintListBox.Items.Clear();
                Conferm.Visibility = Visibility.Hidden;
                Abort.Visibility = Visibility.Hidden;
            }
            catch (CommunicationException com)
            {
                LostConnection();
            }
            catch (Exception ex)
            {
                WarningLabel.Content = ex.Message;
            }
        }

        //отмена выбранных данных
        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            //игрок отключился от сервера
            if (_disconnect)
            {
                LostConnection();
            }
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
                //возвращаемся на стартовый экран
            }
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
                AddSymbol = true;
                AddWord = false;
                HintBox.Text = "";
                HintBox.IsEnabled = false;
                HintListBox.Items.Clear();
                WarningLabel.Content = "";
            }
        }

        //очистка выбранных значений
        private void ClearValue()
        {
            _symbol = "";
            _index = -1;
            _num = -1;
            MBox.Content = "";
            _word = "";
            wordindex.Clear();
            WarningLabel.Content = "";
        }

        //обработчик кнопки подсказка
        private void Hint_Click(object sender, RoutedEventArgs e)
        {
            HintListBox.Visibility = Visibility.Visible;
            HintBox.Visibility = Visibility.Visible;
            HintBox.Text = "Введите слово для проверки";
            HintBox.IsEnabled = true;
        }

        //очистка поля текст блока по клику на него
        private void HintBox_GotFocus(object sender, RoutedEventArgs e)
        {
            HintBox.Text = "";
        }

        //событие ввода текста в текст блок
        private void HintBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            lt = HintBox.Text.ToUpper();
            if (search != null)
            {
                search.Abort();
            }
            HintListBox.Items.Clear();
            search = new Thread(SearchText) { IsBackground = true };
            search.Start();
        }

        //поток поиска слов в словаре по мере набора
        private void SearchText()
        {
            if (WordArray.Count() > 2)
            {
                for (int i = 0; i < WordArray.Count(); i++)
                {
                    Dispatcher.Invoke(new Action(delegate
                    {
                        if (WordArray[i].StartsWith(lt))
                        {
                            HintListBox.Items.Add(WordArray[i]);
                        }
                    }));
                }
            }
        }

        //кнопка пропуска хода
        private void SkipMove_Click(object sender, RoutedEventArgs e)
        {
            skipMove = true;
            WarningLabel.Content = "Вы пропускаете ход?";
            Conferm.Visibility = Visibility.Visible;
            Abort.Visibility = Visibility.Visible;
        }
    }

    //класс вывода игроков в листвью
    public class ScoreView
    {
        public string Word { get; set; }
        public int Score { get; set; }
    }
}
