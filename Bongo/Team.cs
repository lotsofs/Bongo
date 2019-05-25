using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	class Team {
		public string Name = "Default";
		public int[] BoardStatus = new int[25];
		public int[] BoardStatusSpectateOverride = new int[25];

		public int ColorCount = 4;

		enum Statuses { Blank, Todo, Done, Failed };

		public Color[] Colors = new Color[] { Color.LightGray, Color.DodgerBlue, Color.LimeGreen, Color.Red };
		public string[] Icons = new string[] { "", "", ":)", "X" };

		//public Color wontColor = Color.Goldenrod;
		//public string wontIcon = "";
		//public Color wipColor = Color.DarkOrchid;
		//public string wipIcon = "";

		public Team() {

		}

		public Team(Color color) {
			SetColor(color);
		}

		public void SetColor(Color color) {
			Colors[1] = color;
			Colors[2] = color;
			Colors[3] = color;
			Name = color.ToString();
		}

		public int ChangeTileColor(int tile, bool forward) {
			BoardStatus[tile] += forward ? 1 : -1;
			BoardStatus[tile] %= ColorCount;
			return BoardStatus[tile];
		}

		public int ChangeTileOverrideColor(int tile, bool forward) {
			BoardStatusSpectateOverride[tile] += forward ? 1 : -1;
			BoardStatusSpectateOverride[tile] %= ColorCount;
			return BoardStatusSpectateOverride[tile];
		}
	}
}
