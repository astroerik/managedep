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
using System.Net;
using System.Xml;
using System.Data;
using System.Text;
using Sudowin.Common;
using System.Xml.Schema;
using System.Diagnostics;
using System.Globalization;
using System.DirectoryServices;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Runtime.Remoting.Lifetime;

namespace Sudowin.Plugins.Authorization.Xml
{
	/// <summary>
	///		Used to access sudoer information stored in a xml
	///		file that adheres to the XmlAuthorizationPlugin schema.
	/// </summary>
	public class XmlAuthorizationPlugin : AuthorizationPlugin
	{
		private struct FindCommandNodeSearchParameters
		{
			public string Path;
			public string Arguments;
			public string Md5Checksum;
		}
		
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudowin.WindowsService.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		XmlAuthorizationPlugin boolean that allows
		///		bool to be null.
		/// </summary>
		private enum XapBool : short
		{
			False = 0,
			True = 1,
			Null = 2,
		}
		
		/// <summary>
		///		True if the sudoers file has been read;
		///		otherwise false.
		/// </summary>
		private bool m_is_connection_open = false;

		/// <summary>
		///		True if the sudoers file has been read;
		///		otherwise false.
		/// </summary>
		private bool IsConnectionOpen
		{
			get 
			{ 
				 return ( m_is_connection_open );
			}
		}

		/// <summary>
		///		Format for translating a value in an xpath
		///		query into all lowercase.
		/// </summary>
		private const string XpathTranslateFormat = 
			"translate({0}," + 
			"'ABCDEFGHIJKLMNOPQRSTUVWXYZ'," + 
			"'abcdefghijklmnopqrstuvwxyz')";

		/// <summary>
		///		For parsing the xml file.
		/// </summary>
		private XmlDocument m_xml_doc = new XmlDocument();

		/// <summary>
		///		For resolving namespaces in the xml file
		///		so that xpath queries will work.
		/// </summary>
		private XmlNamespaceManager m_namespace_mgr;

		/// <summary>
		///		Default constructor.
		/// </summary>
		public XmlAuthorizationPlugin()
		{
			m_ts.TraceEvent( TraceEventType.Start, 10, "constructing XmlAuthorizationPlugin" );
			m_ts.TraceEvent( TraceEventType.Stop, 10, "constructed XmlAuthorizationPlugin" );
		}

		/// <summary>
		///		Gets the xml node for the given user name.
		/// </summary>
		/// <param name="userName">
		///		The xpath query in this method uses this value
		///		to search for a xml node of type user with a name
		///		attribute value equal to the value of this parameter.
		/// </param>
		/// <returns>
		///		Xml node with the name attribute equal to
		///		the value of the userName parameter of this
		///		method.
		/// </returns>
		private XmlNode FindUserNode( string userName )
		{
			return ( FindUserNode( userName, false ) );
		}

		private XmlNode FindUserNode( string userName, bool searchGroups )
		{
			// find the user in the xml file.  to do this
			// we first build a xpath query which will look for
			// the user with the given user name
			string user_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"//d:user[{0} = {1}]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@name" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'" + userName + "'" ) );

			// find the user's node in the xml file.  if the 
			// user is not found then the query will return null 
			XmlNode user_node = m_xml_doc.SelectSingleNode(
				user_xpq, m_namespace_mgr );

			if ( searchGroups && user_node == null )
			{
				// get the domain/host name and user name parts
				// of the user name
				string[] un_split = userName.Split( new char[] { '\\' } );
				string usr_dhn_part = un_split[ 0 ];
				string usr_un_part = un_split[ 1 ];

				// get a list of all the userGroup nodes in the sudoers file
				XmlNodeList ug_node_list = FindUserGroupNodes();
				foreach ( XmlNode ug_node in ug_node_list )
				{
					string ug_name = ug_node.Attributes[ "name" ].Value;

					// determine if this is a local group or a domain group
					bool is_local_group = false;
					Regex grp_parts_rx = new Regex( @"(?<dhp>[^\\]+)\\(?<np>.*)", RegexOptions.IgnoreCase );
					Match grp_parts_m = grp_parts_rx.Match( ug_name );
					string grp_dhn_part = string.Empty;
					string grp_gn_part = string.Empty;

					if ( !grp_parts_m.Success )
					{
						is_local_group = true;
					}
					else
					{
						// domain or host name part / name part of group
						grp_dhn_part = grp_parts_m.Groups[ "dhp" ].Value;
						grp_gn_part = grp_parts_m.Groups[ "np" ].Value;

						if ( string.Compare( grp_dhn_part, Environment.MachineName, true ) == 0 )
						{
							is_local_group = true;
						}
					}

					// gets set to true if the user is a member of the current group
					bool is_member = false;

					if ( is_local_group )
					{
						// get the directory entries for the localhost and the current group
						DirectoryEntry localhost = new DirectoryEntry(
							string.Format(
							CultureInfo.CurrentCulture,
							"WinNT://{0},computer",
							Environment.MachineName ) );
                        DirectoryEntry group = DirectoryFinder.Find(localhost.Children, ug_name);

                        // group not found so throw exception
                        if (group == null)
                        {
                            throw SudoException.GetException(SudoResultTypes.GroupNotFound, ug_name);
                        }

						// used for asdi calls
						object[] user_path = null;

						// local user
						if ( Regex.IsMatch( usr_dhn_part, Environment.MachineName, RegexOptions.IgnoreCase ) )
						{
							// find the user instead of building the path.  this is 
							// in case this machine belongs to a workgroup or a domain.  
							//  it is easier to search for the user and get their path that 
							// way than it is to get the computer's workgroup
							DirectoryEntry user = DirectoryFinder.Find( localhost.Children, usr_un_part, "user" );

                            // user not found so throw exception
                            if (user == null)
                            {
                                throw SudoException.GetException(SudoResultTypes.UsernameNotFound, usr_dhn_part, usr_un_part);
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
								usr_dhn_part, usr_un_part )
						};
						}

						is_member = bool.Parse( Convert.ToString(
							group.Invoke( "IsMember", user_path ),
							CultureInfo.CurrentCulture ) );
						group.Close();
						localhost.Close();
					}
					else
					{
						DirectoryEntry domain = new DirectoryEntry();

						DirectorySearcher dsrchr = new DirectorySearcher(
							domain, "samAccountName=" + grp_gn_part, null, SearchScope.Subtree );
						SearchResult sr = dsrchr.FindOne();
						if ( sr == null )
						{
							break;
						}
						DirectoryEntry group = sr.GetDirectoryEntry();

						dsrchr = new DirectorySearcher(
							domain, "samAccountName=" + usr_un_part, null, SearchScope.Subtree );
						sr = dsrchr.FindOne();
						if ( sr == null )
						{
							break;
						}
						DirectoryEntry user = sr.GetDirectoryEntry();

						is_member = bool.Parse( Convert.ToString(
							group.Invoke( "IsMember", user.Path ),
							CultureInfo.CurrentCulture ) );
						
						user.Close();
						group.Close();
						domain.Close();
					}

					// if the user belongs to this user group then return
					// the user group node as if it was the user node
					if ( is_member )
					{
						user_node = ug_node;

						// set this node's name attribute to be the 
						// name of the user, not the name of the group
						user_node.Attributes[ "name" ].Value = userName;

						break;
					}
				}
			}

			return ( user_node );
		}

		private XmlNodeList FindUserGroupNodes()
		{
			return ( m_xml_doc.GetElementsByTagName( "userGroup" ) );
		}

		/// <summary>
		///		Opens a connection to the xml file
		///		and validate the data with the given
		///		schema file.
		/// </summary>
		private void Open()
		{
			m_ts.TraceEvent( TraceEventType.Start, 10, "opening XmlAuthorizationPlugin datasource connection" );

			// the sudoers cache file is the primary data source
			if ( DataSourceCacheUseAsPrimary )
			{
				// the sudoers cache file exists
				if ( File.Exists( DataSourceCacheFilePath ) )
				{
					// the sudoers cache file needs to be updated
					// from the sudoers file
					if ( ( DateTime.Now - File.GetLastWriteTime( DataSourceCacheFilePath ) ) >
						DataSourceCacheUpdateFrequency )
					{
						// the sudoers file exists
						if ( File.Exists( DataSourceConnectionString ) )
						{
							LoadFromDataSource( DataSourceConnectionString, DataSourceSchemaUri );
							m_xml_doc.Save( DataSourceCacheFilePath );
						}

						// the sudoers file does not exist
						else
						{
							if ( DataSourceCacheUseStaleCache )
							{
								LoadFromDataSource( DataSourceCacheFilePath, DataSourceSchemaUri );
							}
							else
							{
								throw ( new FileNotFoundException( "sudoers file not found" ) );
							}
						}
					}

					// load the sudoers cache file into m_xml_doc
					else
					{
						LoadFromDataSource( DataSourceCacheFilePath, DataSourceSchemaUri );
					}
				}

				// cache does not exist, create it
				else
				{
					// the sudoers file exists
					if ( File.Exists( DataSourceConnectionString ) )
					{
						LoadFromDataSource( DataSourceConnectionString, DataSourceSchemaUri );
						m_xml_doc.Save( DataSourceCacheFilePath );
					}

					// the sudoers file does not exist
					else
					{
						throw ( new FileNotFoundException( "sudoers file not found" ) );
					}
				}
			}
			
			// the sudoers cache file is not the primary source
			else
			{
				// the sudoers file exists
				if ( File.Exists( DataSourceConnectionString ) )
				{
					// load the sudoers file
					LoadFromDataSource( DataSourceConnectionString, DataSourceSchemaUri );

					// the sudoers cache is enabled
					if ( DataSourceCacheEnabled )
					{
						// the sudoers cache file exists
						if ( File.Exists( DataSourceCacheFilePath ) )
						{
							// the sudoers cache file needs to be updated
							// from the sudoers file
							if ( ( DateTime.Now - File.GetLastWriteTime( DataSourceCacheFilePath ) ) >
								DataSourceCacheUpdateFrequency )
							{
								m_xml_doc.Save( DataSourceCacheFilePath );
							}
						}

						// the sudoers cache file does not exist
						else
						{
							// create the sudoers cache file
							m_xml_doc.Save( DataSourceCacheFilePath );
						}
					}
				}

				// the sudoers file does not exist
				else
				{
					// use the sudoers cache file
					if ( DataSourceCacheEnabled )
					{
						// the sudoers cache file exists
						if ( File.Exists( DataSourceCacheFilePath ) )
						{
							// the sudoers cache file needs to be updated
							// from the sudoers file
							if ( ( DateTime.Now - File.GetLastWriteTime( DataSourceCacheFilePath ) ) >
								DataSourceCacheUpdateFrequency )
							{
								if ( DataSourceCacheUseStaleCache )
								{
									LoadFromDataSource( DataSourceCacheFilePath, DataSourceSchemaUri );
								}
								else
								{
									throw ( new Exception( "DataSourceCacheUseStaleCache not enabled" ) );
								}
							}

							// the sudoers cache is up to date so load it
							else
							{
								LoadFromDataSource( DataSourceCacheFilePath, DataSourceSchemaUri );
							}
						}

						// the sudoers cache file does not exist
						else
						{
							throw ( new FileNotFoundException( "sudoers cache file not found" ) );
						}
					}
					else
					{
						throw ( new Exception( "DataSourceCacheEnabled is false" ) );
					}
				}
			}

			m_ts.TraceEvent( TraceEventType.Stop, 10, "opened XmlAuthorizationPlugin datasource connection" );
		}

		private void LoadFromDataSource( string connectionString, Uri schemaFileUri )
		{
			// create a xmlreadersettings object
			// to specify how to read in the file
			XmlReaderSettings xrs = new XmlReaderSettings();
			xrs.CloseInput = true;
			xrs.IgnoreComments = true;

			xrs.Schemas.Add( null, schemaFileUri.AbsoluteUri );
			xrs.ValidationType = ValidationType.Schema;

			// read in the file
            using (XmlReader xr = XmlReader.Create(connectionString, xrs))
            {
                // load the xml reader into the xml document.
                m_xml_doc.Load(xr);
            }

			// create the namespace manager using the xml file name table
			m_namespace_mgr = new XmlNamespaceManager( m_xml_doc.NameTable );

			// if there is a default namespace specified in the
			// xml file then it needs to be added to the namespace
			// manager so the xpath queries will work
			Regex ns_rx = new Regex(
				@"xmlns\s{0,}=\s{0,}""([\w\d\.\:\/\\]{0,})""",
				RegexOptions.Multiline | RegexOptions.IgnoreCase );
			Match ns_m = ns_rx.Match( m_xml_doc.InnerXml );

			// add the default namespace
			string default_ns = ns_m.Groups[ 1 ].Value;
			m_namespace_mgr.AddNamespace( "d", default_ns );

			m_is_connection_open = true;
		}

		/// <summary>
		///		Present for compliance with IAuthorizationPlugin.
		/// </summary>
		private void Close()
		{
			m_is_connection_open = false;
		}

		#region IAuthorizationPlugin Members

		/// <summary>
		///		Gets a Sudowin.Common.UserInfo structure
		///		from the authorization source for the given user name.
		/// </summary>
		/// <param name="userName">
		///		The name of the user to retrieve the information 
		///		for.  This name should be in the format:
		/// 
		///			HOST_OR_DOMAIN\USERNAME
		/// </param>
		/// <param name="userInfo">
		///		Sudowin.Common.UserInfo structure for
		///		the given user name.
		/// </param>
		/// <returns>
		///		True if the UserInfo struct is successfuly retrieved; 
		///		false if otherwise.
		/// </returns>
		public override bool GetUserInfo( string userName, ref UserInfo userInfo )
		{
			if ( !this.IsConnectionOpen )
			{
				this.Open();
			}

			// get the user node for this user
			XmlNode unode = FindUserNode( userName, true );

			// if we cannot find the user then return false
			if ( unode == null )
				return ( false );

			// temp values
			int itv;
			string stv;
			LoggingLevelTypes llttv;

			GetUserAttributeValue( unode, true, "invalidLogons", out itv );
			userInfo.InvalidLogons = itv;

			GetUserAttributeValue( unode, true, "timesExceededInvalidLogons", out itv );
			userInfo.TimesExceededInvalidLogons = itv;

			GetUserAttributeValue( unode, true, "invalidLogonTimeout", out itv );
			userInfo.InvalidLogonTimeout = itv;

			GetUserAttributeValue( unode, true, "lockoutTimeout", out itv );
			userInfo.LockoutTimeout = itv;

			GetUserAttributeValue( unode, true, "logonTimeout", out itv );
			userInfo.LogonTimeout = itv;

			GetUserAttributeValue( unode, true, "privilegesGroup", out stv );
			userInfo.PrivilegesGroup = stv;

			GetUserAttributeValue( unode, true, "loggingLevel", out llttv );
			userInfo.LoggingLevel = llttv;
			
			return ( true );
		}

		/// <summary>
		///		Gets a Sudowin.Common.CommandInfo structure
		///		from the authorization source for the given user name,
		///		command path, and command arguments.
		/// </summary>
		/// <param name="userName">
		///		The name of the user to retrieve the information 
		///		for.  This name should be in the format:
		/// 
		///			HOST_OR_DOMAIN\USERNAME
		/// </param>
		/// <param name="commandPath">
		///		Command path to get information for.
		/// </param>
		/// <param name="commandArguments">
		///		Command arguments to get information for.
		/// </param>
		/// <param name="commandInfo">
		///		Sudowin.Common.CommandInfo structure for
		///		the given user name, command path, and command 
		///		arguments.
		/// </param>
		/// <returns>
		///		True if the CommandInfo struct is successfuly retrieved; 
		///		false if otherwise.
		/// </returns>
		public override bool GetCommandInfo(
			string username,
			string commandPath,
			string commandArguments,
			ref CommandInfo commandInfo )
		{
			if ( !this.IsConnectionOpen )
			{
				this.Open();
			}
			
			// find the user node
			XmlNode u_node = FindUserNode( username, true );

			if ( u_node == null )
				return ( false );

			// even though the command node might be null, return true now
			// if the user is authorized to sudo all commands.  the null reference
			// will never be referenced later on
			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// are all commands allowed?
			XapBool all_cmds_allwd;
			GetUserAttributeValue(
				u_node, true, "allowAllCommands", out all_cmds_allwd );
			if ( all_cmds_allwd == XapBool.True )
			{
				commandInfo.IsCommandAllowed = true;
				return ( true );
			}

			// find the command node
			//XmlNode c_node = FindCommandNode( u_node, commandPath, commandArguments );
			FindCommandNodeSearchParameters fnsp = new FindCommandNodeSearchParameters();
			fnsp.Path = commandPath;
			fnsp.Arguments = commandArguments;
			fnsp.Md5Checksum = CalculateMd5Checksum( commandPath );
			XmlNode c_node = FindCommandNode( u_node, fnsp );

			if ( c_node == null )
				return ( false );

			commandInfo.IsCommandAllowed = IsCommandAllowed( u_node, c_node, commandArguments );

			LoggingLevelTypes llttv;
			GetCommandAttributeValue( u_node, true, c_node, "loggingLevel", out llttv );
			commandInfo.LoggingLevel = llttv;

			return ( true );
		}

		/// <summary>
		///		Calculates an MD5 Checksum for a given command.
		/// </summary>
		/// <param name="commandPath">
		///		The command to get the MD5 Checksum for.
		/// </param>
		/// <returns>
		///		An MD5 Checksum for a given command if the command
		///		exists; otherwise null.
		/// </returns>
		private string CalculateMd5Checksum( string commandPath )
		{
			if ( !File.Exists( commandPath ) )
				return ( null );
				
			FileStream fs = new FileStream( commandPath, FileMode.Open, FileAccess.Read );
			MD5 md5 = MD5.Create();
			byte[] hash = md5.ComputeHash( fs );
			StringBuilder sb = new StringBuilder( 32 );
			for ( int x = 0; x < hash.Length; ++x )
			{
				sb.AppendFormat( "{0:x2}", hash[ x ] );
			}
			fs.Close();
			return ( sb.ToString() );
		}

		/// <summary>
		///		Checks to see if the user has the right
		///		to execute the given command with Sudowin.
		/// </summary>
		/// <param name="userNode">
		///		User node that represents the user that
		///		invoked Sudowin.
		/// </param>
		/// <param name="searchParameters">
		///		The search parameters.
		/// </param>
		/// <returns>
		///		True if the command is allowed, false if it is not.
		/// </returns>
		private XmlNode FindCommandNode(
			XmlNode userNode,
			FindCommandNodeSearchParameters searchParameters )
		{
			/*
			 * if the user is not disabled we need to discover whether
			 * or not the command they are trying to sudo is allowed.
			 * to do this we must look in 4 potential locations in
			 * the following order
			 * 
			 * 1) commands node local to user
			 * 2) commandGroupRefs node local to user
			 * 3) commands node local to user's parent user group
			 * 4) commandGroupRefs node local to user's parent user group
			 * 
			 * ex. if the command is found in step 1 its allowances
			 * will be decided in step 1 and the result will be returned.
			 * the check only falls to the next step if the command is not
			 * found in a previous step.  in other words, if we reach
			 * step 2 then it means we did not find the command in step 1.
			 * 
			 * define a xml node that will point to the
			 * node that represents the command the user
			 * is attempting to use sudo to execute
			 */
			XmlNode cmd_node = null;

			// 1) commands node local to user
			XmlNode local_cmds = userNode.SelectSingleNode(
				"d:commands", m_namespace_mgr );
			if ( local_cmds != null && local_cmds.HasChildNodes )
				cmd_node = FindCommandNode( searchParameters, local_cmds );
			
			// 2) commandGroupRefs node local to user
			if ( cmd_node == null )
			{
				XmlNode local_cmd_refs = userNode.SelectSingleNode(
					"d:commandGroupRefs", m_namespace_mgr );
				if ( local_cmd_refs != null && local_cmd_refs.HasChildNodes )
				{
					cmd_node = FindCommandNode( userNode,
						local_cmd_refs, searchParameters );
				}
			}
			
			// 3) commands node local to user's parent user group
			if ( cmd_node == null )
			{
				XmlNode parent_cmds = userNode.ParentNode.ParentNode.SelectSingleNode(
					"d:commands", m_namespace_mgr );

				if ( parent_cmds != null && parent_cmds.HasChildNodes )
					cmd_node = FindCommandNode( searchParameters, parent_cmds );
			}

			// 4) commandGroupRefs node local to user's parent user group
			if ( cmd_node == null )
			{
				XmlNode parent_cmd_refs = userNode.ParentNode.ParentNode.SelectSingleNode(
					"d:commandGroupRefs", m_namespace_mgr );
				if ( parent_cmd_refs != null && parent_cmd_refs.HasChildNodes )
				{
					cmd_node = FindCommandNode( userNode,
						parent_cmd_refs, searchParameters );
				}
			}
			return ( cmd_node );
		}

		#endregion

		#region IDisposable Members

		/// <summary>
		///		Present for compliance with IAuthorizationPlugin.
		/// </summary>
		public override void Dispose()
		{
			this.Close();
		}

		#endregion

		/// <summary>
		///		Examines the attributes of a command
		///		node and determines whether or not
		///		the user is allowed to execute the
		///		given command with Sudowin.
		/// </summary>
		/// <param name="userNode">
		///		User node that represents the user that
		///		invoked Sudowin.
		/// </param>
		/// <param name="commandNode">
		///		Command node that represents the command
		///		the user is attempting to execute with Sudowin.
		///	</param>
		/// <param name="commandArguments">
		///		Arguments of the command being executed.
		/// </param>
		/// <returns>
		///		True if the command is allowed, false if it is not.
		/// </returns>
		private bool IsCommandAllowed(
			XmlNode userNode,
			XmlNode commandNode,
			string commandArguments )
		{
			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// is the user enabled?
			XapBool user_enabled;
			GetUserAttributeValue(
				userNode, false, "enabled", out user_enabled );

			if ( user_enabled == XapBool.False )
				return ( false );

			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// is the command enabled?
			//
			// pass a null value for the userNode parameter so that
			// the we won't look past the command node's immediate
			// parent for this attribute value
			XapBool cmd_enabled;
			GetCommandAttributeValue(
				null, false, commandNode, "enabled", out cmd_enabled );
			if ( cmd_enabled == XapBool.False )
				return ( false );

			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// if the command is enabled then check to see if
			// it is being executed within a valid timeframe
			DateTime cmd_st, cmd_et;
			GetCommandAttributeValue(
				userNode, true, commandNode, "startTime", out cmd_st );
			GetCommandAttributeValue(
				userNode, true, commandNode, "endTime", out cmd_et );

			DateTime now = DateTime.Now;
			if ( !( now.TimeOfDay >= cmd_st.TimeOfDay &&
				now.TimeOfDay <= cmd_et.TimeOfDay ) )
				return ( false );

			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// check to see if the command is being executed on an 
			// allowed host or network
			string allwd_nets = string.Empty;
			GetCommandAttributeValue(
				userNode, true, commandNode, "allowedNetworks", out allwd_nets );
			if ( allwd_nets.Length > 0 )
			{
				IPHostEntry host_entry = Dns.GetHostEntry( Dns.GetHostName() );
				
				string[] allwd_nets_vals = allwd_nets.Split( new char[] { ',' } );
				bool host_exists_in_allwd_nets = false;
				Array.ForEach<string>( allwd_nets_vals, delegate( string anv )
				{
					// match * for all networks and match 127.0.0.1 since 
					// it will always resolve to the local host
					if ( Regex.IsMatch( anv, @"^(\*|(127\.0\.0\.1)|(localhost))$", 
						RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace ) )
					{
						host_exists_in_allwd_nets = true;
						return;
					}

					// hostname
					if ( Regex.IsMatch( host_entry.HostName, anv,
						RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace ) )
					{
						host_exists_in_allwd_nets = true;
						return;
					}

					// ip address
					Array.ForEach<IPAddress>( host_entry.AddressList, delegate( IPAddress host_ip )
					{
						if ( Regex.IsMatch( host_ip.ToString(), anv,
							RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace ) )
						{
							host_exists_in_allwd_nets = true;
							return;
						}
					} );

					if ( host_exists_in_allwd_nets )
					{
						return;
					}
					
					/*
					// ip range
					else if ( Regex.IsMatch( anv, @"^(\d{1,3}\.){2}(\d{1,3}\.?)(\d{1,3})?/(\d{1,2}|((\d{1,3}\.){3}\d{1,3}))$" ) )
					{
						// TODO: write an network calculator to handle this case
					}
					*/
				} );

				if ( !host_exists_in_allwd_nets )
				{
					return ( false );
				}
			}
			
			//**********************************************************
			// !!! RETURN RETURN RETURN !!!
			//
			// well, if we made it this far it means the user is
			// allowed to execute the command
			return ( true );
		}

		/// <summary>
		///		Searches for a command group node that has
		///		a name attribute equal to the name of
		///		the command group reference name being
		///		searches for.
		/// </summary>
		/// <param name="userNode">
		///		User node from which to start looking for
		///		the command node.
		/// </param>
		/// <param name="commandGroupRefsParent">
		///		Node that has commandGroupRef nodes as children.
		/// </param>
		/// <param name="commandPath">
		///		Fully qualified path of the command being executed.
		/// </param>
		/// <param name="commandArguments">
		///		Arguments of the command being executed.
		/// </param>
		/// <returns>
		///		Command node associated with the given userNode,
		///		commandPath, and commandArguments from the 
		///		sudoers data store.
		/// </returns>
		private XmlNode FindCommandNode(
			XmlNode userNode,
			XmlNode commandGroupRefsParent,
			FindCommandNodeSearchParameters searchParameters )
			//string commandPath,
			//string commandArguments )
		{
			XmlNode cmd_node = null;

			// for each command group reference build a xpath
			// query and then get the command group
			foreach ( XmlNode cmd_ref in commandGroupRefsParent.ChildNodes )
			{
				string cmdgrp_xpq = string.Format(
					CultureInfo.CurrentCulture,
					@"//d:commandGroup[{0} = {1}]",
					string.Format( CultureInfo.CurrentCulture,
						XpathTranslateFormat, "@name" ),
					string.Format( CultureInfo.CurrentCulture,
						XpathTranslateFormat,
						"'" + cmd_ref.Attributes[ "commandGroupName" ].Value + "'" ) );

				// search for the command group
				XmlNode cmdgrp = m_xml_doc.SelectSingleNode(
					cmdgrp_xpq, m_namespace_mgr );

				// if the command group is found then search
				// the command group for the command
				if ( cmdgrp != null && cmdgrp.HasChildNodes )
					cmd_node = FindCommandNode( searchParameters, cmdgrp );

				// if the command is found determine if it is
				// allowed to be executed
				if ( cmd_node != null )
					break;
			}

			return ( cmd_node );
		}

		/// <summary>
		///		Searches for a command node that has
		///		a path attribute equal to the path of
		///		of the command that the user is attempting
		///		to execute with Sudowin.
		/// </summary>
		/// <param name="searchParameters">
		///		The search parameters.
		/// </param>
		/// <param name="commandNodeParent">
		///		Node that has command nodes as children.
		/// </param>
		/// <returns></returns>
		private XmlNode FindCommandNode(
			FindCommandNodeSearchParameters searchParameters,
			XmlNode commandNodeParent )
		{
			// build the query used to look for the command node in
			// the current node context with the given command path
			// and md5 checksum (if one is defined)
			string cmd_xpq = string.Format(
				CultureInfo.CurrentCulture,
				@"d:command[{0} = {1} and ( {2} = {3} or {2} = '' )]",
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@path" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'" + searchParameters.Path + "'" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "@md5Checksum" ),
				string.Format( CultureInfo.CurrentCulture,
					XpathTranslateFormat, "'" + searchParameters.Md5Checksum + "'" ) );

			// look for the command node
			XmlNode cmd_node = commandNodeParent.SelectSingleNode(
				cmd_xpq, m_namespace_mgr );
			
			// check to see if the arguments match
			if ( cmd_node != null )
			{
				string arg_string = cmd_node.Attributes[ "argumentString" ] == null ?
					null : cmd_node.Attributes[ "argumentString" ].Value;
					;
				if ( arg_string != null )
				{
					Regex is_rx = new Regex( @"^/(?<rx>.*)/$" );
					Match is_rx_m = is_rx.Match( arg_string );
					// argument string is a regular expression
					if ( is_rx_m.Success )
					{
						string rx_patt = is_rx_m.Groups[ "rx" ].Value;
						if ( !Regex.IsMatch( searchParameters.Arguments, rx_patt ) )
						{
							// do not return the command node if the arguments do
							// not match the pattern
							cmd_node = null;
						}
					}
					// argument string is not a regular expression
					else
					{
						if ( searchParameters.Arguments != arg_string )
						{
							// do not return the command node if the arguments do
							// not match the pattern
							cmd_node = null;
						}
					}
				}
			}

			return ( cmd_node );
		}

		#region GetUserAttributeValue +4

		/// <summary>
		///		Gets the value of a user attribute.
		/// </summary>
		/// <param name="userNode">
		///		User node to get the attribute value from.
		/// </param>
		/// <param name="checkDefaults">
		///		Whether or not to look at the default settings
		///		for this attribute if it cannot be found at
		///		a lower level.
		/// </param>
		/// <param name="attributeName">
		///		Name of the attribute value to get.
		/// </param>
		/// <param name="attributeValue">
		///		Value of the of the attribute.
		/// </param>
		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out string attributeValue )
		{
			// is this setting defined on the actual user node?
			if ( userNode.Attributes[ attributeName ] != null )
			{
				attributeValue = userNode.Attributes[ attributeName ].Value;
			}
			// is this setting defined on the userGroup node
			// that this user belongs to?
			else if ( userNode.ParentNode.ParentNode.Attributes[ attributeName ] != null )
			{
				attributeValue = 
					userNode.ParentNode.ParentNode.Attributes[ attributeName ].Value;
			}
			// go to the default settings
			else if ( checkDefaults )
			{
				XmlNode an = userNode.OwnerDocument.DocumentElement.Attributes.GetNamedItem( attributeName );
				if ( an != null )
					attributeValue = an.Value;
				else
					attributeValue = string.Empty;
				//attributeValue =
				//	userNode.OwnerDocument.DocumentElement.Attributes[ attributeName ].Value;
			}
			else
				attributeValue = string.Empty;
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out XapBool attributeValue )
		{
			string temp_value;
			GetUserAttributeValue( userNode, checkDefaults, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				XapBool.Null :
				( XapBool ) Enum.Parse( typeof( XapBool ), temp_value, true );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out LoggingLevelTypes attributeValue )
		{
			string temp_value;
			GetUserAttributeValue( userNode, checkDefaults, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				0 :
				( LoggingLevelTypes ) Enum.Parse( typeof( LoggingLevelTypes ), temp_value, true );

		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out int attributeValue )
		{
			string temp_value;
			GetUserAttributeValue( userNode, checkDefaults, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				-1 :
				int.Parse( temp_value, CultureInfo.CurrentCulture );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetUserAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			string attributeName,
			out DateTime attributeValue )
		{
			string temp_value;
			GetUserAttributeValue( userNode, checkDefaults, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				DateTime.MinValue :
				DateTime.Parse( temp_value, CultureInfo.CurrentCulture );
		}

		#endregion

		#region GetCommandAttributeValue +4

		/// <summary>
		///		Gets the value of a command attribute.
		/// </summary>
		/// <param name="userNode">
		///		User node that the command node is physically
		///		under or logically under by way of a command
		///		group reference.
		/// </param>
		/// <param name="checkDefaults">
		///		Whether or not to look at the default settings
		///		for this attribute if it cannot be found at
		///		a lower level.
		/// </param>
		/// <param name="commandNode">
		///		Command node to get the attribute value from.
		/// </param>
		/// <param name="attributeName">
		///		Name of the attribute value to get.
		/// </param>
		/// <param name="attributeValue">
		///		Value of the of the attribute.
		/// </param>
		/// <remarks>
		///		Only use this method to get attribute values that
		///		are also set at the default level since this method
		///		will travel up to the default level to look for
		///		the attribute value if it cannot find it at a lower
		///		level.
		/// </remarks>
		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out string attributeValue )
		{
			// is this setting defined on the actual command node?
			if ( commandNode.Attributes[ attributeName ] != null )
			{
				attributeValue = commandNode.Attributes[ attributeName ].Value;
			}
			// is this setting defined on the commandGroup node
			// that this command belongs to?
			else if ( commandNode.ParentNode.Attributes[ attributeName ] != null )
			{
				attributeValue = commandNode.ParentNode.Attributes[ attributeName ].Value;
			}
			// look at the user and then up from there for the attribute value
			else if ( userNode != null )
			{
				GetUserAttributeValue(
					userNode, checkDefaults, attributeName, out attributeValue );
			}
			else
			{
				attributeValue = string.Empty;
			}
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out XapBool attributeValue )
		{
			string temp_value;
			GetCommandAttributeValue( 
				userNode, checkDefaults, commandNode, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				XapBool.Null :
				( XapBool ) Enum.Parse( typeof( XapBool ), temp_value, true );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out LoggingLevelTypes attributeValue )
		{
			string temp_value;
			GetCommandAttributeValue(
				userNode, checkDefaults, commandNode, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				0 :
				( LoggingLevelTypes ) Enum.Parse( typeof( LoggingLevelTypes ), temp_value, true );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out int attributeValue )
		{
			string temp_value;
			GetCommandAttributeValue( 
				userNode, checkDefaults, commandNode, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				-1 :
				int.Parse( temp_value, CultureInfo.CurrentCulture );
		}

		[System.Diagnostics.DebuggerStepThrough]
		private void GetCommandAttributeValue(
			XmlNode userNode,
			bool checkDefaults,
			XmlNode commandNode,
			string attributeName,
			out DateTime attributeValue )
		{
			string temp_value;
			GetCommandAttributeValue( 
				userNode, checkDefaults, commandNode, attributeName, out temp_value );
			attributeValue = temp_value == string.Empty ?
				DateTime.MinValue :
				DateTime.Parse( temp_value, CultureInfo.CurrentCulture );
		}

		#endregion

		/// <summary>
		///		Verifies the given user is allowed to execute
		///		the given command with the given arguments.
		/// </summary>
		/// <param name="userName">
		///		The name of the user to verify the command
		///		for.  This name should be in the format:
		/// 
		///			HOST_OR_DOMAIN\USERNAME
		/// </param>
		/// <param name="commandPath">
		///		The path of the command the user is attempting
		///		to execute.
		/// </param>
		/// <param name="commandArguments">
		///		The arguments to the command the user is
		///		attempting to execute.
		/// </param>
		/// <returns>
		///		True if the user is allowed to execute the command;
		///		otherwise false.
		/// </returns>
		public override bool VerifyCommand(
			string userName,
			ref string commandPath,
			string commandArguments )
		{
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering VerifyCommand( string, ref string, string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},commandPath={1},commandArguments={2}",
				userName, commandPath, commandArguments );
				
			if ( !this.IsConnectionOpen )
			{
				this.Open();
			}

			CommandInfo ci = new CommandInfo();

			// declare this method's return value
			bool isCommandVerified = 

				!IsShellCommand( commandPath )

				&&

				IsCommandPathValid( ref commandPath )

				&&

				GetCommandInfo(
					userName,
					commandPath,
					commandArguments,
					ref ci )

				&&

				ci.IsCommandAllowed;

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, isCommandVerified={1}", userName, isCommandVerified );
			m_ts.TraceEvent( TraceEventType.Stop, ( int ) EventIds.ExitMethod,
				"exiting VerifyCommand( string, ref string, string )" );

			return ( isCommandVerified );
		}

		/// <summary>
		///		Checks to see if the given command name
		///		exists exactly as entered, as entered with
		///		known executable file extensions on the end,
		///		or somewhere in one of the directories 
		///		specified in the environment variable %PATH%.
		/// </summary>
		/// <param name="commandPath">
		///		Command to check.  If this method returns true
		///		this parameter will be set to the fully
		///		qualified p of the command.
		/// </param>
		/// <returns>
		///		True if the command exists, otherwise false.
		/// </returns>
		private bool IsCommandPathValid( ref string commandPath )
		{
			// declare this method's return value
			bool isValid = false;

			// check to see if commandPath exists as entered or 
			// as entered with any of the known executable file
			// extensions appended to it
			if ( commandPath.Contains( "\\" ) ||
				commandPath.Contains( "/" ) )
			{
				// check to see if the commandPath exists
				if ( !( isValid = File.Exists( commandPath ) ) )
				{
					// check to see if commandPath exists with any of the
					// known executable file extensions appended to it
					isValid = ( commandPath = TestFileExtensions( commandPath ) ).Length > 0;
				}
			}

			// check to see if commandPath exists in any of the folders
			// listed in the PATH environment variable
			else
			{
				string p = Environment.GetEnvironmentVariable( "PATH" );
				string[] pdirs = p.Split( new char[] { ';' } );
				
				for ( int x = 0; x < pdirs.Length && !isValid; ++x )
				{
					string tmp_cmd_path = commandPath;
					
					string pd = pdirs[ x ];

					// add a trailing slash to the path directory
					// if it does not have one
					if ( !Regex.IsMatch( pd, @"^.+(\\|/)$" ) )
					{
						// add the appropriate type of slash,
						// i.e. a slash or a backslash
						if ( pd.IndexOf( '\\' ) > -1 )
							pd += "\\";
						else
							pd += "/";
					}

					// check to see if tmp_cmd_path exists with
					// the current path directory prepended to it
					tmp_cmd_path = pd + tmp_cmd_path;

					tmp_cmd_path = TestFileExtensions( tmp_cmd_path );
					if ( isValid = tmp_cmd_path.Length > 0 )
					{
						commandPath = tmp_cmd_path;
					}
				}
			}

			return ( isValid );
		}

		/// <summary>
		///		Tests all the executable file extensions on
		///		the command p parameter in order to determine
		///		whether the given command name is a valid 
		///		executable without the extension on the end.
		/// </summary>
		/// <param name="commandPath">
		///		Command to test.
		/// </param>
		/// <returns>
		///		If a command with an executable file extension was
		///		found then this method returns the fully qualified p 
		///		to the command with the correct file extension 
		///		appended to the end.
		/// 
		///		If no command was found this method returns
		///		an empty string.
		/// </returns>
		private string TestFileExtensions( string commandPath )
		{
			// test the commandPath without any extensions
			if ( File.Exists( commandPath ) )
				return ( commandPath );

			// declare this method's return value
			string withExtension = string.Empty;

			// declare a bool that is true of the
			// command exists with the given file
			// extension
			bool ce = false;

			// test all the possible executable extensions
			for ( int x = 0; x < 4 && !ce; ++x )
			{
				switch ( x )
				{
					case 0:
					{
						withExtension = commandPath + ".exe";
						break;
					}
					case 1:
					{
						withExtension = commandPath + ".bat";
						break;
					}
					case 2:
					{
						withExtension = commandPath + ".cmd";
						break;
					}
					case 3:
					{
						withExtension = commandPath + ".lnk";
						break;
					}
				}

				m_ts.TraceEvent( TraceEventType.Verbose, 10, withExtension );

				// set the the return value to an empty string
				// if the file does not exist and this is the
				// last iteration of this loop
				if ( !( ce = File.Exists( withExtension ) ) )
					withExtension = string.Empty;
			}

			return ( withExtension );
		}
	}
}
