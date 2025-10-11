using Chatbot.Scheduler.Job;
using Microsoft.Extensions.DependencyInjection;

namespace Chatbot.Scheduler
{
    public class Worker : BackgroundService
    {
        private readonly IEnumerable<IScheduledJob> _jobs;
        private readonly ILogger<Worker> _logger;

        public Worker(IEnumerable<IScheduledJob> jobs, ILogger<Worker> logger)
        {
            _jobs = jobs;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Scheduler started at: {time}", DateTimeOffset.Now);

            var timers = _jobs.Select(job => RunJobLoop(job, stoppingToken)).ToList();
            await Task.WhenAll(timers);
        }

        private async Task RunJobLoop(IScheduledJob job, CancellationToken token)
        {
            _logger.LogInformation("Job '{jobName}' started with interval {interval}", job.Name, job.Interval);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    await job.ExecuteAsync(token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running job {jobName}", job.Name);
                }

                await Task.Delay(job.Interval, token);
            }
        }
    }
}
