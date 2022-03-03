using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using static denta_manager.Common;

namespace denta_manager
{
    static class WindowsClipboard
    {
        public static void SetText(string text)
        {
            OpenClipboard();

            EmptyClipboard();
            IntPtr hGlobal = default;
            try
            {
                var bytes = (text.Length + 1) * 2;
                hGlobal = Marshal.AllocHGlobal(bytes);

                if (hGlobal == default)
                {
                    ThrowWin32();
                }

                var target = GlobalLock(hGlobal);

                if (target == default)
                {
                    ThrowWin32();
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, target, text.Length);
                }
                finally
                {
                    GlobalUnlock(target);
                }

                if (SetClipboardData(cfUnicodeText, hGlobal) == default)
                {
                    ThrowWin32();
                }

                hGlobal = default;
            }
            finally
            {
                if (hGlobal != default)
                {
                    Marshal.FreeHGlobal(hGlobal);
                }

                CloseClipboard();
            }
        }

        public static void OpenClipboard()
        {
            var num = 10;
            while (true)
            {
                if (OpenClipboard(default))
                {
                    break;
                }

                if (--num == 0)
                {
                    ThrowWin32();
                }

                Thread.Sleep(100);
            }
        }

        const uint cfUnicodeText = 13;

        static void ThrowWin32()
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SetClipboardData(uint uFormat, IntPtr data);

        [DllImport("user32.dll")]
        static extern bool EmptyClipboard();
    }
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "Denta Manager 1.0";

            if(Console.LargestWindowWidth < 1 ||Console.LargestWindowHeight < 1)return;
            
            Console.SetWindowSize((80 < Console.LargestWindowWidth ? 80:Console.LargestWindowWidth), (60 < Console.LargestWindowHeight ? 60 : Console.LargestWindowHeight));
            

            Branch empty_branch = Common.CreateBranch(null, new string[] { "null" }, "null", "null", "null","null","null", new string[0], false);
            Branch root = CreateBranch(null, new string[] { "root" }, "root", "null", "null", "null", "null", new string[] { "", " ", "root" }, true);
            Branch selected_branch = root;

            string[] cmds = new string[] { "exit", "help", "credits" };

            Command[] commands = new Command[3];
            commands[0] = Exit;
            commands[1] = Help;
            commands[2] = Credits;

            List<Branch> endpoints = new List<Branch>();

            Config config = new Config();

            ReadConfig(ref config);

            bool loaded = LoadSource(config, root, empty_branch, endpoints);

            List<Alias> alternative_aliases = new List<Alias>();
            List<Description> descriptions = new List<Description>();

            LoadAliases(config, alternative_aliases);

            LoadDescriptions(config, descriptions);

            if(loaded)
            {
                ApplyDescriptions(endpoints, descriptions);
            }

            List<ColorMark> color_marks_recycle = new List<ColorMark>();
            List<ColorMark> color_marks_alloc = new List<ColorMark>();


            if (loaded)
            {
                DrawTree(root, color_marks_recycle, color_marks_alloc,config.color_base,config.color_highlight);

            }
            else
            {
                Credits(config.color_base,config.color_highlight);
                FastConsole.WriteLine("Could not find source (" + config.path_source_data + ")");
            }

            while (true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();

                if (loaded)
                {
                    if (keyInfo.Key == ConsoleKey.LeftArrow)
                    {
                        MoveUpStructure(root, ref selected_branch, color_marks_recycle, color_marks_alloc,config.color_base,config.color_highlight);
                    }
                    else if (keyInfo.Key == ConsoleKey.RightArrow)
                    {
                        MoveDownStructure(root, ref selected_branch, color_marks_recycle, color_marks_alloc, config.color_base, config.color_highlight);
                    }
                    else if (keyInfo.Key == ConsoleKey.DownArrow)
                    {
                        MoveDownCurrentBranch(root, ref selected_branch, color_marks_recycle, color_marks_alloc, config.color_base, config.color_highlight);
                    }
                    else if (keyInfo.Key == ConsoleKey.UpArrow)
                    {
                        MoveUpCurrentBranch(root, ref selected_branch, color_marks_recycle, color_marks_alloc, config.color_base, config.color_highlight);
                    }
                    else if (keyInfo.Key == ConsoleKey.C)
                    {
                        Console.Clear();
                        CollapseStructure(root, ref selected_branch, color_marks_recycle, color_marks_alloc, config.color_base, config.color_highlight);
                    }
                    else if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        Exit(config.color_base, config.color_highlight);
                    }
                    else if (keyInfo.Key == ConsoleKey.T)
                    {
                        Branch target = selected_branch.branches[selected_branch.current_child_index];
                        WindowsClipboard.SetText(target.tvid);
                        DrawTree(root, color_marks_recycle, color_marks_alloc, config.color_base, config.color_highlight);
                    }
                    else if (keyInfo.Key == ConsoleKey.I)
                    {
                        Branch target = selected_branch.branches[selected_branch.current_child_index];
                        WindowsClipboard.SetText(target.ip_address);
                        DrawTree(root, color_marks_recycle, color_marks_alloc, config.color_base, config.color_highlight);
                    }
                    else if (keyInfo.Key == ConsoleKey.Enter)
                    {
                        if (selected_branch.branches.Count > selected_branch.current_child_index)
                        {
                            Branch target = selected_branch.branches[selected_branch.current_child_index];

                            if (target.endpoint_type == "tv")
                            {
                                RunEndpointTeamViewer(target, config.path_team_viewer);
                            }
                            else if (target.endpoint_type == "rdp")
                            {
                                RunEndpointRemoteDesktop(config.path_remote_desktop, target.ip_address);
                            }
                        }
                    }
                }
                if (keyInfo.Key == ConsoleKey.Spacebar)
                {
                    CommandPrompt(root, alternative_aliases, ref selected_branch, endpoints, empty_branch, cmds, commands, loaded, color_marks_recycle, color_marks_alloc, config.color_base, config.color_highlight);
                }
            }
        }

        

        


        
       
    }
}