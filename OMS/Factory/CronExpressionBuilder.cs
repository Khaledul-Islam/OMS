using Microsoft.Extensions.Configuration;

namespace OMS.Factory
{
    public class CronExpressionBuilder
    {
        private readonly IConfiguration _configuration;

        public CronExpressionBuilder(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string BuildCronExpression()
        {
            // Retrieve the cron expression from appsettings.json
            return _configuration["SchedulerCron:CronExpression"];
        }
    }
}