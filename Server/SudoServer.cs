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

using Win32;
using System;
using System.IO;
using System.Text;
using Sudowin.Common;
using System.Security;
using System.Threading;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Globalization;
using System.ComponentModel;
using System.DirectoryServices;
using System.Security.Principal;
using System.Collections.Generic;
using System.Security.Permissions;
using Sudowin.Plugins.Authorization;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using Sudowin.Plugins.Authentication;
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using Sudowin.Plugins;
using System.Data;
using Microsoft.Win32;

namespace Sudowin.Server
{
	/// <summary>
	///		This is the class that the Sudo Windows service hosts
	///		as the sa object that the clients communicate with.
	/// </summary>
	public class SudoServer :	MarshalByRefObject, 
		
								Sudowin.Common.ISudoServer, 
		
								IDisposable
	{
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudowin.Server.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		Used to authorize user's sudowin requests.
		/// </summary>
		private IAuthorizationPlugin m_plgn_authz = null;
		
		/// <summary>
		///		Used to authenticate users.
		/// </summary>
		private IAuthenticationPlugin m_plgn_authn = null;

		/// <summary>
		///		This is a dummy property.  It enables
		///		clients to trap an exception that will occur
		///		if they do not have permissions to talk to
		///		the server.
		/// </summary>
		public bool IsConnectionOpen
		{
			get
			{
				return ( true );
			}
		}
        
        /// <summary>
        /// Check and download latest csv confiugration from central server and generate new sudoers.xml
        /// </summary>
        public bool UpdateSudoers(bool bForce = false)
        {
            bool bSuccess = false;

            m_plgn_authz.UpdateConfig(bForce);

            return (bSuccess);
        }

		/// <summary>
		///		Default constructor.
		/// </summary>
		public SudoServer()
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterConstructor,
				"constructing SudoServer" );

			// right now sudowin only supports one plugin of each type active
			// at any given time.  in the future this will change, but for now,
			// c'est la vie.
			LoadPlugins();
			
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitConstructor,
				"constructed SudoServer" );
		}

		private void LoadPlugins()
		{
			string plugin_config_uri = ConfigurationManager.AppSettings[
				"pluginConfigurationUri" ];
			string plugin_config_schema_uri = ConfigurationManager.AppSettings[
				"pluginConfigurationSchemaUri" ];
			DataSet plugin_ds = new DataSet();
			try
			{
				plugin_ds.ReadXmlSchema( plugin_config_schema_uri );
				plugin_ds.ReadXml( plugin_config_uri );
			}
			catch ( Exception e )
			{
				string error = string.Format( CultureInfo.CurrentCulture,
					"the plugin config file, {0}, does not contain a valid schema according " +
					"to the given schema file, {1}", plugin_config_uri, plugin_config_schema_uri );
				m_ts.TraceEvent( TraceEventType.Critical, ( int ) EventIds.CriticalError, error );
				throw ( new Exception( error, e ) );
			}

			int x = 0;

			// activate the plugin assemblies
			foreach ( DataRow r in plugin_ds.Tables[ "plugin" ].Rows )
			{
				string plugin_type = Convert.ToString( r[ "pluginType" ], CultureInfo.CurrentCulture );

				bool plugin_enabled = r[ "enabled" ] is DBNull ? true :
					bool.Parse( Convert.ToString( r[ "enabled" ], CultureInfo.CurrentCulture ) );

				string plugin_server_type = r[ "serverType" ] is DBNull ? "SingleCall" :
					Convert.ToString( r[ "serverType" ], CultureInfo.CurrentCulture );

				string plugin_assem_str = Convert.ToString(
					r[ "assemblyString" ], CultureInfo.CurrentCulture );

				string plugin_act_data = r[ "activationData" ] is DBNull ? "" :
					Convert.ToString( r[ "activationData" ], CultureInfo.CurrentCulture );

				if ( plugin_enabled )
				{
					//
					// register the plugin as a remoting object -- the plugins
					// will have the following uri formats:
					//
					// pluginTypeXX.rem 
					// 
					// where the XX is plugin index (0 based) in its section 
					// in the plugin configuration file.  for example, the 2nd 
					// plugin's uri would be:
					//
					// pluginType01.rem
					//
					Type t = Type.GetType( plugin_assem_str, true, true );
					string uri = string.Format( "ipc://sudowin/{0}{1:d2}.rem", plugin_type, x );

					Plugin plugin = Activator.GetObject( typeof( Plugin ), uri ) as Plugin;
					
					// activate the remoting object first before any of the sudo clients
					// do so through a sudo invocation.  this will cause any exceptions
					// that might get thrown in the plugin's construction to do so now,
					// causing this service not to start.  it is better that the sudowin
					// service fail outright than have a client application crash later
					plugin.Activate( plugin_act_data );

					switch ( plugin_type )
					{
						case "authenticationPlugin" :
						{
							m_plgn_authn = ( AuthenticationPlugin ) plugin;
							break;
						}
						case "authorizationPlugin" :
						{
							m_plgn_authz = ( AuthorizationPlugin ) plugin;
							break;
						}
						default :
						{
							// do nothing
							break;
						}
					}
				}

				++x;
			}
		}

		/// <summary>
		///		Adds or removes a user to the privileges
		///		group on the local computer.
		/// </summary>
		/// <param name="userName">
		///		User name to add or remove to the privileges 
		///		group.
		/// </param>
		/// <param name="which">
		///		1 "Add" or 0 "Remove"
		/// </param>
		/// <param name="privilegesGroup">
		///		Name of the group that possesses the
		///		same privileges that the user will when
		///		they use Sudowin.
		/// </param>
		/// <returns>
		///		If this method was invoked with the which parameter
		///		equal to 1 then this method returns whether or not
		///		the given user name was already a member of the group
		///		that it was supposed to be added to.
		/// 
		///		If this method was invoked with the which parameter
		///		equal to 0 then the return value of this method can
		///		be ignored.
		/// </returns>
		[EnvironmentPermission( SecurityAction.LinkDemand )]
		private bool AddRemoveUser( 
			string userName, 
			int which, 
			string privilegesGroup )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering AddRemoveUser( string, int, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"{0}, which={1}, privilegesGroup={2}",
				userName, which, privilegesGroup );

			// get the directory entries for the localhost and the privileges group
			DirectoryEntry localhost = new DirectoryEntry(
				string.Format(
				CultureInfo.CurrentCulture,
				"WinNT://{0},computer",
				Environment.MachineName ) );
			DirectoryEntry group = DirectoryFinder.Find( localhost.Children, privilegesGroup );

            // group not found so throw exception
            if (group == null)
            {
                throw SudoException.GetException(SudoResultTypes.GroupNotFound, privilegesGroup);
            }

			// get the domain/host name and user name
			string[] un_split = userName.Split( new char[] { '\\' } );
			string dhn_part = un_split[ 0 ];
			string un_part = un_split[ 1 ];

			// used for asdi calls
			object[] user_path = null;

			// local user
			if ( Regex.IsMatch( dhn_part, Environment.MachineName, RegexOptions.IgnoreCase ) )
			{
				// find the user instead of building the path.  this is 
				// in case this machine belongs to a workgroup or a domain.  
				//  it is easier to search for the user and get their path that 
				// way than it is to get the computer's workgroup
				DirectoryEntry user = DirectoryFinder.Find( localhost.Children, un_part, "user" );

                // user not found so throw exception
                if (user == null)
                {
                    throw SudoException.GetException(SudoResultTypes.UsernameNotFound, dhn_part, un_part);
                }

				user_path = new object[] 
					{
						user.Path
					};

				user.Close();
			}

			// ad user
			else
			{
				user_path = new object[] 
				{
					string.Format(
						CultureInfo.CurrentCulture,
						"WinNT://{0}/{1}",
						dhn_part, un_part )
				};
			}

			bool isAlreadyMember = bool.Parse( Convert.ToString(
				group.Invoke( "IsMember", user_path ),
				CultureInfo.CurrentCulture ) );

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, isAlreadyMember={1}",
				userName, isAlreadyMember );

			// add user to privileges group
			if ( which == 1 && !isAlreadyMember )
			{
				group.Invoke( "Add", user_path );

				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"{0}, added user to privileges group",
					userName );
			}

			// remove user from privileges group
			else if ( which == 0 && isAlreadyMember )
			{
				group.Invoke( "Remove", user_path );

				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"{0}, removed user from privileges group",
					userName );
			}

			// save changes
			group.CommitChanges();

			// cleanup
			group.Dispose();
			localhost.Dispose();

			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.ExitMethod,
				"exiting AddRemoveUser( string, int, string )" );

			return ( isAlreadyMember );
		}

		/// <summary>
		///		Invokes sudo on the given command path
		/// </summary>
		/// <param name="passphrase">
		///		passphrase of user invoking Sudowin.
		/// </param>
		/// <param name="commandPath">
		///		Fully qualified path of the command that
		///		sudo is being invoked on.
		/// </param>
		/// <param name="commandArguments">
		///		Command arguments of the command that
		///		sudo is being invoked on.
		/// </param>
		/// <returns>
		///		A SudoResultTypes value.
		/// </returns>
		public SudoResultTypes Sudo(
			string passphrase,
			string commandPath,
			string commandArguments )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering Sudo( string, string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"passphrase=,commandPath={0},commandArguments={1}",
				commandPath, commandArguments );

			string un = Thread.CurrentPrincipal.Identity.Name;

			// check to see if the user is present in the sudoers data store
			UserInfo ui = new UserInfo();
			if ( !m_plgn_authz.GetUserInfo( un, ref ui ) )
			{
				m_ts.TraceEvent( TraceEventType.Information, ( int ) EventIds.Information,
					"{0}, user not in sudoers data store", un );
				
				LogResult( un, commandPath, commandArguments, ui.LoggingLevel,
					SudoResultTypes.CommandNotAllowed );
                
                throw SudoException.GetException(SudoResultTypes.CommandNotAllowed);
			}

			// validate the users logon credentials
			if ( !LogonUser( un, passphrase, ref ui /*, ref cc*/ ) )
			{
                LogResult( un, commandPath, commandArguments, ui.LoggingLevel,
					SudoResultTypes.InvalidLogon );
                throw SudoException.GetException(SudoResultTypes.InvalidLogon);
			}

			// handle special cases in the arguments section

			// msi file
			if ( Regex.IsMatch( commandArguments, ".*/package .*\\.msi.*" ) )
			{
				commandArguments = Regex.Replace( commandArguments,
					"(.*)/package (.*.msi)(.*)", "$1/package \"$2\"$3" );
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"handle special commandArguments: {0}", commandArguments );
			}

			// verify the command being sudoed
			if ( !m_plgn_authz.VerifyCommand( un, ref commandPath, commandArguments ) )
			{
                LogResult( un, commandPath, commandArguments, ui.LoggingLevel,
					SudoResultTypes.CommandNotAllowed );
                throw SudoException.GetException(SudoResultTypes.CommandNotAllowed);
			}

			// verify that this service and the sudo console app
			// are both signed with the same strong name key
			if ( !VerifySameSignature( un,
				ConfigurationManager.AppSettings[ "callbackApplicationPath" ] ) )
			{
                LogResult(un, commandPath, commandArguments, ui.LoggingLevel,
                    SudoResultTypes.CommandNotAllowed);
                throw SudoException.GetException(SudoResultTypes.CommandNotAllowed);
			}

			// sudo the command for the user
			Sudo( un, passphrase, ui.PrivilegesGroup, commandPath, commandArguments );

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting Sudo( string, string, string )" );

            return ( LogResult( un, commandPath, commandArguments, ui.LoggingLevel, 
				SudoResultTypes.SudoK ) );
		}

		private void Sudo(
			string userName, 
			string passphrase, 
			string privilegesGroup,
			string commandPath, 
			string commandArguments )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering Sudo( string, string, string, string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},passphrase=,privilegesGroup={1},commandPath={2},commandArguments={3}",
				userName, privilegesGroup, commandPath, commandArguments );

			// get the user's logon token
			IntPtr hUser = IntPtr.Zero;
			QueryUserToken( userName, ref hUser );
			
			// add the user to the group and record if they
			// were already a member of the group
			bool am = AddRemoveUser( userName, 1, privilegesGroup );

            int scValue = (int) Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "scforceoption", 0);
            
            // Disable piv required
            if (scValue == 1)
                Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "scforceoption", 0);
			
            // create the callback process and wait for it to exit so the user is
			// not removed from the privileges group before the indended process starts
			Process p = null;
			if ( CreateProcessAsUser( hUser, passphrase, commandPath, commandArguments, ref p ) )
			{
				p.WaitForExit();
			}

            // set it back to piv required
            if (scValue == 1)
                Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "scforceoption", 1);

			// remove the user from the group if they were not already a member
			if ( !am )
			{
				AddRemoveUser( userName, 0, privilegesGroup );
			}

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting Sudo( string, string, string, string, string )" );
		}

		private SudoResultTypes LogResult(
			string userName,
            string commandPath,
            string commandArguments,
			LoggingLevelTypes loggingLevel,
			SudoResultTypes sudoResultType )
		{
			if ( loggingLevel != LoggingLevelTypes.None )
			{
				EventLogEntryType elet = 0;

				switch ( sudoResultType )
				{
					case SudoResultTypes.CommandNotAllowed:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.FailureAudit;
						break;
					}
					case SudoResultTypes.InvalidLogon:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.FailureAudit;
						break;
					}
					case SudoResultTypes.LockedOut:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.FailureAudit;
						break;
					}
					case SudoResultTypes.SudoK:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.SuccessAudit;
						break;
					}
					case SudoResultTypes.TooManyInvalidLogons:
					{
						if ( loggingLevel == LoggingLevelTypes.Failure ||
							loggingLevel == LoggingLevelTypes.Both )
							elet = EventLogEntryType.FailureAudit;
						break;
					}
				}

                try
                {
                    string source = "Sudowin - Usage";
                    if (!EventLog.SourceExists(source))
                    {
                        EventLog.CreateEventSource(source, "AETD");
                    }

                    EventLog.WriteEntry(source,
                        string.Format(CultureInfo.CurrentCulture,
                            "{0} - {1}\n{2} {3}", userName, sudoResultType, commandPath, commandArguments),
                        elet,
                        (int)sudoResultType);
                }
                catch
                {
                    // [bug #2120197] swallow exception here, since there's nothing we can do if the event log
                    // can't be written to
                }

			}

			return ( sudoResultType );
		}

		private bool LogonUser( 
			string userName,
			string passphrase, 
			ref UserInfo userInfo )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering LogonUser( string, string, ref UserInfo, ref UserCache )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},passphrase=,userInfo=,userCache=", userName);

			// get the domain and user name parts of the userName
			Match m = Regex.Match( userName, @"^([^\\]+)\\(.+)$" );
			string dn_part = m.Groups[ 1 ].Value;
			string un_part = m.Groups[ 2 ].Value;

			// verify the user's credentials
			bool logonSuccessful = m_plgn_authn.VerifyCredentials( dn_part, un_part, passphrase );
            
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, logonSuccessful={1}", userName, logonSuccessful );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting LogonUser( string, ref string, string )" );

			return ( logonSuccessful );
		}
        
		private bool CreateProcessAsUser( 
			IntPtr userToken,
			string passphrase,
			string commandPath, 
			string commandArguments,
			ref Process newProcess )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering CreateProcessAsUser( IntPtr, string, string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userToken=,passphrase=,commandPath={0},commandArguments={1}", 
				commandPath, commandArguments );

			// needed to create a new process
			SecurityAttributes sa = new SecurityAttributes();
			sa.InheritHandle = false;
			sa.SecurityDescriptor = IntPtr.Zero;
			sa.Length = Marshal.SizeOf( sa );

			// bind the new process to the interactive desktop
			StartupInfo si = new StartupInfo();
			si.Desktop = "WinSta0\\Default";
			si.Size = Marshal.SizeOf( si );

			// build a formatted command path to call the Sudowin.ConsoleApplication with
			string fcp = string.Format(
				CultureInfo.CurrentCulture,
				
				// i took this out for now until i decide whether or not
				// i want to bother with command line switches in the callback 
				// application
				//"\"{0}\" -c -p \"{1}\" \"{2}\" {3}",
				
				"\"{0}\"  \"{1}\" \"{2}\" {3}",
				ConfigurationManager.AppSettings[ "callbackApplicationPath" ],
				 passphrase,
				commandPath, commandArguments );

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"formatted command path={0}",
				
				// see the last comment as to why this is commented out
				//Regex.Replace( fcp, @"\-p ""([^""]*)""", "-p" ) );
				
				Regex.Replace( fcp, @"  ""([^""]*)""", "" ) );

			ProcessInformation pi;
			bool newProcessCreated = Win32.Native.CreateProcessAsUser(
				userToken,
				null,
				fcp,
				ref sa, ref sa,
				false,
				( int ) ProcessCreationFlags.CreateNoWindow | ( int ) ProcessPriorityTypes.Normal,
				IntPtr.Zero, null, ref si, out pi );

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"processCreated={0}" + ( newProcessCreated ? "" : ", win32error={1}" ), 
				newProcessCreated, 
				newProcessCreated ? 0 : Marshal.GetLastWin32Error() );

			if ( newProcessCreated )
			{
				// get a managed reference to the process
				newProcess = Process.GetProcessById( pi.ProcessId );
				// free the unmanaged handles
				Win32.Native.CloseHandle( pi.Thread );
				Win32.Native.CloseHandle( pi.Process );
			}

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting CreateProcessAsUser( IntPtr, string, string, string )" );

			return ( newProcessCreated );
		}

		/// <summary>
		///		Retrieves the logon token for the user that is 
		///		logged into the computer with a user name that is
		///		equal to the parameter userName.
		/// </summary>
		/// <param name="userName">
		///		User name to get token for.
		/// </param>
		/// <param name="userToken">
		///		User logon token.
		/// </param>
		/// <returns>
		///		True if the token was retrieved, otherwise false.
		/// </returns>
		private bool QueryUserToken( string userName, ref IntPtr userToken )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering QueryUserToken( string, ref IntPtr )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},userToken=", userName );

			// open a handle to the localhost
			IntPtr hSvr = Win32.Native.WtsOpenServer( null );

			// get a list of the sessions on the localhost
			Win32.WtsSessionInfo[] wsis;
			try
			{
				wsis = Win32.Managed.WtsEnumerateSessions( hSvr );
			}
			catch ( Win32Exception e )
			{
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Error,
					"{0}, WtsEnumerateSessions FAILED, Win32Error={1}",
					userName, e.ErrorCode );
				m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
					"exiting QueryUserToken( userName, ref IntPtr" );
				return ( false );
			}

			// check all the sessions on the server to get the logon token
			// of the user that has the same user name as the userName parameter
			for ( int x = 0; x < wsis.Length && userToken == IntPtr.Zero; ++x )
			{
				// declare 2 strings to hold the user name and domain name
				string un = string.Empty, dn = string.Empty;

				// compare the session's user name with the userName
				// parameter and get the logon token if they are equal
				if ( Win32.Managed.WtsQuerySessionInformation(
						hSvr, wsis[ x ].SessionId,
						Win32.WtsQueryInfoTypes.WtsUserName,
						out un )

					&&

					Win32.Managed.WtsQuerySessionInformation(
						hSvr, wsis[ x ].SessionId,
						Win32.WtsQueryInfoTypes.WtsDomainName,
						out dn )
                    )

				{
			        m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				        "compare userName {0}\\{1} to {2}", dn, un, userName );
                    
					if ( string.Compare( dn + "\\" + un , userName, true ) == 0 )
                    {
					    Win32.Native.WtsQueryUserToken(
						    wsis[ x ].SessionId, ref userToken );
                    }
				}
			}

			if ( hSvr != IntPtr.Zero )
				Win32.Native.WtsCloseServer( hSvr );

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, tokenRetrieved={1}", userName, userToken != IntPtr.Zero );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting QueryUserToken( userName, ref IntPtr" );
			
			return ( userToken != IntPtr.Zero );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="userName"></param>
		/// <param name="otherAssemblyFilePath"></param>
		/// <remarks>
		///		http://blogs.msdn.com/shawnfa/archive/2004/06/07/150378.aspx
		/// </remarks>
		private bool VerifySameSignature( 
			string userName,
			string otherAssemblyFilePath )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering VerifySameSignature( string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"{0}, otherAssemblyFilePath={1}", userName, otherAssemblyFilePath );

			// declare this method's return value
			bool isVerified = false;

			// load the Sudowin.ConsoleApplication assembly
			Assembly ca = Assembly.LoadFile( otherAssemblyFilePath );

			// get a reference to the Sudowin.Server assembly
			Assembly sa = Assembly.GetExecutingAssembly();

			// declare 2 bools to hold the results of both the
			// server and the client private key verficiation tests
			bool ca_wv = false; 
			bool sa_wv = false;

			// verify that the console application and
			// the service application are both signed
			// by the same private key
			if ( isVerified = ( StrongNameSignatureVerificationEx(
					ca.Location, true, ref ca_wv ) &&
				StrongNameSignatureVerificationEx(
					sa.Location, true, ref sa_wv ) ) )
			{
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"private keys verified={0}", isVerified );

				// get the ca and sa public key tokens
				byte[] ca_pubkey = ca.GetName().GetPublicKeyToken();
				byte[] sa_pubkey = sa.GetName().GetPublicKeyToken();

				// verify that both public key tokens are the same length
				if ( ca_pubkey.Length == sa_pubkey.Length )
				{
					// verify that each bit of the the public key tokens
					for ( int x = 0; x < ca_pubkey.Length && isVerified; ++x )
					{
						isVerified = ca_pubkey[ x ] == sa_pubkey[ x ];
					}

					m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
						"public key tokens verified={0}", isVerified );
				}
			}
			else
			{
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"private keys not verified" );
			}

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"key signatures verified={0}", isVerified );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting VerifySameSignature( string, string )" );

			return ( isVerified );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="filePath"></param>
		/// <param name="forceVerficiation"></param>
		/// <param name="wasVerified"></param>
		/// <returns></returns>
		/// <remarks>
		///		http://blogs.msdn.com/shawnfa/archive/2004/06/07/150378.aspx
		/// </remarks>
		[DllImport( "mscoree.dll", CharSet = CharSet.Unicode )]
		private static extern bool StrongNameSignatureVerificationEx( 
			string filePath, 
			bool forceVerficiation, 
			ref bool wasVerified );

		#region IDisposable Members

		/// <summary>
		///		Close resources.
		/// </summary>
		public void Dispose()
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterDispose,
				"entering Dispose" );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitDispose,
				"exiting Dispose" );
		}

		#endregion
	}
}
