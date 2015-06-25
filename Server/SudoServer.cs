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
using System.Runtime.Remoting.Contexts;
using System.Runtime.Remoting.Messaging;
using Sudowin.Plugins;
using System.Data;
using Microsoft.Win32;
using System.Reactive.Concurrency;

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
		private IAuthorizationPlugin m_plgn_authz;
		
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
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterConstructor, "constructing SudoServer" );

			// right now sudowin only supports one plugin of each type active
			// at any given time.  in the future this will change, but for now,
			// c'est la vie.
            LoadPlugins();
            
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitConstructor, "constructed SudoServer" );
		}

		private void LoadPlugins()
		{
            string root = Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6);
			string plugin_config_uri = Path.Combine(root, ConfigurationManager.AppSettings[ "pluginConfigurationUri" ]);
			string plugin_config_schema_uri = Path.Combine(root, ConfigurationManager.AppSettings[ "pluginConfigurationSchemaUri" ]);
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

				bool plugin_enabled = r[ "enabled" ] is DBNull ? true : bool.Parse( Convert.ToString( r[ "enabled" ], CultureInfo.CurrentCulture ) );

				string plugin_server_type = r[ "serverType" ] is DBNull ? "SingleCall" : Convert.ToString( r[ "serverType" ], CultureInfo.CurrentCulture );

				string plugin_assem_str = Convert.ToString( r[ "assemblyString" ], CultureInfo.CurrentCulture );

				string plugin_act_data = r[ "activationData" ] is DBNull ? "" : Convert.ToString( r[ "activationData" ], CultureInfo.CurrentCulture );

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
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod, "entering AddRemoveUser( string, int, string )" );
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

				user_path = new object[] { user.Path };
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

			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.ExitMethod, "exiting AddRemoveUser( string, int, string )" );

			return ( isAlreadyMember );
		}

        /// <summary>
        ///		Invokes unsudo on the given user
        /// </summary>
        /// <param name="sUsername">
        ///		Username that has been authenticated with sudo client
        /// </param>
        /// <returns>
        ///		A SudoResultTypes value.
        /// </returns>
        public SudoResultTypes UnSudo(string sUsername)
        {
            UserInfo ui = new UserInfo();
            if (!m_plgn_authz.GetUserInfo(sUsername, ref ui))
            {
                throw SudoException.GetException(SudoResultTypes.CommandNotAllowed);
            }

            AddRemoveUser(sUsername, 0, ui.PrivilegesGroup);

            return SudoResultTypes.SudoK;
        }

		/// <summary>
		///		Invokes sudo on the given command path
		/// </summary>
        /// <param name="sUsername">
        ///		Username that has been authenticated with sudo client
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
            string sUsername,
            string commandPath,
			string commandArguments )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering Sudo( string, string, string )" );
            m_ts.TraceEvent(TraceEventType.Verbose, (int)EventIds.ParemeterValues,
                "commandPath={0},commandArguments={1}",
                commandPath, commandArguments);

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

			// msi file
			if ( Regex.IsMatch( commandArguments, ".*/package .*\\.msi.*" ) )
			{
				commandArguments = Regex.Replace( commandArguments,
					"(.*)/package (.*.msi)(.*)", "$1/package \"$2\"$3" );
				m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"handle special commandArguments: {0}", commandArguments );
			}

			// verify the command being sudoed
            if (!m_plgn_authz.VerifyCommand(sUsername, ref commandPath, commandArguments))
			{
                LogResult(sUsername, commandPath, commandArguments, ui.LoggingLevel,
					SudoResultTypes.CommandNotAllowed );
                throw SudoException.GetException(SudoResultTypes.CommandNotAllowed);
			}

			// sudo the command for the user
            bool am = AddRemoveUser(sUsername, 1, ui.PrivilegesGroup);
            if (!am)
            {
                ThreadPoolScheduler.Instance.Schedule(TimeSpan.FromSeconds(30), () => AddRemoveUser(sUsername, 0, ui.PrivilegesGroup));
            }

			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting Sudo( string, string, string )" );

            return (LogResult(sUsername, commandPath, commandArguments, ui.LoggingLevel,
                !am ? SudoResultTypes.SudoKAdded : SudoResultTypes.SudoK));
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
