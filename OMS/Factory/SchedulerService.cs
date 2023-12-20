using Microsoft.Extensions.DependencyInjection;
using Quartz.Impl;
using Quartz.Spi;
using Quartz;
using OMS.Entity;

namespace OMS.Factory
{
    public static class SchedulerService
    {
        public static IScheduler CreateScheduler(IServiceProvider provider)
        {
            var schedulerFactory = new StdSchedulerFactory();
            var scheduler = schedulerFactory.GetScheduler().Result;
            scheduler.JobFactory = provider.GetRequiredService<IJobFactory>();
            return scheduler;
        }

        public static async Task ScheduleJob(IScheduler scheduler, JobSchedule jobSchedule)
        {
            if (jobSchedule.JobType.FullName != null)
                await scheduler.ScheduleJob(
                    JobBuilder
                        .Create(jobSchedule.JobType)
                        .WithIdentity(jobSchedule.JobType.FullName)
                        .Build(),
                    TriggerBuilder
                        .Create()
                        .WithCronSchedule(jobSchedule.CronExpression)
                        .Build());
        }
    }
}
