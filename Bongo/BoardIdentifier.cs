using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	public class BoardIdentifier {

		/* -000 0000 0000 LLLD DDSS SSSS SSSS SSSS
		 * S: bits 1 - 14 = seed
		 * D: bits 15 - 17 = difficulty
		 * L: bits 18 - 20 = length
		 * 
		 */

		const int SeedBits = 14;
		const int DifficultyBits = 3;
		const int MaxSeed = 0x3fff;    // 16383
		const int MaxDifficulty = 0b111;	// 7
		const int MaxLength = 0b111;   // 7

		static public int GenerateNew(int seed, int difficulty, int length) {
			int newId = 0;
			newId = WriteSeed(newId, seed);
			newId = WriteDifficulty(newId, difficulty);
			newId = WriteLength(newId, length);
			return newId;
		}

		static public int ReadDifficulty(int id) {
			int newId = id >> SeedBits;
			return newId & MaxDifficulty;
		}

		static public int WriteDifficulty(int id, int difficulty) {
			int newId = id & ~(MaxSeed << SeedBits);
			newId |= difficulty << SeedBits;
			return newId;
		}


		static public int ReadLength(int id) {
			int newId = id >> SeedBits + DifficultyBits;
			return newId & MaxLength;
		}

		static public int WriteLength(int id, int length) {
			int newId = id & ~(MaxSeed << (SeedBits + DifficultyBits));
			newId |= length << (SeedBits + DifficultyBits);
			return newId;
		}


		static public int ReadSeed(int id) {
			return id & MaxSeed;
		}

		static public int WriteSeed(int id, int seed) {
			if (seed > MaxSeed) {
				throw new Exception("seed is too large, max " + MaxSeed);
			}
			else {
				int newId = id & ~MaxSeed;
				newId |= seed;
				return newId;
			}
		}

	}
}
