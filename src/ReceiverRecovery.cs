namespace MRS;
internal static class ReceiverRecovery
{
    // Monotonic I/Q arrival evidence, not merely an opened device or a wall-clock wait.
    internal sealed class Health
    {
        private readonly object gate = new();
        private long first = -1, last = -1;
        internal void Observe(long now)
        {
            lock (gate)
            {
                if (first < 0 || now < last || now - last > 1500) first = now;
                last = now;
            }
        }
        internal bool Sustained { get { lock (gate) return first >= 0 && last - first >= 30000; } }
        internal void Reset() { lock (gate) first = last = -1; }
    }
    internal sealed record Result(RtlNative.Device? Device, int Attempts);
    internal static async Task<Result> Find(string identity, int usedAttempts, Func<Task<List<RtlNative.Device>>> enumerate,
        Func<int, CancellationToken, Task> delay, Action<string> status, CancellationToken token)
    {
        int attempts = usedAttempts;
        while (attempts < 3)
        {
            int wait = new[] { 2000, 5000, 10000 }[attempts]; attempts++;
            status($"Reconnect attempt {attempts}/3 in {wait / 1000} seconds. Stop cancels.");
            await delay(wait, token); token.ThrowIfCancellationRequested();
            try
            {
                var devices = await enumerate(); token.ThrowIfCancellationRequested();
                var match = ReceiverIdentity.Match(devices, identity);
                if (match != null) return new Result(match, attempts);
                status("Saved receiver is absent or its identity is ambiguous; no substitute selected.");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { status("Reconnect discovery failed: " + ex.Message); }
        }
        return new Result(null, attempts);
    }
}
