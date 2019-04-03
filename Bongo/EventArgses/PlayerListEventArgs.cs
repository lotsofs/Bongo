using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	public class PlayerListEventArgs {
		public List<Player> PlayerList;

		public PlayerListEventArgs(List<Player> playerList) {
			PlayerList = playerList;
		}
	}
}
