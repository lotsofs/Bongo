using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	class BingoBoardEventArgs {
		public int[] Board;
		public int Player;
		public bool SameTeam;

		public BingoBoardEventArgs(int[] board, int player, bool sameTeam) {
			SameTeam = sameTeam;
			Board = board;
			Player = player;
		}
	}
}
