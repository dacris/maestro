namespace Dacris.Maestro.Core;

public class EndFlow : Interaction
{
    public override void Specify()
    {
    }

    public override Task RunAsync()
    {
        throw new FlowEndException();
    }
}

[Serializable]
public class FlowEndException : Exception
{
    public FlowEndException()
    {
    }
}
