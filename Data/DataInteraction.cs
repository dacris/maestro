using System.Data.Common;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Dacris.Maestro.Data
{
    public abstract class DataInteraction : Interaction
    {
        private static readonly Dictionary<string, Assembly> _assemblies = new();

        public override void Specify()
        {
            InputSpec.AddDefaultInput();
            InputSpec.Inputs.Add(new InputSpec
            {
                Name = "dataConnections",
                ValueSpec = new ValueSpec {
                    ValueType = ValueTypeSpec.Object,
                    ObjectSpecs = [new InputSpec { Name = "@connName", ValueSpec = new ValueSpec
                    {
                        ValueType = ValueTypeSpec.Object,
                        ObjectSpecs = [
                            new InputSpec {
                                Name = "systemType", ValueSpec = new ValueSpec { ValueType = ValueTypeSpec.Enum,
                                AcceptedValues = ["MemorySql", "SqlServer", "Firebird", "MockData"]
                            } },
                            new InputSpec("connString")
                        ]
                    } }]
                }
            });
            InputSpec.AddInputs("connPath");
            InputSpec.AddTimeout();
            InputSpec.Inputs.Add(new InputSpec
            {
                Name = "customDataProviders",
                ValueSpec = new ValueSpec
                {
                    ValueType = ValueTypeSpec.Object,
                    ObjectSpecs = [
                                    new InputSpec("@systemType").WithSimpleType(ValueTypeSpec.String)
                                ]
                }
            });
        }

        protected async Task<(IDataRepository, DbConnection)> EnsureSessionAsync()
        {
            var path = InputState!["connPath"]!.ToString();
            var connSpec = AppState.Instance.ReadKey("dataConnections")!.SelectToken(path);
            var systemType = Regex.Match(connSpec!["systemType"]!.ToString(), "[A-Za-z0-9_]+").Value;
            if (AppState.Instance.IsMock())
            {
                systemType = "MemorySql";
            }
            var repo = GetRepository(systemType);
            var innerSession = (DbConnection?)null;
            var defaultConnectionString = $"Data Source=Maestro;Mode=Memory;Cache=Shared";
            var connString = connSpec!["connString"]?.ToString() ?? defaultConnectionString;
            if (AppState.Instance.IsMock())
            {
                connString = defaultConnectionString;
            }
            var existingSession = BlockSessionResources!.FirstOrDefault(
                r => typeof(DbConnection).IsAssignableFrom(r.GetType())
                    && ((DbConnection)r).ConnectionString == connString);
            innerSession = (DbConnection)(existingSession ?? repo.OpenSession(BlockSessionResources!, connString));
            repo.Timeout = Math.Max(30, int.Parse(InputState!["timeout"]?.ToString() ?? "900000") / 1000);
            await Task.CompletedTask;
            return (repo, innerSession!);
        }

        private static IDataRepository GetRepository(string? systemType)
        {
            var customProviders = AppState.Instance.ReadKey("customDataProviders");
            if (customProviders?[systemType ?? "none"] is not null)
            {
                var namespaceName = customProviders?[systemType ?? "none"]!.ToString();
                Assembly? assembly;
                if (_assemblies.ContainsKey(namespaceName!))
                {
                    assembly = _assemblies[namespaceName!];
                }
                else
                {
                    _assemblies.Add(namespaceName!, Assembly.LoadFile(System.IO.Path.Combine(
                                        System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                                        namespaceName + ".dll")));
                    assembly = _assemblies[namespaceName!];
                }
                return (IDataRepository)assembly!.CreateInstance(namespaceName + "." + systemType + "DataRepository",
                    true, BindingFlags.Default, null,
                    [], null, null)!;
            }
            else
            {
                var systemTypeFQN = "Dacris.Maestro.Data." + systemType;
                return (IDataRepository)Activator.CreateInstance(Type.GetType(systemTypeFQN, true, true)!)!;
            }
        }
    }
}
