$root = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Definition);

$path_install_program = "C:\Program Files\DentaManager";
$path_install_program = "C:\Temp\test_install_dm";
#Clean install
if([System.IO.Directory]::Exists($path_install_program))
{
    rm $path_install_program -Recurse;
}
else 
{
    New-Item -Path $path_install_program -ItemType Directory;
}

cp ($root + "\..\bin\publish\denta_manager.exe") $path_install_program;
cp ($root + "\..\bin\publish\config.txt") $path_install_program;
cp ($root + "\..\bin\publish\data.txt") $path_install_program;
cp ($root + "\..\bin\publish\aliases.txt") $path_install_program;