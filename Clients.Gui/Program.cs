using System;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Sudowin.Clients.Gui
{
	static class Program
	{
		/// <summary>
		///		The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault( false );
			MainForm mf = new MainForm();
			if ( !mf.ExitedEarlyWithCachedCredentials )
				Application.Run( mf );
		}
	}
}