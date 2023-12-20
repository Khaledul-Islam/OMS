using Microsoft.Extensions.Configuration;

namespace OMS.Factory
{
    public class CronExpressionBuilder(IConfiguration configuration)
    {
        public string BuildCronExpression()
        {
            // Retrieve the cron expression from appsettings.json
            return configuration["SchedulerCron:CronExpression"] ?? string.Empty;
        }
    }
}