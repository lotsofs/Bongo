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

namespace Bongo {
	class Hotkeys {

		IntPtr _hWnd;

		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		public Hotkeys(IntPtr hWnd) {
			_hWnd = hWnd;
		}

		/// <summary>
		/// Register 'toggle hotkeys' hotkey to specified params
		/// </summary>
		/// <param name="hotkeyT"></param>
		/// <param name="modifierT"></param>
		public void RegisterHotkeys(uint hotkeyT, uint modifierT) {
			RegisterHotKey(_hWnd, 7, modifierT, hotkeyT); //Toggle hotkeys
		}

		/// <summary>
		/// Register specified hotkeys
		/// </summary>
		/// <param name="hotkeyU"></param>
		/// <param name="hotkeyD"></param>
		/// <param name="hotkeyL"></param>
		/// <param name="hotkeyR"></param>
		/// <param name="hotkeyP"></param>
		/// <param name="hotkeyN"></param>
		/// <param name="hotkeyH"></param>
		/// <param name="hotkeyT"></param>
		/// <param name="modifierU"></param>
		/// <param name="modifierD"></param>
		/// <param name="modifierL"></param>
		/// <param name="modifierR"></param>
		/// <param name="modifierP"></param>
		/// <param name="modifierN"></param>
		/// <param name="modifierH"></param>
		/// <param name="modifierT"></param>
		public void RegisterHotkeys(uint hotkeyU, uint hotkeyD, uint hotkeyL, uint hotkeyR, uint hotkeyP, uint hotkeyN, uint hotkeyH, uint hotkeyT, uint modifierU, uint modifierD, uint modifierL, uint modifierR, uint modifierP, uint modifierN, uint modifierH, uint modifierT) {
			RegisterHotKey(_hWnd, 0, modifierU, hotkeyU); //up
			RegisterHotKey(_hWnd, 1, modifierD, hotkeyD); //down
			RegisterHotKey(_hWnd, 2, modifierL, hotkeyL); //left
			RegisterHotKey(_hWnd, 3, modifierR, hotkeyR); //right
			RegisterHotKey(_hWnd, 4, modifierP, hotkeyP); //previous color
			RegisterHotKey(_hWnd, 5, modifierN, hotkeyN); //next color
			RegisterHotKey(_hWnd, 6, modifierH, hotkeyH); //special action
			RegisterHotKey(_hWnd, 7, modifierT, hotkeyT); //Toggle hotkeyhs
		}

		/// <summary>
		/// Unregister hotkeys
		/// </summary>
		/// <param name="includingToggle">Also unregister the 'toggle hotkeys' hotkey</param>
		public void UnregisterHotkeys(bool includingToggle) {
			UnregisterHotKey(_hWnd, 0);
			UnregisterHotKey(_hWnd, 1);
			UnregisterHotKey(_hWnd, 2);
			UnregisterHotKey(_hWnd, 3);
			UnregisterHotKey(_hWnd, 4);
			UnregisterHotKey(_hWnd, 5);
			UnregisterHotKey(_hWnd, 6);
			if (includingToggle) {
				UnregisterHotKey(_hWnd, 7);
			}
		}

	}
}
