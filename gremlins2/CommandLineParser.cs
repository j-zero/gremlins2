using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

public enum CmdParameterTypes
{
    STRING,
    INT,
    BOOL,
    DECIMAL
}

public enum CmdCommandTypes
{
    VERB,
    PARAMETER,
    FLAG,
    MULTIPE_PARAMETER,
    UNNAMED
}

public class CmdParameter
{

    public CmdParameterTypes Type { get; set; }
    public object Value { get; set; }
    public long IntValue { get; set; }
    public bool BoolValue { get; set; }
    public decimal DecimalValue { get; set; }
    public string String { get { return Value != null ? Value.ToString() : null; } }

    public CmdParameter(CmdParameterTypes Type, object Value)
    {
        this.Type = Type;
        this.Value = Value;
        try { IntValue = Convert.ToInt64(Value); } catch { }
        try { this.BoolValue = Convert.ToBoolean(Value); } catch { }
        try { this.DecimalValue = Convert.ToDecimal(Value); } catch { }
    }
}

public class CmdParameters : List<CmdParameter>
{
    public new CmdParameters Add(CmdParameter item)
    {
        base.Add(item);
        return this;
    }
    public CmdParameters Add(CmdParameterTypes type, object value)
    {
        base.Add(new CmdParameter(type, value));
        return this;
    }

}

public class CmdOption
{


    public string Name { get; set; }
    public string ShortName { get; set; }
    public CmdCommandTypes CmdType { get; set; }
    public CmdParameters Parameters { get; set; }
    public CmdParameters Values { get; set; }
    public string Description { get; set; }
    public int Count { get; set; }
    public bool WasUserSet { get; set; }
    public bool IsDefaultVerb { get; set; }

    public CmdOption(string Name)
    {
        this.Name = Name;
    }
    public CmdOption(string Name, string Shortname, CmdCommandTypes CmdType, CmdParameters CmdParams, string Description, string aliasFor = null)
    {
        this.Name = Name;
        this.ShortName = Shortname;
        this.CmdType = CmdType;
        this.Parameters = CmdParams;
        this.Values = new CmdParameters();
        this.Description = Description;

        InitDefaultValues();
    }

    public void InitDefaultValues()
    {
        //this.Values.Add(new CmdParameters());
        if(CmdType == CmdCommandTypes.VERB)
        {
            ;
        }
        this.Values.AddRange(this.Parameters);
    }
    public long Int
    {
        get { return Ints.Single(); }
    }
    public long Long
    {
        get { return Longs.Single(); }
    }
    public string String
    {
        get { return Strings.Single(); }
    }
    public bool Bool
    {
        get { return Bools.First(); }
    }
    public decimal Decimal
    {
        get { return Decimals.Single(); }
    }
    public int[] Ints
    {
        get { return GetInts(); }
    }
    public long[] Longs
    {
        get { return GetLongs(); }
    }
    public string[] Strings
    {
        get { return GetStrings(); }
    }

    public bool StringIsNotNull
    {
        get { string[] strs = GetStrings(); return (strs.Length > 0 && strs[0] != null); }
    }

    public bool[] Bools
    {
        get { return GetBools(); }
    }

    public decimal[] Decimals
    {
        get { return GetDecimals(); }
    }

    public long[] GetLongs()
    {
        return this.Values.Select(x => x.IntValue).ToArray(); 
    }
    public int[] GetInts()
    {
        return this.Values.Select(x => (int)x.IntValue).ToArray();
    }

    public bool[] GetBools()
    {
        
        try
        {
            return this.Values.Select(x => x.BoolValue).ToArray();
        }
        catch
        {
            return new bool[] { false };
        }
    }

    public string[] GetStrings()
    {
        string[] result = this.Values.Where(x => x.String != null).Select(x => x.String).ToArray();

        if(result.Length == 0)
            return new string[] { null };

        return result;
        //return this.Values.Select(x => x.String).ToArray();
    }

    public decimal[] GetDecimals()
    {
        return this.Values.Select(x => x.DecimalValue).ToArray();
    }
    public bool IsVerb
    {
        get { return this.CmdType == CmdCommandTypes.VERB; }
    }
}

public class CmdParser : KeyedCollection<string, CmdOption>
{
    private string _longParamPrefix = "--";
    private string _shortParamPrefix = "-";
    //private string _defaultVerb = "";

    private Queue<string> fifo = new Queue<string>();

    public string DefaultParameter { get; set; }
    public string DefaultVerb { get; set; }

    public bool IsVerb
    {
        get; private set;
    }

    public bool HasFlag(string flag)
    {
        return this[flag].Bool;
    }
    public bool HasVerb(string verb)
    {
        return this.Verbs.Contains(verb);
    }

    public bool Empty
    {
        get
        {
            return !this.Any(c => c.WasUserSet);
        }
    }


    public bool IsParameterNullOrEmpty(string parameter)
    {
        if (!this.Contains(parameter))
            return true;
        if (this[parameter].Strings.Length < 1)
            return true;
        if (string.IsNullOrEmpty(this[parameter].Strings[0])) 
            return true;
        return false;
    }
    public bool Exists(string parameter)
    {
        return !IsParameterNullOrEmpty(parameter);
    }

    public string[] Verbs
    {
        get
        {
            string[] verbs = this.Where(c => (c.CmdType == CmdCommandTypes.VERB && c.WasUserSet)).Select(x => x.Name).ToArray();
            return verbs.Length > 0 ? verbs : DefaultVerb != null ? new string[] { DefaultVerb } : new string[0];
        }
    }
    public IEnumerable<CmdOption> SelectOptions
    {
        get
        {
            return this.Where(c => c.CmdType != CmdCommandTypes.VERB);
        }
    }
    public IEnumerable<CmdOption> SelectVerbs
    {
        get
        {
            return this.Where(c => c.CmdType == CmdCommandTypes.VERB);
        }
    }

    public string FirstVerb
    {
        get
        {
            if (this.Verbs.Length > 0)
                return this.Verbs[0];
            return null;
        }
    }

    public CmdParser()
    {
        var Args = SplitCommandLineIntoArguments(Environment.CommandLine, false).Skip(1); // skip assembly

        foreach (var arg in Args)
            fifo.Enqueue(arg);
        
    }

    public CmdParser(string[] Args)
    {
        foreach (var arg in Args)
            fifo.Enqueue(arg);

    }

    private bool TryGetValue(string key, out CmdOption item)
    {

        if (this.Contains(key))
        {
            item = this[key];
            return true;
        }
        else
        {
            item = null;
            return false;
        }
    }

    string RemoveQuotesIfExists(string Input) {

        string result = Input;
        if (Input[0] == '"' && Input[Input.Length - 1] == '"')
        {
            result = result.Remove(0, 1);
            result = result.Remove(result.Length - 1, 1);
        }
        return result;
    }

    public void Parse()
    {
        int counter = 0;

        while (fifo.Count > 0)
        {
            counter++;
            var inputArgument = fifo.Dequeue();

            if(inputArgument.StartsWith(_shortParamPrefix) && !inputArgument.StartsWith(_longParamPrefix) && (inputArgument.Length - _shortParamPrefix.Length > 1)) // multiple short arguments
            {
                string p = inputArgument.Substring(_shortParamPrefix.Length,inputArgument.Length - _shortParamPrefix.Length);
                foreach (char c in p.ToCharArray())
                    fifo.Enqueue($"{_shortParamPrefix}{c.ToString()}");

                continue;
            }

            var currentArgument = inputArgument;

            string parseKey = null;
            string longName = null;
            string shortName = null;

            if (currentArgument != null && currentArgument.StartsWith(_longParamPrefix))
            {
                longName = currentArgument.Substring(2);
                IsVerb = false;
                parseKey = this.Where(x => x.Name == longName).Select(x => x.Name).FirstOrDefault();
                if (parseKey != null)
                    currentArgument = parseKey;
            }
            else if (currentArgument != null && currentArgument.StartsWith(_shortParamPrefix))
            {
                shortName = currentArgument.Substring(1);
                IsVerb = false;
                parseKey = this.Where(x => x.ShortName == shortName).Select(x => x.Name).FirstOrDefault();
                if (parseKey != null)
                    currentArgument = parseKey;
            }
            else if(counter == 1)
            {
                parseKey = this.Where(x => x.Name == currentArgument || x.ShortName == currentArgument).Select(x => x.Name).FirstOrDefault();
                if (parseKey != null)
                {
                    currentArgument = parseKey;
                    IsVerb = true;
                }
                
            }


            
            bool gotValue = this.TryGetValue(currentArgument, out CmdOption arg);
            ;
            if (gotValue)     // known command
            {
                if(arg.IsVerb == false && IsVerb == true) // parse as verb, but is argument (e.g. "--help" vs. "help")
                    throw new ArgumentException($"unknown verb \"{inputArgument}\"");

                string name = arg.Name;
                int parameterCount = arg.Parameters.Count;
                string expectedParamsString = string.Join(", ", arg.Parameters.Select(x => x.Type.ToString()).ToArray());

                arg.WasUserSet = true;
                

                /*
                if (arg.CmdType != CmdCommandTypes.MULTIPE_PARAMETER)
                    this[currentArgument].Values.Add(r);
                */


                if (arg.CmdType == CmdCommandTypes.FLAG)
                {
                    CmdParameter cmdParam = new CmdParameter(CmdParameterTypes.BOOL, true);
                    this[currentArgument].Values[0] = cmdParam;
                }
                else
                {
                    //CmdParameters c = this[currentArgument].Parameters;
                    for(int i = 0; i < this[currentArgument].Parameters.Count; i++)
                    //foreach (var p in this[currentArgument].Parameters)
                    {
                        CmdParameter r = new CmdParameter(this[currentArgument].Parameters[i].Type, null);
                        string f = RemoveQuotesIfExists(fifo.Dequeue());
                        if (r.Type == CmdParameterTypes.BOOL)
                        {
                            string low = f.ToLower().Trim();
                            if (low == "0" || low == "false" || low == "off" || low == "disabled" || low == "disable" || low == "no")
                                r.Value = false;
                            else if (low == "1" || low == "true" || low == "on" || low == "enabled" || low == "enable" || low == "yes")
                                r.Value = true;
                            else
                                throw new Exception($"Can't parse \"{f}\" as {r.Type.ToString()}, {name} expects: {expectedParamsString}.");
                        }
                        else if (r.Type == CmdParameterTypes.INT)
                        {
                            int v = 0;
                            bool success = false;

                            if (f.StartsWith("0x"))
                                success = int.TryParse(f.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out v);
                            else
                                success = int.TryParse(f, out v);

                            if (!success)
                                throw new Exception($"Can't parse \"{f}\" as {r.Type.ToString()}, {name} expects: {expectedParamsString}.");

                            r.Value = v;
                            r.IntValue = v;
                            r.DecimalValue = v;
                            r.BoolValue = Convert.ToBoolean(v);

                        }
                        else if (r.Type == CmdParameterTypes.DECIMAL)
                        {
                            decimal v = 0;

                            if (!decimal.TryParse(f, out v))
                                throw new Exception($"Can't parse \"{f}\" as {r.Type.ToString()}, {name} expects: {expectedParamsString}.");

                            r.Value = v;
                            r.IntValue = (int)v;
                            r.DecimalValue = v;
                            r.BoolValue = Convert.ToBoolean(v);
                        }
                        else if (r.Type == CmdParameterTypes.STRING)
                        {
                            r.BoolValue = f != null;
                            r.Value = f;
                        }

                        //if (i < this[currentArgument].Values.Count)
                        //this[currentArgument].Values[i] = r;
                        if (arg.CmdType == CmdCommandTypes.MULTIPE_PARAMETER)
                        {   if(this[currentArgument].Count == 0)
                                this[currentArgument].Values[i] = r;
                            else
                            { 
                                this[currentArgument].Values.Add(r);
                            }
                                

                        }
                        else if (this[currentArgument].Values.Count == 1)
                        {
                            this[currentArgument].Values[i] = r;
                        }
                        else
                            throw new ArgumentException($"Multiple parameter fuckup @\"{currentArgument}\"!");

                        
                    }

                    this[currentArgument].Count++;
                }
            } 
            else if (inputArgument.StartsWith(_longParamPrefix) || inputArgument.StartsWith(_shortParamPrefix)){
                throw new ArgumentException($"unknown parameter \"{inputArgument}\"");
            }
            else                                                // unnamed
            {
                if (this.DefaultParameter != null && this.Dictionary.ContainsKey(DefaultParameter))
                {
                    this[this.DefaultParameter].Values.Add(CmdParameterTypes.STRING, RemoveQuotesIfExists(currentArgument));
                }
            }
 

        }
    }


    protected override string GetKeyForItem(CmdOption item)
    {
        if (item == null)
            throw new ArgumentNullException("option");
        if (item.Name != null && item.Name.Length > 0)
            return item.Name;
        throw new InvalidOperationException("Option has no names!");
    }

    public CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, string Description)
    {
        CmdParameters defParam = new CmdParameters();

        if (CmdType == CmdCommandTypes.FLAG)
            base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters() { { CmdParameterTypes.BOOL, false } }, Description));
        else
            base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters(), Description));
        
        return this;
    }

    public CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, CmdParameters CmdParams, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, CmdParams, Description));
        return this;
    }

    public CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, CmdParameterTypes Type, object DefaultValue, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters() { { Type, DefaultValue } }, Description));
        return this;
    }
    public CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, bool DefaultValue, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters() { { CmdParameterTypes.BOOL, DefaultValue } }, Description));
        return this;
    }

    /// <summary>
    /// Split a command line by the same rules as Main would get the commands except the original
    /// state of backslashes and quotes are preserved.  For example in normal Windows command line 
    /// parsing the following command lines would produce equivalent Main arguments:
    /// 
    ///     - /r:a,b
    ///     - /r:"a,b"
    /// 
    /// This method will differ as the latter will have the quotes preserved.  The only case where 
    /// quotes are removed is when the entire argument is surrounded by quotes without any inner
    /// quotes. 
    /// </summary>
    /// <remarks>
    /// Rules for command line parsing, according to MSDN:
    /// 
    /// Arguments are delimited by white space, which is either a space or a tab.
    ///  
    /// A string surrounded by double quotation marks ("string") is interpreted 
    /// as a single argument, regardless of white space contained within. 
    /// A quoted string can be embedded in an argument.
    ///  
    /// A double quotation mark preceded by a backslash (\") is interpreted as a 
    /// literal double quotation mark character (").
    ///  
    /// Backslashes are interpreted literally, unless they immediately precede a 
    /// double quotation mark.
    ///  
    /// If an even number of backslashes is followed by a double quotation mark, 
    /// one backslash is placed in the argv array for every pair of backslashes, 
    /// and the double quotation mark is interpreted as a string delimiter.
    ///  
    /// If an odd number of backslashes is followed by a double quotation mark, 
    /// one backslash is placed in the argv array for every pair of backslashes, 
    /// and the double quotation mark is "escaped" by the remaining backslash, 
    /// causing a literal double quotation mark (") to be placed in argv.
    /// </remarks>
    public static List<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments)
    {
        return SplitCommandLineIntoArguments(commandLine, removeHashComments, out _);
    }

    public static List<string> SplitCommandLineIntoArguments(string commandLine, bool removeHashComments, out char? illegalChar)
    {
        var list = new List<string>();
        SplitCommandLineIntoArguments(commandLine, removeHashComments, new StringBuilder(), list, out illegalChar);
        return list;
    }

    private static void SplitCommandLineIntoArguments(string commandLine, bool removeHashComments, StringBuilder builder, List<string> list, out char? illegalChar)
    {
        var i = 0;

        builder.Length = 0;
        illegalChar = null;
        while (i < commandLine.Length)
        {
            while (i < commandLine.Length && char.IsWhiteSpace(commandLine[i]))
            {
                i++;
            }

            if (i == commandLine.Length)
            {
                break;
            }

            if (commandLine[i] == '#' && removeHashComments)
            {
                break;
            }

            var quoteCount = 0;
            builder.Length = 0;
            while (i < commandLine.Length && (!char.IsWhiteSpace(commandLine[i]) || (quoteCount % 2 != 0)))
            {
                var current = commandLine[i];
                switch (current)
                {
                    case '\\':
                        {
                            var slashCount = 0;
                            do
                            {
                                builder.Append(commandLine[i]);
                                i++;
                                slashCount++;
                            } while (i < commandLine.Length && commandLine[i] == '\\');

                            // Slashes not followed by a quote character can be ignored for now
                            if (i >= commandLine.Length || commandLine[i] != '"')
                            {
                                break;
                            }

                            // If there is an odd number of slashes then it is escaping the quote
                            // otherwise it is just a quote.
                            if (slashCount % 2 == 0)
                            {
                                quoteCount++;
                            }

                            builder.Append('"');
                            i++;
                            break;
                        }

                    case '"':
                        builder.Append(current);
                        quoteCount++;
                        i++;
                        break;

                    default:
                        if ((current >= 0x1 && current <= 0x1f) || current == '|')
                        {
                            if (illegalChar == null)
                            {
                                illegalChar = current;
                            }
                        }
                        else
                        {
                            builder.Append(current);
                        }

                        i++;
                        break;
                }
            }

            /*
            // If the quote string is surrounded by quotes with no interior quotes then 
            // remove the quotes here. 
            if (quoteCount == 2 && builder[0] == '"' && builder[builder.Length - 1] == '"')
            {
                builder.Remove(0, length: 1);
                builder.Remove(builder.Length - 1, length: 1);
            }
            */

            if (builder.Length > 0)
            {
                list.Add(builder.ToString());
            }
        }
    }

}