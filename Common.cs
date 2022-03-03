using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace denta_manager
{
    public static class Common
    {
        public delegate void Command(ConsoleColor color_base, ConsoleColor color_highlight);

        #region build
        public static Branch CreateBranch(Branch parent, string[] ou, string display_name, string ip_address, string tvid, string endpoint_type, string password, string[] aliases, bool expanded = false)
        {
            return new Branch()
            {
                parent = parent,
                branches = new List<Branch>(),
                aliases = aliases,
                organizational_units = ou,
                directory_name = ou.Length == 0 ? "root" : ou[ou.Length - 1],
                display_name = display_name,
                ip_address = ip_address,
                password = password,
                expanded = expanded,
                endpoint_type = endpoint_type,
                tvid = tvid,
                description = ""
            };
        }
        public static void AddEndpointToTree(List<Branch> endpoints, Branch current_sub_branch, Branch endpoint, int tree_depth)
        {
            int endpoint_organizational_units = endpoint.organizational_units.Length;
            int tree_organizational_units = current_sub_branch.organizational_units.Length;
            int count = current_sub_branch.branches.Count;

            for (int i = 0; i < count; i++)
            {
                if (tree_depth + 1 < endpoint_organizational_units)
                {
                    if (current_sub_branch.branches[i].display_name == endpoint.organizational_units[tree_depth + 1])
                    {
                        AddEndpointToTree(endpoints, current_sub_branch.branches[i], endpoint, tree_depth + 1);
                        return;
                    }
                }
            }

            string[] organizational_unit = new string[tree_organizational_units + 1];

            int organizational_units = organizational_unit.Length;

            for (int i = 0; i < tree_organizational_units; i++)
            {
                organizational_unit[i] = current_sub_branch.organizational_units[i];
            }

            if (tree_depth + 1 < endpoint_organizational_units - 1)
            {
                organizational_unit[organizational_units - 1] = endpoint.organizational_units[tree_depth + 1];

                Branch new_sub_branch = CreateBranch(current_sub_branch, organizational_unit, organizational_unit[organizational_units - 1], "null", "null", "null", "null", new string[] { organizational_unit[organizational_units - 1] }, false);

                new_sub_branch.index = current_sub_branch.branches.Count;

                current_sub_branch.branches.Add(new_sub_branch);

                AddEndpointToTree(endpoints, new_sub_branch, endpoint, tree_depth + 1);
            }
            else
            {
                endpoint.parent = current_sub_branch;

                endpoint.index = current_sub_branch.branches.Count;
                current_sub_branch.branches.Add(endpoint);
                endpoints.Add(endpoint);
                return;
            }
        }
        public static void AllocateColorMark(string content, int line_number, List<ColorMark> color_marks_recycle, List<ColorMark> color_marks_alloc)
        {
            if (color_marks_recycle.Count > 0)
            {
                ColorMark color_mark = color_marks_recycle[0];
                color_mark.content = content;
                color_mark.line_number = line_number;
                color_mark.color = ConsoleColor.Yellow;
                color_marks_alloc.Add(color_mark);
                color_marks_recycle.RemoveAt(0);

            }
            else
            {
                ColorMark color_mark = new ColorMark()
                {
                    content = content,
                    line_number = line_number,
                    color = ConsoleColor.Yellow
                };
                color_marks_alloc.Add(color_mark);
            }
        }
        public static void ApplyDescriptions(List<Branch> endpoints, List<Description> descriptions)
        {
            for (int i = 0; i < descriptions.Count; i++)
            {
                for (int j = 0; j < endpoints.Count; j++)
                {
                    if (endpoints[j].display_name.ToLower() == descriptions[i].endpoint.ToLower())
                    {
                        endpoints[j].description = descriptions[i].description;
                        break;
                    }
                }
            }
        }
        #endregion

        #region endpoint
        public static void RunEndpointRemoteDesktop(string path, string ip_address)
        {
            Process remote_desktop_process = new Process();
            string executable = Environment.ExpandEnvironmentVariables(path);

            if (executable != null)
            {

                bool use_hostname = true;
                string host_name = "localhost";
                try
                {
                    System.Net.IPHostEntry entry = System.Net.Dns.GetHostEntry(ip_address);
                    host_name = entry.HostName;
                    host_name = host_name.Replace(".bgnet.local", "");
                }
                catch
                {
                    use_hostname = false;
                }

                remote_desktop_process.StartInfo.FileName = executable;
                remote_desktop_process.StartInfo.Arguments = "/v " + ((use_hostname) ? host_name : ip_address);
                remote_desktop_process.Start();
                remote_desktop_process.Dispose();
            }
        }
        public static void RunEndpointTeamViewer(Branch target, string path)
        {
            string pwd = DecryptString(target.password);

            if (target.ip_address != "null")
            {
                Process team_viewer_process = Process.Start(path, ("-i " + target.ip_address + " -P " + pwd));
                team_viewer_process.Dispose();
            }
            else if (target.tvid != "null")
            {
                Process team_viewer_process = Process.Start(path, ("-i " + target.tvid + " -P " + pwd));
                team_viewer_process.Dispose();
            }
        }
        #endregion

        #region navigation

        public static void Expand(Branch tree)
        {
            tree.expanded = true;
            if (tree.parent != null)
            {
                tree.parent.current_child_index = tree.index;
                Expand(tree.parent);
            }
            else
            {
                return;
            }
        }
        public static void Collapse(Branch tree)
        {
            tree.expanded = false;
            if (tree.parent != null)
            {
                Collapse(tree.parent);
            }
            else
            {
                return;
            }
        }
        public static void CollapseStructure(Branch root, ref Branch selected_branch, List<ColorMark> color_marks_recycle, List<ColorMark> color_marks_alloc, ConsoleColor color_base, ConsoleColor color_highlight)
        {
            Collapse(selected_branch);
            selected_branch = root;
            Expand(root);


            DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight);
        }
        public static void MoveUpCurrentBranch(Branch root, ref Branch selected_branch, List<ColorMark> color_marks_recycle, List<ColorMark> color_marks_alloc, ConsoleColor color_base, ConsoleColor color_highlight)
        {
            selected_branch.current_child_index--;

            if (selected_branch.current_child_index < 0)
            {
                selected_branch.current_child_index = selected_branch.branches.Count - 1;
            }

            DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight);
        }
        public static void MoveDownCurrentBranch(Branch root, ref Branch selected_branch, List<ColorMark> color_marks_recycle, List<ColorMark> color_marks_alloc, ConsoleColor color_base, ConsoleColor color_highlight)
        {
            selected_branch.current_child_index++;

            if (selected_branch.current_child_index >= selected_branch.branches.Count)
            {
                selected_branch.current_child_index = 0;
            }


            DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight);
        }
        public static void MoveDownStructure(Branch root, ref Branch selected_branch, List<ColorMark> color_marks_recycle, List<ColorMark> color_marks_alloc, ConsoleColor color_base, ConsoleColor color_highlight)
        {
            if (selected_branch.branches.Count > 0)
            {
                if (selected_branch.branches[selected_branch.current_child_index].branches.Count > 0)
                {
                    selected_branch = selected_branch.branches[selected_branch.current_child_index];
                    selected_branch.expanded = true;

                    DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight, true);
                }
            }
        }
        public static void MoveUpStructure(Branch root, ref Branch selected_branch, List<ColorMark> color_marks_recycle, List<ColorMark> color_marks_alloc, ConsoleColor color_base, ConsoleColor color_highlight)
        {
            if (selected_branch.parent != null)
            {
                selected_branch.expanded = false;
                selected_branch = selected_branch.parent;

                DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight, true);
            }
            else
            {
                if (!selected_branch.expanded)
                {
                    selected_branch.expanded = true;

                    DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight, true);
                }
            }
        }

        #endregion

        #region print
        public static void DrawTree(Branch root, List<ColorMark> color_marks_recycle, List<ColorMark> color_marks_alloc, ConsoleColor color_base, ConsoleColor color_highlight, bool depth_change = false)
        {
            for (int i = color_marks_alloc.Count - 1; i >= 0; i--)
            {
                ColorMark color_mark = color_marks_alloc[i];
                color_marks_recycle.Add(color_mark);
                color_marks_alloc.RemoveAt(i);
            }

            Console.ForegroundColor = color_base;
            Console.SetCursorPosition(0, 0);
            int line_number = 0;
            DisplayTree(root, "", 0, ref line_number, color_marks_recycle, color_marks_alloc);

            if (depth_change)
            {
                int fill = 80 - line_number;
                if (fill > 0)
                {
                    for (int i = 0; i < fill; i++)
                    {
                        FastConsole.WriteLine("                                                                                   ");
                    }
                }
            }
            FastConsole.Flush();
            Console.SetCursorPosition(0, 0);

            Console.ForegroundColor = color_highlight;

            for (int i = 0; i < color_marks_alloc.Count; i++)
            {
                Console.CursorTop = color_marks_alloc[i].line_number;
                Console.WriteLine(color_marks_alloc[i].content);
            }

            Console.ForegroundColor = color_base;

            int count = color_marks_alloc.Count;
            if (count > 0)
            {
                int y = color_marks_alloc[count - 1].line_number;
                Console.SetCursorPosition(0, y);
            }
            else
            {
                Console.SetCursorPosition(0, 0);
            }






        }
        public static void DisplayTree(Branch branch, string indent, int depth, ref int line_number, List<ColorMark> color_marks_recycle, List<ColorMark> color_marks_alloc)
        {
            bool mark = false;
            int max = 64;
            if (branch.parent != null)
            {
                if (branch.parent.branches[branch.parent.current_child_index] == branch)
                {
                    mark = true;
                }

                string ep = "";

                if (branch.ip_address != "null")
                {
                    ep = " [" + branch.ip_address + "] ";
                }
                else if (branch.tvid != "null")
                {
                    ep = " [" + branch.tvid + "] ";
                }

                if (branch.parent.branches.Count > 1)
                {

                    string description = (branch.description == "") ? "" : "[" + branch.description + "]";


                    if (branch != branch.parent.branches[branch.parent.branches.Count - 1])
                    {

                        string content = (indent + "├─" + branch.display_name + ep + description);

                        if (content.Length > 80) { content = content.Substring(0, 80); }
                        int delta = (max - content.Length);
                        for (int i = 0; i < delta; i++)
                        {
                            content += " ";
                        }
                        FastConsole.WriteLine(content);
                        if (mark) AllocateColorMark(content, line_number, color_marks_recycle, color_marks_alloc);
                        line_number += 1;
                        indent += "│     ";
                    }
                    else
                    {
                        string content = (indent + "└─" + branch.display_name + ep + description);
                        if (content.Length > 80) { content = content.Substring(0, 80); }
                        int delta = (max - content.Length);
                        for (int i = 0; i < delta; i++)
                        {
                            content += " ";
                        }
                        FastConsole.WriteLine(content);
                        if (mark) AllocateColorMark(content, line_number, color_marks_recycle, color_marks_alloc);
                        line_number += 1;
                        indent += "      ";
                    }

                }
                else
                {

                    string description = (branch.description == "") ? "" : "[" + branch.description + "]";
                    string content = (indent + "└─" + branch.display_name + ep + description);
                    if (content.Length > 80) { content = content.Substring(0, 80); }
                    int delta = (max - content.Length);
                    for (int i = 0; i < delta; i++)
                    {
                        content += " ";
                    }
                    FastConsole.WriteLine(content);
                    if (mark) AllocateColorMark(content, line_number, color_marks_recycle, color_marks_alloc);
                    line_number += 1;
                    indent += "      ";
                }
            }
            else
            {
                string content = ("[" + branch.display_name + "]");
                if (content.Length > 80) { content = content.Substring(0, 80); }
                int delta = (max - content.Length);
                for (int i = 0; i < delta; i++)
                {
                    content += " ";
                }
                FastConsole.WriteLine(content);
                if (mark) AllocateColorMark(content, line_number, color_marks_recycle, color_marks_alloc);
                line_number += 1;
                indent += "     ";
            }
            int branch_count = branch.branches.Count;

            for (int i = 0; i < branch_count; i++)
            {
                if (branch.expanded)
                {
                    DisplayTree(branch.branches[i], indent, depth + 1, ref line_number, color_marks_recycle, color_marks_alloc);
                }
            }
        }
        public static void Help(ConsoleColor color_base, ConsoleColor color_highlight)
        {

        }
        public static void Credits(ConsoleColor color_base, ConsoleColor color_highlight)
        {
            string[] credits = new string[]
            {
                "│  _______                    __      ___                        │",
                "│ |__   __|                   \\ \\    / (_)                       │",
                "│    | | ___  __ _ _ __ ___    \\ \\  / / _  _____      _____ _ __ │",
                "│    | |/ _ \\/ _` | '_ ` _ \\    \\ \\/ / | |/ _ \\ \\ /\\ / / _ \\ '__|│",
                "│    | |  __/ (_| | | | | | |    \\  /  | |  __/\\ V  V /  __/ |   │",
                "│    |_|\\___|\\__,_|_| |_| |_|     \\/   |_|\\___| \\_/\\_/ \\___|_|   │",
                "│            __  __                                              │",
                "│           |  \\/  |                                             │",
                "│   ______  | \\  / | __ _ _ __   __ _  __ _  ___ _ __   ______   │",
                "│  |______| | |\\/| |/ _` | '_ \\ / _` |/ _` |/ _ \\ '__| |______|  │",
                "│           | |  | | (_| | | | | (_| | (_| |  __/ |              │",
                "│           |_|  |_|\\__,_|_| |_|\\__,_|\\__, |\\___|_|              │",
                "│                                      __/ |                     │",
                "│                                     |___/                      │",
                "│                                                                │",
                "│________________________By: D-Smacker___________________________│",
            };

            Console.ForegroundColor = color_highlight;
            Console.Clear();
            for (int i = 0; i < credits.Length; i++)
            {
                FastConsole.WriteLine(credits[i]);
            }
            FastConsole.Flush();
            Console.ForegroundColor = color_base;
            FastConsole.WriteLine("Press [ENTER] to return");
            FastConsole.Flush();

        Prompt:

            ConsoleKeyInfo kki = Console.ReadKey();
            if (kki.Key != ConsoleKey.Enter)
            {
                goto Prompt;
            }
            Console.Clear();
        }
        #endregion

        #region structs
        public static class FastConsole
        {
            static readonly BufferedStream str;

            static FastConsole()
            {
                Console.OutputEncoding = Encoding.Unicode;
                str = new BufferedStream(Console.OpenStandardOutput(), 0x15000);
            }

            public static void WriteLine(String s) => Write(s + "\r\n");

            public static void Write(String s)
            {
                var rgb = new byte[s.Length << 1];
                Encoding.Unicode.GetBytes(s, 0, s.Length, rgb, 0);

                lock (str)
                {
                    str.Write(rgb, 0, rgb.Length);
                }
            }

            public static void Flush() { lock (str) str.Flush(); }
        };
        public struct Config
        {
            public string src_mode;
            public string path_source_data;
            public string path_team_viewer;
            public string aliases_path;
            public ConsoleColor color_base;
            public ConsoleColor color_highlight;
            public string path_remote_desktop;
            public string descriptions_path;
        }
        public class Branch
        {
            public string[] organizational_units;
            public string directory_name;
            public string display_name;
            public string ip_address;
            public string password;

            public Branch parent;
            public List<Branch> branches;
            public string[] aliases;
            public bool expanded;
            public int current_child_index;
            public int index;
            public string endpoint_type;
            public string tvid;
            public string description;
        }
        public class Alias
        {
            public string key;
            public string[] organizational_units;
        }
        public class Description
        {
            public string endpoint;
            public string description;
        }
        public class ColorMark
        {
            public string content;
            public int line_number;
            public ConsoleColor color;
        }

        #endregion

        #region loading
        public static void ReadConfig(ref Config config)
        {
            if (File.Exists(@"config.txt"))
            {
                string[] console_colors = Enum.GetNames(typeof(ConsoleColor));

                string[] lines = File.ReadAllLines(@"config.txt", Encoding.UTF8);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[0] != "#")
                    {
                        string[] key_value_pair = lines[i].Split("=");
                        if (key_value_pair.Length == 2)
                        {
                            if (key_value_pair[0].ToLower() == "path_source_data")
                            {
                                config.path_source_data = key_value_pair[1];
                            }
                            else if (key_value_pair[0].ToLower() == "src_mode")
                            {
                                config.src_mode = key_value_pair[1];
                            }
                            else if (key_value_pair[0].ToLower() == "path_team_viewer")
                            {
                                config.path_team_viewer = key_value_pair[1];
                            }
                            else if (key_value_pair[0].ToLower() == "path_remote_desktop")
                            {
                                config.path_remote_desktop = key_value_pair[1];
                            }
                            else if (key_value_pair[0].ToLower() == "aliases_path")
                            {
                                config.aliases_path = key_value_pair[1];
                            }
                            else if (key_value_pair[0].ToLower() == "descriptions_path")
                            {
                                config.descriptions_path = key_value_pair[1];
                            }
                            else if (key_value_pair[0].ToLower() == "color_base")
                            {
                                config.color_base = ConsoleColor.Gray;
                                for (int j = 0; j < console_colors.Length; j++)
                                {
                                    if (console_colors[j].ToLower() == key_value_pair[1].ToLower())
                                    {
                                        config.color_base = (ConsoleColor)j;
                                        break;
                                    }
                                }
                            }
                            else if (key_value_pair[0].ToLower() == "color_highlight")
                            {
                                config.color_highlight = ConsoleColor.Yellow;
                                for (int j = 0; j < console_colors.Length; j++)
                                {
                                    if (console_colors[j].ToLower() == key_value_pair[1].ToLower())
                                    {
                                        config.color_highlight = (ConsoleColor)j;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                config.path_source_data = "settings.txt";
                config.src_mode = "file";
                config.path_team_viewer = "C:\\Windows\\System32\\notepad.exe";
                config.path_remote_desktop = @"%SystemRoot%\system32\mstsc.exe";
                config.aliases_path = "";
                config.color_base = ConsoleColor.Gray;
                config.color_highlight = ConsoleColor.Yellow;
            }
        }
        public static bool LoadDescriptions(Config config, List<Description> descriptions)
        {
            if (File.Exists(config.descriptions_path))
            {
                string[] lines = File.ReadAllLines(config.descriptions_path, Encoding.UTF8);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i][0] != '#')
                    {
                        string[] input_data = lines[i].Split(';');

                        if (input_data.Length == 2)
                        {

                            Description description = new Description()
                            {
                                endpoint = input_data[0],
                                description = input_data[1]
                            };
                            descriptions.Add(description);
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool LoadAliases(Config config, List<Alias> alternative_aliases)
        {
            if (File.Exists(config.aliases_path))
            {
                string[] lines = File.ReadAllLines(config.aliases_path, Encoding.UTF8);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i][0] != '#')
                    {
                        string[] input_data = lines[i].Split(';');

                        if (input_data.Length == 2)
                        {
                            string[] aliases = input_data[1].Split(',');
                            string[] organizational_units = input_data[0].Split("/");
                            for (int j = 0; j < aliases.Length; j++)
                            {
                                Alias alias = new Alias()
                                {
                                    organizational_units = organizational_units,
                                    key = aliases[j]
                                };
                                alternative_aliases.Add(alias);
                            }


                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool LoadSource(Config config, Branch root, Branch empty_branch, List<Branch> endpoints)
        {
            if (File.Exists(config.path_source_data))
            {
                string[] lines = File.ReadAllLines(config.path_source_data, Encoding.UTF8);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i][0] != '#')
                    {
                        string[] input_data = lines[i].Split(';');

                        if (input_data.Length == 7)
                        {
                            string[] ou = input_data[0].Split("/");
                            if (ou.Length > 0 && ou.Length < 10)
                            {
                                Branch branch = CreateBranch(empty_branch, input_data[0].Split("/"), input_data[1], input_data[3], input_data[2], input_data[4], input_data[5], input_data[6].Split(','), false);
                                AddEndpointToTree(endpoints, root, branch, 0);
                            }
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region security
        public static string DecryptString(string encrString)
        {
            byte[] b;
            string decrypted;
            try
            {
                b = Convert.FromBase64String(encrString);
                decrypted = System.Text.ASCIIEncoding.ASCII.GetString(b);
            }
            catch (FormatException)
            {
                decrypted = "";
            }
            return decrypted;
        }
        public static string EncryptString(string strEncrypted)
        {
            byte[] b = System.Text.ASCIIEncoding.ASCII.GetBytes(strEncrypted);
            string encrypted = Convert.ToBase64String(b);
            return encrypted;
        }
        #endregion

        #region misc
        public static void CommandPrompt(Branch root, List<Alias> alternative_aliases, ref Branch selected_branch, List<Branch> endpoints, Branch empty_branch, string[] cmds, Command[] commands, bool loaded, List<ColorMark> color_marks_recycle, List<ColorMark> color_marks_alloc, ConsoleColor color_base, ConsoleColor color_highlight)
        {
            Console.Clear();
            FastConsole.WriteLine("Enter Command: ");
            FastConsole.Flush();
            string cmd = Console.ReadLine();

            if (!ExecuteCMD(cmds, commands, cmd, color_base, color_highlight))
            {
                if (loaded)
                {
                    Branch branch;

                    if (SearchAlias(endpoints, cmd, out branch, empty_branch))
                    {
                        Collapse(selected_branch);
                        int child_index = branch.index;
                        selected_branch = branch.parent;
                        selected_branch.current_child_index = child_index;
                        Expand(selected_branch);

                        DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight);
                        return;

                    }


                    for (int i = 0; i < alternative_aliases.Count; i++)
                    {
                        if (alternative_aliases[i].key == cmd)
                        {
                            if (SearchAlternativeAliases(root, alternative_aliases[i], ref branch, 0))
                            {
                                Collapse(selected_branch);
                                selected_branch = branch;
                                Expand(selected_branch);

                                DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight);
                                return;
                            }
                        }
                    }


                    DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight);

                }
            }
            else
            {
                if (loaded)
                {

                    DrawTree(root, color_marks_recycle, color_marks_alloc, color_base, color_highlight);
                }
            }
        }
        public static bool SearchAlternativeAliases(Branch tree, Alias alias, ref Branch branch, int depth)
        {
            string tree_name = tree.organizational_units[tree.organizational_units.Length - 1];

            for (int i = 0; i < tree.branches.Count; i++)
            {
                Branch sub_branch = tree.branches[i];

                string sub_branch_name = sub_branch.organizational_units[sub_branch.organizational_units.Length - 1];

                if (sub_branch_name == alias.organizational_units[depth + 1])
                {
                    if (depth + 1 == alias.organizational_units.Length - 1)
                    {
                        branch = sub_branch;
                        return true;
                    }
                    else
                    {
                        return SearchAlternativeAliases(sub_branch, alias, ref branch, depth + 1);
                    }
                }
            }

            return false;

        }
        public static bool ExecuteCMD(string[] cmds, Command[] commands, string cmd, ConsoleColor color_base, ConsoleColor color_highlight)
        {
            for (int i = 0; i < cmds.Length; i++)
            {
                if (cmd == cmds[i])
                {
                    commands[i].DynamicInvoke(color_base, color_highlight);
                    return true;
                }
            }
            return false;
        }
        public static void Exit(ConsoleColor color_base, ConsoleColor color_highlight)
        {
            System.Environment.Exit(0);
        }
        public static bool SearchAlias(List<Branch> endpoints, string cmd, out Branch endpoint, Branch empty_branch)
        {
            for (int i = 0; i < endpoints.Count; i++)
            {
                for (int j = 0; j < endpoints[i].aliases.Length; j++)
                {
                    if (endpoints[i].aliases[j] == cmd)
                    {
                        endpoint = endpoints[i];
                        return true;
                    }
                }
            }
            endpoint = null;
            return false;
        }
        #endregion
    }
}
