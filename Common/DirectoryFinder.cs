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
using System.Runtime.InteropServices;
using System.DirectoryServices;

namespace Sudowin.Common
{
    public class DirectoryFinder
    {

        private DirectoryFinder()
        {
        }

        /// <summary>
        /// Exception safe replacement for DirectoryEntries.Find. If will catch the COMException
        /// and return null if entry not found
        /// </summary>
        /// <param name="entries">Directory collection to search</param>
        /// <param name="name">Name of item to search for</param>
        /// <returns>Entry if found, or null if not found</returns>
        public static DirectoryEntry Find(DirectoryEntries entries, string name)
        {
            return DirectoryFinder.Find(entries, name, null);
        }

        /// <summary>
        /// Exception safe replacement for DirectoryEntries.Find. If will catch the COMException
        /// and return null if entry not found
        /// </summary>
        /// <param name="entries">Directory collection to search</param>
        /// <param name="name">Name of item to search for</param>
        /// <param name="schemaClassName">Name of schema class</param>
        /// <returns>Entry if found, or null if not found</returns>
        public static DirectoryEntry Find(DirectoryEntries entries, string name, string schemaClassName)
        {
            try
            {
                DirectoryEntry entry = entries.Find(name, schemaClassName);
                return entry;
            }
            catch (COMException ex)
            {
                if (ex.ErrorCode == unchecked((int)0x800708AD) /*The user name could not be found.*/
                 || ex.ErrorCode == unchecked((int)0x80005004) /*An unknown directory object was requested.*/ )
                {
                    return null;
                }
            }
            return null;
        }

    }
}
