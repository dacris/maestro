using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dacris.Maestro.Core;

public class Block : Interaction
{
    public bool Repeat { get; set; }
    public bool Parallel { get; set; }
    public Interaction? ConditionCheck { get; set; }
    public List<Interaction> Statements { get; set; } = [];

    [JsonIgnore]
    public List<IDisposable> SessionResources { get; internal set; } = [];
    [JsonIgnore]
    public bool IsRoot { get; internal set; }
    [JsonIgnore]
    public string? LastInteractionRunning { get; internal set; }

    public override void Specify()
    {
        Description = "In a block";
        AiExclude = true;
    }

    public override async Task RunAsync()
    {
        try
        {
            if (ConditionCheck is not null)
            {
                ConditionCheck.BlockSessionResources = SessionResources;
                ConditionCheck.Options = Options;
            }
            foreach (Interaction a in Statements)
            {
                a.BlockSessionResources = SessionResources;
                a.Options = Options;
            }
            do
            {
                if (!Quiet)
                {
                    System.Console.WriteLine($"Iterating Block with Id={Id}...");
                    AppState.Instance.Logger?.Debug($"Iterating Block with Id={Id}...");
                }
                if (ConditionCheck is not null)
                {
                    await RunInteractionAsync(ConditionCheck);
                }
                if (bool.Parse(AppState.Instance.StateObject[ConditionCheck?.Id ?? "_globalCond"]?.ToString() ?? "true") != false)
                {
                    if (Parallel)
                    {
                        LastInteractionRunning = Statements.FirstOrDefault()?.Id;
                        System.Threading.Tasks.Parallel.Invoke(
                            new ParallelOptions { MaxDegreeOfParallelism = 64 },
                            Statements.Select(s => new Action(() => { s.RunAsync().Wait(); })).ToArray());
                    }
                    else
                    {
                        foreach (Interaction a in Statements)
                        {
                            await RunInteractionAsync(a);
                        }
                    }
                }
                else
                {
                    break;
                }
            }
            while (Repeat);
            if (!Quiet)
            {
                System.Console.WriteLine($"Ended Block with Id={Id}");
                AppState.Instance.Logger?.Debug($"Ended Block with Id={Id}...");
            }
        }
        catch (FlowEndException)
        when (IsRoot)
        {
            if (!Quiet)
            {
                System.Console.WriteLine("Flow ended by an interaction.");
                AppState.Instance.Logger?.Debug("Flow ended by an interaction.");
            }
            throw;
        }
        catch (Exception ex)
        when (IsRoot)
        {
            AppState.Instance.Logger?.Error(ex.ToString());
            AppState.Instance.WriteKey("appError",
                JToken.FromObject(new ErrorDescription(
                    ex.Message,
                    ex.InnerException,
                    ex.StackTrace,
                    LastInteractionRunning,
                    DateTime.UtcNow
                )));
            await new PersistState() { InputStateKey = "appError" }.RunAsync();
            throw;
        }
        finally
        {
            SessionResources.ForEach(x => x.Dispose());
            SessionResources.Clear();
        }
    }

    private async Task RunInteractionAsync(Interaction a)
    {
        if (!Quiet)
        {
            System.Console.WriteLine($"Running interaction with Id={a.Id} and Type={a.GetType().Name}...");
            AppState.Instance.Logger?.Debug($"Running interaction with Id={a.Id} and Type={a.GetType().Name}...");
        }
        LastInteractionRunning = a.Id;
        await a.RunAsync();
        if (!Quiet)
        {
            System.Console.WriteLine($"Ran interaction with Id={a.Id} and Type={a.GetType().Name} successfully.");
            AppState.Instance.Logger?.Debug($"Ran interaction with Id={a.Id} and Type={a.GetType().Name} successfully.");
        }
    }

    public async Task PersistToFileAsync(string? appName = null, bool inPlainEnglish = false)
    {
        if (inPlainEnglish)
        {
            await File.WriteAllTextAsync((appName ?? AppState.Instance.ReadKey("appName")) + ".txt",
                new EnglishLanguageParser().WriteBlockToString(this));
            return;
        }
        await File.WriteAllTextAsync((appName ?? AppState.Instance.ReadKey("appName")) + ".json",
        JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        }));
    }

    public static async Task<Block> ReadFromFileAsync(string appName, bool inPlainEnglish = false)
    {
        if (inPlainEnglish)
        {
            return new EnglishLanguageParser().ReadBlockFromString(await File.ReadAllTextAsync(appName + ".txt"));
        }
        var block = JsonConvert.DeserializeObject<Block>(
        await File.ReadAllTextAsync(appName + ".json"), new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        })!;
        block.IsRoot = true;
        return block;
    }
}
