$root = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Definition);
$module = "DentaManager";
$path_install_module = ("C:\Program Files\WindowsPowerShell\Modules\" + $module);

if([System.IO.Directory]::Exists($path_install_module))
{
    rm $path_install_module -Recurse;
}
cp ($root + "\..\" + $module) "C:\Program Files\WindowsPowerShell\Modules" -Recurse

