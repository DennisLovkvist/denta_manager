function Denta-Manager 
{
    cd "C:\Program Files\DentaManager";
    clear;
    .\denta_manager.exe;  
}

New-Alias -Name dm -Value Denta-Manager

Export-ModuleMember -Function Denta-Manager -Alias *
