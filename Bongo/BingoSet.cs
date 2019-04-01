using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	public struct BingoSet {
		public string Title;
		public string Description;
		public string Version;

		public string Errors;

		public List<Goal> Goals;

		public bool Premade;

		//public int lowestLength;
		//public int highestLength;
		//public int lowestDifficulty;
		//public int highestDifficulty;
	}
}
