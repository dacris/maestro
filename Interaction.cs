using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Dacris.Maestro;

public abstract class Interaction
{
    internal static int _id = 1;

    [JsonIgnore]
    public List<IDisposable>? BlockSessionResources { get; internal set; }
    public string Id { get; set; } = $"a_" + _id++;
    public string? InputStateKey { get; set; }

    public abstract Task RunAsync();
    public abstract void Specify();

    [JsonIgnore]
    protected JToken? InputState => AppState.Instance.ReadKey(InputStateKey ?? Id);
    [JsonIgnore]
    public Dictionary<string, string> Options { get; set; } = [];
    [JsonIgnore]
    public bool Quiet {
        get {
            Options.TryGetValue("quiet", out var value);
            return value is not null && bool.Parse(value);
        }
    }

    [JsonIgnore]
    public InteractionInputSpec InputSpec { get; set; }

    public Interaction()
    {
        InputSpec = new InteractionInputSpec(GetType().FullName!);
    }

    protected async Task RetryAsync<T>(Func<T, Task> action, T param1)
    {
        var retryTimes = int.Parse(InputState!["retryTimes"]?.ToString() ?? "3");
        var retryDelay = int.Parse(InputState!["retryDelay"]?.ToString() ?? "10000");
        var retry = 1;
        while (retry <= retryTimes)
        {
            try
            {
                await action(param1);
                break;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Retrying... " + ex.Message);
                retry++;
                if (retry <= retryTimes)
                {
                    await Task.Delay(retryDelay * retry);
                }
                else if (retry > retryTimes)
                {
                    System.Console.WriteLine("Retries were exhausted.");
                    throw;
                }
            }
        }
    }
}
