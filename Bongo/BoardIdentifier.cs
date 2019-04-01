using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	public class BoardIdentifier {

		/* 0000 0000 0000 LLLD DDSS SSSS SSSS SSSS
		 * S: bits 1 - 14 = seed
		 * D: bits 15 - 17 = difficulty
		 * L: bits 18 - 20 = length
		 * 
		 */

		const int SeedBits = 14;
		const int DifficultyBits = 3;
		const uint MaxSeed = 0x3fff;    // 16383
		const uint MaxDifficulty = 0b111;	// 7
		const uint MaxLength = 0b111;   // 7

		static public uint GenerateNew(uint seed, uint difficulty, uint length) {
			uint newId = 0;
			WriteSeed(newId, seed);
			WriteDifficulty(newId, difficulty);
			WriteLength(newId, length);
			return newId;
		}

		static public uint ReadDifficulty(uint id) {
			uint newId = id >> SeedBits;
			return newId & MaxDifficulty;
		}

		static public uint WriteDifficulty(uint id, uint difficulty) {
			uint newId = id & ~(MaxSeed << SeedBits);
			newId &= difficulty << SeedBits;
			return newId;
		}


		static public uint ReadLength(uint id) {
			uint newId = id >> SeedBits + DifficultyBits;
			return newId & MaxLength;
		}

		static public uint WriteLength(uint id, uint length) {
			uint newId = id & ~(MaxSeed << (SeedBits + DifficultyBits));
			newId &= length << (SeedBits + DifficultyBits);
			return newId;
		}


		static public uint ReadSeed(uint id) {
			return id & MaxSeed;
		}

		static public uint WriteSeed(uint id, uint seed) {
			if (seed > MaxSeed) {
				throw new Exception("seed is too large, max " + MaxSeed);
			}
			else {
				uint newId = id & ~MaxSeed;
				newId &= seed;
				return newId;
			}
		}

	}
}
