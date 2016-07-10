using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;


namespace BaldaServer
{
    [DataContract]
    public class Game
    {
        [DataMember]
        public int GameState;
        [DataMember]
        public string CurGamer;
        [DataMember]
        public List<ClientList> ListGamer;
        [DataMember]
        public List<string> GameField;

        [DataMember]
        public List<string> WordsGamer1;
        [DataMember]
        public List<string> WordsGamer2;
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
    }

    [ServiceContract]
    public interface ServiceGame
    {
        [OperationContract]
        bool Connect();

        [OperationContract]
        bool IamOnLine(string log);

        [OperationContract]
        int Login(string log, string pass);

        [OperationContract]
        bool RegUser(string log, string pass, string name, string logo);

        [OperationContract]
        ClientList GetGamerInfo(string log);

        [OperationContract]
        int SendWord(string log, int idx, string str, string lt, int com = 0);

        [OperationContract]
        List<ClientList> GetGamers(string log);

        [OperationContract]
        void CreateNewGame(string log);

        [OperationContract]
        bool CancelNewGame(string log);

        [OperationContract]
        void ConnectToGame(string creator, string chosen);

        [OperationContract]
        Game GetGame(string log);

        [OperationContract]
        void GameExit(string log);

        [OperationContract]
        void FinishGame(string log);

        [OperationContract]
        void SaveRecord(string log, string state);
    }
}
