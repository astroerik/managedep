/*
Copyright (c) 2005, Schley Andrew Kutz <sakutz@gmail.com>
All rights reserved.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
    this list of conditions and the following disclaimer.
    * Redistributions in binary form must reproduce the above copyright notice,
    this list of conditions and the following disclaimer in the documentation
    and/or other materials provided with the distribution.
    * Neither the name of Lost Creations nor the names of its contributors may
    be used to endorse or promote products derived from this software without
    specific prior written permission.

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
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security;

namespace Win32
{
    /// <summary>
    ///		The WtsConnectionState enumeration type contains 
    ///		values that indicate the connection state of a Terminal 
    ///		Services session.
    /// </summary>
    /// <remarks>
    ///		WTS_CONNECTSTATE_CLASS,	http://msdn.microsoft.com/library/default.asp?url=/library/en-us/termserv/termserv/wts_connectstate_class_str.asp
    /// </remarks>
    public enum WtsConnectionState : int
    {
        /// <summary>
        ///		A user logged on to the WinStation
        /// </summary>
        WtsActive,

        /// <summary>
        ///		The WinStation is connected to the client.
        /// </summary>
        WtsConnected,

        /// <summary>
        ///		The WinStation is in the process of connecting 
        ///		to the client.
        /// </summary>
        WtsConnectQuery,

        /// <summary>
        ///		The WinStation is shadowing another WinStation.
        /// </summary>
        WtsShadow,

        /// <summary>
        ///		The WinStation is active but the client is disconnected.
        /// </summary>
        WtsDisconnected,

        /// <summary>
        ///		The WinStation is waiting for a client to connect.
        /// </summary>
        WtsIdle,

        /// <summary>
        ///		The WinStation is listening for a connection.  
        ///		A listener session waits for requests for new client 
        ///		connections. No user is logged on a listener session.  
        ///		A listener session cannot be reset, shadowed, or changed 
        ///		to a regular client session.
        /// </summary>
        WtsListen,

        /// <summary>
        ///		The WinStation is being reset.
        /// </summary>
        WtsReset,

        /// <summary>
        ///		The WinStation is down due to an error.
        /// </summary>
        WtsDown,

        /// <summary>
        ///		The WinStation is initializing.
        /// </summary>
        WtsInit,
    };

    /// <summary>
    ///		The WtsQueryInfoTypes enumeration type contains values that 
    ///		indicate the type of session information to retrieve in a 
    ///		call to the WtsQuerySessionInformation function.
    /// </summary>
    /// <remarks>
    ///		WTS_INFO_CLASS, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/termserv/termserv/wts_info_class_str.asp
    /// </remarks>
    public enum WtsQueryInfoTypes
    {
        /// <summary>
        ///		A string containing the name of the initial program 
        ///		that Terminal Services runs when the user logs on.
        /// </summary>
        WtsInitialProgram,

        /// <summary>
        ///		A string containing the published name of
        ///		the application the session is running.
        /// </summary>
        WtsApplicationName,

        /// <summary>
        ///		A string containing the default directory
        ///		used when launching the initial program.
        /// </summary>
        WtsWorkingDirectory,

        /// <summary>
        ///		Not used.
        /// </summary>
        WtsOemId,

        /// <summary>
        ///		An INT value containing the session identifier.
        /// </summary>
        WtsSessionId,

        /// <summary>
        ///		A string containing the name of the user 
        ///		associated with the session.
        /// </summary>
        /// <remarks>
        ///		The user name will be returned in this format, 
        ///		DOMAIN|LOCALMACHINE\USERNAME.
        /// </remarks>
        /// <example>
        ///		User 'akutz' logs into the computer 'ALCHEMIST'
        ///		with his domain credentials.  The WtsUserName
        ///		will look like: AUSTIN\akutz.
        /// 
        ///		User 'sakutz' logs into the computer 'ALCHEMIST'
        ///		with his local credentials.  Thee WtsUserName
        ///		will look like: ALCHEMIST\sakutz.	
        /// </example>
        WtsUserName,

        /// <summary>
        ///		A string containing the name of the Terminal Services session.
        /// </summary>
        /// <remarks>
        ///		Despite its name, specifying this type does not return the 
        ///		window station name. Rather, it returns the name of the 
        ///		Terminal Services session. Each Terminal Services session is 
        ///		associated with an interactive window station. Currently, 
        ///		since the only supported window station name for an interactive 
        ///		window station is "WinSta0", each session is associated with its 
        ///		own "WinSta0" window station. For more information, see Windows
        ///		Stations, http://msdn.microsoft.com/library/en-us/dllproc/base/window_stations.asp.
        /// </remarks>
        WtsWinStationName,

        /// <summary>
        ///		A string containing the name of the domain to 
        ///		which the logged-on user belongs.
        /// </summary>
        WtsDomainName,

        /// <summary>
        ///		The session's current connection state.
        /// </summary>
        /// <seealso cref="WtsConnectionState"/>
        WtsConnectState,

        /// <summary>
        ///		An INT value containing the build number of the client.
        /// </summary>
        WtsClientBuildNumber,

        /// <summary>
        ///		A string containing the name of the client.
        /// </summary>
        WtsClientName,

        /// <summary>
        ///		A string containing the directory in which the client is installed.
        /// </summary>
        WtsClientDirectory,

        /// <summary>
        ///		An INT client-specific product identifier.
        /// </summary>
        WtsClientProductId,

        /// <summary>
        ///		An INT value containing a client-specific hardware identifier.
        /// </summary>
        WtsClientHardwareId,

        /// <summary>
        ///		The network type and network address of the client.
        /// </summary>
        WtsClientAddress,

        /// <summary>
        ///		Information about the display resolution of the client.
        /// </summary>
        WtsClientDisplay,

        /// <summary>
        ///		An INT value specifying information about the 
        ///		protocol type for the session.
        /// </summary>
        WtsClientProtocolType,
    };

    /// <summary>
    ///		Priority type of a process.
    /// </summary>
    /// <remarks>
    ///		http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dllproc/base/getpriorityclass.asp
    /// </remarks>
    [Flags]
    public enum ProcessPriorityTypes : int
    {
        /// <summary>
        ///		Process that has priority above Normal but below High.
        /// </summary>
        /// <remarks>
        ///		Windows NT and Windows Me/98/95:  This value is not supported.
        /// </remarks>
        AboveNormal = 0x00008000,

        /// <summary>
        ///		 Process that has priority above Idle but below Normal.
        /// </summary>
        /// <remarks>
        ///		Windows NT and Windows Me/98/95:  This value is not supported.
        /// </remarks>
        BelowNormal = 0x00004000,

        /// <summary>
        ///		Process that performs time-critical tasks that must be executed 
        ///		immediately for it to run correctly.  The threads of a high-priority class 
        ///		process preempt the threads of normal or idle priority class processes.  
        ///		An example is the Task List, which must respond quickly when called by the user, 
        ///		regardless of the load on the operating system.  Use extreme care when 
        ///		using the high-priority class, because a high-priority class CPU-bound application 
        ///		can use nearly all available cycles.
        /// </summary>
        High = 0x00000080,

        /// <summary>
        ///		Process whose threads run only when the system is idle and are preempted by 
        ///		the threads of any process running in a higher priority class.  An example is a 
        ///		screen saver. The idle priority class is inherited by child processes.
        /// </summary>
        Idle = 0x00000040,

        /// <summary>
        ///		Process with no special scheduling needs.
        /// </summary>
        Normal = 0x00000020,

        /// <summary>
        ///		Process that has the highest possible priority.  The threads of a real-time 
        ///		priority class process preempt the threads of all other processes, including 
        ///		operating system processes performing important tasks.  For example, a real-time 
        ///		process that executes for more than a very brief interval can cause disk caches 
        ///		not to flush or cause the mouse to be unresponsive.
        /// </summary>
        Realtime = 0x00000100,
    }

    /// <summary>
    ///		The following process creation flags are used by the CreateProcess and 
    ///		CreateProcessAsUser functions.  They can be specified in any combination, 
    ///		except as noted.
    /// </summary>
    /// <remarks>
    ///		On 32-bit Windows, 16-bit applications are simulated by ntvdm.exe, not run as 
    ///		individual processes. Therefore, the process creation flags apply to ntvdm.exe.  
    ///		Because ntvdm.exe persists after you run the first 16-bit application, when you 
    ///		launch another 16-bit application, the new creation flags are not applied, except 
    ///		for CreateNewConsole and CreateSeperateWowVdm, which create a new ntvdm.exe.
    /// 
    ///		Process Creation Flags, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dllproc/base/process_creation_flags.asp.
    /// </remarks>
    [Flags]
    public enum ProcessCreationFlags : int
    {
        /// <summary>
        ///		The child processes of a process associated with a job are not associated with the job.
        ///		
        ///		If the calling process is not associated with a job, this constant has no effect.  
        ///		If the calling process is associated with a job, the job must set the 
        ///		JobObjectLimitBreakawayOk limit.
        /// </summary>
        /// <remarks>
        ///		Windows NT and Windows Me/98/95:  This value is not supported.
        /// </remarks>
        CreateBreakwayFromJob = 0x01000000,

        /// <summary>
        ///		The new process does not inherit the error mode of the calling process.  
        ///		Instead, the new process gets the default error mode.
        ///
        ///		This feature is particularly useful for multi-threaded shell applications 
        ///		that run with hard errors disabled.
        /// 
        ///		The default behavior is for the new process to inherit the error mode of 
        ///		the caller. Setting this flag changes that default behavior.
        /// </summary>
        CreateDefaultErrorMode = 0x04000000,

        /// <summary>
        ///		The new process has a new console, instead of inheriting its parent's console 
        ///		(the default).  For more information, see Creation of a Console, http://msdn.microsoft.com/library/en-us/dllproc/base/creation_of_a_console.asp.
        ///
        ///		This flag cannot be used with CreateNoWindow or DetachedProcess.
        /// </summary>
        CreateNewConsole = 0x00000010,

        /// <summary>
        ///		The new process is the root process of a new process group.  The process group 
        ///		includes all processes that are descendants of this root process.  The process 
        ///		identifier of the new process group is the same as the process identifier, which is 
        ///		returned in the lpProcessInformation parameter.  Process groups are used by the 
        ///		GenerateConsoleCtrlEvent function to enable sending a CTRL+BREAK signal to a group 
        ///		of console processes.
        ///
        ///		If this flag is specified, CTRL+C signals will be disabled for all processes within 
        ///		the new process group.
        ///
        ///		Windows Server 2003:  This flag is ignored if specified with CreateNewConsole.
        /// </summary>
        CreateNewProcessGroup = 0x00000200,

        /// <summary>
        ///		The process is a console application that is run without a console window.  
        ///		This flag is valid only when starting a console application.
        ///
        ///		This flag cannot be used with CreateNewConsole or DetachedProcess or when starting 
        ///		an MS-DOS-based application.
        /// </summary>
        /// <remarks>
        ///		Windows Me/98/95:  This value is not supported.
        /// </remarks>
        CreateNoWindow = 0x08000000,

        /// <summary>
        ///		Allows the caller to execute a child process that bypasses the process 
        ///		restrictions that would normally be applied automatically to the process.
        /// </summary>
        /// <remarks>
        ///		Windows 2000/NT and Windows Me/98/95:  This value is not supported.
        /// </remarks>
        CreatePreserveCodeAuthzLevel = 0x02000000,

        /// <summary>
        ///		This flag is valid only when starting a 16-bit Windows-based application.  
        ///		If set, the new process runs in a private Virtual DOS Machine (VDM).  
        ///		By default, all 16-bit Windows-based applications run as threads in a single, 
        ///		shared VDM.  The advantage of running separately is that a crash only terminates 
        ///		the single VDM; any other programs running in distinct VDMs continue to function 
        ///		normally.  Also, 16-bit Windows-based applications that are run in separate VDMs 
        ///		have separate input queues.  That means that if one application stops responding 
        ///		momentarily, applications in separate VDMs continue to receive input.  The disadvantage 
        ///		of running separately is that it takes significantly more memory to do so.  You 
        ///		should use this flag only if the user requests that 16-bit applications should run 
        ///		in their own VDM.
        /// </summary>
        /// <remarks>
        ///		Windows Me/98/95:  This value is not supported.
        ///	</remarks>
        CreateSeperateWowVdm = 0x00000800,

        /// <summary>
        ///		The flag is valid only when starting a 16-bit Windows-based application.  
        ///		If the DefaultSeparateVDM switch in the Windows section of WIN.INI is TRUE, 
        ///		this flag overrides the switch.  The new process is run in the shared Virtual DOS Machine.
        /// </summary>
        /// <remarks>
        ///		Windows Me/98/95:  This value is not supported.
        ///	</remarks>
        CreateSharedWowVdm = 0x00001000,

        /// <summary>
        ///		The primary thread of the new process is created in a suspended state, 
        ///		and does not run until the ResumeThread function is called.
        /// </summary>
        CreateSuspended = 0x00000004,

        /// <summary>
        ///		If this flag is set, the environment block pointed to by environment uses Unicode 
        ///		characters.  Otherwise, the environment block uses ANSI characters.
        /// </summary>
        /// <remarks>
        ///		Windows Me/98/95:  This value is not supported.
        ///	</remarks>
        CreateUnicodeEnvironment = 0x00000400,

        /// <summary>
        ///		The calling thread starts and debugs the new process.  It can receive all related 
        ///		debug events using the WaitForDebugEvent function.
        /// </summary>
        DebugOnlyThisProcess = 0x00000002,

        /// <summary>
        ///		The calling thread starts and debugs the new process and all any child processes 
        ///		of the new process that are created with DebugProcess.  It can receive all related 
        ///		debug events using the WaitForDebugEvent function.
        ///
        ///		If this flag is combined with DebugOnlyThisProcess, the caller debugs only 
        ///		the new process.
        /// </summary>
        /// <remarks>
        ///		Windows Me/98/95:  This flag is not valid if the new process is a 16-bit application.
        /// </remarks>
        DebugProcess = 0x00000001,

        /// <summary>
        ///		For console processes, the new process does not inherit its parent's console 
        ///		(the default).  The new process can call the AllocConsole function at a later 
        ///		time to create a console. For more information, see Creation of a Console.
        ///
        ///		This value cannot be used with CreateNewConsole or CreateNoWindow.
        /// </summary>
        DetachedProcess = 0x00000008,
    }

    /// <summary>
    ///		The type of logon operation to perform.
    /// </summary>
    /// <remarks>
    ///		LogonUser, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthn/security/logonuser.asp
    /// </remarks>
    public enum LogonType : int
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
    public enum LogonProvider : int
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
    ///		Used by the StartupInfo structure's Flags member.
    /// </summary>
    [Flags]
    public enum StartupInfoFlags : int
    {
        /// <summary>
        ///		If this value is not specified, the ShowWindow member is ignored.
        /// </summary>
        UseShowWindow = 0x00000001,

        /// <summary>
        ///		If this value is not specified, the XSize and YSize members are ignored.
        /// </summary>
        UseSize = 0x00000002,

        /// <summary>
        ///		If this value is not specified, the X and Y members are ignored.
        /// </summary>
        UsePosition = 0x00000004,

        /// <summary>
        ///		If this value is not specified, the XCountChars and 
        ///		YCountChars members are ignored.
        /// </summary>
        /// <remarks>
        ///		Windows Me/98/95:  This value is not supported.
        /// </remarks>
        UseCountChars = 0x00000008,

        /// <summary>
        ///		If this value is not specified, the FillAttribute member is ignored.
        /// </summary>
        UseFillAttribute = 0x00000010,

        /// <summary>
        ///		This flag is only valid for console applications running on an x86 computer.
        /// </summary>
        /// <remarks>
        ///		Windows Me/98/95:  This value is not supported.
        /// </remarks>
        RunFullScreen = 0x00000020,

        /// <summary>
        ///		Indicates that the cursor is in feedback mode for two seconds after 
        ///		CreateProcess is called. The Working in Background cursor is displayed 
        ///		(see the Pointers tab in the Mouse control panel utility).
        ///
        ///		If during those two seconds the process makes the first GUI call, 
        ///		the system gives five more seconds to the process.  If during those 
        ///		five seconds the process shows a window, the system gives five more 
        ///		seconds to the process to finish drawing the window.
        ///
        ///		The system turns the feedback cursor off after the first call to GetMessage, 
        ///		regardless of whether the process is drawing.
        /// </summary>
        ForceOnFeedback = 0x00000040,

        /// <summary>
        ///		Indicates that the feedback cursor is forced off while the process 
        ///		is starting. The Normal Select cursor is displayed.
        /// </summary>
        ForceOffFeedback = 0x00000080,

        /// <summary>
        /// 	Sets the standard input, standard output, and standard error handles 
        /// 	for the process to the handles specified in the StdInput, StdOutput, 
        /// 	and StdError members of the StartupInfo structure.  For this to work 
        /// 	properly, the handles must be inheritable and the CreateProcess 
        /// 	function's InheritHandles parameter must be set to TRUE. For more 
        /// 	information, see Handle Inheritance, http://msdn.microsoft.com/library/en-us/sysinfo/base/handle_inheritance.asp.
        /// 	
        ///		If this value is not specified, the StdInput, StdOutput, and StdError
        ///		members of the StartupInfo structure are ignored.
        /// </summary>
        UseStdHandles = 0x00000100,
    }

    /// <summary>
    ///		The SecurityAttributes structure contains the security descriptor for an 
    ///		object and specifies whether the handle retrieved by specifying this structure 
    ///		is inheritable. This structure provides security settings for objects created by 
    ///		various functions, such as CreateFile, CreatePipe, CreateProcess, RegCreateKeyEx, 
    ///		or RegSaveKeyEx.
    /// </summary>
    /// <remarks>
    ///		SECURITY_ATTRIBUTES, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/secauthz/security/security_attributes.asp
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct SecurityAttributes
    {
        /// <summary>
        ///		The size, in bytes, of this structure. Set this value to the size of the 
        ///		SecurityAttributes structure.
        /// </summary>
        public int Length;

        /// <summary>
        ///		A pointer to a security descriptor for the object that controls the sharing of it.  
        ///		If NULL is specified for this member, the object is assigned the default security 
        ///		descriptor of the calling process.  This is not the same as granting access to everyone 
        ///		by assigning a NULL discretionary access control list (DACL).  The default security 
        ///		descriptor is based on the default DACL of the access token belonging to the calling 
        ///		process.  By default, the default DACL in the access token of a process allows access 
        ///		only to the user represented by the access token.  If other users must access the object, 
        ///		you can either create a security descriptor with the appropriate access, or add ACEs 
        ///		to the DACL that grants access to a group of users.
        /// </summary>
        /// <remarks>
        ///		Windows Me/98/95:  The SecurityDescriptor member of this structure is ignored.
        ///	</remarks>
        public IntPtr SecurityDescriptor;

        /// <summary>
        ///		A Boolean value that specifies whether the returned handle is inherited when a 
        ///		new process is created.  If this member is TRUE, the new process inherits the handle.
        /// </summary>
        public bool InheritHandle;
    }

    /// <summary>
    ///		The ProcessInformation structure is used with the CreateProcess, 
    ///		CreateProcessAsUser, CreateProcessWithLogonW, or CreateProcessWithTokenW 
    ///		function.  This structure contains information about the newly created 
    ///		process and its primary thread.
    /// </summary>
    /// <remarks>
    ///		If the function succeeds, be sure to call the CloseHandle function 
    ///		to close the Process and Thread handles when you are finished with them.  
    ///		Otherwise, when the child process exits, the system cannot clean up these 
    ///		handles because the parent process did not close them.  However, the system 
    ///		will close these handles when the parent process terminates, so they 
    ///		would be cleaned up at this point.
    /// 
    ///		PROCESS_INFORMATION, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dllproc/base/process_information_str.asp
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessInformation
    {
        /// <summary>
        ///		Handle to the newly created process.  The handle is used to specify the 
        ///		process in all functions that perform operations on the process object.
        /// </summary>
        public IntPtr Process;

        /// <summary>
        ///		Handle to the primary thread of the newly created process.  The handle 
        ///		is used to specify the thread in all functions that perform operations 
        ///		on the thread object.
        /// </summary>
        public IntPtr Thread;

        /// <summary>
        ///		Value that can be used to identify a process.  The value is valid from 
        ///		the time the process is created until the time the process is terminated.
        /// </summary>
        public int ProcessId;

        /// <summary>
        ///		Value that can be used to identify a thread.  The value is valid from 
        ///		the time the thread is created until the time the thread is terminated.
        /// </summary>
        public int ThreadId;
    }

    /// <summary>
    ///		The StartupInfo structure is used with the CreateProcess, 
    ///		CreateProcessAsUser, and CreateProcessWithLogonW functions 
    ///		to specify the window station, desktop, standard handles, 
    ///		and appearance of the main window for the new process.
    /// </summary>
    /// <remarks>
    ///		For graphical user interface (GUI) processes, this information affects 
    ///		the first window created by the CreateWindow function and shown by the 
    ///		ShowWindow function.  For console processes, this information affects the 
    ///		console window if a new console is created for the process. A process can 
    ///		use the GetStartupInfo function to retrieve the StartupInfo structure 
    ///		specified when the process was created.
    ///
    ///		If a GUI process is being started and neither ForceOnFeedback or 
    ///		ForceOffFeedback is specified, the process feedback cursor is used.  
    ///		A GUI process is one whose subsystem is specified as "windows."
    /// 
    ///		STARTUPINFO, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dllproc/base/startupinfo_str.asp
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct StartupInfo
    {
        /// <summary>
        ///		Size of the structure, in bytes.
        /// </summary>
        public Int32 Size;

        /// <summary>
        ///		Reserved.  Set this member to NULL before passing the 
        ///		structure to CreateProcess.
        /// </summary>
        public string Reserved;

        /// <summary>
        ///		A string that specifies either the name of the desktop, 
        ///		or the name of both the desktop and window station for 
        ///		this process.  A backslash in the string indicates that the 
        ///		string includes both the desktop and window station names.
        /// </summary>
        /// <remarks>
        ///		Windows Me/98/95:  Desktops and window stations are not supported.
        /// </remarks>
        public string Desktop;

        /// <summary>
        ///		For console processes, this is the title displayed in the 
        ///		title bar if a new console window is created. If NULL, the 
        ///		name of the executable file is used as the window title 
        ///		instead. This parameter must be NULL for GUI or console 
        ///		processes that do not create a new console window.
        /// </summary>
        public string Title;

        /// <summary>
        ///		If Flags specifies UsePosition, this member is the x offset 
        ///		of the upper left corner of a window if a new window is created, 
        ///		in pixels.  Otherwise, this member is ignored.
        ///		
        ///		The offset is from the upper left corner of the screen.  For GUI 
        ///		processes, the specified position is used the first time the new 
        ///		process calls CreateWindow to create an overlapped window if the x 
        ///		parameter of CreateWindow is CwUseDefault.
        /// </summary>
        public Int32 X;

        /// <summary>
        ///		If Flags specifies UsePosition, this member is the y offset 
        ///		of the upper left corner of a window if a new window is created, 
        ///		in pixels.  Otherwise, this member is ignored.
        ///		
        ///		The offset is from the upper left corner of the screen.  For GUI 
        ///		processes, the specified position is used the first time the new 
        ///		process calls CreateWindow to create an overlapped window if the x 
        ///		parameter of CreateWindow is CwUseDefault.
        /// </summary>
        public Int32 Y;

        /// <summary>
        ///		If Flags specifies UseSize, this member is the width of the window 
        ///		if a new window is created, in pixels.  Otherwise, this member is ignored.
        ///
        ///		For GUI processes, this is used only the first time the new process calls 
        ///		CreateWindow to create an overlapped window if the nWidth parameter 
        ///		of CreateWindow is CwUseDefault.
        /// </summary>
        public Int32 XSize;

        /// <summary>
        ///		If Flags specifies UseSize, this member is the height of the window 
        ///		if a new window is created, in pixels.  Otherwise, this member is ignored.
        ///
        ///		For GUI processes, this is used only the first time the new process calls 
        ///		CreateWindow to create an overlapped window if the nWidth parameter 
        ///		of CreateWindow is CwUseDefault.
        /// </summary>
        public Int32 YSize;

        /// <summary>
        ///		If Flags specifies UseCountChars, if a new console window is created 
        ///		in a console process, this member specifies the screen buffer width, in 
        ///		character columns.  Otherwise, this member is ignored.
        /// </summary>
        public Int32 XCountChars;

        /// <summary>
        ///		If Flags specifies UseCountChars, if a new console window is created 
        ///		in a console process, this member specifies the screen buffer height, in 
        ///		character rows.  Otherwise, this member is ignored.
        /// </summary>
        public Int32 YCountChars;

        /// <summary>
        ///		If Flags specifies UseFillAttribute, this member is the initial text 
        ///		and background colors if a new console window is created in a console 
        ///		application.  Otherwise, this member is ignored.
        ///
        ///		This value can be any combination of the following values: 
        ///		FOREGROUND_BLUE, FOREGROUND_GREEN, FOREGROUND_RED, 
        ///		FOREGROUND_INTENSITY, BACKGROUND_BLUE, BACKGROUND_GREEN, 
        ///		BACKGROUND_RED, and BACKGROUND_INTENSITY.  
        ///		For example, the following combination of values produces red text 
        ///		on a white background:
        /// 
        ///		<code>
        ///			FOREGROUND_RED | BACKGROUND_RED | BACKGROUND_GREEN | BACKGROUND_BLUE
        ///		</code>
        /// </summary>
        public Int32 FillAttribute;

        /// <summary>
        ///		Bit field that determines whether certain StartupInfo members are used 
        ///		when the process creates a window.
        /// </summary>
        /// <seealso cref="Win32.StartupInfoFlags"/>
        public Int32 Flags;

        /// <summary>
        ///		If dwFlags specifies STARTF_USESHOWWINDOW, this member can be any of the 
        ///		SW_ constants defined in Winuser.h.  Otherwise, this member is ignored.
        ///
        ///
        ///		For GUI processes, wShowWindow specifies the default value the first time 
        ///		ShowWindow is called.  The nCmdShow parameter of ShowWindow is ignored.  
        ///		In subsequent calls to ShowWindow, the wShowWindow member is used if the 
        ///		nCmdShow parameter of ShowWindow is set to SW_SHOWDEFAULT.
        /// </summary>
        public Int16 ShowWindow;

        /// <summary>
        ///		Reserved for use by the C Run-time; must be zero.
        /// </summary>
        public Int16 sReserved2;

        /// <summary>
        ///		Reserved for use by the C Run-time; must be NULL.
        /// </summary>
        public Int32 Reserved2;

        /// <summary>
        ///		If dwFlags specifies STARTF_USESTDHANDLES, this member is a handle to 
        ///		be used as the standard input handle for the process.  Otherwise, 
        ///		this member is ignored.
        /// </summary>
        public Int32 StdInput;

        /// <summary>
        ///		If dwFlags specifies STARTF_USESTDHANDLES, this member is a handle to 
        ///		be used as the standard output handle for the process.  Otherwise, 
        ///		this member is ignored.
        /// </summary>
        public Int32 StdOutput;

        /// <summary>
        ///		If dwFlags specifies STARTF_USESTDHANDLES, this member is a handle to 
        ///		be used as the standard error handle for the process.  Otherwise, 
        ///		this member is ignored.
        /// </summary>
        public Int32 StdError;
    }

    /// <summary>
    ///		The WtsSessionInfo structure contains information 
    ///		about a client session on a terminal server.
    /// </summary>
    /// <remarks>
    ///		WTS_SESSION_INFO, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/termserv/termserv/wts_session_info_str.asp
    /// </remarks>
    public struct WtsSessionInfo
    {
        /// <summary>
        ///		Session identifier of the session.
        /// </summary>
        public int SessionId;

        /// <summary>
        ///		A string containing the name of the 
        ///		WinStation for this session.
        /// </summary>
        [MarshalAs(UnmanagedType.LPTStr)]
        public string WinStationName;

        /// <summary>
        ///		A value from the 
        ///		<see cref="Win32.WtsConnectionState">WtsConnectionState</see> 
        ///		enumeration type indicating the session's current connection state.
        /// </summary>
        public WtsConnectionState State;
    }

    /// <summary>
    ///		.NET method signatures for some Win32 functions.
    /// </summary>
    internal class Native
    {
        static Native()
        {
        }

        /// <summary>
        ///		Handle to the server that this code is running on.
        /// </summary>
        public const int WtsCurrentServerHandle = -1;

        /// <summary>
        ///		The WtsOpenServer function opens a handle 
        ///		to the specified terminal server.
        /// </summary>
        /// <param name="serverName">
        ///		A string specifying the NetBIOS name of the terminal server.
        /// </param>
        /// <returns>
        ///		If the function succeeds, the return value is a 
        ///		handle to the specified server.
        ///
        ///		If the function fails, the return value is NULL.  
        ///		To get extended error information, call 
        ///		<see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error">GetLastWin32Error()</see>
        /// </returns>
        /// <remarks>
        ///		When you are finished with the handle returned by WtsOpenServer, 
        ///		call the WtsCloseServer function to close it.
        ///
        ///		You do not need to open a handle for operations performed on the 
        ///		terminal server on which your application is running. Use the constant 
        ///		<see cref="Native.WtsCurrentServerHandle">WtsCurrentServerHandle</see> instead.
        /// </remarks>
        [DllImport(
            "wtsapi32.dll",
            EntryPoint = "WTSOpenServer",
            CharSet = CharSet.Auto,
            SetLastError = true),
            SuppressUnmanagedCodeSecurityAttribute
        ]
        public static extern IntPtr WtsOpenServer(
            string serverName
        );

        /// <summary>
        ///		The WtsCloseServer function closes an open 
        ///		handle to a terminal server.
        /// </summary>
        /// <param name="serverHandle">
        ///		Handle to a terminal server opened by a call to the 
        ///		<see cref="Native.WtsOpenServer">WtsOpenServer</see> 
        ///		function.
        ///
        ///		Do not specify 
        ///		<see cref="Native.WtsCurrentServerHandle">WtsCurrentServerHandle</see> 
        ///		for this parameter.
        /// </param>
        /// <remarks>
        ///		Call the WtsCloseServer function as part of your program's 
        ///		clean-up routine to close all the server handles opened by 
        ///		calls to the WtsOpenServer function.
        /// </remarks>
        [DllImport(
            "wtsapi32.dll",
            EntryPoint = "WTSCloseServer",
            CharSet = CharSet.Auto,
            SetLastError = true),
            SuppressUnmanagedCodeSecurityAttribute
        ]
        public static extern void WtsCloseServer(
            IntPtr serverHandle
        );

        /// <summary>
        ///		The WtsEnumerateSessions function retrieves 
        ///		a list of sessions on a specified terminal server.
        /// </summary>
        /// <param name="serverHandle">
        ///		Handle to a terminal server. Specify a handle opened 
        ///		by the 
        ///		<see cref="Native.WtsOpenServer">WtsOpenServer</see> 
        ///		function, or specify 
        ///		<see cref="Native.WtsCurrentServerHandle">WtsCurrentServerHandle</see>
        ///		to indicate the terminal server on which your application is running.
        /// </param>
        /// <param name="reserved">
        ///		Reserved; must be zero.
        /// </param>
        /// <param name="version">
        ///		Specifies the version of the enumeration request.  Must be 1.
        /// </param>
        /// <param name="wtsSessionInfoStructures">
        ///		Pointer to a variable that receives a pointer to an array of 
        ///		<see cref="Win32.WtsSessionInfo">WtsSessionInfo</see> 
        ///		structures.  Each structure in the array contains 
        ///		information about a session on the specified terminal server.  To free 
        ///		the returned buffer, call the 
        ///		<see cref="Native.WtsFreeMemory">WtsFreeMemory</see> function.
        ///
        ///		To be able to enumerate a session, you need to have the 
        ///		Query Information permission. For more information, 
        ///		see Terminal Services Permissions, http://msdn.microsoft.com/library/en-us/termserv/termserv/terminal_services_permissions.asp.  
        ///		To modify permissions on a session, use the Terminal Services
        ///		Configuration administrative tool.
        /// </param>
        /// <param name="wtsSessionInfoStructuresLength">
        ///		Pointer to the variable that receives the number of WtsSessionInfo 
        ///		structures returned in the ppSessionInfo buffer.
        /// </param>
        /// <returns>
        ///		If the function succeeds, the return value is a nonzero value.
        ///
        ///		If the function fails, the return value is zero.  
        ///		To get extended error information, call 
        ///		<see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error">GetLastWin32Error()</see>
        /// </returns>
        /// <remarks>
        ///		This is the native version of 
        ///		<see cref="Managed.WtsEnumerateSessions">WtsEnumerateSessions</see>.
        /// 
        ///		WTSEnumerateSessions, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/termserv/termserv/wtsenumeratesessions.asp
        /// </remarks>
        [DllImport(
            "wtsapi32.dll",
            EntryPoint = "WTSEnumerateSessions",
            CharSet = CharSet.Auto,
            SetLastError = true),
            SuppressUnmanagedCodeSecurityAttribute
        ]
        public static extern bool WtsEnumerateSessions(
            IntPtr serverHandle,
            int reserved,
            uint version,
            out IntPtr wtsSessionInfoStructures,
            out int wtsSessionInfoStructuresLength
        );

        ///	<summary>
        ///		The WtsQueryUserToken function obtains the primary access token 
        ///		of the logged-on user specified by the session id.  To call this 
        ///		function successfully, the calling application must be running 
        ///		within the context of the LocalSystem account, http://msdn.microsoft.com/library/en-us/dllproc/base/localsystem_account.asp,
        ///		and have the SE_TCB_NAME privilege.  It is not necessary that 
        ///		Terminal Services be running for the function to succeed, 
        ///		but if Terminal Services is not running, the only valid session 
        ///		identifier is zero (0).
        ///
        ///		Caution		WtsQueryUserToken is intended for highly trusted 
        ///		services.  Service providers must use caution that they do not 
        ///		leak user tokens when calling this function. Service providers 
        ///		must close token handles after they have finished with them. 
        /// </summary>
        /// <param name="sessionId">
        ///		A Terminal Services session identifier.  Any program running 
        ///		in the context of a service will have a session identifier of 
        ///		zero (0). You can use the 
        ///		<see cref="Native.WtsEnumerateSessions">WtsEnumerateSessions</see> 
        ///		function to retrieve the identifiers of all sessions on a 
        ///		specified terminal server.
        ///
        ///		To be able to query information for another user's session, 
        ///		you need to have the Query Information permission.  For more information, 
        ///		see Terminal Services Permissions, http://msdn.microsoft.com/library/en-us/termserv/termserv/terminal_services_permissions.asp.  
        ///		To modify permissions on a session, use the Terminal Services 
        ///		Configuration administrative tool. 
        /// </param>
        /// <param name="userToken">
        ///		If the function succeeds, receives a pointer to the token handle 
        ///		for the logged-on user. Note that you must call the 
        ///		<see cref="Native.CloseHandle">CloseHandle</see> 
        ///		function to close this handle.
        /// </param>
        /// <returns>
        ///		If the function succeeds, the return value is a nonzero value, 
        ///		and the userToke parameter points to the primary token of the user.
        ///
        ///		If the function fails, the return value is zero. To get extended error 
        ///		information, call <see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error">GetLastWin32Error</see>.  
        ///		Among other errors, GetLastWin32Error can return one of the following errors.
        /// 
        ///		Return code 						Description
        ///		ERROR_PRIVILEGE_NOT_HELD 			The caller does not have the SE_TCB_NAME privilege.
        ///		ERROR_INVALID_PARAMETER 			One of the parameters to the function was incorrect; for example, the phToken parameter was passed a NULL parameter.
        ///		ERROR_ACCESS_DENIED 				The caller does not have the appropriate permissions to call this function. The caller must be running within the context of the LocalSystem account and have the SE_TCB_NAME privilege.
        ///		ERROR_CTX_WINSTATION_NOT_FOUND 		The token query is for a session that does not exist.
        ///		ERROR_NO_TOKEN 						The token query is for a session in which no user is logged-on. This occurs, for example, when the session is in the idle state.
        /// </returns>
        /// <remarks>
        ///		For information about primary tokens, see Access Tokens, http://msdn.microsoft.com/library/en-us/secauthz/security/access_tokens.asp.  
        ///		For more information about account privileges, see Privileges (http://msdn.microsoft.com/library/en-us/secauthz/security/privileges.asp) 
        ///		and Authorization Constants (http://msdn.microsoft.com/library/en-us/secauthz/security/authorization_constants.asp.
        ///
        ///		See LocalSystem account (http://msdn.microsoft.com/library/en-us/dllproc/base/localsystem_account.asp) 
        ///		for information about the privileges associated with that account.
        /// 
        ///		WTSQueryUserToken, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/termserv/termserv/wtsqueryusertoken.asp
        /// </remarks>
        [DllImport(
            "wtsapi32.dll",
            EntryPoint = "WTSQueryUserToken",
            CharSet = CharSet.Auto,
            SetLastError = true),
            SuppressUnmanagedCodeSecurityAttribute
        ]
        public static extern bool WtsQueryUserToken(
            int sessionId,
            ref IntPtr userToken
        );


        /// <summary>
        ///		The WtsQuerySessionInformation function retrieves session 
        ///		information for the specified session on the specified terminal server.  
        ///		It can be used to query session information on local and 
        ///		remote terminal servers.
        /// </summary>
        /// <param name="serverHandle">
        ///		Handle to a terminal server.  Specify a handle opened by the 
        ///		<see cref="Native.WtsOpenServer">WtsOpenServer</see> 
        ///		function, or specify WtsCurrentServerHandle to indicate 
        ///		the terminal server on which your application is running.
        /// </param>
        /// <param name="sessionId">
        ///		A Terminal Services session identifier.  Any program running 
        ///		in the context of a service will have a session identifier of 
        ///		zero (0). You can use the 
        ///		<see cref="Native.WtsEnumerateSessions">WtsEnumerateSessions</see> 
        ///		function to retrieve the identifiers of all sessions on a 
        ///		specified terminal server.
        ///
        ///		To be able to query information for another user's session, 
        ///		you need to have the Query Information permission.  For more information, 
        ///		see Terminal Services Permissions, http://msdn.microsoft.com/library/en-us/termserv/termserv/terminal_services_permissions.asp.  
        ///		To modify permissions on a session, use the Terminal Services 
        ///		Configuration administrative tool. 
        /// </param>
        /// <param name="wtsQueryInfoKey">
        ///		Specifies the type of information to retrieve.  This parameter can 
        ///		be one of the values from the 
        ///		<see cref="Win32.WtsQueryInfoTypes">WtsQueryInfoTypes</see> 
        ///		enumeration type.
        /// </param>
        /// <param name="wtsQueryInfoValue">
        ///		Value of the requested information.  The format and contents 
        ///		of the data depend on the information class specified in the 
        ///		WtsInfoClass parameter. To free the returned buffer, 
        ///		call the WtsFreeMemory function.
        /// </param>
        /// <param name="wtsQueryInfoValueSize">
        ///		The size, in bytes, of the data in wtsQueryInfoValue.
        /// </param>
        /// <returns>
        ///		If the function succeeds, the return value is a nonzero value.
        ///
        ///		If the function fails, the return value is zero.  
        ///		To get extended error information, call 
        ///		<see cref="System.Runtime.InteropServices.Marshal.GetLastWin32Error">GetLastWin32Error()</see>
        /// </returns>
        /// <remarks>
        ///		Use WtsApi32.Managed.QuerySessionInformation instead.
        /// 
        ///		To retrieve the session id for the current session when Terminal 
        ///		Services is running, call WtsQuerySessionInformation and specify 
        ///		WtsCurrentSessionId for the SessionId parameter and WtsSessionId for 
        ///		the WtsQueryInfoTypes parameter. The session id will be returned in 
        ///		the wtsQueryInfoValue parameter. If Terminal Services is not running, 
        ///		calls to WtsQuerySessionInformation fail. In this situation, you can 
        ///		retrieve the current session id by calling the ProcessIdToSessionId function.
        ///		
        ///		To determine whether your application is running on the physical console, 
        ///		you can do the following:
        ///
        ///		As described previously, call WtsQuerySessionInformation and specify 
        ///		WtsCurrentSessionId for the SessionId parameter and WtsSessionId for 
        ///		the WtsQueryInfoTypes parameter. The session id returned in wtsQueryInfoValue 
        ///		is zero for the Terminal Services console session.
        ///
        ///		Session 0 might not be attached to the physical console. This is 
        ///		because session 0 can be attached to a remote session. Additionally, 
        ///		fast user switching is implemented using Terminal Services sessions.  
        ///		The first user to log on uses session 0, the next user to log on uses 
        ///		session 1, and so on.  To determine if your application is running 
        ///		on the physical console, call the WtsGetActiveConsoleSessionId function as follows:
        /// 
        ///		<code>
        ///			(CurrentSessionId == WtsGetActiveConsoleSessionId ())
        ///		</code>
        /// 
        ///		It is not necessary that Terminal Services be running for 
        ///		WtsGetActiveConsoleSessionId to succeed.
        /// 
        ///		WTSQuerySessionInformation, http://msdn.microsoft.com/library/default.asp?url=/library/en-us/termserv/termserv/wtsquerysessioninformation.asp
        /// </remarks>
        [DllImport(
            "wtsapi32.dll",
            EntryPoint = "WTSQuerySessionInformation",
            CharSet = CharSet.Auto,
            SetLastError = true),
            SuppressUnmanagedCodeSecurityAttribute
        ]
        public static extern bool WtsQuerySessionInformation(
            IntPtr serverHandle,
            int sessionId,
            WtsQueryInfoTypes wtsQueryInfoKey,
            out IntPtr wtsQueryInfoValue,
            out int wtsQueryInfoValueSize
        );

        /// <summary>
        ///		The WtsFreeMemory function frees memory allocated 
        ///		by a Terminal Services function.
        /// </summary>
        /// <param name="memory">
        ///		The memory to free.
        /// </param>
        /// <remarks>
        ///		Several Terminal Services functions allocate buffers to 
        ///		return information. Use the WtsFreeMemory function to 
        ///		free these buffers.
        /// </remarks>
        [DllImport(
            "wtsapi32.dll",
            EntryPoint = "WTSFreeMemory",
            SetLastError = true),
            SuppressUnmanagedCodeSecurityAttribute
        ]
        public static extern void WtsFreeMemory(IntPtr memory);

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
        public static extern bool CloseHandle(IntPtr handle);

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
        public static extern bool LogonUser(
            string userName,
            string domainName,
            string password,
            LogonType logonType,
            LogonProvider logonProvider,
            out IntPtr userToken);

        /// <summary>
        ///		The CreateProcessAsUser function creates a new process and its 
        ///		primary thread.  The new process then runs the specified executable file.
        /// 
        ///		The CreateProcessAsUser function is similar to the CreateProcess function, 
        ///		except that the new process runs in the security context of the user 
        ///		represented by the userToken parameter.  This function is also similar to 
        ///		the SHCreateProcessAsUserW function.
        /// </summary>
        /// <param name="userToken">
        ///		Handle to a primary token that represents a user.  The handle must have 
        ///		the TokenQuery, TokenDuplicate, and TokenAssignPrimary access rights.  
        ///		For more information, see Access Rights for Access-Token Objects. 
        ///		The user represented by the token must have read and execute access to 
        ///		the application specified by the applicationName or the commandLine parameter.
        /// 
        ///		To get a primary token that represents the specified user, call the 
        ///		LogonUser function.  Alternatively, you can call the DuplicateTokenEx function 
        ///		to convert an impersonation token into a primary token.  This allows a server 
        ///		application that is impersonating a client to create a process that has the 
        ///		security context of the client.
        /// 
        ///		Terminal Services:  The process is run in the session specified in the 
        ///		token.  By default, this is the same session that called LogonUser.  
        ///		To change the session, use the SetTokenInformation function.
        /// </param>
        /// <param name="applicationName">
        ///		A string that specifies the module to execute.  The specified module can 
        ///		be a Windows-based application.  It can be some other type of module 
        ///		(for example, MS-DOS or OS/2) if the appropriate subsystem is available 
        ///		on the local computer.
        /// 
        ///		The string can specify the full path and file name of the module to 
        ///		execute or it can specify a partial name.  In the case of a partial name, 
        ///		the function uses the current drive and current directory to complete the 
        ///		specification. The function will not use the search path. If the file name 
        ///		does not contain an extension, .exe is assumed.  Therefore, if the file 
        ///		name extension is .com, this parameter must include the .com extension.
        /// 
        ///		The applicationName parameter can be NULL.  In that case, the module 
        ///		name must be the first white space-delimited token in the commandLine 
        ///		string.  If you are using a long file name that contains a space, 
        ///		use quoted strings to indicate where the file name ends and the arguments 
        ///		begin; otherwise, the file name is ambiguous.  For example, consider 
        ///		the string "c:\program files\sub dir\program name".  This string can be 
        ///		interpreted in a number of ways.  The system tries to interpret the 
        ///		possibilities in the following order:
        /// 
        ///		<code>
        ///			c:\program.exe files\sub dir\program name
        ///			c:\program files\sub.exe dir\program name
        ///			c:\program files\sub dir\program.exe name
        ///			c:\program files\sub dir\program name.exe
        ///		</code>
        /// 
        ///		If the executable module is a 16-bit application, applicationName should be 
        ///		NULL, and the string pointed to by commandLine should specify the executable 
        ///		module as well as its arguments.  By default, all 16-bit Windows-based 
        ///		applications created by CreateProcessAsUser are run in a separate VDM 
        ///		(equivalent to CreateSeperateWowVdm in CreateProcess).
        /// </param>
        /// <param name="commandLine">
        ///		A string that specifies the command line to execute.  The maximum length of 
        ///		this string is 32,000 characters.
        /// 
        ///		Windows 2000:  The maximum length of this string is MaxPath characters.
        /// 
        ///		The Unicode version of this function, CreateProcessAsUserW, will fail 
        ///		if this parameter is a const string.
        /// 
        ///		The commandLine parameter can be NULL.  In that case, the function uses 
        ///		the string pointed to by applicationName as the command line.
        /// 
        ///		If both applicationName and commandLine are non-NULL, *applicationName 
        ///		specifies the module to execute, and *commandLine specifies the command line.  
        ///		The new process can use GetCommandLine to retrieve the entire command line.  
        ///		Console processes written in C can use the argc and argv arguments to parse 
        ///		the command line. Because argv[0] is the module name, C programmers generally 
        ///		repeat the module name as the first token in the command line.
        /// 
        ///		If applicationName is NULL, the first white-space – delimited token of 
        ///		the command line specifies the module name. If you are using a long file name 
        ///		that contains a space, use quoted strings to indicate where the file name ends 
        ///		and the arguments begin (see the explanation for the lpApplicationName parameter).  
        ///		If the file name does not contain an extension, .exe is appended.  Therefore, 
        ///		if the file name extension is .com, this parameter must include the .com extension.  
        ///		If the file name ends in a period (.) with no extension, or if the file name 
        ///		contains a path, .exe is not appended.  If the file name does not contain a 
        ///		directory path, the system searches for the executable file in the following sequence:
        /// 
        ///			1. The directory from which the application loaded.
        ///			
        ///			2. The current directory for the parent process.
        ///			
        ///			3. The 32-bit Windows system directory. Use the GetSystemDirectory 
        ///				function to get the path of this directory.
        ///			
        ///			4. The 16-bit Windows system directory. There is no function 
        ///				that obtains the path of this directory, but it is searched.
        ///			
        ///			5. The Windows directory. Use the GetWindowsDirectory function 
        ///				to get the path of this directory.
        ///			
        ///			6. The directories that are listed in the PATH environment variable.
        /// 
        ///		The system adds a null character to the command line string to separate the 
        ///		file name from the arguments.  This divides the original string into two 
        ///		strings for internal processing.
        /// </param>
        /// <param name="processAttributes">
        ///		Pointer to a SecurityAttributes structure that specifies a security 
        ///		descriptor for the new process and determines whether child processes 
        ///		can inherit the returned handle.  If lpProcessAttributes is NULL or 
        ///		SecurityDescriptor is NULL, the process gets a default security descriptor 
        ///		and the handle cannot be inherited.  The default security descriptor is 
        ///		that of the user referenced in the hToken parameter.  This security 
        ///		descriptor may not allow access for the caller, in which case the 
        ///		process may not be opened again after it is run.  The process handle 
        ///		is valid and will continue to have full access rights.
        /// </param>
        /// <param name="threadAttributes">
        ///		Pointer to a SecurityAttributes structure that specifies a security 
        ///		descriptor for the new process and determines whether child processes 
        ///		can inherit the returned handle.  If ThreadAttributes is NULL or 
        ///		SecurityDescriptor is NULL, the thread gets a default security descriptor 
        ///		and the handle cannot be inherited.  The default security descriptor 
        ///		is that of the user referenced in the hToken parameter.  This security 
        ///		descriptor may not allow access for the caller.
        /// </param>
        /// <param name="inheritHandles">
        ///		If this parameter is TRUE, each inheritable handle in the calling process 
        ///		is inherited by the new process.  If the parameter is FALSE, the handles 
        ///		are not inherited.  Note that inherited handles have the same value and 
        ///		access rights as the original handles.
        /// 
        ///		Terminal Services:  You cannot inherit handles across sessions.  Additionally, 
        ///		if this parameter is TRUE, you must create the process in the same session 
        ///		as the caller.
        /// </param>
        /// <param name="creationFlags">
        ///		Flags that control the priority class and the creation of the process.  
        ///		For a list of values, see Process Creation Flags.
        ///
        ///		This parameter also controls the new process's priority class, which 
        ///		is used to determine the scheduling priorities of the process's threads.  
        ///		For a list of values, see GetPriorityClass.  If none of the priority class 
        ///		flags is specified, the priority class defaults to NormalPriorityClass unless 
        ///		the priority class of the creating process is IdlePriorityClass or 
        ///		BelowNormalPriorityClass.  In this case, the child process receives the 
        ///		default priority class of the calling process.
        /// </param>
        /// <param name="environment">
        ///		Pointer to an environment block for the new process.  If this parameter is NULL, 
        ///		the new process uses the environment of the calling process.
        ///
        ///		An environment block consists of a null-terminated block of strings.  
        ///		Each string is in the form:
        /// 
        ///		<code>name=value</code>
        /// 
        ///		Because the equal sign is used as a separator, it must not be used in the 
        ///		name of an environment variable.
        /// 
        ///		An environment block can contain either Unicode or ANSI characters.  If 
        ///		the environment block pointed to by environment contains Unicode characters, 
        ///		be sure that creationFlags includes CreateUnicodeEnvironment.
        /// 
        ///		Note that an ANSI environment block is terminated by two zero bytes: one for 
        ///		the last string, one more to terminate the block.  A Unicode environment 
        ///		block is terminated by four zero bytes: two for the last string, two more 
        ///		to terminate the block.
        /// 
        ///		To retrieve a copy of the environment block for a given user, use the 
        ///		CreateEnvironmentBlock function.
        /// </param>
        /// <param name="currentDirectory">
        ///		Pointer to a null-terminated string that specifies the full path to the current 
        ///		directory for the process. The string can also specify a UNC path.
        /// 
        ///		If this parameter is NULL, the new process will have the same current drive 
        ///		and directory as the calling process. (This feature is provided primarily for 
        ///		shells that need to start an application and specify its initial drive 
        ///		and working directory.)
        /// </param>
        /// <param name="startupInfo">
        ///		Pointer to a StartupInfo structure that specifies the window station, 
        ///		desktop, standard handles, and appearance of the main window for the new process.
        /// </param>
        /// <param name="processInformation">
        ///		Pointer to a ProcessInformation structure that receives identification 
        ///		information about the new process.
        /// 
        ///		Handles in ProcessInformation must be closed with CloseHandle when 
        ///		they are no longer needed.
        /// </param>
        /// <returns>
        ///		If the function succeeds, the return value is nonzero.
        ///
        ///		If the function fails, the return value is zero.  To get extended error 
        ///		information, call GetLastError.
        /// </returns>
        /// <remarks>
        ///		Typically, the process that calls the CreateProcessAsUser function must have the 
        ///		SE_ASSIGNPRIMARYTOKEN_NAME and SE_INCREASE_QUOTA_NAME privileges.  However, if 
        ///		userToken is a restricted version of the caller's primary token, the 
        ///		SE_ASSIGNPRIMARYTOKEN_NAME privilege is not required.  If the necessary privileges 
        ///		are not already enabled, CreateProcessAsUser enables them for the duration of the 
        ///		call.  For more information, see Running with Special Privileges, http://msdn.microsoft.com/library/en-us/secbp/security/running_with_special_privileges.asp.
        /// 
        ///		CreateProcessAsUser must be able to open the primary token of the calling process 
        ///		with the TOKEN_DUPLICATE and TOKEN_IMPERSONATE access rights.
        /// 
        ///		By default, CreateProcessAsUser creates the new process on a noninteractive window 
        ///		station with a desktop that is not visible and cannot receive user input.  To 
        ///		enable user interaction with the new process, you must specify the name of the 
        ///		default interactive window station and desktop, "winsta0\default", in the 
        ///		lpDesktop member of the StartupInfo structure. In addition, before calling 
        ///		CreateProcessAsUser, you must change the discretionary access control list (DACL) 
        ///		of both the default interactive window station and the default desktop.  
        ///		The DACLs for the window station and desktop must grant access to the user or 
        ///		the logon session represented by the userToken parameter.
        /// 
        ///		CreateProcessAsUser does not load the specified user's profile into the 
        ///		HKEY_USERS registry key.  Therefore, to access the information in the 
        ///		HKEY_CURRENT_USER registry key, you must load the user's profile information 
        ///		into HKEY_USERS with the LoadUserProfile function before calling CreateProcessAsUser.  
        ///		Be sure to call UnloadUserProfile after the new process exits.
        /// 
        ///		If the environment parameter is NULL, the new process inherits the environment 
        ///		of the calling process.  CreateProcessAsUser does not automatically modify the 
        ///		environment block to include environment variables specific to the user 
        ///		represented by hToken.  For example, the USERNAME and USERDOMAIN variables are 
        ///		inherited from the calling process if lpEnvironment is NULL.  It is your 
        ///		responsibility to prepare the environment block for the new process and specify 
        ///		it in environment.
        /// 
        ///		The CreateProcessWithLogonW and CreateProcessWithTokenW functions are similar 
        ///		to CreateProcessAsUser, except that the caller does not need to call the LogonUser 
        ///		function to authenticate the user and get a token.
        /// 
        ///		CreateProcessAsUser allows you to access the specified directory and executable image 
        ///		in the security context of the caller or the target user. By default, 
        ///		CreateProcessAsUser accesses the directory and executable image in the security 
        ///		context of the caller. In this case, if the caller does not have access to the 
        ///		directory and executable image, the function fails.  To access the directory and 
        ///		executable image using the security context of the target user, specify userToken 
        ///		in a call to the ImpersonateLoggedOnUser function before calling CreateProcessAsUser.
        /// 
        ///		The process is assigned a process identifier.  The identifier is valid until 
        ///		the process terminates. It can be used to identify the process, or specified in 
        ///		the OpenProcess function to open a handle to the process.  The initial thread in 
        ///		the process is also assigned a thread identifier.  It can be specified in the 
        ///		OpenThread function to open a handle to the thread.  The identifier is valid 
        ///		until the thread terminates and can be used to uniquely identify the thread 
        ///		within the system. These identifiers are returned in the ProcessInformation structure.
        /// 
        ///		The calling thread can use the WaitForInputIdle function to wait until the 
        ///		new process has finished its initialization and is waiting for user input 
        ///		with no input pending.  This can be useful for synchronization between parent 
        ///		and child processes, because CreateProcessAsUser returns without waiting for 
        ///		the new process to finish its initialization.  For example, the creating process 
        ///		would use WaitForInputIdle before trying to find a window associated with the new process.
        /// 
        ///		The preferred way to shut down a process is by using the ExitProcess function, because 
        ///		this function sends notification of approaching termination to all DLLs attached to 
        ///		the process. Other means of shutting down a process do not notify the attached DLLs.  
        ///		Note that when a thread calls ExitProcess, other threads of the process are terminated 
        ///		without an opportunity to execute any additional code (including the thread termination 
        ///		code of attached DLLs). For more information, see Terminating a Process, http://msdn.microsoft.com/library/en-us/dllproc/base/terminating_a_process.asp.
        /// 
        ///		Security Remarks
        /// 
        ///		The applicationName parameter can be NULL, in which case the executable name must 
        ///		be the first white space-delimited string in commandLine.  If the executable or 
        ///		path name has a space in it, there is a risk that a different executable could be 
        ///		run because of the way the function parses spaces.  The following example is dangerous 
        ///		because the function will attempt to run "Program.exe", if it exists, 
        ///		instead of "MyApp.exe".
        /// 
        ///		<code>CreateProcessAsUser(hToken, NULL, "C:\\Program Files\\MyApp", ...)</code>
        /// 
        ///		If a malicious user were to create an application called "Program.exe" on a system, 
        ///		any program that incorrectly calls CreateProcessAsUser using the Program Files 
        ///		directory will run this application instead of the intended application.
        /// 
        ///		To avoid this problem, do not pass NULL for lpApplicationName.  If you do pass NULL 
        ///		for applicationName, use quotation marks around the executable path in commandLine, 
        ///		as shown in the example below.
        /// 
        ///		<code>CreateProcessAsUser(hToken, NULL, "\"C:\\Program Files\\MyApp.exe\"", ...)</code>
        /// </remarks>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool CreateProcessAsUser(
            IntPtr userToken,
            string applicationName,
            string commandLine,
            ref SecurityAttributes processAttributes,
            ref SecurityAttributes threadAttributes,
            bool inheritHandles,
            int creationFlags,
            IntPtr environment,
            string currentDirectory,
            ref StartupInfo startupInfo,
            out ProcessInformation processInformation);
    }

    internal class Managed
    {
        static Managed()
        {
        }

        /// <summary>
        ///		The WtsQuerySessionInformation method retrieves session 
        ///		information for the specified session on the specified terminal server.  
        ///		It can be used to query session information on local and 
        ///		remote terminal servers.
        /// </summary>
        /// <param name="serverHandle">
        ///		Handle to a terminal server.  Specify a handle opened by the 
        ///		<see cref="Native.WtsOpenServer">WtsOpenServer</see> 
        ///		function, or specify WtsCurrentServerHandle to indicate 
        ///		the terminal server on which your application is running.
        /// </param>
        /// <param name="sessionId">
        ///		A Terminal Services session identifier.  Any program running 
        ///		in the context of a service will have a session identifier of 
        ///		zero (0). You can use the 
        ///		<see cref="Native.WtsEnumerateSessions">WtsEnumerateSessions</see> 
        ///		function to retrieve the identifiers of all sessions on a 
        ///		specified terminal server.
        ///
        ///		To be able to query information for another user's session, 
        ///		you need to have the Query Information permission.  For more information, 
        ///		see Terminal Services Permissions, http://msdn.microsoft.com/library/en-us/termserv/termserv/terminal_services_permissions.asp.  
        ///		To modify permissions on a session, use the Terminal Services 
        ///		Configuration administrative tool. 
        /// </param>
        /// <param name="wtsQueryInfoKey">
        ///		Specifies the type of information to retrieve.  This parameter can 
        ///		be one of the string values from the 
        ///		<see cref="Win32.WtsQueryInfoTypes">WtsQueryInfoTypes</see> 
        ///		enumeration type.
        /// </param>
        /// <param name="wtsQueryInfoValue">
        ///		The returned string.
        /// </param>
        /// <returns>True if the method suceeded, false if otherwise.</returns>
        /// <remarks>
        ///		This method invokes the native 
        ///		<see cref="Native.WtsQuerySessionInformation">WtsQuerySessionInformation</see>, 
        ///		copies its results into managed memory, and frees the unmanaged
        ///		memory for you.
        /// </remarks>
        public static bool WtsQuerySessionInformation(
            IntPtr serverHandle,
            int sessionId,
            WtsQueryInfoTypes wtsQueryInfoKey,
            out string wtsQueryInfoValue)
        {
            IntPtr ppbuff;
            int ppct;

            // call the native method to fetch the value
            bool result =
                Native.WtsQuerySessionInformation(serverHandle, sessionId, wtsQueryInfoKey, out ppbuff, out ppct);

            // if the function failed then throw a win32 exception
            if (!result)
            {
                throw (new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()));
            }
            // if the function succeeded copy the string from unmanaged
            // memory into a managed string and free the unmanaged memory
            else
            {
                wtsQueryInfoValue = Marshal.PtrToStringAuto(ppbuff);
                Native.WtsFreeMemory(ppbuff);
                return (result);
            }
        }

        /// <summary>
        ///		The WtsEnumerateSessions method retrieves 
        ///		a list of sessions on a specified terminal server.
        /// </summary>
        /// <param name="serverHandle">
        ///		Handle to a terminal server.
        /// </param>
        /// <returns>
        ///		An array of WtsSessionInfo structures containing
        ///		information about the sessions on the terminal server.
        /// </returns>
        /// <remarks>
        ///		This is the managed version of 
        ///		<see cref="Managed.WtsEnumerateSessions">WtsApi32.NativeMethods.WtsEnumerateSessions</see>
        /// </remarks>
        /// <exception cref="System.ComponentModel.Win32Exception" />
        public static WtsSessionInfo[] WtsEnumerateSessions(IntPtr serverHandle)
        {
            // pointer to pointer of where the SessionInfo
            // structures are
            IntPtr ppWsi = IntPtr.Zero;

            // number of structures that ppWsi
            // is pointing to
            int wsi_ct = 0;

            // array of session info structures that
            // this method will return if all goes
            // according to plan
            WtsSessionInfo[] wsi = null;

            // try to enumerate the sessions on the server.  if
            // the enumeration fails throw an exception with the
            // win32 error code.
            if (!Native.WtsEnumerateSessions(
                serverHandle, 0, 1, out ppWsi, out wsi_ct))
            {
                throw (new System.ComponentModel.Win32Exception(
                    Marshal.GetLastWin32Error()));
            }
            // marshal the return structures to a managed array
            // and return that array from ths method
            else
            {
                wsi = new WtsSessionInfo[wsi_ct];

                int ppWsi_index = 0;

                // save the pointer to the original structure
                IntPtr ppWsi_original = ppWsi;

                try
                {
                    for (int x = 0; x < wsi.Length; ++x)
                    {
                        wsi[x] = (WtsSessionInfo)Marshal.PtrToStructure(ppWsi, typeof(WtsSessionInfo));

                        // Whoo Hoo!  Pointer arithemetic!  I almost forgot how to do this.
                        ppWsi_index = (int)(ppWsi) + Marshal.SizeOf(typeof(WtsSessionInfo));

                        // Get the location of the next structure.
                        ppWsi = (IntPtr)(ppWsi_index);
                    }
                }
                finally
                {
                    // Free the memory.
                    Native.WtsFreeMemory(ppWsi_original);
                }

                return (wsi);
            }
        }
    }
}
