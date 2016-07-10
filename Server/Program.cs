using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.ServiceModel;

namespace BaldaServer
{
    class Program
    {
        //список онлайн пользователей зашедших в игру или зарегистрировавшихся в игре
        public static List<OnLineGamers> onlineGamerList = new List<OnLineGamers>();

        public static List<Game> ListGames = new List<Game>();
        public static ServiceHost Host;
        //public static BaldaDataBase BaldaDB = new BaldaDataBase();
        public static ReaderWriterLockSlim Rws = new ReaderWriterLockSlim();
        public static string[] MyDict;
        public static List<string> UserDict = new List<string>();

        static void Main()
        {
            try
            {
                GetDictionary();
                WcfConnect();

                Console.Title = @"Balda Server";

                Thread timer = new Thread(TimeOut) { IsBackground = true };
                timer.Start();

                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        //запись словаря в память
        private static void GetDictionary()
        {
            try
            {
                if (File.Exists("dictionary.txt"))
                {
                    MyDict = File.ReadAllLines("dictionary.txt");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        //ведение лога
        public static void Log(string login, int idx, string word, string letter, int com, TextWriter w) 
        {
            w.Write("\r\nLogEntry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            w.WriteLine(" :");
            w.WriteLine("login :{0}, index :{1}, word :{2}, letter :{3}, command :{4}", login, idx, word, letter, com);
            w.WriteLine("---------------------------------------------------------------------");
        }

        public static void Exit(string login, string str, TextWriter w)
        {
            w.Write("\r\nLogEntry : ");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            w.WriteLine(" :");
            w.WriteLine("login :{0} {1}", login, str);
            w.WriteLine("---------------------------------------------------------------------");
        }

        //соединение с сервером
        private static void WcfConnect()
        {
            try
            {
                Host = new ServiceHost(typeof(ServiceGame));
                Host.Open();

                Console.WriteLine(@"Сервер запущен, для завершения работы нажмите любую клавишу");

                Console.WriteLine(DataBaseConnect()
                    ? "Соединение с базой установлено успешно"
                    : "Соединение с базой не установлено");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        //соединение с базой данных
        private static bool DataBaseConnect()
        {
            try
            {
                BaldaDataBase BaldaDB = new BaldaDataBase();
                List<UserList> res = BaldaDB.UserLists.Where(x => x.Id != 0).ToList();
                string resList = res.ElementAt(0).login;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        //определение отключения игроков по таймауту
        private static void TimeOut(object obj)
        {
            while (true)
            {
                bool remGame = false;

                if (onlineGamerList.Any())
                {
                    Rws.EnterReadLock();
                    List<OnLineGamers> copyGamers = new List<OnLineGamers>(onlineGamerList);
                    Rws.ExitReadLock();

                    foreach (OnLineGamers gamer in copyGamers)
                    {
                        DateTime tNow = DateTime.Now;
                        try
                        {
                            TimeSpan ts = tNow.Subtract(gamer.timeQuery);
                            //Console.WriteLine(@"gamer:{0}, timespan:{1}", gamer.login, ts.Seconds);
                            if (ts.Seconds >= 200)
                            {
                                Rws.EnterReadLock();
                                Game gm = ListGames.Find(c => c.ListGamer.Find(a => a.UserLogin == gamer.login) != null);
                                Rws.ExitReadLock();

                                if (gm != null)
                                {
                                    lock (gm)
                                    {
                                        gm.GameState = -1;
                                        ClientList client = gm.ListGamer.Find(x => x.UserLogin == gamer.login);

                                        if (client != null)
                                        {
                                            gm.ListGamer.Remove(client);
                                        }

                                        if (gm.ListGamer.Count == 0)
                                        {
                                            remGame = true;
                                        }
                                    }

                                    Rws.EnterWriteLock();

                                    if (remGame)
                                    {
                                        if (!File.Exists("log.txt"))
                                        {
                                            using (StreamWriter w = File.CreateText("log.txt"))
                                            {
                                                Program.Exit(gamer.login, "игра удалена", w);
                                            }
                                        }
                                        using (StreamWriter w = File.AppendText("log.txt"))
                                        {
                                            Program.Exit(gamer.login, "игра удалена", w);
                                        }
                                        ListGames.Remove(gm);
                                    }
                                    Rws.ExitWriteLock();
                                }

                                Rws.EnterWriteLock();
                                onlineGamerList.Remove(gamer);
                                if (!File.Exists("log.txt"))
                                {
                                    using (StreamWriter w = File.CreateText("log.txt"))
                                    {
                                        Program.Exit(gamer.login, "отключился по таймауту", w);
                                    }
                                }
                                using (StreamWriter w = File.AppendText("log.txt"))
                                {
                                    Program.Exit(gamer.login, "отключился по таймауту", w);
                                }
                                Rws.ExitWriteLock();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }
    }
}