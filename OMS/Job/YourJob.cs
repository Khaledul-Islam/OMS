using Quartz;
using OMS.DbContext;

namespace OMS.Job
{
    public class YourJob(AppDbContext dbContext) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await dbContext.ExecuteStoredProcedureAsync("usp_usp_GetEmployees");

                Console.WriteLine("Job executed successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }
}