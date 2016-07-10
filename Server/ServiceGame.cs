using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;

namespace BaldaServer
{
    [ServiceContract]
    class ServiceGame
    {
        List<ClientList> _gamersList = new List<ClientList>();
        
        //проверка соединения
        [OperationContract]
        public bool Connect()
        {
            return true;
        }

        //метод для обновления времени таймаута
        [OperationContract]
        public bool IamOnLine(string log)
        {
            Program.Rws.EnterReadLock();
            OnLineGamers gamer = Program.onlineGamerList.Find(g => g.login == log);
            Program.Rws.ExitReadLock();

            if (gamer == null)
            {
                return false;
            }

            gamer.timeQuery = DateTime.Now;
            return true;
        }

        //вход в игру
        [OperationContract]
        public int Login(string log, string pass)
        {
            Program.Rws.EnterReadLock();
            BaldaDataBase BaldaDB = new BaldaDataBase();
            UserList user = BaldaDB.UserLists.FirstOrDefault(x => x.login == log);
            bool addUser = Program.onlineGamerList.Any(x => x.login == log);
            Program.Rws.ExitReadLock();

            if (user == null) return 0;
            if (addUser)
            {
                return 1;
            }
            if (user.password != pass)
            {
                return 2;
            }

            Program.Rws.EnterWriteLock();
            Program.onlineGamerList.Add(new OnLineGamers(log, DateTime.Now));
            Program.Rws.ExitWriteLock();

            return 3;
        }

        //регистрация в игре
        [OperationContract]
        public bool RegUser(string log, string pass, string name, string logo)
        {
            Program.Rws.EnterWriteLock();

            BaldaDataBase BaldaDB = new BaldaDataBase();

            UserList user = new UserList { login = log, password = pass, userName = name, gamecount = 0, gamewin = 0, userLogo = logo };
            Program.onlineGamerList.Add(new OnLineGamers(log, DateTime.Now));
            BaldaDB.UserLists.InsertOnSubmit(user);
            BaldaDB.SubmitChanges();

            Program.Rws.ExitWriteLock();

            return true;
        }

        //получение информации об игроке при входе в игру
        [OperationContract]
        public ClientList GetGamerInfo(string log)
        {
            ClientList client = null;
            Program.Rws.EnterReadLock();
            BaldaDataBase BaldaDB = new BaldaDataBase();
            UserList user = BaldaDB.UserLists.FirstOrDefault(x => x.login == log);
            if (user != null)
            {
                client = new ClientList(user.login, user.userName, user.gamecount, user.gamewin, user.userLogo);
            }
            Program.Rws.ExitReadLock();
            return client;
        }

        //получение информации от игроков
        [OperationContract]
        public int SendWord(string log, int idx, string str, string lt, int com = 0)
        {
            Program.Rws.EnterReadLock();
            Game gm = Program.ListGames.Find(g => g.ListGamer.Find(x => x.UserLogin == log) != null);
            Program.Rws.ExitReadLock();

            Program.Rws.EnterWriteLock();
            if (!File.Exists("log.txt"))
            {
                using (StreamWriter w = File.CreateText("log.txt"))
                {
                    Program.Log(log, idx, str, lt, com, w);
                }
            }
            using (StreamWriter w = File.AppendText("log.txt"))
            {
                Program.Log(log, idx, str, lt, com, w);
            }
            Program.Rws.ExitWriteLock();

            if (gm != null)
            {
                Program.Rws.EnterWriteLock();
                if (!File.Exists("log.txt"))
                {
                    using (StreamWriter w = File.CreateText("log.txt"))
                    {
                        Program.Exit(log, gm.ToString(), w);
                    }
                }
                using (StreamWriter w = File.AppendText("log.txt"))
                {
                    Program.Exit(log, gm.ToString(), w);
                }
                Program.Rws.ExitWriteLock();

                if (com == 0)
                {
                    if (idx == 0 && str == "" && lt == "")
                    {
                        lock (gm)
                        {
                            if (gm.GameState == 2)
                            {
                                gm.GameState = 3;
                                gm.CurGamer = gm.ListGamer[1].UserLogin;
                            }
                            else
                            {
                                gm.GameState = 2;
                                gm.CurGamer = gm.ListGamer[0].UserLogin;
                            }
                            return 3; // игрок пропустил ход
                        }
                    }

                    if (gm.WordsGamer1.Contains(str) || gm.WordsGamer2.Contains(str))
                    {
                        return 2; //Введенное слово уже использовано в игре
                    }
                    if (Program.MyDict.Contains(str) || Program.UserDict.Contains(str))
                    {
                        lock (gm)
                        {
                            gm.GameField[idx] = lt;
                            switch (gm.GameState)
                            {
                                case 2:
                                    if (!gm.Winner())
                                    {
                                        gm.WordsGamer1.Add(str);
                                        gm.GameState = 3;
                                        gm.CurGamer = gm.ListGamer[1].UserLogin;
                                    }
                                    break;
                                case 3:
                                    if (!gm.Winner())
                                    {
                                        gm.WordsGamer2.Add(str);
                                        gm.GameState = 2;
                                        gm.CurGamer = gm.ListGamer[0].UserLogin;
                                    }
                                    break;
                            }
                        }
                        return 1; //слово найдено в словарях и ход переходит к другому игроку
                    }
                    return -1;
                }
                if (com == 1)
                {
                    Program.Rws.EnterWriteLock();
                    Program.UserDict.Add(str);
                    Program.Rws.ExitWriteLock();

                    lock (gm)
                    {
                        gm.GameField[idx] = lt;
                        switch (gm.GameState)
                        {
                            case 2:
                                if (!gm.Winner())
                                {
                                    gm.WordsGamer1.Add(str);
                                    gm.GameState = 3;
                                    gm.CurGamer = gm.ListGamer[1].UserLogin;
                                }
                                break;
                            case 3:
                                if (!gm.Winner())
                                {
                                    gm.WordsGamer2.Add(str);
                                    gm.GameState = 2;
                                    gm.CurGamer = gm.ListGamer[0].UserLogin;
                                }
                                break;
                        }
                    }
                    return 1; //слово добавлено в пользовательский словарь и ход переходит к другому игроку
                }
            }
            return 0; //слово не найдено в словаре
        }

        //получение списка игроков
        [OperationContract]
        public List<ClientList> GetGamers(string log)
        {
            UserList user = null;

            Program.Rws.EnterReadLock();
            try
            {
                BaldaDataBase BaldaDB = new BaldaDataBase();
                user = BaldaDB.UserLists.FirstOrDefault(x => x.login == log);
            }
            catch (Exception ex) { Console.WriteLine(ex.StackTrace); }
            if (user != null)
            {
                OnLineGamers onlineG = Program.onlineGamerList.Find(x => x.login == log);
                if (onlineG != null)
                {
                    int min = (int)user.gamewin - 5;
                    int max = (int)user.gamewin + 5;

                    List<Game> gmLists = Program.ListGames.FindAll(x => x.GameState == 1);

                    _gamersList =
                        (from game in gmLists
                         select game.ListGamer[0]).ToList();
                }
            }
            Program.Rws.ExitReadLock();
            return _gamersList;
        }

        //создане новой игры
        [OperationContract]
        public void CreateNewGame(string log)
        {
            try
            {
                Program.Rws.EnterWriteLock();

                BaldaDataBase BaldaDB = new BaldaDataBase();

                UserList user = BaldaDB.UserLists.FirstOrDefault(x => x.login == log);
                ClientList client = new ClientList(user.login, user.userName, user.gamecount, user.gamewin, user.userLogo);
                Game gm = new Game(client);

                Program.ListGames.Add(gm);

                gm.CurGamer = user.login;
                Console.WriteLine(@"Игрок " + user.login + @" создал игру в " + DateTime.Now.ToShortTimeString());

                gm.GameState = 1;

                Program.Rws.ExitWriteLock();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                Program.Rws.ExitWriteLock();
            }
        }

        //отмена создания новой игры
        [OperationContract]
        public bool CancelNewGame(string log)
        {
            Program.Rws.EnterReadLock();
            Game gm = Program.ListGames.Find(x => x.ListGamer.Find(g => g.UserLogin == log) != null);
            Program.Rws.ExitReadLock();

            if (gm != null)
            {
                lock (gm)
                {
                    gm.GameState = 7;
                    if (gm.GameState == 7) //7 - статус отмены созданной игры
                    {
                        Console.WriteLine(@"Игрок " + log + @" удалил игру в " + DateTime.Now.ToShortTimeString());
                        Program.ListGames.Remove(gm);
                        return true;
                    }
                }
            }
            return false;
        }

        //подключение к созданной ранее игре
        [OperationContract]
        public void ConnectToGame(string creator, string chosen)
        {
            Game gm = null;

            Program.Rws.EnterReadLock();
            BaldaDataBase BaldaDB = new BaldaDataBase();

            UserList user1 = BaldaDB.UserLists.FirstOrDefault(x => x.login == creator);
            UserList user2 = BaldaDB.UserLists.FirstOrDefault(x => x.login == chosen);
            if (user1 != null && user2 != null)
            {
                gm = Program.ListGames.Find(x => x.CurGamer == user1.login);
            }
            Program.Rws.ExitReadLock();

            if (gm != null)
            {
                lock (gm)
                {
                    ClientList client2 = new ClientList(user2.login, user2.userName, user2.gamecount, user2.gamewin,
                        user2.userLogo);
                    gm.ListGamer.Add(client2);
                    Console.WriteLine(@"Игрок " + user2.login + @" подключился к игре с игроком " + user1.login + @" в " + DateTime.Now.ToShortTimeString());
                    gm.GameState = 2;
                }
            }
        }

        //получение состояния игры
        [OperationContract]
        public Game GetGame(string log)
        {
            bool remGame = false;

            Program.Rws.EnterReadLock();
            Game gm = Program.ListGames.Find(g => g.ListGamer.Find(x => x.UserLogin == log) != null);
            Program.Rws.ExitReadLock();

            if (gm == null) return null;

            lock (gm)
            {
                if (gm.GameState == 4 || gm.GameState == 5 || gm.GameState == 6 || gm.GameState == -1)
                {
                    ClientList remClientList = gm.ListGamer.Find(x => x.UserLogin == log);
                    if (remClientList != null)
                    {
                        gm.ListGamer.Remove(remClientList);
                    }

                    if (gm.ListGamer.Count == 0)
                    {
                        remGame = true;
                    }
                }
            }
           
            if (remGame)
            {
                Program.Rws.EnterWriteLock();
                Program.ListGames.Remove(gm);
                Program.Rws.ExitWriteLock();
            }
            return gm;
        }

        //выход из игры
        [OperationContract]
        public void GameExit(string log)
        {
            Program.Rws.EnterReadLock();
            OnLineGamers gamer = Program.onlineGamerList.Find(x => x.login == log);
            Program.Rws.ExitReadLock();

            if (gamer != null)
            {
                Program.Rws.EnterReadLock();
                Game gm = Program.ListGames.Find(g => g.ListGamer.Find(x => x.UserLogin == log) != null);
                Program.Rws.ExitReadLock();

                if (gm != null)
                {
                    lock (gm)
                    {
                        if (gm.GameState == 1)
                        {

                            ClientList client = gm.ListGamer.Find(x => x.UserLogin == log);
                            Console.WriteLine(@"Игрок " + client.UserLogin + @" отключился в " + DateTime.Now.ToShortTimeString());

                            gm.ListGamer.Remove(client);
                            Program.ListGames.Remove(gm);
                        }

                        if (gm.GameState == 2 || gm.GameState == 3)
                        {
                            lock (gm)
                            {
                                gm.GameState = -1;
                                ClientList client = gm.ListGamer.Find(x => x.UserLogin == log);
                                Console.WriteLine(@"Игрок " + client.UserLogin + @" отключился в " + DateTime.Now.ToShortTimeString());
                                gm.ListGamer.Remove(client);
                            }
                        }
                    }
                }
                Program.Rws.EnterWriteLock();
                Program.onlineGamerList.Remove(gamer);
                Program.Rws.ExitWriteLock();
            }
        }

        //окончание текущей партии
        [OperationContract]
        private void FinishGame(string log)
        {
            Program.Rws.EnterReadLock();
            Game gm = Program.ListGames.Find(g => g.ListGamer.Find(x => x.UserLogin == log) != null);
            Program.Rws.ExitReadLock();

            if (gm != null)
            {
                lock (gm)
                {
                    ClientList gamer = gm.ListGamer.Find(x => x.UserLogin == log);
                    Console.WriteLine(@"Игрок " + gamer.UserLogin + @" отключился в " + DateTime.Now.ToShortTimeString());
                    gm.ListGamer.Remove(gamer);
                    gm.GameState = -1;
                }
            }
        }

        //запись результатов игры в базу данных
        [OperationContract]
        public void SaveRecord(string log, string state)
        {
            UserList gamer = null;
            BaldaDataBase BaldaDB = new BaldaDataBase();

            try
            {
                if (state == "exit" || state == "win")
                {
                    gamer = BaldaDB.UserLists.First(x => x.login == log);
                    if (gamer != null)
                    {
                        Program.Rws.EnterWriteLock();
                        gamer.gamewin++;
                        Console.WriteLine(@"Игрок " + gamer.login + @" выиграл в " + DateTime.Now.ToShortTimeString());

                        BaldaDB.SubmitChanges();
                        Program.Rws.ExitWriteLock();

                        return;
                    }
                }

                if (state == "enter")
                {
                    gamer = BaldaDB.UserLists.First(x => x.login == log);
                    if (gamer != null)
                    {
                        Program.Rws.EnterWriteLock();
                        gamer.gamecount++;
                        BaldaDB.SubmitChanges();
                        Program.Rws.ExitWriteLock();
                    }
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}