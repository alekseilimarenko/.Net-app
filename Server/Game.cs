using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;


namespace BaldaServer
{
    [DataContract]
    public class Game
    {
        [DataMember] public int GameState;

        [DataMember] public string CurGamer;

        [DataMember] public List<ClientList> ListGamer = new List<ClientList>();

        [DataMember] public List<string> GameField = new List<string>();

        [DataMember] public List<string> WordsGamer1 = new List<string>();

        [DataMember] public List<string> WordsGamer2 = new List<string>();

        public Game(ClientList client)
        {
            ListGamer.Add(client);
            SendWordToClient();
        }

        private void SendWordToClient()
        {
            Random rnd = new Random();

            for (int i = 0; i < 25; i++)
            {
                GameField.Add("");
            }

            for (int i = 0; i < Program.MyDict.Length - 1; i++)
            {
                bool find = false;
                while (!find)
                {
                    int j = rnd.Next(0, Program.MyDict.Length - 1);
                    if (Program.MyDict[j].Length == 5)
                    {
                        string word = Program.MyDict[j];

                        GameField[10] = word[0].ToString();
                        GameField[11] = word[1].ToString();
                        GameField[12] = word[2].ToString();
                        GameField[13] = word[3].ToString();
                        GameField[14] = word[4].ToString();
                        find = true;
                    }
                }
            }
        }

        public bool Winner()
        {
            int score1 = 0, score2 = 0;
            if (!GameField.Contains(""))
            {
                score1 += WordsGamer1.Sum(s => s.Count());

                score2 += WordsGamer2.Sum(s => s.Count());

                if (score1 > score2)
                {
                    GameState = 4;
                }
                if (score1 < score2)
                {
                    GameState = 5;
                }
                if (score1 == score2)
                {
                    GameState = 6;
                }
                return true;
            }
            return false;
        }
    }

    [DataContract]
    public class ClientList
    {
        [DataMember]
        public string UserLogin;
        [DataMember]
        public string UserNik;
        [DataMember]
        public int GameCount;
        [DataMember]
        public int WinCount;
        [DataMember]
        public string UserLogo;
        [DataMember]
        public int Score;

        public ClientList(string log, string nik, int gCount, int wCount, string logo, int sc = 0)
        {
            UserLogin = log;
            UserNik = nik;
            GameCount = gCount;
            WinCount = wCount;
            UserLogo = logo;
            Score = sc;
        }
    }
}
