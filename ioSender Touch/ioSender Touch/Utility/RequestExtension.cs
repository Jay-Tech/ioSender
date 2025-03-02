using System;
using System.Threading;
using System.Threading.Tasks;
using ioSenderTouch.GrblCore;
using ioSenderTouch.ViewModels;
using Action = System.Action;

namespace ioSenderTouch.Utility
{
    public static class RequestExtension
    {
        public static async Task SendSettings(GrblViewModel model, byte command, string key = null, Action process = null, int timeout = 500)
        {
            try
            {
                bool res = false;
                using var cancellationToken = new CancellationTokenSource();
                model.Poller.SetState(0);

                void ProcessSettings(string response)
                {
                    key ??= "ok";
                    if(!response.Contains(key, StringComparison.OrdinalIgnoreCase)) return;
                    process?.Invoke();
                    res = true;
                }
                void Send()
                {
                    Comms.com.DataReceived -= ProcessSettings;
                    Comms.com.DataReceived += ProcessSettings;
                    Comms.com.WriteByte(command);
                    while (!res)
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            break;
                        }
                        Task.Delay(50, cancellationToken.Token);
                    }
                    Comms.com.DataReceived -= ProcessSettings;
                    model.Poller.SetState(model.PollingInterval);
                }

                var task = Task.Factory.StartNew(Send, cancellationToken.Token);
                if (await Task.WhenAny(task, Task.Delay(timeout, cancellationToken.Token)) == task)
                {
                    cancellationToken.Cancel();
                    await task;
                   
                }
                else
                {
                    cancellationToken.Cancel();
                    
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                model.Poller.SetState(200);
            }
        }


    }
}

public static class StringExtensions
{
    public static bool Contains(this string source, string toCheck, StringComparison comp)
    {
        return source?.IndexOf(toCheck, comp) >= 0;
    }
}
public static class Extension
{
    public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
    {
        using var timeoutCancellationTokenSource = new CancellationTokenSource();
        var completedTask = await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token));
        if (completedTask == task)
        {
            timeoutCancellationTokenSource.Cancel();
            return await task;  // Very important in order to propagate exceptions
        }
        else
        {
            throw new TimeoutException("The operation has timed out.");
        }
    }

}