using System.Security;

namespace Dacris.Maestro.Console;

public class ReadSensitive : Interaction
{
    public override void Specify()
    {
        // no inputs
    }

    public override Task RunAsync()
    {
        var input = GetPassword();
        AppState.Instance.WriteKey(InputStateKey ?? "consoleInput", ToPlainString(input), true);
        return Task.CompletedTask;
    }

    public static string ToPlainString(SecureString secureStr)
    {
        string plainStr = new System.Net.NetworkCredential(string.Empty,
                          secureStr).Password;
        return plainStr;
    }

    public SecureString GetPassword()
    {
        var pwd = new SecureString();
        while (true)
        {
            ConsoleKeyInfo i = System.Console.ReadKey(true);
            if (i.Key == ConsoleKey.Enter)
            {
                break;
            }
            else if (i.Key == ConsoleKey.Backspace)
            {
                if (pwd.Length > 0)
                {
                    pwd.RemoveAt(pwd.Length - 1);
                    System.Console.Write("\b \b");
                }
            }
            else if (i.KeyChar != '\u0000') // KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
            {
                pwd.AppendChar(i.KeyChar);
                System.Console.Write("*");
            }
        }
        return pwd;
    }
}
