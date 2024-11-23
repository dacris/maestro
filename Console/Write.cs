namespace Dacris.Maestro.Console;
public class Write : Interaction
{
    public override void Specify()
    {
        Description = "Writes a message to the console.";
        AiExclude = true;
        InputSpec.AddDefaultInput();
    }

    public override async Task RunAsync()
    {
        await Task.CompletedTask;
        System.Console.WriteLine(InputState?.ToString());
    }
}
