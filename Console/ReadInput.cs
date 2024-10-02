﻿namespace Dacris.Maestro.Console;

public class ReadInput : Interaction
{
    public override void Specify()
    {
        // no inputs
    }

    public override Task RunAsync()
    {
        var input = System.Console.ReadLine();
        AppState.Instance.WriteKey(InputStateKey ?? "consoleInput", input);
        return Task.CompletedTask;
    }
}