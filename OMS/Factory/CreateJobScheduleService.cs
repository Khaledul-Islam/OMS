using Microsoft.Extensions.Configuration;
using OMS.Entity;

namespace OMS.Factory
{
    public static class CreateJobScheduleService
    {
        public static JobSchedule CreateJobSchedule<TJob>(IConfiguration configuration)
        {
            var cronExpressionBuilder = new CronExpressionBuilder(configuration);
            var cronExpression = cronExpressionBuilder.BuildCronExpression();

            return new JobSchedule(typeof(TJob), cronExpression);
        }
    }
}
