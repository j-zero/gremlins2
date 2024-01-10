using System.Text;
using Pastel;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Linq;

namespace gremlins
{
    internal class gremlins
    {
        static CmdParser cmd;
        static int defaultLineCount = 10;
        static char nonPrintableChar = '·';
        static char[] trimArray = new char[] { ' ', '\t', '\r' };

        static string outputFile = null;
        static Encoding encoding = Encoding.UTF8;
        static Encoding defaultEncoding = Encoding.UTF8;

        static string color1 = "70e000";
        static string color2 = "008000";
        static void Main(string[] args)
        {
            cmd = new CmdParser()
            {

                { "cut", "c", CmdCommandTypes.PARAMETER,
                    new CmdParameters() {
                        { CmdParameterTypes.INT, 0},
                        { CmdParameterTypes.INT, 0},
                    },
                    "Look from here to there."
                },

                { "head", "h", CmdCommandTypes.FLAG, $"Show first X lines, can be modified with {"--lines".Pastel(color1)}."},
                { "tail", "t", CmdCommandTypes.FLAG, $"Show first X lines, can be modified with {"--lines".Pastel(color1)}."},

                { "help", "", CmdCommandTypes.FLAG, "Show this help." },

                { "lines", "n", CmdCommandTypes.PARAMETER,
                    new CmdParameters() {
                        { CmdParameterTypes.INT, defaultLineCount},
                    },
                    $"Number of lines for head/tail. Default: {defaultLineCount}"
                },


                { "only-non-ascii", "", CmdCommandTypes.FLAG, "Everything above 0xff is gremlin" },
                { "no-empty-lines", "E", CmdCommandTypes.FLAG, "Don't parse empty lines as gremlins" },
                { "no-space-on-line-end", "S", CmdCommandTypes.FLAG, "Don't parse spaces on line end as gremlin" },
                { "crlf", "W", CmdCommandTypes.FLAG, "Don't parse CR on line end as gremlin (Windows format)" },
                { "all", "a", CmdCommandTypes.FLAG, "Show all lines" },
                { "invert", "v", CmdCommandTypes.FLAG, "Show all lines but no gremlins" },

                { "no-cr", "", CmdCommandTypes.FLAG, "Don't show carrige return" },
                { "no-space", "", CmdCommandTypes.FLAG, "Don't mark spaces" },
                { "no-tab", "", CmdCommandTypes.FLAG, "Don't mark tabulators" },
                { "no-colors", "", CmdCommandTypes.FLAG, "Don't color output" },
                { "no-line-numbers", "l", CmdCommandTypes.FLAG, "Don't show line numbers" },
                { "no-hex", "X", CmdCommandTypes.FLAG, "Don't show unprintable gremlins as hex values" },
                { "hex", "x", CmdCommandTypes.FLAG, "Show gremlins as hex values" },

                { "plain", "p", CmdCommandTypes.FLAG, $"Combines {"--no-cr".Pastel(color1)}, {"--no-space".Pastel(color1)}, {"--no-tab".Pastel(color1)},{"--no-line-numbers".Pastel(color1)}, {"--no-colors".Pastel(color1)}, {"--no-hex".Pastel(color1)}" },

                { "regex", "r", CmdCommandTypes.MULTIPE_PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Additional regex for gremlin detection" },

                { "regex-only", "R", CmdCommandTypes.FLAG, "Only use regex detection" },

                { "file", "f", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                }, "File to read" },

                { "output", "o", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                }, "Output file" },

                 { "encoding", "e", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, "utf8" }
                  },   $"Force encoding to {"ascii".Pastel(color2)}, {"utf8".Pastel(color2)} (default), {"utf16".Pastel(color2)}, {"utf7".Pastel(color2)}, {"utf32".Pastel(color2)}, {"utf16be".Pastel(color2)} or <{"int".Pastel(color2)}> as codepage" },
                 //   }, "Force encoding. <string> could be \"utf8\" (default), \"ascii\", \"utf7\", \"utf16\", \"utf16be\",\"utf32le\", \"utf32be\"" },

                { "output-append", "A", CmdCommandTypes.FLAG, "Append to output file" },

                { "trim-end", "", CmdCommandTypes.FLAG, "Trim spaces at end of line for output" },
                { "trim-start", "", CmdCommandTypes.FLAG, "Trim spaces at start of line for output" },
                { "trim", "", CmdCommandTypes.FLAG, "Trim spaces at start/end of line for output" },

                { "remove-cr", "C", CmdCommandTypes.FLAG, "Remove carrige return in output" }

            };

            cmd.DefaultParameter = "file";
            try
            {

                cmd.Parse();

                // HELP
                if (cmd.HasFlag("help"))
                {
                    ShowLongHelp();
                }



                if (cmd.HasFlag("no-colors") || cmd.HasFlag("plain") || cmd.HasFlag("output") || Console.IsOutputRedirected)
                    Pastel.ConsoleExtensions.Disable();

                string[] lines = new string[0];

                long lineCount = cmd["lines"].Longs[0];

                long offset = cmd["cut"].Longs[0];
                long length = cmd["cut"].Longs[1];

                if (cmd.HasFlag("tail"))
                {
                    offset = lineCount * -1;
                    length = lineCount;
                }
                else if (cmd.HasFlag("head"))
                {
                    offset = 0;
                    length = lineCount;
                }

                if (cmd["encoding"].WasUserSet)
                {
                    // "utf8" (default), "ascii", "utf7", "utf16", "utf16be","utf32", "utf32be"
                    //encoding = EncodingHelper.GetEncodingFromName(cmd["encoding"].Strings[0]);
                    string str_encoding = cmd["encoding"].String.Trim().ToLower();

                    switch (str_encoding)
                    {
                        case "ascii":
                            encoding = Encoding.ASCII;
                            break;
                        case "utf8":
                            encoding = Encoding.UTF8;
                            break;
                        case "utf16":
                            encoding = Encoding.Unicode;
                            break;
                        case "utf7":
                            encoding = Encoding.UTF7;
                            break;
                        case "utf32":
                            encoding = Encoding.UTF32;
                            break;
                        case "utf16be":
                            encoding = Encoding.BigEndianUnicode;
                            break;
                        default:
                            int codepage = 0;
                            if (int.TryParse(str_encoding, out codepage))
                            {
                                try
                                {
                                    encoding = Encoding.GetEncoding(codepage);
                                }
                                catch
                                {
                                    Die("Invalid codepage for " + "--encoding".Pastel("#a71e34") + ": \"" + str_encoding + "\"", 2);
                                }
                            }
                            else
                            {
                                Die("Unkown option for " + "--encoding".Pastel("#a71e34") + ": \"" + str_encoding + "\"", 2);
                            }
                            break;
                    }

                }
                Console.OutputEncoding = encoding;

                if (Console.IsInputRedirected)
                {
                    Pastel.ConsoleExtensions.Disable();
                    using (Stream s = Console.OpenStandardInput())
                    {
                        using (MemoryStream reader = new MemoryStream())
                        {
                            s.CopyTo(reader);
                            lines = encoding.GetString(reader.ToArray()).Split('\n');
                        }
                    }
                }
                else
                {
                    if (cmd["file"].StringIsNotNull)
                    {
                        string path = cmd["file"].Strings[0];

                        if (!cmd["encoding"].WasUserSet)
                        {
                            //encoding = EncodingHelper.GetEncodingFromFile(path);
                            encoding = defaultEncoding;
                            if (encoding != Encoding.ASCII && encoding != Encoding.UTF8)
                                Console.Error.WriteLine($"{"Warning:".Pastel(ColorTheme.OffsetColorHighlight)} files has {encoding.EncodingName} encoding.");
                        }

                        lines = encoding.GetString(File.ReadAllBytes(path)).Split('\n');
                        //lines = File.ReadAllText(path, encoding).Split('\n');
                    }
                    else
                    {
                        ShowHelp();
                        // Exit
                    }

                }

                if (cmd["output"].Strings.Length > 0 && cmd["output"].Strings[0] != null)
                {  // output file
                    outputFile = cmd["output"].Strings[0];
                    if (!cmd.HasFlag("output-append"))
                        System.IO.File.WriteAllText(outputFile, string.Empty);
                }

                GremlinDump(lines, (int)offset, (int)length);
            }
            catch (FileNotFoundException ex)
            {
                WriteError(ex.Message);
            }
            catch (ArgumentException ex)
            {
                WriteError(ex.Message);
                Exit(255);
            }
            catch (Exception ex)
            {
                WriteError(ex.Message);
                Console.WriteLine(ex.StackTrace);
                Exit(255);
            }

            Exit(0);
        }

        public static void GremlinDump(string[] lines, int start = 0, int length = 0)
        {
            if (lines.Length == 0) return;
            //string[] lines = Encoding.UTF8.GetString(bytes).Split('\n');
            int lineNumberLength = (int)Math.Log10(lines.Length) + 1;
            //.PadLeft(offsetLength, '0')
            int offset = start;
            bool utf8Gremlin = cmd.HasFlag("only-non-ascii");

            if (offset < 0)
            {
                if (lines.Length < (start * -1))
                    offset = 0;
                else
                    offset = lines.Length + offset;
            }

            int lineNumber = offset;

            int lastLine = offset + length;

            if (lastLine > lines.Length || lastLine == 0)
                lastLine = lines.Length;

            bool isCRLFFormat = (lines.Length > 2 && lines[0].EndsWith("\r") && lines[1].EndsWith("\r"));

            for (int l = offset; l < lastLine; l++)
            {
                string newLine = "";
                string line = lines[l];

                bool lineEndsWithSpace = line.EndsWith("\t\r") || line.EndsWith(" \r") || line.EndsWith(" ") || line.EndsWith("\t");
                bool isGremlin = false;

                if (!cmd.HasFlag("regex-only"))
                    isGremlin = (!cmd.HasFlag("no-space-on-line-end") && lineEndsWithSpace) || !cmd.HasFlag("no-empty-lines") && (string.IsNullOrEmpty(line) || Regex.IsMatch(line, @"^\s*$"));

                bool isCustomGremlin = false;

                //if ((l == lastLine - 1) && (lines[l] == string.Empty))
                //    continue;

                lineNumber++;

                //string l = line.Replace("\r", "\\r".Pastel("80ff80"));


                bool hadCR = line.EndsWith("\r");

                if (cmd.HasFlag("trim-end"))
                    line = line.TrimEnd(trimArray);
                if (cmd.HasFlag("trim-start"))
                    line = line.TrimStart(trimArray);
                if (cmd.HasFlag("trim"))
                    line = line.Trim(trimArray);

                bool hasCR = line.EndsWith("\r");

                if (hadCR && !hasCR) // add CR if it was removed
                    line += "\r";

                if (cmd.HasFlag("remove-cr"))
                    line = line.TrimEnd('\r');

                foreach (char c in line)
                {
                    int i = (int)c;
                    string color = ColorTheme.GetColor(i, true);

                    //color = "9CDCFE";
                    if (!cmd.HasFlag("regex-only"))
                    {
                        if (!isGremlin)
                        {
                            //                                                               LF           CR           TAB
                            //isGremlin = (i < 32 || i > (utf8Gremlin ? 0xff : 0x7f)) && (i != 0x0a && i != 0x0d && i != 0x09);
                            isGremlin = (i < 32 || i > (utf8Gremlin ? 0xff : 0x7f)) && (i != 0x0a && (i != 0x0d || !cmd.HasFlag("crlf")) && i != 0x09);
                            /*
                            if (utf8Gremlin)
                                //                                  LF           CR           TAB
                                isGremlin = (i < 32 || i > 0xff) && (i != 0x0a && i != 0x0d && i != 0x09);
                            else
                                isGremlin = (i < 32 || i > 0x7f) && (i != 0x0a && i != 0x0d && i != 0x09);
                            */
                        }
                    }

                    if (i == 0x0d && !cmd.HasFlag("hex"))    // CR
                        newLine += ((cmd.HasFlag("no-cr") || cmd.HasFlag("plain") ? "\r" : "\\r").Pastel(ColorTheme.DarkColor));
                    else if (i == 0x09)    // Tab
                        newLine += ((cmd.HasFlag("no-tab") || cmd.HasFlag("plain") ? "\t" : $"\\t{nonPrintableChar}{nonPrintableChar}").Pastel(ColorTheme.DarkColor));
                    else if (i == 0x20)    // Space
                        newLine += ((cmd.HasFlag("no-space") || cmd.HasFlag("plain") ? " " : "_").Pastel(ColorTheme.DarkColor));
                    else if (isGremlin || !IsPrintable(c))
                    {    // Unprintable (control chars, UTF-8, etc.)
                        if (cmd.HasFlag("hex"))
                            newLine += ("\\x" + i.ToString("X").ToLower()).Pastel(ColorTheme.HighLight2);
                        else
                        {
                            if (cmd.HasFlag("plain"))
                                newLine += c.ToString().Pastel(ColorTheme.HighLight2);
                            else if (!IsPrintable(c))
                            {
                                newLine += cmd.HasFlag("no-hex") ? c.ToString() : ("\\x" + i.ToString("X").ToLower()).Pastel(ColorTheme.HighLight2);
                            }
                            else
                            {
                                newLine += c.ToString().Pastel(ColorTheme.HighLight2);
                            }
                        }
                        //newLine += (cmd.HasFlag("plain") || (!ConsoleHelper.IsPrintable(c) || cmd.HasFlag("no-hex")) ? c.ToString() : ("\\x" + i.ToString("X").ToLower())).Pastel(ColorTheme.HighLight2);
                    }
                    else
                        newLine += ($"{c}".Pastel(color));

                }

                if (cmd.Exists("regex"))
                {
                    foreach (string pattern in cmd["regex"].Strings)
                        isCustomGremlin |= Regex.IsMatch(lines[l], pattern);
                    if (!isGremlin)
                        isGremlin = isCustomGremlin;

                }

                if ((!cmd.HasFlag("invert") && isGremlin) ^ (cmd.HasFlag("invert") && !isGremlin) || cmd.HasFlag("all"))
                {
                    string offsetColor = lineNumber % 2 == 0 ? ColorTheme.OffsetColor : ColorTheme.OffsetColor2;
                    string offsetHighlightColor = lineNumber % 2 == 0 ? ColorTheme.OffsetColorHighlight : ColorTheme.OffsetColorHighlight2;

                    string lineColor = isGremlin ? offsetHighlightColor : offsetColor;

                    if (isCustomGremlin)
                        lineColor = ColorTheme.HighLight2;

                    string strLineNumber = lineNumber.ToString().PadLeft(lineNumberLength, '0').Pastel(lineColor);
                    Write(
                        (cmd.HasFlag("no-line-numbers") || cmd.HasFlag("plain") ? "" : $"{strLineNumber}: ") + $"{newLine}\n"
                       );
                }
            }
        }

        public static bool IsPrintable(char c)
        {
            // The set of Unicode character categories containing non-rendering,
            // unknown, or incomplete characters.
            // !! Unicode.Format and Unicode.PrivateUse can NOT be included in
            // !! this set, because they may (private-use) or do (format)
            // !! contain at least *some* rendering characters.
            var nonRenderingCategories = new UnicodeCategory[] {
            UnicodeCategory.Control,
            UnicodeCategory.OtherNotAssigned,
            UnicodeCategory.Surrogate, UnicodeCategory.OtherSymbol, UnicodeCategory.PrivateUse,
        };

            // Char.IsWhiteSpace() includes the ASCII whitespace characters that
            // are categorized as control characters. Any other character is
            // printable, unless it falls into the non-rendering categories.
            var isPrintable = (c == 0x0a && c == 0x09) || !nonRenderingCategories.Contains(Char.GetUnicodeCategory(c));


            return isPrintable;
        }

        static void Write(string output)
        {
            if (outputFile != null)
            {
                File.AppendAllText(outputFile, output);
            }
            else
            {
                Console.Write(output);
            }
        }
        public static void Die(string msg, int errorcode)
        {
            WriteError(msg);
            Environment.Exit(errorcode);
        }

        public static void WriteError(string message)
        {
            Console.Error.Write($"{message.Pastel(ColorTheme.Error1)}\n");
        }

        static void ShowHelp(bool more = true)
        {
            ShowVersion();
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName.Replace(".exe", "").Pastel(color1)} [{"Options".Pastel(color2)}] \"{"file".Pastel(color2)}\"");
            if (more)
                Console.WriteLine($"For more options, use {"--help".Pastel(color1)}");
        }
        static void ShowLongHelp()
        {
            ShowHelp(false);
            //Console.WriteLine($"gremlins, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"\n{"Options".Pastel(color2)}:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel(color1) + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel(color1)}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel(color2)).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Exit(0);
        }

        static void ShowVersion()
        {
            Console.WriteLine(@"  ▄▀  █▄▄▄▄ ▄███▄   █▀▄▀█ █    ▄█    ▄      ▄▄▄▄▄   ".Pastel("#ccff33"));
            Console.WriteLine(@"▄▀    █  ▄▀ █▀   ▀  █ █ █ █    ██     █    █     ▀▄ ".Pastel("#9ef01a"));
            Console.WriteLine(@"█ ▀▄  █▀▀▌  ██▄▄    █ ▄ █ █    ██ ██   █ ▄  ▀▀▀▀▄   ".Pastel("#70e000"));
            Console.WriteLine(@"█   █ █  █  █▄   ▄▀ █   █ ███▄ ▐█ █ █  █  ▀▄▄▄▄▀    ".Pastel("#38b000"));
            Console.WriteLine(@" ███    █   ▀███▀      █      ▀ ▐ █  █ █            ".Pastel("#008000"));
            Console.WriteLine(@"       ▀              ▀           █   ██ ".Pastel("#007200") + ("v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()).Pastel("#006400"));
            //Console.WriteLine($"{"gremlins".Pastel(color1)} is part of " + ConsoleHelper.GetVersionString(color2, color2));
        }

        static void Exit(int exitCode)
        {
            //string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
            //Console.WriteLine(parrentProcess);

            if (System.Diagnostics.Debugger.IsAttached) // is debugger attached or started by double-click/file-drag
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }

            Environment.Exit(exitCode);
        }
    }
}
