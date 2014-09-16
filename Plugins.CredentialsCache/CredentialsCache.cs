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
	[Serializable]
	public struct CredentialsCache
	{
		/// <summary>
		///		Number of invalid logon attempts the 
		///		user has made.
		/// 
		///		Each time a user executes sudo they 
		///		get a number of chances during that
		///		execution to enter their correct
		///		passphrase.
		/// 
		///		The invalid logon attempts that occur
		///		during a single execution of sudo get
		///		totalled in this member.
		/// </summary>
		public int InvalidLogonCount;

		/// <summary>
		///		Each time a user executes sudo they 
		///		get a number of chances during that
		///		execution to enter their correct
		///		passphrase.  
		/// 
		///		This member represents the number of
		///		times a user has exceeded their invalid
		///		logon limit while attempting to execute
		///		Sudowin.
		/// </summary>
		public int TimesExceededInvalidLogonCount;

		/// <summary>
		///		The user's passphrase.
		/// </summary>
		public string Passphrase;

		/// <summary>
		///		True if the user has valid cached credentials;
		///		otherwise false.
		/// </summary>
		public bool HasValidCachedCredentials;
	}
}
