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

namespace Sudowin.Plugins.CredentialsCache
{
	public interface ICredentialsCachePlugin : Sudowin.Plugins.IPlugin
	{
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
		bool GetCache( string userName, ref CredentialsCache credCache );
		
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
		bool GetCache( string userName, ref string passphrase );

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
		void SetCache( string userName, CredentialsCache credCache );
		
		/// <summary>
		///		Set's the given user's passphrase.
		/// 
		///		If the given user's passphrase does not
		///		already exist then it is added.
		/// 
		///		If the given user's passphrase already
		///		exists then it is updated.
		/// </summary>
		/// <param name="userName">
		///		The name of the user to set the passphrase.  
		///		This name should be in the format:
		/// 
		///			HOST_OR_DOMAIN\USERNAME
		/// </param>
		/// <param name="passphrase">
		///		The given user's passphrase.
		/// </param>
		void SetCache( string userName, string passphrase );

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
		void ExpireCache( string userName, int seconds );
	}
}
