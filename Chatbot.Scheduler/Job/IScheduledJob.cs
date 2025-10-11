using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chatbot.Scheduler.Job
{
    public interface IScheduledJob
    {
        string Name { get; }
        TimeSpan Interval { get; }
        Task ExecuteAsync(CancellationToken cancellationToken);
    }

}
