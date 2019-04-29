using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	class BingoBoardEventArgs {
		public int[] Board;
		public int Player;

		public BingoBoardEventArgs(int[] board, int player) {
			Board = board;
			Player = player;
		}
	}
}
