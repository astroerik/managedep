using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Sudowin.Common;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

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

            string[] args = Environment.GetCommandLineArgs();
            string cmd_path = args[1];
            string cmd_args = string.Join(" ", args, 2, args.Length - 2);

            InvokeSudo(cmd_path, cmd_args);
            
    		Application.Exit(  );
		}

        private static void InvokeSudo(
            string commandPath,
            string commandArguments)
        {
            System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController("Sudowin");
            if (sc.Status == System.ServiceProcess.ServiceControllerStatus.Stopped)
            {
                MessageBox.Show("Sudowin service is stopped", "SudoWin",
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Stop);
                return;
            }

            #region configure remoting

            // get path to the actual exe
            Uri uri = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase);

            // configure remoting channels and objects
            RemotingConfiguration.Configure(uri.LocalPath + ".config", true);

            // get the server object that is used to elevate
            // privleges and act as a backend store for
            // caching credentials

            // get an array of the registered well known client urls
            WellKnownClientTypeEntry[] wkts = RemotingConfiguration.GetRegisteredWellKnownClientTypes();

            // loop through the list of well known clients until
            // the SudoServer object is found
            ISudoServer iss = null;
            for (int x = 0; x < wkts.Length && iss == null; ++x)
            {
                iss = Activator.GetObject(typeof(ISudoServer), wkts[x].ObjectUrl) as ISudoServer;
            }

            #endregion

            bool is_sudo_server_comm_link_open = false;
            try
            {
                is_sudo_server_comm_link_open = iss.IsConnectionOpen;
            }
            catch
            {
            }

            if (!is_sudo_server_comm_link_open)
            {
                MessageBox.Show("Sudowin service is dead to you", "SudoWin",
                                 MessageBoxButtons.OK,
                                 MessageBoxIcon.Stop);
                return;
            }

            // holds the result of the sudo invocation
            SudoResultTypes srt = SudoResultTypes.SudoError;

            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            try
            {
                iss.UpdateSudoers(true);
                srt = iss.Sudo(identity.Name, commandPath, commandArguments);
                if (srt == SudoResultTypes.SudoK || srt == SudoResultTypes.SudoKAdded)
                {
                    var exeName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    ProcessStartInfo startInfo = new ProcessStartInfo(commandPath);
                    startInfo.Arguments = commandArguments;
                    startInfo.Verb = "runas";
                    try
                    {
                        System.Diagnostics.Process.Start(startInfo);
                    }
                    catch (Exception)
                    {
                        //if the user cancels we don't really care, just unsudo them and exit
                    }
                }
            }
            catch (SudoException ex)
            {
                MessageBox.Show(ex.Message, "SudoWin",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Stop);
                srt = ex.SudoResultType;
            }
            catch (Exception)
            {
                MessageBox.Show("Unknown error occurred", "SudoWin",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Stop);
                srt = SudoResultTypes.SudoError;
            }
            finally
            {
                   
                try
                {
                    if (srt == SudoResultTypes.SudoKAdded)
                    {
                        iss.UnSudo(identity.Name);
                    }
                }
                catch (Exception ex) {
                    System.Console.WriteLine(ex.Message);
                }
            }
        }
	}
}