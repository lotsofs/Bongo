using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	public static class Layout {
		//public static void GenerateRandom(uint difficulty, uint seed) {
		//	Random rand = new Random((int)seed);

		//	// determine how many tiles there are of each difficulty
		//	int mineCount;
		//	int hardCount;
		//	int mediumCount;
		//	int easyCount;

		//	switch (difficulty) {
		//		default: // disregard
		//			mineCount = -1;
		//			hardCount = -1;
		//			mediumCount = -1;
		//			easyCount = -1;
		//			break;
		//		case 1: // very easy
		//			mineCount = 0;
		//			hardCount = 0;
		//			mediumCount = 1;
		//			easyCount = 24;
		//			break;
		//		case 2: // easy
		//			mineCount = 0;
		//			hardCount = 2;
		//			mediumCount = 5;
		//			easyCount = 18;
		//			break;
		//		case 3: // normal
		//			mineCount = 1;
		//			hardCount = 3;
		//			mediumCount = 11;
		//			easyCount = 10;
		//			break;
		//		case 4: // hard
		//			mineCount = 2;
		//			hardCount = 5;
		//			mediumCount = 12;
		//			easyCount = 6;
		//			break;
		//		case 5: // very hard
		//			mineCount = 3;
		//			hardCount = 8;
		//			mediumCount = 13;
		//			easyCount = 1;
		//			break;
		//	}

		//	// create a list of tiles and shuffle them
		//	List<int> tiles = new List<int>(Enumerable.Range(0, 25));
		//	List<int> shuffledTiles = new List<int>();

		//	while (tiles.Count > 0) {
		//		int index = rand.Next(tiles.Count);
		//		shuffledTiles.Add(tiles[index]));
		//		tiles.RemoveAt(index);
		//	}

		//	// create the bingo board layout
		//	List<int> field = new List<int>(25);

		//	for (int i = 0; i < 25; i++) {
		//		int tile = shuffledTiles[i];
		//		while (mineCount > 0) {
		//			mineCount--;
		//			field[tile] 
		//		}

		//	}
		//}
	}
}
