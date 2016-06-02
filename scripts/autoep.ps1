#sign: Set-AuthenticodeSignature \\fileserver.localdomain\scripts\autoep.ps1 @(Get-ChildItem cert:\CurrentUser\My -codesign)[0]
# Require that code is set
[CmdletBinding()]
Param ()

Import-Module "\\gfileserver.localdomain\PSModules$\Code"
$code = GetCode
Write-Verbose "Hostname returned Code: $code"

Add-Type -AssemblyName System.DirectoryServices.AccountManagement
$fileroot = "\\fileserver.localdomain\scripts\autoep\"
$validEPGroups = @(
    'AD-Group-1',
    'AD-Group-2',)

$debug = $PSCmdlet.MyInvocation.BoundParameters["Debug"].IsPresent
if ($debug) {
	$DebugPreference = "Continue"
}

$ErrorActionPreference = "Stop"
$logName = "COMPANYLOG"

Import-Module "\\fileserver.localdomain\PSModules$\Logging"
$success = (SetupLogs -sources @('AutoEP'))
if ($success -ne 0) {
    Write-Verbose "Unable to setup logs properly"
    Exit
}

if ($code -eq 0) {
    Write-Verbose "Error getting code from hostname!"
    Write-EventLog -LogName $logName -Source AutoEP -Message ("Unable to get Code from Hostname") -EventId (1) -EntryType error
    Exit
}

try {
    $eventid = 100
    
    if ($debug) {
        $eventid = 200
    }
    $reallyrun = $false
    
    Write-Debug "Not going to perform any actions, just log them"
    
    $admins = @((Get-Content ($fileroot + $code + "\globaladmins.txt")  | 
        Where-Object { !$_.StartsWith("#") -And !$_.StartsWith(",") } | 
        ConvertFrom-Csv -Header Account, System |
        where {$env:COMPUTERNAME -match $_.system -or $_.system -eq ""})| ForEach-Object { $_.account })
        
    $ignoreaccounts = @('renamed_admin') + @((Get-Content ($fileroot + $code + "\ignoreaccounts.txt")  | 
        Where-Object { !$_.StartsWith("#") } | 
        ConvertFrom-Csv -Header Account, System|
        where {$env:COMPUTERNAME -match $_.system -or $_.system -eq ""}) |ForEach-Object { $_.account })

    $ep = Get-Content ($fileroot + $code + "\ep.csv")  | 
        Where-Object { !$_.StartsWith("#") -And !$_.StartsWith(",") } | 
        ConvertFrom-Csv -Header System, Account|
        where {$env:COMPUTERNAME -match $_.system -or $_.system -eq ""}

    $validEPGroups = '(|' + [string]::join('', @($validEPGroups | ForEach-Object { '(CN=' + $_ + ')' } )) + ')'

    $searcher = New-Object DirectoryServices.DirectorySearcher
    $searcher.Filter = $validEPGroups

    $local_context = $(New-Object -TypeName System.DirectoryServices.AccountManagement.PrincipalContext -ArgumentList $([System.DirectoryServices.AccountManagement.ContextType]::Machine), $env:COMPUTERNAME)
    $domain_context = [System.DirectoryServices.AccountManagement.ContextType]::Domain
    #$(New-Object -TypeName System.DirectoryServices.AccountManagement.PrincipalContext 'Domain', "DOMAIN")

    # now they're converted to distingishednames
    $validEPGroups = '(|' + [string]::join('', @($searcher.findall() |ForEach-Object { '(memberOf=' + $_.Properties['distinguishedname'] + ')' })) + ')'
    $epMembers = ''
    $validEP = $admins
    if ($ep) {
        $epMembers = '(|' + [string]::join('', @($ep|ForEach-Object { '(sAMAccountName=' + $_.account.Split('\')[1] + ')' })) + ')'
        $searcher.Filter = '(&' + $validEPGroups + $epMembers + ')'
        $validEP += @($searcher.findall() |ForEach-Object { 'domain\' + $_.Properties['samaccountname'] })
    }

    $localadmins = @()
    $admin_group = $([System.DirectoryServices.AccountManagement.GroupPrincipal]::FindByIdentity($local_context, $([System.DirectoryServices.AccountManagement.IdentityType]::SID), "S-1-5-32-544"))
    $admin_group.Members | ForEach-Object { If (!$_.Description -eq "Built-in account for administering the computer/domain") { $localadmins += $_ } }

    $localadmins | ForEach-Object {
        $account = $_
        $accountname = $_.SamAccountName
        If ($_.ContextType -eq "Domain") { $accountname = $("DOMAIN\" + $accountname) }
        Write-Verbose "Local Admin: $accountname" 
        if ($validEP -notcontains $accountname -and $ignoreaccounts -notcontains $accountname) {
            try {
                Write-Verbose "Removing Account: $accountname" 
                if (-Not $debug) {
                    $ret = $admin_group.Members.Remove($account)
                    $admin_group.Save()
                }
                Write-EventLog -LogName $logName -Source AutoEP -Message ($accountname) -EventId ($eventid + 1) -EntryType information
            } catch [Exception] {
                Write-Verbose $_
                Write-Verbose "FAILED! Removing Account: $accountname" 
                Write-EventLog -LogName $logName -Source AutoEP -Message ("Account: " + $accountname + "`n" + "Message: " +$_) -EventId ($eventid + 2) -EntryType error
            }
        }
    }
    
    $validEP | ForEach-Object {
        $account = $_
        Write-Verbose "Valid Admin: $account" 
        
        $user = $null
        $context = $local_context
        If ($_.StartsWith("DOMAIN")) {
            $account = $account -replace "DOMAIN\\", ''
            $context = $domain_context
        }
        
        $user = [System.DirectoryServices.AccountManagement.UserPrincipal]::FindByIdentity( $context, $_)
        If ($user -eq $null) {
            $user = [System.DirectoryServices.AccountManagement.GroupPrincipal]::FindByIdentity( $context, $_)
        }

        if (($account) -and ($localadmins -notcontains $user)) {
            Write-Verbose "$account not set as Administrator" 
            if (-Not $debug) {
                Write-Verbose "User: ${user}"
                $ret = $admin_group.Members.Add($user)
                $admin_group.Save()
            }
            Write-EventLog -LogName $logName -Source AutoEP -Message ($_) -EventId ($eventid + 50) -EntryType information
        }
    }
} catch [Exception] {
    $_.Exception.Message
}
