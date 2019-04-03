using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	class PointGenerator {
		/// <summary>
		/// Generates random value between 0 and highestValue, uses algebraic functions to give a bias towards lower or higher numbers
		/// </summary>
		/// <param name="seed"></param>
		/// <param name="bias">1 = add bias towards lower, 5 = higher. 0 = ignore</param>
		/// <param name="highestValue"></param>
		/// <returns></returns>
		public static double Random(int seed, int bias, int highestValue) {
			Random rand = new Random((int)seed);
			double num;
			switch (bias) {
				case 1: // very little
					num = rand.Next(0, 100);
					num = Math.Pow(num, 4);
					num /= 4000000;
					break;
				case 2: // little
					num = rand.Next(0, 100);
					num = Math.Pow(num, 3);
					num /= 15000;
					break;
				default:
				case 3: // medium
					num = rand.Next(0, 100);
					num = Math.Pow(num, 2);
					num /= 110;
					break;
				case 4: // a lot
					num = rand.Next(0, 100);
					num = Math.Pow(num, 1.5);
					num /= 10;
					break;
				case 5: // a whole lot
					num = rand.Next(0, 100);
					break;
				case 6: // madness
					num = rand.Next(0, 100);
					num = Math.Pow(num, 0.5);
					num /= 0.1;
					break;
			}
			num /= (100 / highestValue);
			num = Math.Round(num);
			return num;
		}
	}
}
