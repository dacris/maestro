using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Newtonsoft.Json.Linq;

namespace Dacris.Maestro.Core
{
    public class CustomCSharpCode : Interaction
    {
        public override void Specify()
        {
            Description = "Compiles and executes code from a .cs file.";
            InputSpec.AddDefaultInput();
            InputSpec.Inputs[0].WithSimpleType(ValueTypeSpec.LocalPath);
        }

        public override async Task RunAsync()
        {
            // Contract:
            // static Task CustomInteraction.RunAsync(Interaction parent);

            string sourcePath = InputState?.ToString() ?? "CustomCode.cs";
            string sourceCode = File.ReadAllText(sourcePath);
            string assemblyPath = Path.ChangeExtension(Path.GetFileNameWithoutExtension(sourcePath), "DLL");

            var codeString = SourceText.From(sourceCode);
            var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10);

            var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
                MetadataReference.CreateFromFile(GetType().Assembly.Location),
                MetadataReference.CreateFromFile(typeof(JToken).Assembly.Location)
            };

            Assembly.GetEntryAssembly()?.GetReferencedAssemblies().ToList()
                .ForEach(a => references.Add(MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            var csCompilation = CSharpCompilation.Create(assemblyPath,
                new[] { parsedSyntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                    optimizationLevel: OptimizationLevel.Debug,
                    assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default));

            WeakReference? assemblyLoadContextWeakRef = null;
            using (var peStream = new MemoryStream())
            {
                var result = csCompilation.Emit(peStream);

                if (result.Success)
                {
                    System.Console.WriteLine("Compilation done without any error.");
                    peStream.Seek(0, SeekOrigin.Begin);
                    var compiledAssembly = peStream.ToArray();

                    using (var asm = new MemoryStream(compiledAssembly))
                    {
                        var assemblyLoadContext = new SimpleUnloadableAssemblyLoadContext();
                        var assembly = assemblyLoadContext.LoadFromStream(asm);
                        Task? task = (Task?)assembly!
                            .GetType("CustomInteraction")!
                            .GetMethod("RunAsync")!
                            .Invoke(null, new object[] { this });
                        await task!;

                        assemblyLoadContext.Unload();
                        assemblyLoadContextWeakRef = new WeakReference(assemblyLoadContext);
                    } // using
                }
                else
                {
                    System.Console.WriteLine("Compilation done with error.");
                    var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);
                    foreach (var diagnostic in failures)
                    {
                        System.Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                    throw new Exception("Compilation error: " + failures.ElementAt(0).GetMessage());
                }
            } // using

            if (assemblyLoadContextWeakRef != null)
            {
                for (var i = 0; i < 8 && assemblyLoadContextWeakRef.IsAlive; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Task.Delay(50);
                }
                System.Console.WriteLine(assemblyLoadContextWeakRef.IsAlive ? "Unloading failed!" : "Unloading success!");
            }
        }
    }

    internal class SimpleUnloadableAssemblyLoadContext : AssemblyLoadContext
    {
        public SimpleUnloadableAssemblyLoadContext()
            : base(true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
