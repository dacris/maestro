using FluentEmail.MailKitSmtp;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace Dacris.Maestro.Email;

public class SendSmtp : Interaction
{
    public override void Specify()
    {
        Description = "Sends an email using SMTP over SSL.";
        InputSpec.AddInputs("smtpServer", "smtpUser", "smtpPasswordKey", "bodyHtmlTemplate", "from", "to", "subject", "attach");
        InputSpec.StateObjectKey("bodyHtmlTemplate").WithSimpleType(ValueTypeSpec.LocalPath);
        InputSpec.StateObjectKey("attach").ValueSpec = new ValueSpec
        {
            ValueType = ValueTypeSpec.Array,
            InnerSpec = new ValueSpec { ValueType = ValueTypeSpec.LocalPath }
        };
    }

    public override async Task RunAsync()
    {
        var state = AppState.Instance.StateObject;
        var emailCfg = (JObject)InputState!;
        FluentEmail.Core.Email.DefaultSender = new MailKitSender(new SmtpClientOptions
        {
            UseSsl = true,
            Server = emailCfg["smtpServer"]!.ToString(),
            User = emailCfg["smtpUser"]!.ToString(),
            Password = state[emailCfg["smtpPasswordKey"]!.ToString()]!.ToString(),
            RequiresAuthentication = true,
            Port = 465
        });
        var fileName = emailCfg!["bodyHtmlTemplate"];
        var template = await File.ReadAllTextAsync(fileName!.ToString());
        AppState.Instance.WriteKey("_templateLevel", "1");
        AppState.Instance.WriteKey("_templateFormat", "html");
        var body = Regex.Replace(template, StringTemplate.Regex, MatchEval);
        AppState.Instance.ClearKey("_templateLevel");
        AppState.Instance.ClearKey("_templateFormat");
        var email = FluentEmail.Core.Email
            .From(emailCfg["from"]!.ToString())
            .To(emailCfg["to"]!.ToString())
            .Subject(emailCfg["subject"]!.ToString())
            .Body(body, true);
        if (emailCfg["attach"] is not null)
        {
            foreach (var attachment in emailCfg["attach"]!.Values())
            {
                email = email.AttachFromFilename(attachment.ToString(), "application/octet-stream");
            }
        }
        if (!AppState.Instance.IsMock())
        {
            var response = await email.SendAsync();
            if (!response.Successful)
            {
                System.Console.Error.WriteLine("Send email failed!");
                System.Console.Error.WriteLine(string.Join('\n', response.ErrorMessages));
                throw new Exception(response.ErrorMessages[0]);
            }
        }
    }

    private string MatchEval(Match match) => StringTemplate.MatchEval(match);
}
