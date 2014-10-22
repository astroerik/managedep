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
using System.Diagnostics;
using System.Globalization;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Lifetime;

namespace Sudowin.Plugins
{
    public class Plugin : MarshalByRefObject, IPlugin
	{
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudowin.Server.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );
		
		/// <summary>
		///		This class is not meant to be directly instantiated.
		/// </summary>
		protected Plugin()
		{
			
		}

		/// <summary>
		///		This will always return a lifetime lease for plugins
		///		that operate as singletons.  Plugins that operate as
		///		singlecall objects will simply ignore this.
		/// </summary>
		/// <returns>
		///		This object's lease.
		/// </returns>
		/// <remarks>
		///		See http://msdn2.microsoft.com/en-us/library/23bk23zc.aspx 
		///		for more information on leases.
		/// </remarks>
		public override object InitializeLifetimeService()
		{
			// i have to talk with someone who knows more about remoting
			// and leases than me.  for some reason this is not working.
			//
			/*if ( ServerType == "Singleton" )
			{
				ILease lease = base.InitializeLifetimeService() as ILease;
				if ( lease.CurrentState == LeaseState.Initial )
				{
					lease.InitialLeaseTime = TimeSpan.FromSeconds( PluginServerLifetime );
					//lease.SponsorshipTimeout = TimeSpan.FromMinutes( 2 );
					//lease.RenewOnCallTime = TimeSpan.FromSeconds( 2 );
				}
				return ( lease );
			}
			else
			{
				return ( base.InitializeLifetimeService() );
			}*/
			
			return ( null );
		}
		
		/// <summary>
		///		The sudowin service's plugin configuration.
		/// </summary>
		private DataSet m_config_file = null;

		/// <summary>
		///		The sudowin service's plugin configuration.
		/// </summary>
		private DataSet ConfigFile
		{
			get
			{
				if ( m_config_file == null )
				{
					string plugin_config_uri = ConfigurationManager.AppSettings[
						"pluginConfigurationUri" ];
					string plugin_config_schema_uri = ConfigurationManager.AppSettings[
						"pluginConfigurationSchemaUri" ];
					m_config_file = new DataSet();
					try
					{
						m_config_file.ReadXmlSchema( plugin_config_schema_uri );
						m_config_file.ReadXml( plugin_config_uri );
					}
					catch ( Exception e )
					{
						string error = string.Format( CultureInfo.CurrentCulture,
							"the plugin config file, {0}, does not contain a valid schema according " +
							"to the given schema file, {1}", plugin_config_uri, plugin_config_schema_uri );
						m_ts.TraceEvent( TraceEventType.Critical, ( int ) EventIds.CriticalError, error );
						throw ( new Exception( error, e ) );
					}
				}
				
				return ( m_config_file );
			}
		}
		
		private string m_data_source_connection_string = null;

		/// <summary>
		///		The connection string used to connect to this
		///		plugin's data source.  If it is not defined
		///		in the plugin configuration file then this 
		///		property will return null.
		/// </summary>
		protected string DataSourceConnectionString
		{
			get
			{
				if ( m_data_source_connection_string == null )
				{
					m_data_source_connection_string = 
						GetStringValue( ConfigFile.Tables[ "plugin" ].Rows[ 
						Index ][ "dataSourceConnectionString" ], null );
				}
				return ( m_data_source_connection_string );
			}
		}

		private Uri m_data_source_schema_uri = null;

		/// <summary>
		///		The Uri of the schema file used to validate
		///		this plugin's data source.  If it is not defined
		///		in the plugin configuration file then this 
		///		property will return null.
		/// </summary>
		protected Uri DataSourceSchemaUri
		{
			get
			{
				if ( m_data_source_schema_uri == null )
				{
					string uri_string =
						GetStringValue( ConfigFile.Tables[ "plugin" ].Rows[ 
						Index ][ "dataSourceSchemaUri" ], null );
					if ( uri_string != null )
					{
						m_data_source_schema_uri = new Uri( uri_string );
					}
				}
				return ( m_data_source_schema_uri );
			}
		}

		private string m_server_type = null;

		/// <summary>
		///		The type of remoting object this plugin
		///		should function as.  Returns either "SingleCall"
		///		or "Singleton."  If it is not defined in the
		///		plugin configuration file then this property will
		///		return "SingleCall."
		/// </summary>
		protected string ServerType
		{
			get
			{
				if ( m_server_type == null )
				{
					m_server_type =
						GetStringValue( ConfigFile.Tables[ "plugin" ].Rows[ 
						Index ][ "serverType" ], "SingleCall" );
				}
				return ( m_server_type );
			}
		}

		/*/// <summary>
		///		Returns this plugin's server lifetime if it
		///		is defined in the plugin configuration file;
		///		otherwise 0.
		/// </summary>
		private int m_server_lifetime = -1;

		/// <summary>
		///		Returns this plugin's server lifetime if it
		///		is defined in the plugin configuration file;
		///		otherwise 0.
		/// </summary>
		protected int ServerLifetime
		{
			get
			{
				if ( m_server_lifetime == -1 )
				{
					m_server_lifetime =
						GetInt32Value( ConfigFile.Tables[ "plugin" ].Rows[ Index ][ "serverLifetime" ], 0 );
				}
				return ( m_server_lifetime );
			}
		}*/

		/// <summary>
		///		The 0-based index of the plugin in the plugin configuration file.
		/// </summary>
		private int m_index = -1;
		
		/// <summary>
		///		The 0-based index of the plugin in the plugin configuration file.
		/// </summary>
		protected int Index
		{
			get
			{
				if ( m_index == -1 )
				{
					string uri = System.Runtime.Remoting.RemotingServices.GetObjectUri( this );
					string index = Regex.Match( uri, @"^.*(?<index>\d{2})\.rem$", 
						RegexOptions.IgnoreCase ).Groups[ "index" ].Value;
					m_index = Convert.ToInt32( index );
				}
				return ( m_index );
			}
		}
		
		/// <summary>
		///		Gets a string value from a DB value and returns the
		///		given defaultValue if the give value is DBNull.
		/// </summary>
		/// <param name="value">The DB value to convert to a string.</param>
		/// <param name="defaultValue">The value to return if the given value is DBNull.</param>
		/// <returns>
		///		If value is not DBNull then that value converted to a string;
		///		otherwise defaultValue.
		/// </returns>
		private string GetStringValue( object value, string defaultValue )
		{
			return ( value is DBNull ? defaultValue : Convert.ToString( value, CultureInfo.CurrentCulture ) );
		}

		/// <summary>
		///		Gets an integer value from a DB value and returns the
		///		given defaultValue if the give value is DBNull.
		/// </summary>
		/// <param name="value">The DB value to convert to an integer.</param>
		/// <param name="defaultValue">The value to return if the given value is DBNull.</param>
		/// <returns>
		///		If value is not DBNull then that value converted to an integer;
		///		otherwise defaultValue.
		/// </returns>
		private int GetInt32Value( object value, int defaultValue )
		{
			return ( value is DBNull ? defaultValue : Convert.ToInt32( value ) );
		}

		/// <summary>
		///		Gets a boolean value from a DB value and returns the
		///		given defaultValue if the give value is DBNull.
		/// </summary>
		/// <param name="value">The DB value to convert to a boolean.</param>
		/// <param name="defaultValue">The value to return if the given value is DBNull.</param>
		/// <returns>
		///		If value is not DBNull then that value converted to a boolean;
		///		otherwise defaultValue.
		/// </returns>
		private bool GetBoolValue( object value, bool defaultValue )
		{
			return ( value is DBNull ? defaultValue : bool.Parse(
				Convert.ToString( value, CultureInfo.CurrentCulture ) ) );
		}
		
		/// <summary>
		///		Activates the plugin for first-time use.  This method is necessary
		///		because not all plugins are activated with the 'new' keyword, instead
		///		some are activated with 'Activator.GetObject' and a method is required
		///		to force the plugin's construction in order to catch any exceptions that
		///		may be associated with a plugin's construction.
		/// </summary>
		public virtual void Activate()
		{
			this.Activate( string.Empty );
		}

		/// <summary>
		///		Activates the plugin for first-time use.  This method is necessary
		///		because not all plugins are activated with the 'new' keyword, instead
		///		some are activated with 'Activator.GetObject' and a method is required
		///		to force the plugin's construction in order to catch any exceptions that
		///		may be associated with a plugin's construction.
		/// </summary>
		/// <param name="activationData">
		///		A plugin designer can pass data into the plugin from the plugin configuration
		///		file by passing the data into the plugin as a string formatted variable.
		/// </param>
		public virtual void Activate( string activationData )
		{
			
		}
	}
}
