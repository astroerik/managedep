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

namespace Sudowin.Plugins
{
	public interface IPlugin
	{
		/// <summary>
		///		Activates the plugin for first-time use.  This method is necessary
		///		because not all plugins are activated with the 'new' keyword, instead
		///		some are activated with 'Activator.GetObject' and a method is required
		///		to force the plugin's construction in order to catch any exceptions that
		///		may be associated with a plugin's construction.
		/// </summary>
		void Activate();

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
		void Activate( string activationData );
	}
}
