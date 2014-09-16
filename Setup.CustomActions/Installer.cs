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
using System.Xml;
using System.Globalization;
using System.Windows.Forms;
using System.ComponentModel;
using System.DirectoryServices;
using System.Collections.Generic;
using System.Configuration.Install;
using System.Text.RegularExpressions;

namespace Sudowin.Setup.CustomActions
{
	[RunInstaller( true )]
	public partial class Installer : System.Configuration.Install.Installer
	{
		bool m_sudoers_group_already_exists = true;
        bool m_sudoers_file_already_exists = true;

		public Installer()
		{
			InitializeComponent();
		}

		public override void Install( System.Collections.IDictionary stateSaver )
		{
			base.Install( stateSaver );

			string target_dir = this.Context.Parameters[ "TargetDir" ];
			string server_config_path = string.Format( @"{0}Server\Sudowin.Server.exe.config",
				target_dir );

			#region Create the sudoers group

			// create a group called Sudoers on the local machine if it
			// does not exist
			DirectoryEntry de = new DirectoryEntry( string.Format( "WinNT://{0},computer",
				Environment.MachineName ) );
			DirectoryEntry grp = null;
			try
			{
				grp = de.Children.Find( "Sudoers", "group" );
			}
			catch
			{
			}

			if ( grp == null )
			{
				m_sudoers_group_already_exists = false;
				grp = de.Children.Add( "Sudoers", "group" );
				grp.Properties[ "description" ].Value = "Members in this group have the required " +
					"privileges to initiate secure communication channels with the sudo server.";
				grp.CommitChanges();
			}

			grp.Close();
			de.Close();

			#endregion

			// TODO: ask what users should be sudoers and add them to the group and sudoers.xml file

			#region Edit the Sudowin.Server.exe.config file

			const string XpathTranslateFormat =
				"translate({0},'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz')";

			// throw an exception if the xml file is not found
			if ( !System.IO.File.Exists( server_config_path ) )
				throw new System.IO.FileNotFoundException(
					"xml file not found", server_config_path );

			// create a xmlreadersettings object
			// to specify how to read in the file
			XmlReaderSettings svr_cfg_xrs = new XmlReaderSettings();
			svr_cfg_xrs.CloseInput = true;
			svr_cfg_xrs.IgnoreComments = false;

			// read in the file
			XmlReader svr_cfg_xr = XmlReader.Create( server_config_path, svr_cfg_xrs );

			// load the xml reader into the xml document.
			XmlDocument svr_cfg_xml_doc = new XmlDocument();
			svr_cfg_xml_doc.Load( svr_cfg_xr );

			// close the xmlreader
			svr_cfg_xr.Close();

			// create the namespace manager using the xml file name table
			XmlNamespaceManager svr_cfg_xml_ns_mgr = new XmlNamespaceManager( svr_cfg_xml_doc.NameTable );

			// if there is a default namespace specified in the
			// xml file then it needs to be added to the namespace
			// manager so the xpath queries will work
			Regex svr_ns_rx = new Regex(
				@"xmlns\s{0,}=\s{0,}""([\w\d\.\:\/\\]{0,})""",
				RegexOptions.Multiline | RegexOptions.IgnoreCase );
			Match svr_ns_m = svr_ns_rx.Match( svr_cfg_xml_doc.InnerXml );

			// add the default namespace
			string svr_default_ns = svr_ns_m.Groups[ 1 ].Value;
			svr_cfg_xml_ns_mgr.AddNamespace( "d", svr_default_ns );
			
			// build the query, find the node, set the value
			string user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:add[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@key" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'pluginConfigurationUri'" ) );
			XmlNode node = svr_cfg_xml_doc.SelectSingleNode( user_xpq, svr_cfg_xml_ns_mgr );
			if ( node != null )
			{
				string plugin_config_path = string.Format( @"{0}Server\pluginConfiguration.xml", target_dir );
				node.Attributes[ "value" ].Value = plugin_config_path;

				//
				// in the next part we scrub the plugin configuration file
				// so that any paths it contains are rewritten to coincide with
				// this particular installation
				//
				
				// create a xmlreadersettings object
				// to specify how to read in the file
				XmlReaderSettings plugin_cfg_xrs = new XmlReaderSettings();
				plugin_cfg_xrs.CloseInput = true;
				plugin_cfg_xrs.IgnoreComments = false;

				// read in the file
				XmlReader plugin_cfg_xr = XmlReader.Create( plugin_config_path, plugin_cfg_xrs );

				// load the xml reader into the xml document.
				XmlDocument plugin_cfg_xml_doc = new XmlDocument();
				plugin_cfg_xml_doc.Load( plugin_cfg_xr );

				// close the xmlreader
				plugin_cfg_xr.Close();

				// create the namespace manager using the xml file name table
				XmlNamespaceManager plugin_cfg_xml_ns_mgr = new XmlNamespaceManager( plugin_cfg_xml_doc.NameTable );

				// if there is a default namespace specified in the
				// xml file then it needs to be added to the namespace
				// manager so the xpath queries will work
				Regex plugin_ns_rx = new Regex(
					@"xmlns\s{0,}=\s{0,}""([\w\d\.\:\/\\]{0,})""",
					RegexOptions.Multiline | RegexOptions.IgnoreCase );
				Match plugin_ns_m = plugin_ns_rx.Match( plugin_cfg_xml_doc.InnerXml );

				// add the default namespace
				string plugin_default_ns = plugin_ns_m.Groups[ 1 ].Value;
				plugin_cfg_xml_ns_mgr.AddNamespace( "d", plugin_default_ns );
				
				user_xpq = string.Format(
					CultureInfo.CurrentCulture,
					@"//d:plugin[{0} = {1}]",
					string.Format( CultureInfo.CurrentCulture,
						XpathTranslateFormat, "@pluginType" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'authorizationPlugin'" ) );
				node = plugin_cfg_xml_doc.SelectSingleNode( user_xpq, plugin_cfg_xml_ns_mgr );
				if ( node != null )
				{
					node.Attributes[ "dataSourceConnectionString" ].Value = string.Format(
						CultureInfo.CurrentCulture, 
						@"{0}Server\sudoers.xml",
						target_dir );
					node.Attributes[ "dataSourceSchemaUri" ].Value = string.Format(
						CultureInfo.CurrentCulture,
						@"{0}Server\XmlAuthorizationPluginSchema.xsd",
						target_dir );
					node.Attributes[ "dataSourceCacheFilePath" ].Value = string.Format(
						CultureInfo.CurrentCulture,
						@"{0}Server\sudoers.xml.cache",
						target_dir );
				}

				// save it back to the file
				plugin_cfg_xml_doc.Save( plugin_config_path );
			}

			// build the query, find the node, set the value
			user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:add[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@key" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'pluginConfigurationSchemaUri'" ) );
			node = svr_cfg_xml_doc.SelectSingleNode( user_xpq, svr_cfg_xml_ns_mgr );
			if ( node != null )
				node.Attributes[ "value" ].Value = string.Format( @"{0}Server\PluginConfigurationSchema.xsd",
					target_dir );

			// build the query, find the node, set the value
			user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:add[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@key" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'callbackApplicationPath'" ) );
			node = svr_cfg_xml_doc.SelectSingleNode( user_xpq, svr_cfg_xml_ns_mgr );
			if ( node != null )
				node.Attributes[ "value" ].Value = string.Format( @"{0}Callback\Sudowin.CallbackApplication.exe",
					target_dir );

			// build the query, find the node, set the value
			user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:source[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@name" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'traceSrc'" ) );
			node = svr_cfg_xml_doc.SelectSingleNode( user_xpq, svr_cfg_xml_ns_mgr );
			if ( node != null )
				node.Attributes[ "switchValue" ].Value = "Error";

			// build the query, find the node, set the value
			user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:add[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@name" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'traceListener'" ) );
			node = svr_cfg_xml_doc.SelectSingleNode( user_xpq, svr_cfg_xml_ns_mgr );
			if ( node != null )
				node.Attributes[ "initializeData" ].Value = string.Format( @"{0}Server\service.log",
					target_dir );

			// save it back to the file
			svr_cfg_xml_doc.Save( server_config_path );

			#endregion

			#region Rename Sudowin.Clients.Console.exe to sudo.exe and add directory to system path

			string exe_old_path = string.Format( @"{0}Clients\Console\Sudowin.Clients.Console.exe", target_dir );
			string cfg_old_path = string.Format( @"{0}Clients\Console\Sudowin.Clients.Console.exe.config", target_dir );
			string exe_new_path = string.Format( @"{0}Clients\Console\sudo.exe", target_dir );
			string cfg_new_path = string.Format( @"{0}Clients\Console\sudo.exe.config", target_dir );

			File.Move( exe_old_path, exe_new_path );
			File.Move( cfg_old_path, cfg_new_path );

			string path = Environment.GetEnvironmentVariable( "PATH" );
			path = path[ path.Length - 1 ] == ';' ?
				string.Format( @"{0}{1}Clients\Console", path, target_dir ) :
				string.Format( @"{0};{1}Clients\Console", path, target_dir );
			Environment.SetEnvironmentVariable( "PATH", path, EnvironmentVariableTarget.Machine );

			#endregion

			#region Copy the sudoers file into place

			string sudoers_old_file_path = string.Format( CultureInfo.CurrentCulture,
				@"{0}sudoers.xml", target_dir );
			string sudoers_new_file_path = string.Format( CultureInfo.CurrentCulture,
				@"{0}Server\sudoers.xml", target_dir );

			// if the sudoers file already exists then delete the stock one 
			// that comes with the installer
			if ( File.Exists( sudoers_new_file_path ) )
			{
				m_sudoers_file_already_exists = true;
				File.Delete( sudoers_old_file_path );
			}

			// if the sudoers file does not already exist then move the stock
			// sudoers file that comes with the installer into the Server 
			// directory
			else
			{
				File.Move( sudoers_old_file_path, sudoers_new_file_path );
			}

			#endregion
		}

		public override void Rollback( System.Collections.IDictionary savedState )
		{
			base.Rollback( savedState );

			string target_dir = this.Context.Parameters[ "TargetDir" ];

			#region Remove the Sudoers group

			if ( !m_sudoers_group_already_exists )
			{

				// delete the Sudoers group on the local machine if it exists
				DirectoryEntry de = new DirectoryEntry( string.Format( "WinNT://{0},computer",
					Environment.MachineName ) );
				DirectoryEntry grp = null;
				try
				{
					grp = de.Children.Find( "Sudoers", "group" );
				}
				catch
				{
				}

				if ( grp != null )
				{
					de.Children.Remove( grp );
					grp.Close();
				}
				de.Close();
			}
			
			#endregion

			#region Remove sudo.exe and remove directory from system path

			string exe_new_path = string.Format( @"{0}Clients\Console\sudo.exe", target_dir );
			string cfg_new_path = string.Format( @"{0}Clients\Console\sudo.exe.config", target_dir );

			if ( File.Exists( exe_new_path ) )
				File.Delete( exe_new_path );
			if ( File.Exists( cfg_new_path ) )
				File.Delete( cfg_new_path );

			string path = Environment.GetEnvironmentVariable( "PATH" );
			string rem_path_patt = string.Format( @"(.*)({0}Clients\\Console)(.*)" );
			if ( Regex.IsMatch( path, rem_path_patt, RegexOptions.IgnoreCase ) )
			{
				path = Regex.Replace( path, rem_path_patt, "$1$3", RegexOptions.IgnoreCase );
				path.Replace( ";;", ";" );
				Environment.SetEnvironmentVariable( "PATH", path, EnvironmentVariableTarget.Machine );
			}

			#region Delete the sudoers file
			
			string sudoers_file_path = string.Format( CultureInfo.CurrentCulture,
					@"{0}Server\sudoers.xml", target_dir );
			if ( File.Exists( sudoers_file_path ) )
				File.Delete( sudoers_file_path );

			#endregion

			#endregion
		}

		public override void Uninstall( System.Collections.IDictionary savedState )
		{
			string target_dir = this.Context.Parameters[ "TargetDir" ];

            // [bug #1995331] add support for quiet uninstall
            bool isQuiet = this.Context.Parameters["ClientUILevel"] == "3";

			base.Uninstall( savedState );

			#region Remove the Sudoers group

            DialogResult dr;

            // default to Not deleteing Sudoers group
            dr = DialogResult.No;
            if (!isQuiet)
            {
                dr = MessageBox.Show(
                    "Delete the Sudoers group?",
                    "Click \"Yes\" to delete the Sudoers group (not recommended if you are upgrading).",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            }
            
            if (dr == DialogResult.Yes)
            {
                // delete the Sudoers group on the local machine if it exists
                DirectoryEntry de = new DirectoryEntry(string.Format("WinNT://{0},computer",
                    Environment.MachineName));
                DirectoryEntry grp = null;
                try
                {
                    grp = de.Children.Find("Sudoers", "group");
                }
                catch
                {
                }

                if (grp != null)
                {
                    de.Children.Remove(grp);
                    grp.Close();
                }
                de.Close();
            }

			#endregion

			#region Remove the Sudoers file

            // default to Not deleteing Sudoers file
            dr = DialogResult.No;

            if (!isQuiet)
            {
                dr = MessageBox.Show(
                    "Delete the Sudoers file?",
                    "Click \"Yes\" to delete the Sudoers file (not recommended if you are upgrading).",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            }
			if ( dr == DialogResult.Yes )
			{
				string sudoers_file_path = string.Format( CultureInfo.CurrentCulture,
					@"{0}Server\sudoers.xml", target_dir );
				if ( File.Exists( sudoers_file_path ) )
					File.Delete( sudoers_file_path );
			}

			#endregion
			
			#region Remove sudo.exe and remove directory from system path

			string exe_new_path = string.Format( @"{0}Clients\Console\sudo.exe", target_dir );
			string cfg_new_path = string.Format( @"{0}Clients\Console\sudo.exe.config", target_dir );

			if ( File.Exists( exe_new_path ) )
				File.Delete( exe_new_path );
			if ( File.Exists( cfg_new_path ) )
				File.Delete( cfg_new_path );
			
			string path = Environment.GetEnvironmentVariable( "PATH" );
			string rem_path_patt = string.Format( @"(.*)({0}Clients\\Console)(.*)",
				target_dir.Replace( @"\", @"\\" ) );
			if ( Regex.IsMatch( path, rem_path_patt, RegexOptions.IgnoreCase ) )
			{
				path = Regex.Replace( path, rem_path_patt, "$1$3", RegexOptions.IgnoreCase );
				path.Replace( ";;", ";" );
				Environment.SetEnvironmentVariable( "PATH", path, EnvironmentVariableTarget.Machine );
			}

			#endregion
		}
	}
}