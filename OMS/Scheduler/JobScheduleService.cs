using Microsoft.Extensions.Configuration;
using OMS.Factory;

namespace OMS.Scheduler
{
    public static class JobScheduleService
    {
        public static JobSchedule CreateJobSchedule<TJob>(IConfiguration configuration)
        {
            var cronExpressionBuilder = new CronExpressionBuilder(configuration);
            var cronExpression = cronExpressionBuilder.BuildCronExpression();

            return new JobSchedule(typeof(TJob), cronExpression);
        }
    }
}
