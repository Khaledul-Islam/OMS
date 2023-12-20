using Quartz.Spi;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace OMS.Factory
{
    public class JobFactory(IServiceProvider serviceProvider) : IJobFactory
    {
        public IJob NewJob(TriggerFiredBundle bundle, IScheduler scheduler)
        {
            return serviceProvider.GetRequiredService(bundle.JobDetail.JobType) as IJob ?? throw new InvalidOperationException();
        }

        public void ReturnJob(IJob job)
        {
            // Nothing to do here
        }
    }
}
