using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Bongo {
	class Board {
		public string Title;
		public string Description;
		public string Version;

		public string Errors;

		public List<Goal> Goals;

		public bool Premade;


		/// <summary>
		/// Generate new bingo board
		/// </summary>
		/// <param name="path"></param>
		/// <param name="id"></param>
		/// <returns></returns>
		public void Generate(string path, int id) {
			XmlDocument goalsDoc = new XmlDocument();
			goalsDoc.Load(path);

			SetInfo(goalsDoc);
			SetGoals(goalsDoc);

			if (Premade) {
				return;
			}

			// Get data from the board id
			int seed = BoardIdentifier.ReadSeed(id);
			int difficulty = BoardIdentifier.ReadDifficulty(id);
			int length = BoardIdentifier.ReadLength(id);

			Random rand = new Random((int)seed);

			// Select the goals for the board by picking a random length & difficulty, then picking the nearest goal
			Goals = ChooseGoals(Goals, seed, difficulty, length);
			return;
		}

		/// <summary>
		/// Choose goals from potentialGoals based on other parameters
		/// </summary>
		/// <param name="potentialGoals"></param>
		/// <param name="seed"></param>
		/// <param name="difficulty"></param>
		/// <param name="length"></param>
		List<Goal> ChooseGoals(List<Goal> potentialGoals, int seed, int difficulty, int length) {
			Goal[] selectedGoals = new Goal[25];
			Random rand = new Random(seed);

			// shuffle the goals list
			List<Goal> shuffledGoals = new List<Goal>(potentialGoals.Count);
			while (potentialGoals.Count > 0) {
				int index = rand.Next(potentialGoals.Count);
				shuffledGoals.Add(potentialGoals[index]);
				potentialGoals.RemoveAt(index);
			}

			// create a list of tiles and shuffle them
			List<int> tiles = new List<int>(Enumerable.Range(0, 25));
			List<int> shuffledTiles = new List<int>();

			while (tiles.Count > 0) {
				int index = rand.Next(tiles.Count);
				shuffledTiles.Add(tiles[index]);
				tiles.RemoveAt(index);
			}

			for (int i = 0; i < 25; i++) {
				double x = PointGenerator.Random(seed, difficulty, 3);
				double y = PointGenerator.Random(seed, length, 5);
				// TODO: dont make the above 3 & 5 hard coded

				double nearestDistance = double.MaxValue;
				Goal nearestGoal = shuffledGoals[0];

				foreach (Goal goal in shuffledGoals) {
					if (difficulty == 0) {

					}
					double distance = S.Math.Distance2D(x, y, goal.Difficulty, goal.Length);
					if (distance < nearestDistance) {
						nearestDistance = distance;
						nearestGoal = goal;
					}
					if (distance == 0) {
						break;
					}
				}

				int indexToAddGoal = shuffledTiles[i];
				selectedGoals[indexToAddGoal] = nearestGoal;
				shuffledGoals.Remove(nearestGoal);
			}
			return selectedGoals.ToList();
		}

		/// <summary>
		/// Sets the title, description & version of BingoSet set based on XmlDocument doc
		/// </summary>
		/// <param name="set"></param>
		/// <param name="doc"></param>
		void SetInfo(XmlDocument doc) {
			XmlNode info = doc.DocumentElement.SelectSingleNode("info");

			// title
			if (info.Attributes["title"] != null) {
				Title = info.Attributes["title"].InnerText;
			}
			else {
				Title = "Untitled goals set";
			}
			// description
			if (info.Attributes["description"] != null) {
				Description = info.Attributes["description"].InnerText;
			}
			else {
				Description = "No description available";
			}
			// premade
			if (info.Attributes["premade"] != null) {
				bool.TryParse(info.Attributes["premade"].InnerText, out Premade);
			}

			XmlNode version = doc.DocumentElement.SelectSingleNode("version");
			// version
			if (version.Attributes["version"] != null) {
				Version = version.Attributes["version"].InnerText;
			}
			else {
				Version = "Unknown version";
			}
		}

		/// <summary>
		/// Adds all the goals found in XmlDocument doc to BingoSet set's list of goals
		/// </summary>
		/// <param name="set"></param>
		/// <param name="doc"></param>
		void SetGoals(XmlDocument doc) {
			int unnamedGoals = 0;
			int undescribedGoals = 0;
			string errors = string.Empty;

			Goals = new List<Goal>();
			XmlNodeList goalsAll = doc.DocumentElement.GetElementsByTagName("goal");
			foreach (XmlNode goal in goalsAll) {
				string title = string.Empty;
				string description = string.Empty;

				float length = -1;
				float difficulty = -1;

				// title
				if (goal.Attributes["name"] != null) {
					title = goal.Attributes["name"].InnerText;
				}
				else {
					unnamedGoals++;
					continue;
				}
				// description
				if (goal.Attributes["description"] != null) {
					description = goal.Attributes["description"].InnerText;
				}
				else {
					undescribedGoals++;
					description = "<No description set>";
				}
				// length
				if (goal.Attributes["length"] != null) {
					if (!float.TryParse(goal.Attributes["length"].InnerText, out length)) {
						errors += string.Format("Goal '{0}' has an invalid length value\n", title);
					}
				}
				else {
					errors += string.Format("Goal '{0}' has no length value\n", title);
				}
				// difficulty
				if (goal.Attributes["difficulty"] != null) {
					if (!float.TryParse(goal.Attributes["difficulty"].InnerText, out difficulty)) {
						errors += string.Format("Goal '{0}' has an invalid difficulty value\n", title);
					}
				}
				else {
					errors += string.Format("Goal '{0}' has no difficulty value\n", title);
				}

				Goals.Add(new Goal(title, description, length, difficulty));
			}
			if (unnamedGoals > 0) {
				errors += string.Format("Error: {0} goals have no title\n", unnamedGoals);
			}
			if (undescribedGoals > 0) {
				errors += string.Format("{0} goals have no description\n", undescribedGoals);
			}
			if (goalsAll.Count < 25) {
				errors += "Error: Not enough goals\n";
			}
			Errors = errors;
		}

	}
}
