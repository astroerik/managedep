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
using System.Runtime.Serialization;

namespace Sudowin.Common
{
    [Serializable]
    public class SudoException : Exception
    {
        private SudoResultTypes _sudoResultType = SudoResultTypes.SudoError;    // default to generic error
        public SudoResultTypes SudoResultType
        {
            get { return _sudoResultType; }
        }

        public SudoException()
        {
        }

        public SudoException(SudoResultTypes sudoResultType, string message)
            : base(message)
        {
            _sudoResultType = sudoResultType;
        }

        public SudoException(SudoResultTypes sudoResultType, string message, Exception innerException)
            : base(message, innerException)
        {
            _sudoResultType = sudoResultType;
        }

        protected SudoException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            _sudoResultType = (SudoResultTypes) info.GetValue("SudoResultType", typeof(SudoResultTypes));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("SudoResultType", SudoResultType);
        }

        public static SudoException GetException(SudoResultTypes sudoResultType)
        {
            return new SudoException(sudoResultType, getMessage(sudoResultType));
        }

        public static SudoException GetException(SudoResultTypes sudoResultType, Exception innerException)
        {
            return new SudoException(sudoResultType, getMessage(sudoResultType), innerException);
        }

        public static SudoException GetException(SudoResultTypes sudoResultType, params object[] args)
        {
            return new SudoException(sudoResultType, getMessage(sudoResultType, args));
        }

        public static SudoException GetException(SudoResultTypes sudoResultType, Exception innerException, params object[] args)
        {
            return new SudoException(sudoResultType, getMessage(sudoResultType, args), innerException);
        }

        private static string getMessage(SudoResultTypes sudoResultType, params object[] args)
        {
            string message = "";

            switch (sudoResultType)
            {
                case SudoResultTypes.InvalidLogon:
                    {
                        message = "Invalid logon attempt";
                        break;
                    }
                case SudoResultTypes.TooManyInvalidLogons:
                    {
                        message = "Invalid logon limit exceeded";
                        break;
                    }
                case SudoResultTypes.CommandNotAllowed:
                    {
                        message = "Command not allowed";
                        break;
                    }
                case SudoResultTypes.LockedOut:
                    {
                        message = "Locked out";
                        break;
                    }
                case SudoResultTypes.UsernameNotFound:
                    {
                        message = string.Format("Username {0}\\{1} not found", args[0], args[1]);
                        break;
                    }

                case SudoResultTypes.GroupNotFound:
                    {
                        message = string.Format("Group {0} not found", args[0]);
                        break;
                    }
                default:
                    {
                        if (args.Length == 0)
                        {
                            message = sudoResultType.ToString();
                        }
                        else if (args.Length == 1)
                        {
                            message = args[0].ToString();
                        }
                        else
                        {
                            // args[0] is format string
                            // args[1..n] are args (so need to shift left)

                            object[] argsNew = new object[args.Length - 1];
                            Array.Copy(args, 1, argsNew, 0, argsNew.Length);
                            message = string.Format(args[0].ToString(), argsNew);
                        }
                        break;
                    }

            }
            return message;

        }
    }
}
