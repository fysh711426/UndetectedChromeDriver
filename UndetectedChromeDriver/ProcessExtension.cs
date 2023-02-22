using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SeleniumUndetectedChromeDriver
{
    internal static class ProcessExtension
    {
        public static async Task WaitForExitPatchAsync(this Process process,
            CancellationToken cancellationToken = default)
        {
#if (NET48 || NET47 || NET46 || NET45 || NETSTANDARD2_1 || NETSTANDARD2_0)
            await process._WaitForExitAsync(cancellationToken);
#else
            await process.WaitForExitAsync(cancellationToken);
#endif
        }

        // refer: https://stackoverflow.com/questions/470256/process-waitforexit-asynchronously
        private static Task _WaitForExitAsync(this Process process,
            CancellationToken cancellationToken = default)
        {
            if (process.HasExited)
                return Task.FromResult(true);

            var tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(true);
            if (cancellationToken != default)
                cancellationToken.Register(() => tcs.SetCanceled());

            return process.HasExited ? Task.FromResult(true) : tcs.Task;
        }
    }
}
