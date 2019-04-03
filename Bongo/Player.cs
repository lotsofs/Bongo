using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	public class Player {
		public Socket Socket;
		public string Name = "Player";
		public byte Color;
		public byte Id;
	}
}
