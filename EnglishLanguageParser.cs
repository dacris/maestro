using Dacris.Maestro.Core;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Dacris.Maestro;

public class EnglishLanguageParser
{
    private Dictionary<string, string> _aliases = new();
    private Stack<Block> _blocks = new Stack<Block>();
    private static Dictionary<string, Assembly> _assemblies = new();

    private string ToPascalCase(string value) =>
        value switch
        {
            null => throw new ArgumentNullException(nameof(value)),
            "" => throw new ArgumentException($"{nameof(value)} cannot be empty", nameof(value)),
            _ => string.Concat(value[0].ToString().ToUpper(), value.AsSpan(1))
        };

    private void Parse(Block block, string line, ref int lineNumber, bool ignoreBlockLevel)
    {
        //Split tokens for this line
        var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens is null || tokens.Length == 0)
        {
            lineNumber++;
            return;
        }

        // Count the tabs at the start of the line
        var tabCount = line.TrimEnd().Count(c => c.Equals('\t'));
        while (!ignoreBlockLevel && tabCount < _blocks.Count - 1)
        {
            _blocks.Pop();
            block = _blocks.Peek();
        }
        if (tabCount > _blocks.Count - 1)
        {
            //Remove current level of tabs from this line
            var newBlock = new Block();
            _blocks.Push(newBlock);
            block.Statements.Add(newBlock);
            Parse(_blocks.Peek(), line, ref lineNumber, true);
            return;
        }

        var lineWithoutTabs = line.Substring(tabCount, line.Length - tabCount);
        tokens = lineWithoutTabs.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        switch (tokens[0].ToLowerInvariant())
        {
            case "repeat":
                block.Repeat = true;
                break;

            case "in":
                if (tokens[1].ToLowerInvariant().Equals("parallel"))
                {
                    block.Parallel = true;
                }
                else
                {
                    throw new Exception("Expecting a keyword to follow 'in'");
                }
                break;

            case "alias":
                {
                    var longName = tokens[1];
                    var shortName = tokens[3].ToLowerInvariant();
                    _aliases.Add(shortName, longName);
                    break;
                }

            default:
                {
                    var i = 0;
                    var classNameString = string.Empty;
                    var namespaceNameString = string.Empty;
                    var isConditionCheck = false;
                    var id = "i" + lineNumber;
                    if (tokens[i].ToLowerInvariant().Equals("check"))
                    {
                        isConditionCheck = true;
                        id = tokens[i + 1];
                        i += 3;
                    }
                    while (i < tokens.Length && !tokens[i].ToLowerInvariant().Equals("using"))
                    {
                        var lowerToken = tokens[i].ToLowerInvariant();
                        _aliases.TryGetValue(lowerToken, out var translatedToken);
                        translatedToken ??= lowerToken;
                        translatedToken = ToPascalCase(translatedToken);
                        classNameString += translatedToken;
                        i++;
                    }
                    i++;
                    var start = i;
                    // if namespaces are more than 3 levels deep, consider using aliases
                    while (i < Math.Min(start + 3, tokens.Length) && !tokens[i].ToLowerInvariant().Equals("with"))
                    {
                        var lowerToken = tokens[i].ToLowerInvariant();
                        _aliases.TryGetValue(lowerToken, out var translatedToken);
                        translatedToken ??= tokens[i];
                        namespaceNameString += translatedToken + ".";
                        i++;
                    }
                    var key = (string?)null;
                    if (i < tokens.Length && tokens[i].ToLowerInvariant().Equals("with"))
                    {
                        i++;
                        _aliases.TryGetValue(tokens[i], out var translatedToken);
                        translatedToken ??= tokens[i];
                        key = translatedToken;
                    }
                    var interaction = (object?)null;
                    if (!namespaceNameString.StartsWith("Dacris.Maestro"))
                    {
                        var assembly = namespaceNameString + "dll";
                        var assemblyObj = (Assembly?)null;
                        if (!_assemblies.ContainsKey(assembly))
                        {
                            assemblyObj = Assembly.LoadFile(Path.Combine(
                                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                                    assembly));
                            _assemblies[assembly] = assemblyObj;
                        }
                        assemblyObj = _assemblies[assembly];
                        interaction = assemblyObj.CreateInstance(namespaceNameString + classNameString, true);
                        if (interaction is null)
                        {
                            throw new Exception("Type not found: " + namespaceNameString + classNameString);
                        }
                    }
                    else
                    {
                        interaction = Activator.CreateInstance(Type.GetType(namespaceNameString + classNameString, true, true)!)!;
                    }
                    interaction.GetType().GetProperty("InputStateKey")!.SetValue(interaction, key);
                    interaction.GetType().GetProperty("Id")!.SetValue(interaction, id);
                    if (isConditionCheck)
                    {
                        block.ConditionCheck = (Interaction)interaction;
                    }
                    else
                    {
                        block.Statements.Add((Interaction)interaction);
                    }
                    break;
                }
        }

        lineNumber++;
    }

    public Block ReadBlockFromString(string input)
    {
        Interaction._id = 1;
        var block = new Block() { IsRoot = true };
        _blocks.Push(block);
        //Split the lines first
        var lines = input.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var lineNumber = 1;
        foreach (var line in lines)
        {
            Parse(_blocks.Peek(), line, ref lineNumber, false);
        }
        return block;
    }

    public string WriteBlockToString(Block block)
    {
        StringBuilder sb = new StringBuilder();
        if (_blocks.Count == 0)
        {
            //Write aliases
            sb.AppendLine("alias Dacris.Maestro as the");
            //Find side-by-side blocks and insert no-ops
            InsertNoOps(block);
            ValidateConditionChecks(block);
        }
        _blocks.Push(block);
        sb.AppendLine();
        //Repeat
        if (block.Repeat)
        {
            AppendTabs(sb);
            sb.Append("repeat");
            sb.AppendLine();
        }
        //In Parallel
        if (block.Parallel)
        {
            AppendTabs(sb);
            sb.Append("in parallel");
            sb.AppendLine();
        }
        //Condition Check
        if (block.ConditionCheck is not null)
        {
            AppendTabs(sb);
            sb.Append($"check {block.ConditionCheck.Id} from ");
            sb.Append(WriteInteraction(block.ConditionCheck));
            sb.AppendLine();
        }
        foreach (var interaction in block.Statements)
        {
            if (interaction is not Block)
            {
                AppendTabs(sb);
            }
            sb.Append(WriteInteraction(interaction));
            sb.AppendLine();
        }
        _blocks.Pop();
        return sb.ToString();
    }

    private void InsertNoOps(Block block)
    {
        var prevStatement = (Interaction?)null;
        for (var i = 0; i < block.Statements.Count; i++)
        {
            var statement = block.Statements[i];
            if (prevStatement is not null && prevStatement is Block && statement is Block)
            {
                block.Statements.Insert(i, new NoOp());
                prevStatement = block.Statements[i + 1];
                i++;
                continue;
            }
            if (statement is Block)
            {
                InsertNoOps((Block)statement);
            }
            prevStatement = statement;
        }
    }

    private void ValidateConditionChecks(Block block)
    {
        if (block.ConditionCheck is not null && block.ConditionCheck is Block)
        {
            throw new Exception($"Block {block.Id} has a condition that is a Block. This is not allowed in plain English app spec.");
        }
        for (var i = 0; i < block.Statements.Count; i++)
        {
            var statement = block.Statements[i];
            if (statement is not null && statement is Block)
            {
                ValidateConditionChecks((Block)statement);
            }
        }
    }

    private void AppendTabs(StringBuilder sb)
    {
        for (var tabLevel = 1; tabLevel < _blocks.Count; tabLevel++)
        {
            sb.Append("\t");
        }
    }

    private string WriteInteraction(Interaction interaction)
    {
        if (interaction is Block)
        {
            return WriteBlockToString((Block)interaction);
        }
        var ns = interaction.GetType().Namespace!;
        ns = ns.Replace("Dacris.Maestro.", "the.");
        var className = interaction.GetType().Name;
        Regex splitPascalCase = new Regex(@"(?<=[A-Z])(?=[A-Z][a-z])|(?<=[^A-Z])(?=[A-Z])|(?<=[A-Za-z])(?=[^A-Za-z])");
        var classWords = splitPascalCase.Replace(className, " ").ToLowerInvariant();
        var splitNamespace = ns.Split('.');
        if (splitNamespace.Length > 3)
        {
            System.Console.WriteLine($"The namespace {interaction.GetType().Namespace} has too many levels (more than 3) for English language spec. Please shorten it.");
            throw new Exception("Namespace too long");
        }
        var namespaceWords = string.Join(' ', splitNamespace);
        var keyString = string.Empty;
        if (interaction.InputStateKey is not null)
        {
            keyString = " with " + interaction.InputStateKey;
        }
        return classWords + " using " + namespaceWords + keyString;
    }
}
