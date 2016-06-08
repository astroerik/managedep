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
using System.Security;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Configuration;
using System.Runtime.InteropServices;

namespace Sudowin.Common
{
	/// <summary>
	///		Provides managed methods common to this .NET solution.
	/// </summary>
	public class Managed
	{
		/// <summary>
		///		Static class.  Do not instantiate.
		/// </summary>
		[DebuggerHidden]
		private Managed()
		{
		}

		/// <summary>
		///		Get an string value from the host process's
		///		config file.
		/// </summary>
		/// <param name="keyName">
		///		Name of key to get.
		/// </param>
		/// <param name="keyValue">
		///		Will be be the key value.  Empty string
		///		if not found.
		///	</param>
		[DebuggerHidden]
		static public void GetConfigValue(
			string keyName,
			out string keyValue )
		{
			// if the key is defined in the config file 
			// then get it else return an empty string
			if ( Array.IndexOf( ConfigurationManager.AppSettings.AllKeys,
				keyName ) > -1 )
				keyValue = ConfigurationManager.AppSettings[ keyName ];
			// empty string
			else
				keyValue = string.Empty;
		}

		/// <summary>
		///		Get an integer value from the host process's
		///		config file.
		/// </summary>
		/// <param name="keyName">
		///		Name of key to get.
		/// </param>
		/// <param name="keyValue">
		///		Will be the parsed integer.
		///		-1 if not found or cannot parse.
		///	</param>
		[DebuggerHidden]
		static public void GetConfigValue(
			string keyName,
			out int keyValue )
		{
			string temp;
			GetConfigValue( keyName, out temp );
			if ( temp.Length == 0 )
				keyValue = 0;
			else if ( !int.TryParse( temp, out keyValue ) )
				keyValue = -1;
		}

		[DebuggerHidden]
		[Conditional( "TRACE" )]
		static public void TraceWrite(
			string userName,
			string messageFormat,
			params string[] args )
		{
			// get the first method that does not have
			// the same declaring type of the declaring type
			// of this method
			MethodBase mb_this = MethodBase.GetCurrentMethod();
			Type t_this = mb_this.DeclaringType;
			int sf_offset = 1;
			MethodBase mb_caller;
			StackFrame sf;
			do
			{
				sf = new StackFrame( sf_offset );
				mb_caller = sf.GetMethod();
				++sf_offset;
			}
			while ( mb_caller.DeclaringType == mb_this.DeclaringType );

			string msg = string.Format( CultureInfo.CurrentCulture,
				"{0:yyyy/MM/dd HH:mm:ss},{1},{2}.{3},\"{4}\"",
				DateTime.Now,
				userName,
				mb_caller.DeclaringType,
				mb_caller.Name,
				string.Format( CultureInfo.CurrentCulture,
					messageFormat, args ) );

			Trace.WriteLine( msg );
		}
	}
}
