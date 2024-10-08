﻿namespace Dacris.Maestro.Data
{
    public class Modify : DataInteraction
    {
        public override void Specify()
        {
            base.Specify();
            InputSpec.AddInputs("query", "parameters");
            InputSpec.AddRetry();
        }

        public override async Task RunAsync()
        {
            var query = InputState!["query"]!.ToString();
            var parameters = InputState!["parameters"];

            if (AppState.Instance.IsMock())
                return;

            var session = await EnsureSessionAsync();
            var repo = session.Item1;
            var conn = session.Item2;

            await RetryAsync(async (x) =>
            {
                repo.Modify(conn, query, parameters);
                await Task.CompletedTask;
            }, (object?)null);
        }
    }
}
