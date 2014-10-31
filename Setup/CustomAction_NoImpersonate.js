// NoImpersonate.js <msi-file>
// Performs a post-build fixup of an msi to change all deferred custom actions to NoImpersonate

// Constant values from Windows Installer
var msiOpenDatabaseModeTransact = 1;

var msiViewModifyInsert = 1
var msiViewModifyUpdate = 2
var msiViewModifyAssign = 3
var msiViewModifyReplace = 4
var msiViewModifyDelete = 6

var msidbCustomActionTypeInScript = 0x00000400;
var msidbCustomActionTypeNoImpersonate = 0x00000800

if (WScript.Arguments.Length != 1) {
    WScript.StdErr.WriteLine(WScript.ScriptName + " file");
    WScript.Quit(1);
}

var filespec = WScript.Arguments(0);
var installer = WScript.CreateObject("WindowsInstaller.Installer");
var database = installer.OpenDatabase(filespec, msiOpenDatabaseModeTransact);

var sql
var view
var record

try {
    sql = "UPDATE Property SET Value='ALL' WHERE Property='FolderForm_AllUsers'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    database.commit();
}
catch (e) {
    WScript.StdErr.WriteLine(e.description);
}

try {
    sql = "UPDATE Property SET Value=1 WHERE Property='ALLUSERS'";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    database.commit();
}
catch (e) {
    WScript.StdErr.WriteLine(e.description);
}

//try {
//    sql = "UPDATE InstallExecuteSequence SET Sequence=1450 WHERE Action='RemoveExistingProducts'";
//    view = database.OpenView(sql);
//    view.Execute();
//    view.Close();
//    database.commit();
//}
//catch (e) {
//    WScript.StdErr.WriteLine(e.description);
//}

try {
    sql = "SELECT `Component_` FROM `File` WHERE `FileName`='SUDOWI~2.EXE|Sudowin.Server.exe'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    var serviceC = record.StringData(1);
    view.Close();
    database.commit();

    sql = "INSERT INTO ServiceControl (ServiceControl,Name,Event,Arguments,Wait,Component_) VALUES ('Sudowin Install','Sudowin',1,null,0,'" + serviceC + "')";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    database.commit();
    
    sql = "INSERT INTO ServiceControl (ServiceControl,Name,Event,Arguments,Wait,Component_) VALUES ('Sudowin Stop\\Uninstall','Sudowin',160,null,1,'" + serviceC + "')";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    database.commit();
    
    sql = "INSERT INTO ServiceControl (ServiceControl,Name,Event,Arguments,Wait,Component_) VALUES ('Sudowin Install Stop\\Delete','Sudowin',10,null,1,'" + serviceC + "')";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    database.commit();

    sql = "INSERT INTO ServiceInstall (ServiceInstall,Name,DisplayName,ServiceType,StartType,ErrorControl,LoadOrderGroup,Dependencies,StartName,Password,Arguments,Component_,Description) VALUES ('SudowinInstall1','Sudowin','Sudowin',16,2,1,null,null,null,null,null,'" + serviceC + "','Hosts the server that sudo clients communicate with in order to facilitate privilege escalation.')";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    database.commit();
}
catch (e) {
    WScript.StdErr.WriteLine("Service Config");
    WScript.StdErr.WriteLine(e.description);
}


try {
    sql = "SELECT `Component_` FROM `File` WHERE `FileName` = 'SUDO.EXE|sudo.exe'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    var consoleClientC = record.StringData(1);
    view.Close();
    database.commit();

    sql = "INSERT INTO Environment (Environment,Name,Value,Component_) VALUES ('SudowinConsole','*+-Path','[~];[TARGETDIR]Clients\\Console','" + consoleClientC + "')";
    view = database.OpenView(sql);
    view.Execute();
    view.Close();
    database.commit();
}
catch (e) {
    WScript.StdErr.WriteLine(e.description);
}

//try {
//    sql = "SELECT `Action`, `Type`, `Source`, `Target` FROM `CustomAction`";
//    view = database.OpenView(sql);
//    view.Execute();
//    record = view.Fetch();
//    while (record) {
//        if (record.IntegerData(2) & msidbCustomActionTypeInScript) {
//            record.IntegerData(2) = record.IntegerData(2) | msidbCustomActionTypeNoImpersonate;
//            view.Modify(msiViewModifyReplace, record);
//        }
//        record = view.Fetch();
//    }

//    view.Close();
//    database.Commit();
//}
//catch (e) {
//    WScript.StdErr.WriteLine(e.description);
//    WScript.Quit(1);
//}

try {
    sql = "SELECT `Value` FROM `Property` WHERE `Property` = 'ProductVersion'";
    view = database.OpenView(sql);
    view.Execute();
    record = view.Fetch();
    var productVersion = record.StringData(1)
    var newFilename = filespec.replace('setup.msi', "AETD-Sudowin-" + productVersion + ".msi")

    var fso = new ActiveXObject("Scripting.FileSystemObject")
    fso.CopyFile(filespec, newFilename)

    view.Close();
    database.commit();
}
catch (e) {
    WScript.StdErr.WriteLine(e.description);
}
