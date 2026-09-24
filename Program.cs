using System.Runtime.InteropServices;

var app = WebApplication.CreateBuilder(args).Build();

// Every item in this list = 1 MB of memory that the app is holding on to
var held = new List<IntPtr>();

// Shows how much memory the app holds, and the limit Kubernetes gave it
app.MapGet("/status", () => new
{
    heldByAppMB = held.Count,
    containerLimit = ReadLimit()
});

// Adds memory. Example: /eat?mb=50 adds 50 MB each time you call it
app.MapGet("/eat", (int mb = 50) =>
{
    for (int i = 0; i < mb; i++)
    {
        var chunk = Marshal.AllocHGlobal(1024 * 1024);          // ask for 1 MB
        for (int offset = 0; offset < 1024 * 1024; offset += 4096)
            Marshal.WriteByte(chunk, offset, 1);                // really use it
        held.Add(chunk);
    }
    return new { heldByAppMB = held.Count };
});


// In simple words - this API keeps the CPU busy for the given seconds
// In real world - high traffic causes this same high CPU usage
// HPA will see this high CPU and add more pods
app.MapGet("/burn", (int seconds = 30) =>
{
    var end = DateTime.UtcNow.AddSeconds(seconds);
    while (DateTime.UtcNow < end)
    {
        Math.Sqrt(12345.6789); // pointless math, just to keep the CPU busy
    }
    return new { podName = Environment.MachineName, burned = $"{seconds}s" };
});


app.Run();

// Reads the memory limit that Kubernetes set (your 256Mi)
static string ReadLimit()
{
    foreach (var path in new[] { "/sys/fs/cgroup/memory.max", "/sys/fs/cgroup/memory/memory.limit_in_bytes" })
        if (File.Exists(path) && long.TryParse(File.ReadAllText(path).Trim(), out var bytes))
            return $"{bytes / 1024 / 1024} MB";
    return "no limit found";
}