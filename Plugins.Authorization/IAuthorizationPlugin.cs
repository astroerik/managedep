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
using Sudowin.Common;

namespace Sudowin.Plugins.Authorization
{
	/// <summary>
	///		IAuthorizationPlugin defines the interface for all classes that 
	///		are designed to operate as an authorization plugin for Sudowin.  
	///		The sudo server uses authorization plugins to verify that a user 
	///		is allowed to execute a given command with elevated privileges.
	/// </summary>
	public interface IAuthorizationPlugin : IPlugin, IDisposable
	{
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
		bool GetUserInfo( string userName, ref UserInfo userInfo );

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
		bool GetCommandInfo(
			string userName,
			string commandPath,
			string commandArguments,
			ref CommandInfo commandInfo );
		
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
		bool VerifyCommand(
			string userName,
			ref string commandPath,
			string commandArguments );
	}
}
