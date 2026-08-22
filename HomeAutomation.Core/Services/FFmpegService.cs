using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace HomeAutomation.Core.Services;

public interface IFFmpegService
{
    Task<byte[]> Execute(IEnumerable<string> arguments, TimeSpan timeout, CancellationToken cancellationToken);
}

public sealed class FFmpegService(IConfiguration configuration) : IFFmpegService
{
    public async Task<byte[]> Execute(IEnumerable<string> arguments, TimeSpan timeout, CancellationToken cancellationToken)
    {
        string? executablePath = configuration["FFmpeg:Path"];
        if (string.IsNullOrWhiteSpace(executablePath))
            throw new InvalidOperationException("Configuration value 'FFmpeg:Path' must contain the FFmpeg executable path.");

        using Process process = new();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (string argument in arguments)
            process.StartInfo.ArgumentList.Add(argument);

        if (!process.Start())
            throw new InvalidOperationException("FFmpeg could not be started.");

        using CancellationTokenSource timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(timeout);

        using MemoryStream output = new();
        Task copyOutput = process.StandardOutput.BaseStream.CopyToAsync(output, CancellationToken.None);
        Task<string> readError = process.StandardError.ReadToEndAsync(CancellationToken.None);

        try
        {
            await process.WaitForExitAsync(timeoutCancellation.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Kill(process);
            throw new TimeoutException("FFmpeg did not complete within the configured timeout.");
        }
        catch
        {
            Kill(process);
            throw;
        }
        finally
        {
            Kill(process);
        }

        await copyOutput;
        _ = await readError;

        if (process.ExitCode != 0)
            throw new InvalidOperationException($"FFmpeg exited with code {process.ExitCode}.");

        return output.ToArray();
    }

    private static void Kill(Process process)
    {
        if (!process.HasExited)
            process.Kill(entireProcessTree: true);
    }

}
