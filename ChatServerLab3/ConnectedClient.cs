using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ChatServerLab3
{
    class ConnectedClient
    {
        public TcpClient Client {  get; set; }
        public string Name { get; set; }

        public ConnectedClient(TcpClient client, string name)
        {
            Client = client;
            Name = name;
        }
    }
}
