$root = [System.IO.Path]::GetDirectoryName($myInvocation.MyCommand.Definition);

$path_data = $root + "\bin\publish\data.txt";
$path_aliases = $root + "\bin\publish\aliases.txt"; 

if(![System.IO.File]::Exists($path_data))
{
    New-Item $path_data;
}
if(![System.IO.File]::Exists($path_aliases))
{
    New-Item $path_aliases;
}

$lines = New-Object Collections.ArrayList;

for($i = 0;$i -lt 3; $i++)
{
    $lines.Add("#Sample Store Chain " + $i)>$null;
    for($j = 0;$j -lt 10; $j++)
    {
        for($k = 0;$k -lt 4; $k++)
        {
            for($l = 0;$l -lt 4; $l++)
            {                
                $lines.Add("root/Sample Store Chain " + $i + "/Sample Store " + $j + "/Department " + $k + "/[ComputerName];Regiter " + $l + ";null;172.0.0.1;tv;password;c" + $i +"s" + $j + "d" + $k + "r" + $l)>$null;
            }
        }
    }
}

Set-Content -Value $lines -Path $path_data;

$lines.Clear();

for($i = 0;$i -lt 3; $i++)
{
    $lines.Add("#Sample Store Chain " + $i)>$null;
    for($j = 0;$j -lt 10; $j++)
    {                      
         $lines.Add("root/Sample Store Chain " + $i + "/Sample Store " + $j + ";c" + $i +"s" + $j)>$null;    
    }
}
Set-Content -Value $lines -Path $path_aliases;