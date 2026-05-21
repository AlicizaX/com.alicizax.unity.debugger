using Cysharp.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AlicizaX.Console
{
    public static class AlicizaXConsoleMacros
    {
        private static readonly Dictionary<string, string> _macroTable = new Dictionary<string, string>();

        public static IReadOnlyDictionary<string, string> GetMacros()
        {
            return _macroTable;
        }

        /// <summary>
        /// 展开给定文本里的所有宏。
        /// </summary>
        /// <returns>宏展开后的文本。</returns>
        /// <param name="text">要展开宏的文本。</param>
        /// <param name="maximumExpansions">抛出异常前最多允许展开多少次宏。</param>
        public static string ExpandMacros(string text, int maximumExpansions = 1000)
        {
            if (_macroTable.Count == 0)
            {
                return text;
            }

            KeyValuePair<string, string>[] orderedTableCache = null;

            int expansionCount = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '#')
                {
                    if (orderedTableCache == null)
                    {
                        orderedTableCache =
                            _macroTable
                                .OrderByDescending(x => x.Key.Length)
                                .ToArray();
                    }

                    foreach (KeyValuePair<string, string> macro in orderedTableCache)
                    {
                        string key = macro.Key;
                        int keyLength = key.Length;
                        if (i + keyLength < text.Length)
                        {
                            if (string.CompareOrdinal(text, i + 1, key, 0, keyLength) == 0)
                            {
                                if (expansionCount >= maximumExpansions)
                                {
                                    throw new ArgumentException(ZString.Format("Maximum macro expansions of {0} was exhausted: infinitely recursive macro is likely.", maximumExpansions));
                                }

                                string start = text.Substring(0, i);
                                string end = text.Substring(i + 1 + keyLength);
                                text = ZString.Concat(start, macro.Value, end);

                                expansionCount++;
                                i--;
                            }
                        }
                    }
                }
            }

            return text;
        }

        [Command("#define")]
        [CommandDescription("Adds a macro to the macro table which can then be used in the AlicizaX Console. If the macro 'name' is added, " +
                            "then all instances of '#name' will be expanded into the full macro expansion. This allows you to define " +
                            "shortcuts for various things such as long type names or commonly used command strings.\n\n" +
                            "Macros may not contain hashtags or whitespace in their name.\n\n" +
                            "Note: macros will not be expanded when using #define, this is so that defining nested macros is possible.")]
        public static void DefineMacro(string macroName, string macroExpansion)
        {
            macroName = macroName.Trim();
            if (macroName.Contains(' ')) { throw new ArgumentException("Macro names cannot contain whitespace."); }
            if (macroName.Contains('\n')) { throw new ArgumentException("Macro names cannot contain newlines."); }
            if (macroName.Contains('#')) { throw new ArgumentException("Macro names cannot contain hashtags."); }
            if (macroName == "define") { throw new ArgumentException("Macros cannot be named define."); }
            if (macroExpansion.Contains('\n')) { throw new ArgumentException("Macro names cannot contain newlines."); }
            if (macroExpansion.Contains(ZString.Concat('#', macroName))) { throw new ArgumentException("Macros cannot contain themselves within the expansion."); }

            if (_macroTable.ContainsKey(macroName)) { _macroTable[macroName] = macroExpansion; }
            else { _macroTable.Add(macroName, macroExpansion); }
        }

        [Command("remove-macro")]
        [CommandDescription("Removes the specified macro from the macro table")]
        public static void RemoveMacro(string macroName)
        {
            if (_macroTable.ContainsKey(macroName)) { _macroTable.Remove(macroName); }
            else { throw new Exception(ZString.Format("Specified macro #{0} as it was not defined.", macroName)); }
        }

        [Command("clear-macros")]
        [CommandDescription("Clears the macro table")]
        public static void ClearMacros() { _macroTable.Clear(); }

        [Command("all-macros", "Displays all of the macros currently stored in the macro table")]
        private static string GetAllMacros()
        {
            if (_macroTable.Count == 0) { return "Macro table is empty"; }
            else { return ZString.Concat("Macro table:\n", ZString.Join("\n", _macroTable.Select((x) => ZString.Format("#{0} = {1}", x.Key, x.Value)))); }
        }

        [Command("dump-macros", "Creates a file dump of macro table which can the be loaded to repopulate the table using load-macros")]
        [CommandPlatform(Platform.AllPlatforms ^ (Platform.WebGLPlayer))]
        public static void DumpMacrosToFile(string filePath)
        {
            using (StreamWriter dumpFile = new StreamWriter(filePath))
            {
                foreach (KeyValuePair<string, string> macro in _macroTable)
                {
                    dumpFile.WriteLine(ZString.Format("{0} {1}", macro.Key, macro.Value));
                }

                dumpFile.Flush();
                dumpFile.Close();
            }
        }

        [Command("load-macros", "Loads macros from an external file into the macro table")]
        [CommandPlatform(Platform.AllPlatforms ^ (Platform.WebGLPlayer))]
        public static string LoadMacrosFromFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new ArgumentException(ZString.Format("file at the specified path '{0}' did not exist.", filePath));
            }

            using (StreamReader macroFile = new StreamReader(filePath))
            {
                List<string> messages = new List<string>();
                while (!macroFile.EndOfStream)
                {
                    string line = macroFile.ReadLine();
                    string[] parts = line.Split(" ".ToCharArray(), 2);
                    if (parts.Length != 2)
                    {
                        messages.Add(ZString.Format("'{0}' is not a valid macro definition", line));
                    }

                    try
                    {
                        DefineMacro(parts[0], parts[1]);
                        messages.Add(ZString.Format("#{0} was successfully defined", parts[0]));
                    }
                    catch (Exception e)
                    {
                        messages.Add(ZString.Format("#{0} could not be defined: {1}", parts[0], e.Message));
                    }
                }

                macroFile.Close();
                return ZString.Join("\n", messages);
            }
        }
    }
}
