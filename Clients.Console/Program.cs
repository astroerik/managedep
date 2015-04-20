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
using System.Collections.Generic;
using System.Text;
using Sudowin.Common;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Runtime.Remoting;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using CommandLine;
using CommandLine.Text;

namespace Sudowin.Clients.Console
{
	class Program
	{
        class Options
        {
            [ValueOption(0)]
            public string Command { get; set; }

            [ValueList(typeof(List<string>))]
            public IList<string> Arguments { get; set; }

            [ParserState]
            public IParserState LastParserState { get; set; }

            [HelpOption]
            public string GetUsage()
            {
                return HelpText.AutoBuild(this,
                    (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
            }
        }

		static void Main( string[] args )
		{
            var options = new Options();
            if (Parser.Default.ParseArguments(args, options))
            {
                InvokeSudo(string.Empty, options.Command,
                    string.Join(" ", options.Arguments));
            }

            //        }
            //        break;
            //    }
            //}
		}

		private static void InvokeSudo( 
			string password, 
			string commandPath, 
			string commandArguments )
		{
			System.ServiceProcess.ServiceController sc = new System.ServiceProcess.ServiceController( "Sudowin" );
			if ( sc.Status == System.ServiceProcess.ServiceControllerStatus.Stopped )
			{
				System.Console.WriteLine();
				System.Console.WriteLine( "Sudowin service is stopped" );
				return;
			}

			#region configure remoting

			// get path to the actual exe
			Uri uri = new Uri(
				Assembly.GetExecutingAssembly().GetName().CodeBase );

			// configure remoting channels and objects
			RemotingConfiguration.Configure( uri.LocalPath + ".config", true );

			// get the server object that is used to elevate
			// privleges and act as a backend store for
			// caching credentials

			// get an array of the registered well known client urls
			WellKnownClientTypeEntry[] wkts =
				RemotingConfiguration.GetRegisteredWellKnownClientTypes();

			// loop through the list of well known clients until
			// the SudoServer object is found
			ISudoServer iss = null;
			for ( int x = 0; x < wkts.Length && iss == null; ++x )
			{
				iss = Activator.GetObject( typeof( ISudoServer ),
					wkts[ x ].ObjectUrl ) as ISudoServer;
			}

			#endregion

			bool is_sudo_server_comm_link_open = false;
			try
			{
				is_sudo_server_comm_link_open = iss.IsConnectionOpen;
			}
			catch
			{
			}

			if ( !is_sudo_server_comm_link_open )
			{
				System.Console.WriteLine( "Sudowin service is dead to you" );
				return;
			}

			// holds the result of the sudo invocation
			SudoResultTypes srt;

            iss.UpdateSudoers(true);

			do
			{
                //if ( iss.ExceededInvalidLogonLimit )
                //{
                //    System.Console.WriteLine( "Locked out" );
                //    srt = SudoResultTypes.LockedOut;
                //}
                //else
                //{
					// if the password was not passed into this program as
					// a command line argument and the user's credentials
					// are not cached then ask the user for their password
                    if (password.Length == 0 /*&& !iss.AreCredentialsCached*/)
                    {
                        password = GetPassword();
                    }

					// invoke sudo
                    try
                    {
                        srt = iss.Sudo(password, commandPath, commandArguments);
                    }
                    catch (SudoException ex)
                    {
                        System.Console.WriteLine(ex.Message);
                        srt = ex.SudoResultType;
                    }

					// set the password to an empty string.  this is not for
					// security as one might think but rather so if the result
					// of Sudo was InvalidLogon the next iteration through this
					// loop will prompt the user for their password instead
					// of using this password known to be invalid
					password = string.Empty;
                //}
			} while ( srt == SudoResultTypes.InvalidLogon );
		}

		static private string GetPassword()
		{
			// password for this method to return
			string password = string.Empty;

			System.Text.StringBuilder pwd =
				new System.Text.StringBuilder( 100 );
			System.Console.WriteLine();
			System.Console.Write( "Passphrase: " );
			ConsoleKeyInfo cki;
			do
			{
				cki = System.Console.ReadKey( true );
                if (cki.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.Length--;   // chop character off end
                    }
                }
                else if (cki.Key != ConsoleKey.Enter)
                {
                    pwd.Append(cki.KeyChar);
                }
			} while ( cki.Key != ConsoleKey.Enter );
			password = pwd.ToString();
					
			return ( password );
		}
	}
}
