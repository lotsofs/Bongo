using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	class PointGenerator {
		// TODO: This assumes there's only 4 difficulty levels for goals. Make this flexible.
		public static double Random(uint seed, uint scale, uint highestValue) {
			Random rand = new Random((int)seed);
			double num;
			switch (scale) {
				default: // disregarded
					num = rand.Next(0, 100);
					break;
				case 1: // very easy
					num = rand.Next(0, 100);
					num = Math.Pow(num, 4);
					num /= 4000000;
					break;
				case 2: // easy
					num = rand.Next(0, 100);
					num = Math.Pow(num, 3);
					num /= 15000;
					break;
				case 3: // medium
					num = rand.Next(0, 100);
					num = Math.Pow(num, 2);
					num /= 110;
					break;
				case 4: // hard
					num = rand.Next(0, 100);
					num = Math.Pow(num, 1.5);
					num /= 10;
					break;
				case 5: // very hard
					num = rand.Next(0, 100);
					num = Math.Pow(num, 0.5);
					num /= 0.1;
					break;
			}
			num /= (100 / highestValue);
			return num;
		}
	}
}
