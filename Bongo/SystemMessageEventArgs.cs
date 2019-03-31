using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bongo {
	public class SystemMessageEventArgs : EventArgs {
		public string Message;

		public SystemMessageEventArgs(string message) {
			Message = message;
		}

	}
}
