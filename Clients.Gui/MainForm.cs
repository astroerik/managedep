/*
Copyright (c) 2005-2008, Schley Andrew Kutz <akutz@lostcreations.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
    this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice,
    this list of conditions and the following disclaimer in the documentation
    and/or other materials provided with the distribution.
    * Neither the name of l o s t c r e a t i o n s nor the names of its 
    contributors may be used to endorse or promote products derived from this 
    software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.IO;
using System.Text;
using System.Drawing;
using Sudowin.Common;
using System.Reflection;
using System.Windows.Forms;
using System.ComponentModel;
using System.Runtime.Remoting;
using System.Security.Principal;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Sudowin.Clients.Gui
{
	public partial class MainForm : Form
	{
		/// <summary>
		///		The secure channel used to communicate with the sudo server.
		/// </summary>
		private ISudoServer m_isudo_server;

		public MainForm()
        {
            InitializeComponent();

			#region configure remoting

			// get path to the actual exe
			Uri uri = new Uri(
				Assembly.GetExecutingAssembly().GetName().CodeBase );

            // configure remoting channels and objects
			RemotingConfiguration.Configure( uri.LocalPath + ".config", true );

			// get the server object that is used to elevate
			// privleges and act as a backend store for
			// caching credentials

			// get an array of the registered well known client urls
			WellKnownClientTypeEntry[] wkts =
				RemotingConfiguration.GetRegisteredWellKnownClientTypes();

            // loop through the list of well known clients until
			// the SudoServer object is found
			for ( int x = 0; x < wkts.Length && m_isudo_server == null; ++x )
			{
				m_isudo_server = Activator.GetObject( typeof( ISudoServer ),
					wkts[ x ].ObjectUrl ) as ISudoServer;
			}

            bool is_sudo_server_comm_link_open = false;
			try
			{
				is_sudo_server_comm_link_open = m_isudo_server.IsConnectionOpen;
			}
			catch
			{
			}

			#endregion

            // finding the executable -- special cases
			string[] args = Environment.GetCommandLineArgs();
			if ( args.Length > 1 )
			{
                string env_args = string.Join( " ", args );
				string sudoed_cmd_string = args[ 1 ];

				// msi files
				Regex rx_msi = new Regex( @".*/package (?<sudoedCmd>.*\.msi).*" );
				Match mt_msi = rx_msi.Match( env_args );
				if ( mt_msi.Success )
				{
					sudoed_cmd_string = mt_msi.Groups[ "sudoedCmd" ].Value;
				}

				// display the file being sudoed
				FileInfo sudoed_cmd = new FileInfo( sudoed_cmd_string );
				m_lbl_sudoed_cmd.Text = sudoed_cmd.Name;
                m_picbox_user_icon.Image =
					Icon.ExtractAssociatedIcon( sudoed_cmd.FullName ).ToBitmap();
            }
            else
            {
                string icon_directory_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    @"Microsoft\User Account Pictures\Default Pictures");
                if (Directory.Exists(icon_directory_path))
                {
                    string[] icon_file_paths = Directory.GetFiles(icon_directory_path);
                    Random r = new Random();
                    int icon_file_paths_index = r.Next(0, icon_file_paths.Length - 1);
                    m_picbox_user_icon.Load(icon_file_paths[icon_file_paths_index]);
                }
            }

			// check to see if the Sudowin service is stopped
			System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController( "Sudowin" );
			if ( sc.Status == System.ServiceProcess.ServiceControllerStatus.Stopped )
			{
				m_txtbox_password.Enabled = false;
				m_btn_ok.Enabled = false;
				m_lbl_warning.Text = "Sudowin service is stopped";
			}
			else if ( !is_sudo_server_comm_link_open )
			{
				m_txtbox_password.Enabled = false;
				m_btn_ok.Enabled = false;
				m_lbl_warning.Text = "Sudowin service is dead to you";
			}

            //enable button if we can potentially update sudoers.xml
            if (File.Exists(@"C:\ProgramData\dell\KACE\AMP_CONNECTED") && m_btn_ok.Enabled)
            {
                updateSudoers.Enabled = true;
                sendUpdateSudoersSignal(false);
            }
		}

		private void btnOk_Click( object sender, EventArgs e )
		{
			
			// holds the result of the sudo invocation
			SudoResultTypes srt;

			// get the password
			string password = m_txtbox_password.Text;

			// get the command path and arguments
			string[] args = Environment.GetCommandLineArgs();
			string cmd_path = args[ 1 ];
			string cmd_args = string.Join( " ", args, 2, args.Length - 2 );

			// invoke sudo
            try
            {
                srt = m_isudo_server.Sudo(password, cmd_path, cmd_args);
            }
            catch (SudoException ex)
            {
                m_lbl_warning.Text = ex.Message;
                srt = ex.SudoResultType;
            }

            // clear password
            m_txtbox_password.Text = string.Empty;
            switch (srt)
			{
				case SudoResultTypes.SudoK :
				{
					this.Close();
					break;
				}
				case SudoResultTypes.InvalidLogon:
				{
					break;
				}
				default:
				{
					m_txtbox_password.Enabled = false;
					m_btn_ok.Enabled = false;
					break;
				}
			}
		}

		private void m_btn_cancel_Click( object sender, EventArgs e )
		{
			Application.Exit();
		}

        private void MainForm_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void sendUpdateSudoersSignal(bool bForce=false)
        {
            bool enabled = updateSudoers.Enabled;
            bool bUpdated = false;
            try
            {
                updateSudoers.Text = "...";
                m_btn_ok.Enabled = false;
                updateSudoers.Enabled = false;
                bUpdated = m_isudo_server.UpdateSudoers(bForce);
            }
            finally
            {
                updateSudoers.Text = bUpdated ? "Updated" : "Update";
                updateSudoers.Enabled = enabled;
                m_btn_ok.Enabled = true;
                m_txtbox_password.Enabled = true;
            }
        }

        private void updateSudoers_Click(object sender, EventArgs e)
        {
            sendUpdateSudoersSignal(true);
        }
	}
}