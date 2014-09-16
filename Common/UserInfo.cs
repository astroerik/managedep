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

namespace Sudowin.Common
{
	/// <summary>
	///		Information about user listed in the sudoers data store.
	/// </summary>
	[Serializable]
	public struct UserInfo
	{
		/// <summary>
		///		Number of invalid logon attempts the
		///		user is allowed.
		/// </summary>
		public int InvalidLogons;

		/// <summary>
		///		Number of times the user has exceeded
		///		their invalid logon attempt limit before
		///		the sudowin server will lock them out.
		/// </summary>
		public int TimesExceededInvalidLogons;

		/// <summary>
		///		Number of seconds that the sudo server keeps
		///		track of a user's invalid logon attempts.
		/// </summary>
		public int InvalidLogonTimeout;

		/// <summary>
		///		Number of seconds that a user is locked out
		///		after exceeding their invalid logon attempt
		///		limit.
		/// </summary>
		public int LockoutTimeout;

		/// <summary>
		///		Number of seconds that a
		///		user's valid logon is cached.
		/// </summary>
		public int LogonTimeout;

		/// <summary>
		///		Name of the group that possesses the
		///		same privileges that the user will when
		///		they use Sudowin.
		/// </summary>
		public string PrivilegesGroup;

		/// <summary>
		///		Whether to log nothing, all failed
		///		actions, all successful actions, or
		///		both.
		/// </summary>
		public LoggingLevelTypes LoggingLevel;
	}
}
