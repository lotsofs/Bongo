using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	public class Goal {
		public string Name;
		public string Description;
		public float Length;
		public float Difficulty;

		public Goal(string name, string description, float length, float difficulty) {
			Name = name;
			Description = description;
			Length = length;
			Difficulty = difficulty;
		}
	}
}
