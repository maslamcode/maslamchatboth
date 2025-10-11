using Chatbot.Scheduler.Job;

namespace Chatbot.Scheduler.Job
{
    public class FileCleanupJob : IScheduledJob
    {
        private readonly ILogger<FileCleanupJob> _logger;

        public string Name => "File Cleanup Job";
        public TimeSpan Interval => TimeSpan.FromMinutes(1); 

        public FileCleanupJob(ILogger<FileCleanupJob> logger)
        {
            _logger = logger;
        }

        //INI SAMPLE UNTUK JOB LAIN BARANGKALI NANTI DIPERLUKAN UNTUK CLEAN DATA
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting cleanup process at {time}", DateTimeOffset.Now);

            try
            {
                await Task.Delay(1500, cancellationToken);

                var deletedFiles = new[] { "temp1.log", "temp2.log", "cache.tmp" };
                foreach (var file in deletedFiles)
                {
                    _logger.LogInformation("Deleted file: {file}", file);
                }

                _logger.LogInformation("Cleanup complete at {time}", DateTimeOffset.Now);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Cleanup job was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup job.");
            }
        }
    }
}
