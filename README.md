# denta_manager
## Description
Console application written in C# for managing Team Viewer connections.

Language: C#

Dependencies: .NET Core 5.0

## About
When working a servicedesk job I was looking for a replacement to Team Viewer Manager.
Me and some of my colleagues wanted something faster and more lightweight that could run within the terminal (powershell).


## Future development
* Visual changes on how the tree is drawn.
* Ability to change key-mappings.
* Fix "rendering" when window is resized.
* Better password storage

## Installation
### Build
```
$ git clone https://github.com/DennisLovkvist/denta_manager.git
$ cd denta_manager
$ ./scripts/publish.ps1
$ ./scripts/generate_sample_data.ps1 (optional)
$ ./scripts/install.ps1
$ ./scripts/configure_powershell_module.ps1 (optional)
```
### Test Run
```
$ ./scripts/run.ps1
```
### Run
From CLI
```
$ .\C:\Program Files\DentaManager\denta_manager.exe
```
In Powershell as module
```
$ dm
```

## Configuration
### data.txt
The file data.txt contains the information used to create the endpoints and the tree where they are located. 
Each line is semi-colon seperated and holds the endpoints full path, name, team viwer id, ip address, password and alias. The tree is constructed automatically.
If two or more endpoints are added and they share the same path, no duplicate branches are created. 
Example:

* root/Test Store Chain 0/Test Store 0/Department 0/[ComputerName];Regiter 0;null;172.0.0.1;tv;password;test_alias
* root/Test Store Chain 0/Test Store 0/Department 0/[ComputerName];Regiter 1;null;172.0.0.1;tv;password;test_alias

![Alt text](screenshots/readme_img_0.png?raw=true "Screenshot")

### aliases.txt
This file is used to create aliases for the nodes in the tree. Appending "root/Sample Store Chain 0/Sample Store 1;store1" to the file allows one to find Sample Store 1 by the alias store1. This is done by hitting the spacebar, writing store1 and pressing enter.

![Alt text](screenshots/readme_img_1.png?raw=true "Screenshot")
![Alt text](screenshots/readme_img_2.png?raw=true "Screenshot")

### config.txt
This file is used to change where the program is looking for the different text files, path to team viewer and remote desktop as well ass a few other minor settings.

## Controls
* Navigate up tree: A
* Navigate down tree: D
* Navigate up current branch: W
* Navigate down current branch: S
* Enter command mode: Spacebar
* Connect to endpoint: Enter

