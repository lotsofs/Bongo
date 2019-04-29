using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using System.IO;
using System.Runtime.InteropServices;
using System.Net;
using System.Net.Sockets;

namespace Bongo {
	public partial class BongoForm : Form {

		Network _network = new Network();
		Board _currentBoard = new Board();

		Label[] labels = new Label[25];
		Label[] labelsSpectate = new Label[25];

		int _selectedLabelIndex = 25;
		int _selectedLabelIndexSpectate = 25;
		bool _bingoActive = false;

		// TODO: Make separate color script
		Color[] colors = new Color[] { Color.LightGray, Color.DodgerBlue, Color.LimeGreen, Color.Red };
		string[] icons = new string[] { "", "", ":)", "X" };
		Color[] colorsExtended = new Color[] { Color.LightGray, Color.DodgerBlue, Color.LimeGreen, Color.Red, Color.DarkOrchid, Color.Goldenrod };

		Color[] colorsRed = new Color[] { Color.LightGray, Color.FromArgb(255, 0, 0), Color.FromArgb(255, 0 , 0), Color.FromArgb(255, 0, 0) };
		Color[] colorsYellow = new Color[] { Color.LightGray, Color.FromArgb(255, 255, 0), Color.FromArgb(255, 255, 0), Color.FromArgb(255, 255, 0) };
		Color[] colorsGreen = new Color[] { Color.LightGray, Color.FromArgb(0, 255, 0), Color.FromArgb(0, 255, 0), Color.FromArgb(0, 255, 0) };
		Color[] colorsBlue = new Color[] { Color.LightGray, Color.FromArgb(0, 255, 255), Color.FromArgb(0, 255, 255), Color.FromArgb(0, 255, 255) };
		Color[][] colorsList;


		// TODO: Move all of this to hotkeys.cs?
		private Hotkeys hotkeys;
		IntPtr thisWindow;

		public uint modifierU = 0;
		public uint modifierD = 0;
		public uint modifierL = 0;
		public uint modifierR = 0;
		public uint modifierP = 0;
		public uint modifierN = 0;
		public uint modifierH = 0;
		public uint modifierT = 0;

		Dictionary<string, uint> KeyCodesCustom = new Dictionary<string, uint> {
			{"None", 0x00 },
			{ "Backspace", 0x08 },
			{"Tab", 0x09},
			{"Enter", 0x0D},
			{"Pause", 0x13},
			{"CapsLock", 0x14},
			{"Escape", 0x1B},
			{"Space", 0x20},
			{"PageUp", 0x21},
			{"PageDn", 0x22},
			{"End", 0x23},
			{"Home", 0x24},
			{"LeftArrow", 0x25},
			{"UpArrow", 0x26},
			{"RightArrow", 0x27},
			{"DownArrow", 0x28},
			{"PrtScr", 0x2C},
			{"Insert", 0x2D},
			{"Delete", 0x2E},
			{"0", 0x30},
			{"1", 0x31},
			{"2", 0x32},
			{"3", 0x33},
			{"4", 0x34},
			{"5", 0x35},
			{"6", 0x36},
			{"7", 0x37},
			{"8", 0x38},
			{"9", 0x39},

			{"A", 0x41},
			{"B", 0x42},
			{"C", 0x43},
			{"D", 0x44},
			{"E", 0x45},
			{"F", 0x46},
			{"G", 0x47},
			{"H", 0x48},
			{"I", 0x49},
			{"J", 0x4A},
			{"K", 0x4B},
			{"L", 0x4C},
			{"M", 0x4D},
			{"N", 0x4E},
			{"O", 0x4F},
			{"P", 0x50},
			{"Q", 0x51},
			{"R", 0x52},
			{"S", 0x53},
			{"T", 0x54},
			{"U", 0x55},
			{"V", 0x56},
			{"W", 0x57},
			{"X", 0x58},
			{"Y", 0x59},
			{"Z", 0x5A},

			{"Apps", 0x5D},
			{"KP_0", 0x60},
			{"KP_1", 0x61},
			{"KP_2", 0x62},
			{"KP_3", 0x63},
			{"KP_4", 0x64},
			{"KP_5", 0x65},
			{"KP_6", 0x66},
			{"KP_7", 0x67},
			{"KP_8", 0x68},
			{"KP_9", 0x69},
			{"KP_*", 0x6A},
			{"KP_+", 0x6B},
			{"KP_-", 0x6D},
			{"KP_.", 0x6E},
			{"KP_/", 0x6F},


			{"F1", 0x70},
			{"F2", 0x71},
			{"F3", 0x72},
			{"F4", 0x73},
			{"F5", 0x74},
			{"F6", 0x75},
			{"F7", 0x76},
			{"F8", 0x77},
			{"F9", 0x78},
			{"F10", 0x79},
			{"F11", 0x7A},
			{"F12", 0x7B},
			{"F13", 0x7C},
			{"F14", 0x7D},
			{"F15", 0x7E},
			{"F16", 0x7F},
			{"F17", 0x80},
			{"F18", 0x81},
			{"F19", 0x82},
			{"F20", 0x83},
			{"F21", 0x84},
			{"F22", 0x85},
			{"F23", 0x86},
			{"F24", 0x87},

			{"NumLock", 0x90 },
			{"ScrollLock",0x91 },

			{":",0xBA },
			{"+",0xBB },
			{",",0xBC },
			{"-",0xBD },
			{".",0xBE },
			{"/",0xBF },
			{"~",0xC0 },

			{"[",0xDB },
			{"\\",0xDC },
			{"]",0xDD },
			{"'",0xDE },

		}; // Todo: Import this mess from S Keys 9

		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(String sClassName, String sAppName);

		public BongoForm() {
			InitializeComponent();
		}

		private void Form1_Load(object sender, EventArgs e) {
			RegisterNetworkEvents();
			//Lookup goal directories and put them in the goal selector dropdown
			string[] files = Directory.GetDirectories(@"Goals\");
			for (int i = 0; i < files.Length; i++) {
				BottomFileBox.Items.Add(files[i].Substring(6));
				files[i].Substring(7);
			}
			BottomFileBox.SelectedItem = BottomFileBox.Items[0];
			//Register hotkeys
			thisWindow = FindWindow(null, "Bungo");
			hotkeys = new Hotkeys(thisWindow);
			#region hotkey combobox content assignment
			comboU.DataSource = new BindingSource(KeyCodesCustom, null);
			comboU.DisplayMember = "Key";
			comboU.ValueMember = "Value";
			comboD.DataSource = new BindingSource(KeyCodesCustom, null);
			comboD.DisplayMember = "Key";
			comboD.ValueMember = "Value";
			comboL.DataSource = new BindingSource(KeyCodesCustom, null);
			comboL.DisplayMember = "Key";
			comboL.ValueMember = "Value";
			comboR.DataSource = new BindingSource(KeyCodesCustom, null);
			comboR.DisplayMember = "Key";
			comboR.ValueMember = "Value";
			comboP.DataSource = new BindingSource(KeyCodesCustom, null);
			comboP.DisplayMember = "Key";
			comboP.ValueMember = "Value";
			comboN.DataSource = new BindingSource(KeyCodesCustom, null);
			comboN.DisplayMember = "Key";
			comboN.ValueMember = "Value";
			comboH.DataSource = new BindingSource(KeyCodesCustom, null);
			comboH.DisplayMember = "Key";
			comboH.ValueMember = "Value";
			comboT.DataSource = new BindingSource(KeyCodesCustom, null);
			comboT.DisplayMember = "Key";
			comboT.ValueMember = "Value";
			#endregion	
			ConfigXMLRead();
			hotkeys.RegisterHotkeys((uint)comboT.SelectedValue, modifierT);
			HotkeyEnabledCheckbox_CheckedChanged(null, null);
			//Put all 25 labels in the array
			int labelIndex = 0;
			foreach (Label label in BoardTableLayoutPanel.Controls) {
				labels[labelIndex] = label;
				labelIndex++;
			}
			SpectatorModeSetup();
		}

		private void ConfigXMLRead() {
			// Load config
			XmlDocument docConfig = new XmlDocument();
			docConfig.Load(@"BingoConfig.xml");
			// Restore last game
			XmlNode node = docConfig.DocumentElement.SelectSingleNode("lastgame");
			BottomFileBox.SelectedItem = node.InnerText;
			// Restore hotkeys
			XmlNode node4 = docConfig.DocumentElement.SelectSingleNode("hotkeysenabled");
			HotkeyEnabledCheckbox.Checked = node4.InnerText == "true";
			XmlNode node2 = docConfig.DocumentElement.SelectSingleNode("hotkeys");
			comboU.SelectedValue = UInt32.Parse(node2.Attributes["U"].InnerText);
			comboD.SelectedValue = UInt32.Parse(node2.Attributes["D"].InnerText);
			comboL.SelectedValue = UInt32.Parse(node2.Attributes["L"].InnerText);
			comboR.SelectedValue = UInt32.Parse(node2.Attributes["R"].InnerText);
			comboP.SelectedValue = UInt32.Parse(node2.Attributes["P"].InnerText);
			comboN.SelectedValue = UInt32.Parse(node2.Attributes["N"].InnerText);
			comboH.SelectedValue = UInt32.Parse(node2.Attributes["H"].InnerText);
			comboT.SelectedValue = UInt32.Parse(node2.Attributes["T"].InnerText);
			// Check to see if restored properly
			if (comboU.SelectedIndex == -1) comboU.SelectedIndex = 0;
			if (comboD.SelectedIndex == -1) comboD.SelectedIndex = 0;
			if (comboL.SelectedIndex == -1) comboL.SelectedIndex = 0;
			if (comboR.SelectedIndex == -1) comboR.SelectedIndex = 0;
			if (comboP.SelectedIndex == -1) comboP.SelectedIndex = 0;
			if (comboN.SelectedIndex == -1) comboN.SelectedIndex = 0;
			if (comboH.SelectedIndex == -1) comboH.SelectedIndex = 0;
			if (comboT.SelectedIndex == -1) comboT.SelectedIndex = 0;
			// Restore modifiers
			XmlNode node3 = docConfig.DocumentElement.SelectSingleNode("modifiers");

			altU.Checked = Int32.Parse(node3.Attributes["U"].InnerText) % 2 == 1;
			ctrlU.Checked = Int32.Parse(node3.Attributes["U"].InnerText) % 4 >= 2;
			shiftU.Checked = Int32.Parse(node3.Attributes["U"].InnerText) % 8 >= 4;
			winU.Checked = Int32.Parse(node3.Attributes["U"].InnerText) >= 8;

			altD.Checked = Int32.Parse(node3.Attributes["D"].InnerText) % 2 == 1;
			ctrlD.Checked = Int32.Parse(node3.Attributes["D"].InnerText) % 4 >= 2;
			shiftD.Checked = Int32.Parse(node3.Attributes["D"].InnerText) % 8 >= 4;
			winD.Checked = Int32.Parse(node3.Attributes["D"].InnerText) >= 8;

			altL.Checked = Int32.Parse(node3.Attributes["L"].InnerText) % 2 == 1;
			ctrlL.Checked = Int32.Parse(node3.Attributes["L"].InnerText) % 4 >= 2;
			shiftL.Checked = Int32.Parse(node3.Attributes["L"].InnerText) % 8 >= 4;
			winL.Checked = Int32.Parse(node3.Attributes["L"].InnerText) >= 8;

			altR.Checked = Int32.Parse(node3.Attributes["R"].InnerText) % 2 == 1;
			ctrlR.Checked = Int32.Parse(node3.Attributes["R"].InnerText) % 4 >= 2;
			shiftR.Checked = Int32.Parse(node3.Attributes["R"].InnerText) % 8 >= 4;
			winR.Checked = Int32.Parse(node3.Attributes["R"].InnerText) >= 8;

			altP.Checked = Int32.Parse(node3.Attributes["P"].InnerText) % 2 == 1;
			ctrlP.Checked = Int32.Parse(node3.Attributes["P"].InnerText) % 4 >= 2;
			shiftP.Checked = Int32.Parse(node3.Attributes["P"].InnerText) % 8 >= 4;
			winP.Checked = Int32.Parse(node3.Attributes["P"].InnerText) >= 8;

			altN.Checked = Int32.Parse(node3.Attributes["N"].InnerText) % 2 == 1;
			ctrlN.Checked = Int32.Parse(node3.Attributes["N"].InnerText) % 4 >= 2;
			shiftN.Checked = Int32.Parse(node3.Attributes["N"].InnerText) % 8 >= 4;
			winN.Checked = Int32.Parse(node3.Attributes["N"].InnerText) >= 8;

			altH.Checked = Int32.Parse(node3.Attributes["H"].InnerText) % 2 == 1;
			ctrlH.Checked = Int32.Parse(node3.Attributes["H"].InnerText) % 4 >= 2;
			shiftH.Checked = Int32.Parse(node3.Attributes["H"].InnerText) % 8 >= 4;
			winH.Checked = Int32.Parse(node3.Attributes["H"].InnerText) >= 8;

			altT.Checked = Int32.Parse(node3.Attributes["T"].InnerText) % 2 == 1;
			ctrlT.Checked = Int32.Parse(node3.Attributes["T"].InnerText) % 4 >= 2;
			shiftT.Checked = Int32.Parse(node3.Attributes["T"].InnerText) % 8 >= 4;
			winT.Checked = Int32.Parse(node3.Attributes["T"].InnerText) >= 8;
		}

		// Write to the config XML that this is the last game the player played (is this needed?)
		private void SaveLastGameData() {
			XmlDocument doc = new XmlDocument();
			doc.Load(@"BingoConfig.xml");
			XmlNode node = doc.DocumentElement.SelectSingleNode("lastgame");
			node.InnerText = BottomFileBox.SelectedItem.ToString();
			doc.Save(@"BingoConfig.xml");
		}

		#region board display
		/// <summary>
		/// Hides or unhides the board
		/// </summary>
		/// <param name="hide"></param>
		private void HideBoard(bool hide) {
			foreach (Label l in labels) {
				l.Visible = !hide;
				//tabControl2.Visible = !hide;
			}
			BoardUnhideButton.Visible = hide;
			SpectateUnhideButton.Visible = hide;
			BoardVersionDisplay.Visible = hide;
		}

		// TODO: This does two different things, fix.
		private int GenerateUID() {
			int uid;
			// Specific uID entered, so set all the sliders and checkboxes in the settings to match
			if (!string.IsNullOrEmpty(LoadUidBox.Text)) {
				uid = int.Parse(LoadUidBox.Text);
				BottomUidBox.Text = uid.ToString();

				// Set seed settings
				int seed = (BoardIdentifier.ReadSeed(uid));
				CreateSeedBox.Text = seed.ToString();

				// Set difficulty settings
				int difficulty = (BoardIdentifier.ReadDifficulty(uid));
				if (difficulty == 0) {
					CreateDifficultyDisregardBox.Checked = true;
					CreateDifficultyBar.Enabled = false;
				}
				else {
					CreateDifficultyDisregardBox.Checked = false;
					CreateDifficultyBar.Enabled = true;
					CreateDifficultyBar.Value = difficulty;
				}
				// Set length settings
				int length = (BoardIdentifier.ReadLength(uid));
				if (length == 0) {
					CreateLengthDisregardBox.Checked = true;
					CreateLengthBar.Enabled = false;
				}
				else {
					CreateLengthDisregardBox.Checked = false;
					CreateLengthBar.Enabled = true;
					CreateLengthBar.Value = length;
				}
			}
			// User creates a new board using the settings, so turn these settings into an uID.
			else {
				int difficulty = CreateDifficultyBar.Value;
				difficulty *= CreateDifficultyDisregardBox.Checked ? 0 : 1;
				int length = CreateLengthBar.Value;
				length *= CreateLengthDisregardBox.Checked ? 0 : 1;
				int seed;

				if (string.IsNullOrEmpty(CreateSeedBox.Text)) {
					Random randSeed = new Random();	// Generates a seed
					seed = randSeed.Next(0, 10000);
				}
				else {
					seed = Math.Abs(int.Parse(CreateSeedBox.Text));
				}
				uid = BoardIdentifier.GenerateNew(seed, difficulty, length);
				BottomUidBox.Text = uid.ToString();
			}
			return uid;
		}

		/// <summary>
		/// Updates the large boxes of text showing descriptions, titles, rules, etc.
		/// </summary>
		private void UpdateRulesInfoTexts() {
			string infoText = string.Format(
				"{3}----------\n\nGoals title: {0}\nVersion: {2}\n\nGoals description:\n{1}",
				_currentBoard.Title,
				_currentBoard.Description,
				_currentBoard.Version,
				_currentBoard.Errors
			);
			infoText = infoText.Replace("\n", Environment.NewLine);
			BoardInfoTextField.Text = infoText;
			RulesInfoTextField.Text = infoText;

			string nameText = string.Format(
				"{0}\nGoals version: {1}\nBongo version: {2}",
				_currentBoard.Title,
				_currentBoard.Version,
				HelpVersionLabel.Text
			);
			nameText = nameText.Replace("\n", Environment.NewLine);
			BoardVersionDisplay.Text = nameText;
		}

		/// <summary>
		/// Make a new board and display it
		/// </summary>
		private void GenerateBoard() {
			ResetBoardColors();
			_bingoActive = true;

			// TODO: Load XML using browser prompt, work on UID
			_currentBoard.Generate(@"Goals\\" + BottomFileBox.SelectedItem + "\\Goals.xml", GenerateUID());

			UpdateRulesInfoTexts();

			for (int i = 0; i < 25; i++) {
				labels[i].Text = _currentBoard.Goals[i].Name;
			}

			AssembleSpectateBoard();
		}

		#endregion

		#region spectator tiles

		/// <summary>
		/// Puts all 25 spectate board tiles into an array
		/// </summary>
		private void SpectatorModeSetup() {
			colorsList = new Color[][] { colors, colorsRed, colorsYellow, colorsGreen, colorsBlue };
			int labelIndex = 0;
			foreach (TableLayoutPanel tlp in SpectateTableLayoutPanel.Controls) {
				labelsSpectate[labelIndex] = (Label)tlp.Controls[0];
				labelIndex++;
			}
		}

		private void SpectateTile_Click(object sender, MouseEventArgs e) {
			Label clickedLabel = sender as Label;
			ClickSpectateTile(clickedLabel);
		}

		private void ClickSpectateTile(Label clickedLabel) {
			int clickedLabelIndex = Array.FindIndex(labelsSpectate, item => item == clickedLabel);
			// reset currently selected label
			if (_selectedLabelIndexSpectate != 25) {
				EnlargeTile((TableLayoutPanel)labelsSpectate[_selectedLabelIndexSpectate].Parent, false);
			}
			// select new
			_selectedLabelIndexSpectate = clickedLabelIndex;
			EnlargeTile((TableLayoutPanel)labelsSpectate[_selectedLabelIndexSpectate].Parent, true);

			if (_bingoActive) {
				SpectateInfoTextField.Text = _currentBoard.Goals[_selectedLabelIndexSpectate].Description;
			}
		}

		private void AssembleSpectateBoard() {
			int labelIndex = 0;
			foreach (TableLayoutPanel tlp in SpectateTableLayoutPanel.Controls) {
				foreach (Label label in tlp.Controls) {
					label.BackColor = colors[0];
					label.Text = icons[0];
				}
				tlp.Controls[0].Text = labels[labelIndex].Text;
				labelIndex++;
			}
		}
		
		#endregion

		#region select/change bingo board tiles

		/// <summary>
		/// Changes label's color to the next item in colors array
		/// </summary>
		/// <param name="label"></param>
		/// <param name="forward">whether to set it to the next or previous color in colors array</param>
		private void ChangeTileColor(bool forward) {
			Label label = labels[_selectedLabelIndex];
			int colorIndex = Array.FindIndex(colors, item => item == label.BackColor);
			if (forward) {
				label.BackColor = colors[(colorIndex + 1) % colors.Length];
			}
			else {
				label.BackColor = colors[(colorIndex + colors.Length - 1) % colors.Length];
			}
			SendBingoBoard();
		}

		/// <summary>
		/// Makes a tile larger and bold text, or resets it to normal if larger = false
		/// </summary>
		/// <param name="larger"></param>
		private void EnlargeTile(Label tile, bool larger) {
			if (larger) {
				Font selectedFont = new Font(tile.Font, FontStyle.Bold);
				tile.Font = selectedFont;
				Padding selectedPadding = new Padding(tile.Margin.All - 5);
				tile.Margin = selectedPadding;
			}
			else {
				Font regularFont = new Font(tile.Font, FontStyle.Regular);
				tile.Font = regularFont;
				Padding regularPadding = new Padding(tile.Margin.All + 5);
				tile.Margin = regularPadding;
			}
		}

		/// <summary>
		/// Makes a tile larger and bold text, or resets it to normal if larger = false
		/// </summary>
		/// <param name="larger"></param>
		private void EnlargeTile(TableLayoutPanel container, bool larger) {
			Label tile = (Label)container.Controls[0];
			if (larger) {
				Font selectedFont = new Font(tile.Font, FontStyle.Bold);
				tile.Font = selectedFont;
				Padding selectedPadding = new Padding(container.Margin.All - 5);
				container.Margin = selectedPadding;
			}
			else {
				Font regularFont = new Font(tile.Font, FontStyle.Regular);
				tile.Font = regularFont;
				Padding regularPadding = new Padding(container.Margin.All + 5);
				container.Margin = regularPadding;
			}
		}

		/// <summary>
		/// Upon clicking on a tile, either select it, or change its color if it already is.
		/// </summary>
		/// <param name="clickedLabel"></param>
		/// <param name="leftMouseButton"></param>
		private void ClickTile(Label clickedLabel, bool leftMouseButton) {
			int clickedLabelIndex = Array.FindIndex(labels, item => item == clickedLabel);
			if (_selectedLabelIndex == clickedLabelIndex) {
				ChangeTileColor(leftMouseButton);
			}
			else {
				// reset currently selected label
				if (_selectedLabelIndex != 25) {
					EnlargeTile(labels[_selectedLabelIndex], false);
				}
				// select new
				_selectedLabelIndex = clickedLabelIndex;
				EnlargeTile(labels[_selectedLabelIndex], true);

				if (_bingoActive) {
					BoardInfoTextField.Text = _currentBoard.Goals[_selectedLabelIndex].Description;
				}
			}
		}

		/// <summary>
		/// Moves a tile based on hotkeys
		/// </summary>
		/// <param name="amount">The amount of tiles to move right</param>
		private void SelectNextTile(int amount) {
			if (_selectedLabelIndex != 25) {
				EnlargeTile(labels[_selectedLabelIndex], false);
				_selectedLabelIndex = (_selectedLabelIndex + amount) % 25;
			}
			else {
				_selectedLabelIndex = 0;
			}
			EnlargeTile(labels[_selectedLabelIndex], true);
			if (_bingoActive) {
				BoardInfoTextField.Text = _currentBoard.Goals[_selectedLabelIndex].Description;
			}
		}

		/// <summary>
		/// Resets the board colors to all blank and deselects whatever label was selected
		/// </summary>
		private void ResetBoardColors() {
			foreach (Label label in labels) {
				label.BackColor = colors[(0)];
			}
			if (_selectedLabelIndex != 25) {
				EnlargeTile(labels[_selectedLabelIndex], false);
			}
			_selectedLabelIndex = 25;
		}
		#endregion

		#region hotkeys

		// Reads for global hotkey presses.
		protected override void WndProc(ref Message keyPressed) {
			if (keyPressed.Msg == 0x0312) {
				int key = keyPressed.WParam.ToInt32();
				if (key == 7) {		// Toggle hotkeys
					HotkeyEnabledCheckbox.Checked = !HotkeyEnabledCheckbox.Checked;
				}
				else if (!BoardUnhideButton.Visible) {
					switch (key) {
						default:
							break;
						case 0: //up
							SelectNextTile(20);
							break;
						case 1: //down
							SelectNextTile(5);
							break;
						case 2: //left
							SelectNextTile(24);
							break;
						case 3: //rght
							SelectNextTile(1);
							break;
						case 4: //colorback
							ChangeTileColor(false);
							break;
						case 5: //colornext
							ChangeTileColor(true);
							break;
					}
				}
				else if (key == 6) {		// Unhide board
					HideBoard(false);
				}

			}
			base.WndProc(ref keyPressed);
		}

		// Upon clicking the apply button, make the hotkeys work, and update the config file.
		private void HotkeyApplyButton_Click(object sender, EventArgs e) {
			hotkeys.UnregisterHotkeys(true);
			hotkeys.RegisterHotkeys((uint)comboU.SelectedValue, (uint)comboD.SelectedValue, (uint)comboL.SelectedValue, (uint)comboR.SelectedValue, (uint)comboP.SelectedValue, (uint)comboN.SelectedValue, (uint)comboH.SelectedValue, (uint)comboT.SelectedValue, modifierU, modifierD, modifierL, modifierR, modifierP, modifierN, modifierH, modifierT);

			XmlDocument doc = new XmlDocument();
			doc.Load(@"BingoConfig.xml");
			XmlNode node = doc.DocumentElement.SelectSingleNode("hotkeys");
			node.Attributes["U"].InnerText = comboU.SelectedValue.ToString();
			node.Attributes["D"].InnerText = comboD.SelectedValue.ToString();
			node.Attributes["L"].InnerText = comboL.SelectedValue.ToString();
			node.Attributes["R"].InnerText = comboR.SelectedValue.ToString();
			node.Attributes["P"].InnerText = comboP.SelectedValue.ToString();
			node.Attributes["N"].InnerText = comboN.SelectedValue.ToString();
			node.Attributes["H"].InnerText = comboH.SelectedValue.ToString();
			node.Attributes["T"].InnerText = comboT.SelectedValue.ToString();
			XmlNode node2 = doc.DocumentElement.SelectSingleNode("modifiers");
			node2.Attributes["U"].InnerText = (Convert.ToUInt32(ctrlU.Checked) * 2 + Convert.ToUInt32(altU.Checked) + Convert.ToUInt32(shiftU.Checked) * 4 + Convert.ToUInt32(winU.Checked) * 8).ToString();
			node2.Attributes["D"].InnerText = (Convert.ToUInt32(ctrlD.Checked) * 2 + Convert.ToUInt32(altD.Checked) + Convert.ToUInt32(shiftD.Checked) * 4 + Convert.ToUInt32(winD.Checked) * 8).ToString();
			node2.Attributes["L"].InnerText = (Convert.ToUInt32(ctrlL.Checked) * 2 + Convert.ToUInt32(altL.Checked) + Convert.ToUInt32(shiftL.Checked) * 4 + Convert.ToUInt32(winL.Checked) * 8).ToString();
			node2.Attributes["R"].InnerText = (Convert.ToUInt32(ctrlR.Checked) * 2 + Convert.ToUInt32(altR.Checked) + Convert.ToUInt32(shiftR.Checked) * 4 + Convert.ToUInt32(winR.Checked) * 8).ToString();
			node2.Attributes["P"].InnerText = (Convert.ToUInt32(ctrlP.Checked) * 2 + Convert.ToUInt32(altP.Checked) + Convert.ToUInt32(shiftP.Checked) * 4 + Convert.ToUInt32(winP.Checked) * 8).ToString();
			node2.Attributes["N"].InnerText = (Convert.ToUInt32(ctrlN.Checked) * 2 + Convert.ToUInt32(altN.Checked) + Convert.ToUInt32(shiftN.Checked) * 4 + Convert.ToUInt32(winN.Checked) * 8).ToString();
			node2.Attributes["H"].InnerText = (Convert.ToUInt32(ctrlH.Checked) * 2 + Convert.ToUInt32(altH.Checked) + Convert.ToUInt32(shiftH.Checked) * 4 + Convert.ToUInt32(winH.Checked) * 8).ToString();
			node2.Attributes["T"].InnerText = (Convert.ToUInt32(ctrlT.Checked) * 2 + Convert.ToUInt32(altT.Checked) + Convert.ToUInt32(shiftT.Checked) * 4 + Convert.ToUInt32(winT.Checked) * 8).ToString();
			doc.Save(@"BingoConfig.xml");
		}

		#region modifiercheckboxes
		private void ctrlU_CheckedChanged(object sender, EventArgs e) {
			modifierU = Convert.ToUInt32(ctrlU.Checked) * 2 + Convert.ToUInt32(altU.Checked) + Convert.ToUInt32(shiftU.Checked) * 4 + Convert.ToUInt32(winU.Checked) * 8;
		}

		private void ctrlD_CheckedChanged(object sender, EventArgs e) {
			modifierD = Convert.ToUInt32(ctrlD.Checked) * 2 + Convert.ToUInt32(altD.Checked) + Convert.ToUInt32(shiftD.Checked) * 4 + Convert.ToUInt32(winD.Checked) * 8;
		}

		private void ctrlL_CheckedChanged(object sender, EventArgs e) {
			modifierL = Convert.ToUInt32(ctrlL.Checked) * 2 + Convert.ToUInt32(altL.Checked) + Convert.ToUInt32(shiftL.Checked) * 4 + Convert.ToUInt32(winL.Checked) * 8;
		}

		private void ctrlR_CheckedChanged(object sender, EventArgs e) {
			modifierR = Convert.ToUInt32(ctrlR.Checked) * 2 + Convert.ToUInt32(altR.Checked) + Convert.ToUInt32(shiftR.Checked) * 4 + Convert.ToUInt32(winR.Checked) * 8;
		}

		private void ctrlP_CheckedChanged(object sender, EventArgs e) {
			modifierP = Convert.ToUInt32(ctrlP.Checked) * 2 + Convert.ToUInt32(altP.Checked) + Convert.ToUInt32(shiftP.Checked) * 4 + Convert.ToUInt32(winP.Checked) * 8;
		}

		private void ctrlN_CheckedChanged(object sender, EventArgs e) {
			modifierN = Convert.ToUInt32(ctrlN.Checked) * 2 + Convert.ToUInt32(altN.Checked) + Convert.ToUInt32(shiftN.Checked) * 4 + Convert.ToUInt32(winN.Checked) * 8;
		}

		private void ctrlH_CheckedChanged(object sender, EventArgs e) {
			modifierH = Convert.ToUInt32(ctrlH.Checked) * 2 + Convert.ToUInt32(altH.Checked) + Convert.ToUInt32(shiftH.Checked) * 4 + Convert.ToUInt32(winH.Checked) * 8;
		}

		private void ctrlT_CheckedChanged(object sender, EventArgs e) {
			modifierT = Convert.ToUInt32(ctrlT.Checked) * 2 + Convert.ToUInt32(altT.Checked) + Convert.ToUInt32(shiftT.Checked) * 4 + Convert.ToUInt32(winT.Checked) * 8;
		}
		#endregion

		#endregion

		#region form interactions

		/// <summary>
		/// Clicking on a bingo tile
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Tile_Click(object sender, MouseEventArgs e) {
			Label clickedLabel = sender as Label;
			ClickTile(clickedLabel, e.Button == MouseButtons.Left);
		}

		private void BottomGenerateButton_Click(object sender, EventArgs e) {
			HideBoard(BottomHideBoardCheck.Checked);
			SaveLastGameData();
			GenerateBoard();
			TabControlMain.SelectedTab = TabBoard;
		}

		private void BottomHideButton_Click(object sender, EventArgs e) {
			HideBoard(false);
		}

		private void Form1_FormClosed(object sender, FormClosedEventArgs e) {
			hotkeys.UnregisterHotkeys(true);
			SaveLastGameData();
		}

		private void CreateSeedBox_TestChanged(object sender, EventArgs e) {
			int dump;
			if (!int.TryParse(CreateSeedBox.Text, out dump)) {
				CreateSeedBox.Clear();
			}
			GenerateUID();
		}

		private void CreateDifficultyBar_Scroll(object sender, EventArgs e) {
			GenerateUID();
		}

		private void CreateLengthBar_Scroll(object sender, EventArgs e) {
			GenerateUID();
		}

		private void CreateLengthDisregardBox_CheckedChanged(object sender, EventArgs e) {
			CreateLengthBar.Enabled = !CreateLengthDisregardBox.Checked;
			GenerateUID();
		}

		private void CreateDifficultyDisregardBox_CheckedChanged(object sender, EventArgs e) {
			CreateDifficultyBar.Enabled = !CreateDifficultyDisregardBox.Checked;
			GenerateUID();
		}

		private void HotkeyEnabledCheckbox_CheckedChanged(object sender, EventArgs e) {
			if (!HotkeyEnabledCheckbox.Checked) {
				hotkeys.UnregisterHotkeys(false);
				BottomHotkeyLabel.Text = "Hotkeys DISABLED";
			}
			else {
				hotkeys.RegisterHotkeys((uint)comboU.SelectedValue, (uint)comboD.SelectedValue, (uint)comboL.SelectedValue, (uint)comboR.SelectedValue, (uint)comboP.SelectedValue, (uint)comboN.SelectedValue, (uint)comboH.SelectedValue, (uint)comboT.SelectedValue, modifierU, modifierD, modifierL, modifierR, modifierP, modifierN, modifierH, modifierT);
				BottomHotkeyLabel.Text = "Hotkeys enabled";
			}
		}

		#endregion

		#region online stuff

		#region receiving data

		/// <summary>
		/// Write a message in the textbox
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnSystemMessage(object sender, SystemMessageEventArgs e) {
			this.BeginInvoke(new MethodInvoker(() => {
				NetworkMessagebox.AppendText(e.Message);
				NetworkMessagebox.AppendText(Environment.NewLine);
			}));
		}

		/// <summary>
		/// Run the background worker
		/// TODO: Move this to network
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnConnectedToServer(object sender, EventArgs e) {
			backgroundWorkerReceive.RunWorkerAsync();
		}

		/// <summary>
		/// Received bingo board colors, set the appropriate bingo board
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnReceivedBingoBoard(object sender, BingoBoardEventArgs e) {
			for (int i = 0; i < 25; i++) {
				if (e.Player == 0) {
					continue;
				}
				int colorIndex = e.Board[i];
				labels[i].BackColor = colors[colorIndex];
				Label tag = (Label)SpectateTableLayoutPanel.Controls[i].Controls[e.Player];
				tag.BackColor = colorsList[e.Player][colorIndex];
				tag.Text = icons[colorIndex];
				//foreach (TableLayoutPanel tlp in SpectateTableLayoutPanel.Controls) {
				//	((Label)tlp.Controls[e.Player]).BackColor = colorsList[e.Player][colorIndex];
				//}
			}
		}

		// Todo: Show seed, version etc for each player here.
		void OnPlayerListUpdated(object sender, PlayerListEventArgs e) {
			string list = string.Empty;
			foreach (Player p in e.PlayerList) {
				list += string.Format("{0}: {1} ({2})\n", p.Id, p.Name, NetworkPlayerColor.Items[p.Color]);
			}
			list = list.Replace("\n", Environment.NewLine);
			this.BeginInvoke(new MethodInvoker(() => {
				NetworkPlayerInfoText.Text = list;
			}));
		}

		void OnServerShutdown(object sender, EventArgs e) {
			NetworkGameBox.Enabled = false;
			NetworkHostBox.Enabled = true;
			NetworkConnectBox.Enabled = true;
		}

		#endregion

		#region sending data

		/// <summary>
		/// Compile bingo board colors to int array
		/// </summary>
		private void SendBingoBoard() {
			if (!_network.Connected) {
				return;
			}
			// take the colors of the bingo board
			int[] colorsInt = new int[25];
			for (int i = 0; i < 25; i++) {
				colorsInt[i] = Array.FindIndex(colors, item => item == labels[i].BackColor);
			}
			_network.SendBingoBoard(colorsInt);
		}

		#endregion

		#region connection stuff (server/client)

		/// <summary>
		/// Registers events for networking
		/// </summary>
		void RegisterNetworkEvents() {
			_network.OnSystemMessage += OnSystemMessage;
			_network.OnConnectedToServer += OnConnectedToServer;
			_network.OnReceivedBingoBoard += OnReceivedBingoBoard;
			_network.OnPlayerListUpdated += OnPlayerListUpdated;
			_network.OnServerShutdown += OnServerShutdown;
		}

		/// <summary>
		/// When clicking the 'start server' button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NetworkServerStart_Click(object sender, EventArgs e) {
			int port = int.Parse(NetworkServerPortBox.Text);
			_network.StartServer(port);
			NetworkGameBox.Enabled = true;
			NetworkHostBox.Enabled = false;
			NetworkConnectBox.Enabled = false;
		}

		/// <summary>
		/// When clicking the 'connect to server' button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NetworkClientConnectButton_Click(object sender, EventArgs e) {
			IPAddress ip = IPAddress.Parse(NetworkClientIpBox.Text);
			int port = int.Parse(NetworkClientPortBox.Text);
			_network.ConnectToServer(ip, port);
			NetworkGameBox.Enabled = true;
			NetworkHostBox.Enabled = false;
			NetworkConnectBox.Enabled = false;
		}

		/// <summary>
		/// When clicking the 'disconnect' button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NetworkGameDisconnectButton_Click(object sender, EventArgs e) {
			_network.Disconnect();
		}

		/// <summary>
		/// Continuously checks to see if a client message is received
		/// TODO: Move this to network
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BackgroundWorkerReceive_DoWork(object sender, DoWorkEventArgs e) {
			while (_network.Connected) {
				_network.ReceiveResponse();
			}
		}

		#endregion

		#region chat

		/// <summary>
		/// When clicking 'send message' button
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void NetworkButtonSend_Click(object sender, EventArgs e) {
			if (textBoxSend.Text != string.Empty) {
				_network.SendChat(textBoxSend.Text);
				textBoxSend.Text = string.Empty;
			}
		}

		#endregion

		#region settings buttons

		private void NetworkPlayerNameBox_TextChanged(object sender, EventArgs e) {
			if (!string.IsNullOrEmpty(NetworkPlayerNameBox.Text)) {
				_network.SendName(NetworkPlayerNameBox.Text);
			}
		}

		private void NetworkPlayerColor_SelectedIndexChanged(object sender, EventArgs e) {
			_network.SendColor((byte)NetworkPlayerColor.SelectedIndex);
		}

		#endregion

		#endregion
	}
}
