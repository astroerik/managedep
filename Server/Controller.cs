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
using System.Data;
using Sudowin.Common;
using Sudowin.Plugins;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Globalization;
using System.ComponentModel;
using System.ServiceProcess;
using System.Runtime.Remoting;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;

namespace Sudowin.Server
{
	/// <summary>
	///		Service controller for the Sudowin service.
	/// </summary>
	public partial class Controller : ServiceBase
	{
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudowin.Server.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		Default constructor.
		/// </summary>
		public Controller()
		{
			InitializeComponent();
		}

		/// <summary>
		///		Callback when the service is started.
		/// </summary>
		/// <param name="args">
		///		
		/// </param>
		protected override void OnStart( string[] args )
		{
			// let the SCM tell the service to sleep a certain amount of time when
			// starting in order to give debuggers time to attach
			if ( args.Length == 1 )
			{
				m_ts.TraceEvent(TraceEventType.Verbose, ( int ) EventIds.Verbose,
					"sleeping start for {0} seconds", args[ 0 ] );
				System.Threading.Thread.Sleep( Convert.ToInt32( args[ 0 ] ) * 1000 );
			}
			
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod, 
				"entering OnStart" );
			
			// get the path to the actual service executable
			Uri uri = new Uri( 
				System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase );

			string remote_config_uri = uri.LocalPath + ".config";

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose, 
				"configuring remoting with " + remote_config_uri );
				
			// configure remoting channels and objects
			RemotingConfiguration.Configure( remote_config_uri, true );
			
			// load the plugins
			LoadPlugins();
			
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod, 
				"exiting OnStart" );
		}

		/// <summary>
		///		Stops this service.
		/// </summary>
		protected override void OnStop()
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int )  EventIds.EnterMethod, 
				"entering OnStop" );
			m_ts.Close();
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod, 
				"exiting OnStop" );
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
			catch( Exception e )
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
					string uri = string.Format( "{0}{1:d2}.rem", plugin_type, x );
					RemotingConfiguration.RegisterWellKnownServiceType( t, uri, 
						( WellKnownObjectMode ) Enum.Parse( typeof( WellKnownObjectMode ),
							Convert.ToString( plugin_server_type, CultureInfo.CurrentCulture ) ) );
					
					// get a reference to the remoting object we just created
					uri = string.Format( "ipc://sudowin/{0}", uri );
					Plugin plugin = Activator.GetObject( typeof( Plugin ), uri ) as Plugin;

					// activate the remoting object first before any of the sudo clients
					// do so through a sudo invocation.  this will cause any exceptions
					// that might get thrown in the plugin's construction to do so now,
					// causing this service not to start.  it is better that the sudowin
					// service fail outright than have a client application crash later
					plugin.Activate( plugin_act_data );
				}

				++x;
			}
		}
	}
}
