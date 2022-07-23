using System.Timers;
using BanAd.Processing.Workflow;
using Timer = System.Timers.Timer;

namespace BanAd.Processing.Monitoring;

public abstract class Monitor : IDisposable
{
    
    protected AdSubmissionProcessor Processor { get; }
    private Timer Timer { get; }

    protected Monitor(AdSubmissionProcessor processor, TimeSpan interval)
    {
        Processor = processor;

        Timer = new Timer(interval.TotalMilliseconds);
        Timer.Elapsed += Elapsed;
        Timer.Start();
    }
    
    #region " Tick "

    private bool Running { get; set; }
    private readonly object _locker = new();
    private async void Elapsed(object? sender, ElapsedEventArgs e)
    {
        if (!Running)
        {
            bool run = false;
            lock (_locker)
            {
                if (!Running)
                {
                    Running = true;
                    run = true;
                }
            }

            if (run)
            {
                try
                {
                    await Run();
                }
                finally
                {
                    lock (_locker)
                    {
                        Running = false;
                    }
                }
            }
        }
    }
    
    private async Task Run()
    {
        Complete.Reset();
        try
        {
            await Tick();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
        Complete.Set();
    }

    protected abstract Task Tick();
    
    #endregion
    
    #region " Stopping "

    private ManualResetEventSlim Complete { get; } = new();

    public ManualResetEventSlim Stop()
    {
        Timer.Stop();
        if (!Running)
        {
            Complete.Set();
        }
        return Complete;
    }
    
    public void Dispose()
    {
        Timer.Elapsed -= Elapsed;
        using (Timer)
        {
            Timer.Stop();
        }

        GC.SuppressFinalize(this);
    }
    
    #endregion
    
}