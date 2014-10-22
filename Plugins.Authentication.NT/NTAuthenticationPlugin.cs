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

using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;

namespace Sudowin.Plugins.Authentication.NT
{
    public class NTAuthenticationPlugin : AuthenticationPlugin
    {
        /// <summary>
        ///		The type of logon operation to perform.
        /// </summary>
        /// <remarks>
        ///		LogonUser, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/logonuser.asp
        /// </remarks>
        private enum LogonType : int
        {
            /// <summary>
            ///		This logon type is intended for users who will be 
            ///		interactively using the computer, such as a user being 
            ///		logged on by a terminal server, remote shell, or similar 
            ///		process. This logon type has the additional expense of caching 
            ///		logon information for disconnected operations; therefore, 
            ///		it is inappropriate for some client/server applications, 
            ///		such as a mail server.
            /// </summary>
            Interactive = 2,

            /// <summary>
            ///		This logon type is intended for high performance servers to 
            ///		authenticate plaintext passwords. The LogonUser function does 
            ///		not cache credentials for this logon type.
            /// </summary>
            Network = 3,

            /// <summary>
            ///		This logon type is intended for batch servers, where 
            ///		processes may be executing on behalf of a user without 
            ///		their direct intervention. This type is also for higher 
            ///		performance servers that process many plaintext 
            ///		authentication attempts at a time, such as mail or Web 
            ///		servers. The LogonUser function does not cache credentials 
            ///		for this logon type.
            /// </summary>
            Batch = 4,

            /// <summary>
            ///		Indicates a service-type logon. The account provided must 
            ///		have the service privilege enabled.
            /// </summary>
            Service = 5,

            /// <summary>
            ///		This logon type is for GINA DLLs that log on users who 
            ///		will be interactively using the computer. This logon type 
            ///		can generate a unique audit record that shows when the 
            ///		workstation was unlocked.
            /// </summary>
            Unlock = 7,

            /// <summary>
            ///		This logon type preserves the name and password in the 
            ///		authentication package, which allows the server to make 
            ///		connections to other network servers while impersonating 
            ///		the client. A server can accept plaintext credentials from 
            ///		a client, call LogonUser, verify that the user can access 
            ///		the system across the network, and still communicate with 
            ///		other servers.
            /// </summary>
            /// <remarks>
            ///		Windows NT:  This value is not supported.
            /// </remarks>
            NetworkClearText = 8,

            /// <summary>
            ///		This logon type allows the caller to clone its current 
            ///		token and specify new credentials for outbound connections.  
            ///		The new logon session has the same local identifier but uses 
            ///		different credentials for other network connections.
            /// 
            ///		This logon type is supported only by the Logon32ProviderWinnt50
            ///		logon provider.
            /// </summary>
            /// <remarks>
            ///		Windows NT:  This value is not supported.
            ///	</remarks>
            NewCredentials = 9,
        }

        /// <summary>
        ///		Specifies the logon provider.
        /// </summary>
        private enum LogonProvider : int
        {
            /// <summary>
            ///		Use the standard logon provider for the system.  The default 
            ///		security provider is negotiate, unless you pass NULL for 
            ///		the domain name and the user name is not in UPN format.  
            ///		In this case, the default provider is NTLM.
            /// </summary>
            /// <remarks>
            ///		Windows 2000/NT:   The default security provider is NTLM.
            /// </remarks>
            Default,

            /// <summary>
            ///		Use the Windows NT 3.5 logon provider.
            /// </summary>
            WinNT35,

            /// <summary>
            ///		Use the NTLM logon provider.
            /// </summary>
            WinNT40,

            /// <summary>
            ///		Use the negotiate logon provider.
            /// </summary>
            /// <remarks>
            ///		Windows NT:  This value is not supported.
            /// </remarks>
            WinNT50,
        }

        /// <summary>
        ///		The CloseHandle function closes an open object handle.
        /// </summary>
        /// <param name="handle">
        ///		Handle to an open object.  This parameter can be a 
        ///		pseudo handle or InvalidHandleValue.
        /// </param>
        /// <returns>
        ///		If the function succeeds, the return value is a nonzero value.
        ///
        ///		If the function fails, the return value is zero.  
        ///		To get extended error information, call 
        ///		<see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error">GetLastWin32Error</see>
        ///
        ///		If the application is running under a debugger, the function 
        ///		will throw an exception if it receives either a handle value that 
        ///		is not valid or a pseudo-handle value. This can happen if you close 
        ///		a handle twice, or if you call CloseHandle on a handle returned by 
        ///		the FindFirstFile (http://msdn.microsoft.com/library/en-us/fileio/fs/findfirstfile.asp) 
        ///		function.
        /// </returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr handle);

        /// <summary>
        ///		The LogonUser function attempts to log a user on to the local computer.  
        ///		The local computer is the computer from which LogonUser was called.  
        ///		You cannot use LogonUser to log on to a remote computer.  
        ///		You specify the user with a user name and domain, and authenticate the user 
        ///		with a plaintext password. If the function succeeds, you receive a handle 
        ///		to a token that represents the logged-on user. You can then use this 
        ///		token handle to impersonate the specified user or, in most cases, to 
        ///		create a process that runs in the context of the specified user.
        /// </summary>
        /// <param name="userName">
        ///		A string that specifies the name of the user.  This is the name of the user account 
        ///		to log on to.  If you use the user principal name (UPN) format, user@DNS_domain_name, 
        ///		the domainName parameter must be NULL.
        /// </param>
        /// <param name="domainName">
        ///		A string that specifies the name of the domain or server whose account database 
        ///		contains the lpszUsername account.  If this parameter is NULL, the user name must 
        ///		be specified in UPN format.  If this parameter is ".", the function validates the 
        ///		account using only the local account database.
        /// </param>
        /// <param name="password">
        ///		A string that specifies the plaintext password for the user account specified by 
        ///		userName.  When you have finished using the password, clear the password from memory 
        ///		by calling the SecureZeroMemory function.  For more information about protecting 
        ///		passwords, see Handling Passwords, http://msdn.microsoft.com/library/en-us/secbp/security/handling_passwords.asp.
        /// </param>
        /// <param name="logonType">
        ///		The type of logon operation to perform.
        /// </param>
        /// <param name="logonProvider">
        ///		Specifies the logon provider.
        /// </param>
        /// <param name="userToken">
        ///		A pointer to a handle variable that receives a handle to a token that represents 
        ///		the specified user.
        ///		
        ///		You can use the returned handle in calls to the ImpersonateLoggedOnUser function.
        ///		
        ///		In most cases, the returned handle is a primary token that you can use in calls 
        ///		to the CreateProcessAsUser function. However, if you specify the Network flag, 
        ///		LogonUser returns an impersonation token that you cannot use in CreateProcessAsUser 
        ///		unless you call DuplicateTokenEx to convert it to a primary token.
        ///		
        ///		When you no longer need this handle, close it by calling the CloseHandle function.
        /// </param>
        /// <returns>
        ///		If the function succeeds, the return value is a nonzero value.
        ///
        ///		If the function fails, the return value is zero.  
        ///		To get extended error information, call 
        ///		<see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error">GetLastWin32Error</see>
        /// </returns>
        /// <remarks>
        ///		The Network logon type is fastest, but it has the following limitations:
        /// 
        ///		- The function returns an impersonation token, not a primary token. You cannot 
        ///		use this token directly in the CreateProcessAsUser function. However, you can 
        ///		call the DuplicateTokenEx function to convert the token to a primary token, 
        ///		and then use it in CreateProcessAsUser.
        /// 
        ///		- If you convert the token to a primary token and use it in CreateProcessAsUser 
        ///		to start a process, the new process cannot access other network resources, such 
        ///		as remote servers or printers, through the redirector. An exception is that if 
        ///		the network resource is not access controlled, then the new process will be able 
        ///		to access it.
        /// 
        ///		The SE_TCB_NAME privilege is not required for this function unless you are 
        ///		logging onto a Passport account.
        /// 
        ///		Windows 2000:  The process calling LogonUser requires the SE_TCB_NAME privilege.  
        ///		If the calling process does not have this privilege, LogonUser fails and 
        ///		GetLastWin32Error returns ERROR_PRIVILEGE_NOT_HELD.  In some cases, the process that 
        ///		calls LogonUser must also have the SE_CHANGE_NOTIFY_NAME privilege enabled; 
        ///		otherwise, LogonUser fails and GetLastError returns ERROR_ACCESS_DENIED.  
        ///		This privilege is not required for the local system account or accounts that are 
        ///		members of the administrators group.  By default, SE_CHANGE_NOTIFY_NAME is enabled 
        ///		for all users, but some administrators may disable it for everyone. For more 
        ///		information about privileges, see Privileges, http://msdn.microsoft.com/library/en-us/secauthz/security/privileges.asp.
        /// 
        ///		The account being logged on, that is, the account specified by lpszUsername, 
        ///		must have the necessary account rights.  For example, to log on a user with the 
        ///		Interactive flag, the user (or a group to which the user belongs) must have the 
        ///		SE_INTERACTIVE_LOGON_NAME account right.  For a list of the account rights that 
        ///		affect the various logon operations, see Account Rights Constants, http://msdn.microsoft.com/library/en-us/secauthz/security/authorization_constants.asp.
        /// 
        ///		A user is considered logged on if at least one token exists.  If you call CreateProcessAsUser 
        ///		and then close the token, the system considers the user as still logged on until the 
        ///		process (and all child processes) have ended.
        /// 
        ///		If the LogonUser call is successful, the system notifies network providers that the 
        ///		logon occurred by calling the provider's NPLogonNotify entry-point function.
        /// </remarks>
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LogonUser(
            string userName,
            string domainName,
            string password,
            LogonType logonType,
            LogonProvider logonProvider,
            out IntPtr userToken);

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
        ///		Password for the given username.
        /// </param>
        /// <returns>
        ///		True if the credentials are successfully verified; otherwise false.
        /// </returns>
        public override bool VerifyCredentials(string domainOrComputerName, string userName, string password)
        {
            int scValue = (int)Registry.GetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "scforceoption", 0);

            // Disable piv required
            if (scValue == 1)
                Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "scforceoption", 0);

            IntPtr hLogon = IntPtr.Zero;
            bool logonSuccessful = LogonUser(userName, domainOrComputerName, password,
                LogonType.Interactive, LogonProvider.WinNT50, out hLogon);

            // set it back to piv required
            if (scValue == 1)
                Registry.SetValue("HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System", "scforceoption", 1);

            if (logonSuccessful)
                CloseHandle(hLogon);

            return (logonSuccessful);
        }
    }
}
