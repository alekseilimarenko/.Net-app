using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BaldaServer
{
    class OnLineGamers
   {
        public string login;
        public DateTime timeQuery;

        public OnLineGamers(string log, DateTime tQ)
        {
            login = log;
            timeQuery = tQ;
        }
    }
}
