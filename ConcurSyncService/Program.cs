using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using ConcurSyncLib;

namespace ConcurSyncSvc
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.LogFatal("Application failed to start.", ex);
                throw;
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(builder =>
            {
                builder.AddLog4Net("log4net.config");
            })

                .ConfigureServices(ConfigureWorkerServices);
        }

        private static void ConfigureWorkerServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.AddHostedService<WorkerDaily>();
        }

        public class WorkerHourly : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                Log.LogInfo("ExecuteAsync starting.");

                // Run once at startup
                await DoWork();

                while (!stoppingToken.IsCancellationRequested)
                {
                    Log.LogInfo("Next run scheduled in 1 hour.");
                    try
                    {
                        await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Wait for 1 hour before next execution
                        if (!stoppingToken.IsCancellationRequested)
                        {
                            await DoWork();
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Graceful shutdown
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.LogError("Error in scheduled task.", ex);
                    }
                }
            }

            private async Task DoWork()
            {
                Log.LogInfo("DoWork starting.");
                try
                {
                    await ConcurSyncLib.Main.DoWork();
                }
                catch (Exception ex)
                {
                    Log.LogError("DoWork encountered an error.", ex);
                }
                Log.LogInfo("DoWork done.");
            }
        }


        public class WorkerDaily : BackgroundService
        {
            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                Log.LogInfo("ExecuteAsync starting.");

                // Run once at startup
                await DoWork();

                TimeSpan runTime = TimeSpan.FromHours(20); // 8:00 PM
                while (!stoppingToken.IsCancellationRequested)
                {
                    TimeSpan now = DateTime.Now.TimeOfDay;
                    TimeSpan delay = (runTime > now) ? (runTime - now) : (TimeSpan.FromDays(1) - (now - runTime));

                    Log.LogInfo($"Next run scheduled in {delay.TotalHours:F2} hours.");
                    try
                    {
                        await Task.Delay(delay, stoppingToken); // Wait until the next scheduled time
                        if (!stoppingToken.IsCancellationRequested)
                        {
                            await DoWork();
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Graceful shutdown
                        break;
                    }
                    catch (Exception ex)
                    {
                        Log.LogError("Error in scheduled task.", ex);
                    }
                }
            }

            private async Task DoWork()
            {
                Log.LogInfo("DoWork starting.");
                try
                {
                    await ConcurSyncLib.Main.DoWork();
                }
                catch (Exception ex)
                {
                    Log.LogError("DoWork encountered an error.", ex);
                }
                Log.LogInfo("DoWork done.");
            }
        }
    }
}
