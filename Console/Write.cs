namespace Dacris.Maestro.Console;
public class Write : Interaction
{
    public override void Specify()
    {
        InputSpec.AddDefaultInput();
    }

    public override async Task RunAsync()
    {
        await Task.CompletedTask;
        System.Console.WriteLine(InputState?.ToString());
    }
}
