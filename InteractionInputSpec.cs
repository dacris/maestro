﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Asn1;
using System.Reflection;
using System.Text;

namespace Dacris.Maestro;

public class InteractionInputSpec
{
    public string InteractionName { get; set; }
    public List<InputSpec> Inputs { get; set; } = [];

    public InteractionInputSpec(string interactionName)
    {
        InteractionName = interactionName;
    }

    public InputSpec StateObjectKey(string key)
    {
        return Inputs[0].ValueSpec.ObjectSpecs!.Single(x => x.Name == key);
    }

    public void AddInputs(params string[] inputNames)
    {
        AddDefaultInput();
        var inputs = inputNames.Select(i => new InputSpec(i)).ToList();
        if (Inputs[0].ValueSpec.ValueType != ValueTypeSpec.Object)
        {
            Inputs[0].ValueSpec = new ValueSpec
            {
                ValueType = ValueTypeSpec.Object,
                ObjectSpecs = inputs
            };
        }
        else
        {
            Inputs[0].ValueSpec.ObjectSpecs!.AddRange(inputs);
        }
    }

    public void AddTimeout()
    {
        AddInputs("timeout");
    }

    public void AddRetry()
    {
        AddInputs();
        Inputs[0].ValueSpec.ObjectSpecs!.Add(new InputSpec("retryTimes").WithSimpleType(ValueTypeSpec.Integer));
        Inputs[0].ValueSpec.ObjectSpecs!.Add(new InputSpec("retryDelay").WithSimpleType(ValueTypeSpec.Integer));
    }

    public void AddDefaultInput()
    {
        if (Inputs.Count == 0)
        {
            Inputs.Add(new InputSpec());
        }
    }

    public static IEnumerable<AssemblyName> GetNamesOfAssembliesReferencedBy(Assembly assembly)
    {
        return assembly.GetReferencedAssemblies();
    }

    private static void GenerateInteractionMap(Assembly assembly, bool forAI = false)
    {
        List<string> lines = new List<string>();
        string fileName = forAI ? "InteractionMap.tsv" : "BuiltInInteractionMap.tsv";
        foreach (Type interaction in assembly.GetTypes().Where(t => typeof(Interaction).IsAssignableFrom(t) && !t.IsAbstract).OrderBy(t => t.FullName))
        {
            var interactionObj = (Interaction)Activator.CreateInstance(interaction)!;
            interactionObj.Specify();
            
            if (interactionObj.Description is null)
                continue;
            if (interactionObj.AiExclude && forAI)
                continue;
            if (!interactionObj.AiExclude && !forAI)
                continue;

            lines.Add(interaction.FullName + ", " + assembly.GetName().Name + "\t" + interactionObj.Description);
        }
        File.AppendAllLines(fileName, lines);
    }

    public static void GenerateDocumentation()
    {
        var generator = new DocGenerator();
        var descriptions = generator.LoadDescriptions();
        var fqns = new DirectoryInfo("Json").GetDirectories().OrderBy(x => x.Name)
                            .Select(x =>
                            new List<string>(x.GetFiles().OrderBy(x => x.Name).Select(
                                y => x.Name + "." + Path.GetFileNameWithoutExtension(y.Name))));
        Directory.CreateDirectory("HtmlDoc");
        var index = new StringBuilder();
        index.AppendLine("<html><head><title>Maestro - Documentation</title>" +
        "<link rel=\"stylesheet\" type=\"text/css\" href=\"style.css\" />" +
        "</head><body>" +
        "<h1>Maestro Interaction List</h1><ul>");
        foreach (string fqn in fqns.SelectMany(x => x))
        {
            if (!descriptions.ContainsKey(fqn))
                continue;

            var state = new JObject();
            generator.GenerateSampleState(state, fqn, "sampleKey");
            var html = "<html><head><title>Maestro - " + fqn + " Interaction</title>" +
            "<link rel=\"stylesheet\" type=\"text/css\" href=\"style.css\" /></head><body>" +
            "<h1>" + fqn + " Interaction</h1>" + descriptions[fqn] + "<hr /><h2>Sample State.json Input</h2>" +
            "<blockquote><pre>" + state + "</pre></blockquote></body></html>";
            File.WriteAllText("HtmlDoc/" + fqn + ".html", html);
            index.AppendLine("<li><a href=\"" + fqn + ".html\">" + fqn + "</a></li>");
        }
        index.AppendLine("</ul></body></html>");
        File.WriteAllText("HtmlDoc/index.html", index.ToString());
    }

    public static async Task GenerateAllMetadataAsync()
    {
        var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var assemblies = GetNamesOfAssembliesReferencedBy(Assembly.GetExecutingAssembly()).ToList();
        string[] subAssemblyFiles = Directory.GetFiles(rootPath, "Dacris.*.dll");
        var subAssemblies = subAssemblyFiles.SelectMany(
            a => GetNamesOfAssembliesReferencedBy(Assembly.LoadFrom(a)));
        assemblies.AddRange(subAssemblies);

        if (File.Exists("InteractionMap.tsv"))
            File.Delete("InteractionMap.tsv");

        if (File.Exists("BuiltInInteractionMap.tsv"))
            File.Delete("BuiltInInteractionMap.tsv");

        File.WriteAllLines("InteractionMap.tsv", ["InteractionFullName\tAiDescription"]);
        File.WriteAllLines("BuiltInInteractionMap.tsv", ["InteractionFullName\tAiDescription"]);

        foreach (string dll in Directory.GetFiles(rootPath, "*.dll").OrderBy(x => x))
        {
            try
            {
                if (!Path.GetFileName(dll).Equals("Dacris.Maestro.dll")
                    && assemblies.Any(a => a.Name == Path.GetFileNameWithoutExtension(dll)))
                {
                    continue;
                }

                var assembly = Assembly.LoadFrom(dll);
                var hasInteractions = assembly.ExportedTypes.Any(t => typeof(Interaction).IsAssignableFrom(t) && !t.IsAbstract);
                if (!hasInteractions)
                    continue;

                await WriteAsync(Path.Combine(Environment.CurrentDirectory, "Json"), assembly);

                GenerateInteractionMap(assembly, true);
                GenerateInteractionMap(assembly, false);
            }
            catch
            {
                // not an assembly
            }
        }

        GenerateDocumentation();
    }

    public static async Task WriteAsync(string rootDir, Assembly assembly)
    {
        foreach (Type interaction in assembly.GetTypes().Where(t => typeof(Interaction).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var interactionObj = (Interaction)Activator.CreateInstance(interaction)!;
            interactionObj.Specify();
            var json = JsonConvert.SerializeObject(interactionObj.InputSpec, Formatting.Indented);
            var nsDir = Path.Combine(rootDir, interaction.Namespace!);
            if (!Directory.Exists(nsDir))
            {
                Directory.CreateDirectory(nsDir);
            }
            await File.WriteAllTextAsync(Path.Combine(nsDir, interaction.Name + ".json"), json);
        }
    }
}

public class InputDependencySpec
{
    public string? ReferenceName { get; set; }
    public string[] AcceptedValues { get; set; } = [];
}

public class InputSpec
{
    public string Name { get; set; } = "InputState";
    public ValueSpec ValueSpec { get; set; }
    public List<InputDependencySpec> Dependencies { get; set; } = [];

    public InputSpec()
    {
        ValueSpec = RuleBasedType();
    }

    public InputSpec(string name)
    {
        Name = name;
        ValueSpec = RuleBasedType();
    }

    public InputSpec DependsOn(string referenceName, string[] acceptedValues)
    {
        Dependencies.Add(new InputDependencySpec { ReferenceName = referenceName, AcceptedValues = acceptedValues });
        return this;
    }

    public InputSpec WithSimpleType(ValueTypeSpec type)
    {
        ValueSpec = new ValueSpec { ValueType = type };
        return this;
    }

    private ValueSpec RuleBasedType()
    {
        if (Name.EndsWith("Key"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.Key };
        }
        if (Name.EndsWith("Path"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.JsonPath };
        }
        if (Name.ToLowerInvariant().Contains("file") || Name.ToLowerInvariant().Contains("dir"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.LocalPath };
        }
        if (Name.Equals("formula"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.Expression };
        }
        if (Name.ToLowerInvariant().Contains("schema"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.Object, ObjectSpecs = [
                new InputSpec { Name = "@fieldName", ValueSpec = 
                new ValueSpec {  ValueType = ValueTypeSpec.Enum, AcceptedValues =
                ["string", "integer", "number", "date"]
                }
            }] };
        }
        if (Name.ToLowerInvariant().EndsWith("url"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.Uri };
        }
        if (Name.ToLowerInvariant().EndsWith("query"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.Constant };
        }
        if (Name.Equals("connString"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.ConnString };
        }
        if (Name.Equals("header"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.Array, InnerSpec = new ValueSpec { 
                ValueType = ValueTypeSpec.Object,
                ObjectSpecs = [
                    new InputSpec { Name = "key", ValueSpec = new ValueSpec { ValueType = ValueTypeSpec.String } },
                    new InputSpec { Name = "value", ValueSpec = new ValueSpec { ValueType = ValueTypeSpec.String } }
                ]
            } };
        }
        if (Name.Equals("parameters"))
        {
            return new ValueSpec
            {
                ValueType = ValueTypeSpec.Object,
                ObjectSpecs = [
                    new InputSpec("schema"),
                    new InputSpec
                    {
                        Name = "data",
                        ValueSpec = new ValueSpec
                        {
                            ValueType = ValueTypeSpec.Object,
                            ObjectSpecs = [new InputSpec { Name = "@fieldName" }]
                        }
                    }
            ]
            };
        }
        if (Name.Equals("timeout"))
        {
            return new ValueSpec { ValueType = ValueTypeSpec.Integer };
        }
        return new ValueSpec { ValueType = ValueTypeSpec.String };
    }
}

public class ValueSpec
{
    public ValueTypeSpec ValueType { get; set; }
    public string[] AcceptedValues { get; set; } = [];
    public ValueSpec? InnerSpec { get; set; } // For Arrays
    public List<InputSpec>? ObjectSpecs { get; set; } // For Objects
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ValueTypeSpec
{
    String,
    JsonPath,
    Enum,
    Integer,
    Number,
    Date,
    Boolean,
    Key,
    StoragePath,
    LocalPath,
    ConnString,
    Expression,
    Uri,
    Constant,
    Array,
    Object
}