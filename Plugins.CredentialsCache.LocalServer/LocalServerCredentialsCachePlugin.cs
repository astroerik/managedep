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
using System.Text;
using Sudowin.Common;
using System.Security;
using System.Threading;
using System.Resources;
using System.Diagnostics;
using System.Security.Principal;
using System.Collections.Generic;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Configuration;

namespace Sudowin.Plugins.CredentialsCache.LocalServer
{
	public class LocalServerCredentialsCachePlugin : Sudowin.Plugins.CredentialsCache.CredentialsCachePlugin
	{
		/// <summary>
		///		Trace source that can be defined in the 
		///		config file for Sudowin.WindowsService.
		/// </summary>
		private TraceSource m_ts = new TraceSource( "traceSrc" );

		/// <summary>
		///		Collection of CredentialsCache structures used
		///		to track information about users.
		/// </summary>
		private Dictionary<string, CredentialsCache> m_ccs =
			new Dictionary<string, CredentialsCache>();

		/// <summary>
		///		Collection of SecureStrings used to persist
		///		the passphrases of the users who invoke Sudowin.
		/// </summary>
		private Dictionary<string, SecureString> m_passphrases =
			new Dictionary<string, SecureString>();

		/// <summary>
		///		Collection of timers used to remove members
		///		of m_ccs when their time is up.
		/// </summary>
		private Dictionary<string, Timer> m_tmrs =
			new Dictionary<string, Timer>();

		/// <summary>
		///		This mutex is used to synchronize access
		///		to the m_ccs, m_passphrases, and m_tmrs collections.
		/// </summary>
		private Mutex m_coll_mtx = new Mutex( false );

		/// <summary>
		///		Retrieves the given user's cached credentials.
		/// </summary>
		/// <param name="userName">
		///		The name of the user to retrieve the cached credentials 
		///		for.  This name should be in the format:
		/// 
		///			HOST_OR_DOMAIN\USERNAME
		/// </param>
		/// <param name="credCache">
		///		A reference to a cached credentials structure.
		/// </param>
		/// <returns>
		///		True and the user's cached credentials if found; 
		///		otherwise false and an empty cached credentials set.
		/// </returns>
		public override bool GetCache( string userName, ref CredentialsCache credCache )
		{
			if ( !WindowsIdentity.GetCurrent().IsSystem )
			{
				throw ( new SecurityException( "Restricted to the SYSTEM account." ) );
			}

			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering GetCredentialsCache( string, ref CredentialsCache )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},credCache=", userName );

			m_coll_mtx.WaitOne();
			bool isCredentialsCacheCached = m_ccs.TryGetValue( userName, out credCache );
			m_coll_mtx.ReleaseMutex();

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, isCredentialsCacheCached={1}", userName, isCredentialsCacheCached );
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.ExitMethod,
				"exiting GetCredentialsCache( string, ref CredentialsCache )" );

			return ( isCredentialsCacheCached );
		}

		/// <summary>
		///		Retrieves the given user's cached passphrase.
		/// </summary>
		/// <param name="userName">
		///		The name of the user to retrieve the cached credentials 
		///		for.  This name should be in the format:
		/// 
		///			HOST_OR_DOMAIN\USERNAME
		/// </param>
		/// <param name="passphrase">
		///		A reference to a string to contain the user's password.
		/// </param>
		/// <returns>
		///		True and the user's passphrase if found; otherwise false.
		/// </returns>
		public override bool GetCache( string userName, ref string passphrase )
		{
			if ( !WindowsIdentity.GetCurrent().IsSystem )
			{
				throw ( new SecurityException( "Restricted to the SYSTEM account." ) );
			}

			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.EnterMethod,
				"entering GetUserCache( string, ref string )" );
			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.ParemeterValues,
				"userName={0},passphrase=", userName );

			m_coll_mtx.WaitOne();
			bool ispassphraseCached;
			if ( ispassphraseCached = m_passphrases.ContainsKey( userName ) )
			{
				SecureString ss = m_passphrases[ userName ];
				IntPtr ps = Marshal.SecureStringToBSTR( ss );
				passphrase = Marshal.PtrToStringBSTR( ps );
				Marshal.FreeBSTR( ps );
			}
			m_coll_mtx.ReleaseMutex();

			m_ts.TraceEvent( TraceEventType.Verbose, ( int ) EventIds.Verbose,
				"{0}, ispassphraseCached={1}", userName, ispassphraseCached );
			m_ts.TraceEvent( TraceEventType.Start, ( int ) EventIds.ExitMethod,
				"exiting GetUserCache( string, ref string )" );

			return ( ispassphraseCached );
		}

		/// <summary>
		///		Set's the given user's cached credentials.
		/// 
		///		If the given user's cached credentials do not
		///		already exist then they are added.
		/// 
		///		If the given user's cached credentials already
		///		exist then they are updated.
		/// </summary>
		/// <param name="userName">
		///		The name of the user to set the cached credentials 
		///		for.  This name should be in the format:
		/// 
		///			HOST_OR_DOMAIN\USERNAME
		/// </param>
		/// <param name="credCache">
		///		The given user's cached credentials.
		/// </param>
		public override void SetCache( string userName, CredentialsCache credCache )
		{
			if ( !WindowsIdentity.GetCurrent().IsSystem )
			{
				throw ( new SecurityException( "Restricted to the SYSTEM account." ) );
			}

			m_coll_mtx.WaitOne();

			// whether or not the CredentialsCache structure
			// with the userName parameter for its key
			// is already is the m_ucs collection
			bool is_cached = m_ccs.ContainsKey( userName );

			if ( is_cached )
			{
				m_ccs[ userName ] = credCache;
			}
			else
			{
				m_ccs.Add( userName, credCache );
			}

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Creates a SecureString version of the given
		///		plain-text passphrase in the m_passphrases collection
		///		for the given userName.
		/// </summary>
		/// <param name="userName">
		///		User name to create SecureString passphrase for
		///		and to use as the key for the m_passphrases collection.
		/// </param>
		/// <param name="passphrase">
		///		Plain-text passphrase to convert into a SecureString.
		/// </param>
		public override void SetCache( string userName, string passphrase )
		{
			if ( !WindowsIdentity.GetCurrent().IsSystem )
			{
				throw ( new SecurityException( "Restricted to the SYSTEM account." ) );
			}

			m_coll_mtx.WaitOne();

			SecureString ss = new SecureString();

			for ( int x = 0; x < passphrase.Length; ++x )
				ss.AppendChar( passphrase[ x ] );

			if ( m_passphrases.ContainsKey( userName ) )
			{
				m_passphrases[ userName ].Clear();
				m_passphrases[ userName ] = ss;
			}
			else
			{
				m_passphrases.Add( userName, ss );
			}

			m_coll_mtx.ReleaseMutex();
		}

		/// <summary>
		///		Expire the given user's cached credentials in the 
		///		given number of seconds.
		/// </summary>
		/// <param name="userName">
		///		The name of the user to expire the cached credentials 
		///		for.  This name should be in the format:
		/// 
		///			HOST_OR_DOMAIN\USERNAME
		/// </param>
		/// <param name="seconds">
		///		Number of seconds to wait until expiration.
		/// </param>
		public override void ExpireCache( string userName, int seconds )
		{
			m_coll_mtx.WaitOne();

			// if the timers collection already contains a timer
			// for this user then change when it is supposed to fire
			if ( m_tmrs.ContainsKey( userName ) )
			{
				m_tmrs[ userName ].Change(
					seconds * 1000, Timeout.Infinite );
			}
			// add a new timer and a new changed value
			else
			{
				m_tmrs.Add( userName, new Timer(
					new TimerCallback( ExpireCacheCallback ),
					userName, seconds * 1000, Timeout.Infinite ) );
			}

			m_coll_mtx.ReleaseMutex();
		}

		private void ExpireCacheCallback( object state )
		{
			m_coll_mtx.WaitOne();

			// cast this callback's state as 
			// a user name string
			string un = state as string;

			// if the user has a UserCache structure
			// in the collection then remove it
			if ( m_ccs.ContainsKey( un ) )
				m_ccs.Remove( un );

			// if the user has a persisted passphrase clear
			// it and then remove it from the collection
			if ( m_passphrases.ContainsKey( un ) )
			{
				m_passphrases[ un ].Clear();
				m_passphrases.Remove( un );
			}

			// dispose of the timer that caused this
			// callback and remove it from m_tmrs
			m_tmrs[ un ].Dispose();
			m_tmrs.Remove( un );

			m_coll_mtx.ReleaseMutex();
		}
	}
}
