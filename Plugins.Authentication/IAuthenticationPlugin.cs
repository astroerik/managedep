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

namespace Sudowin.Plugins.Authentication
{
	/// <summary>
	///		IAuthenticationPlugin defines the interface for all classes that 
	///		are designed to operate as an authentication plugin for Sudowin.
	///		The sudo server uses authentication plugins to verify the 
	///		credentials of a user.
	/// </summary>
	public interface IAuthenticationPlugin : Sudowin.Plugins.IPlugin
	{
		/// <summary>
		///		Verifies the credentials of a user with a passphrase.
		/// </summary>
		/// <param name="domainOrComputerName">
		///		Domain name or computer name the user account belongs to.
		/// </param>
		/// <param name="userName">
		///		Username of account to validate.
		/// </param>
		/// <param name="passphrase">
		///		Passphrase for the given username.
		/// </param>
		/// <returns>
		///		True if the credentials are successfully verified; otherwise false.
		/// </returns>
		bool VerifyCredentials( string domainOrComputerName, string userName, string passphrase );
	}
}
