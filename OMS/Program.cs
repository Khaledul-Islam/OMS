using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OMS.DbContext;
using OMS.Entity;
using OMS.Factory;
using OMS.Job;
using Quartz;
using Quartz.Spi;

namespace OMS
{
    class Program
    {
        static async Task Main()
        {
            var serviceProvider = ConfigureServices();

            var scheduler = serviceProvider.GetService<IScheduler>();
            var jobSchedule = serviceProvider.GetService<JobSchedule>();

            if (scheduler != null && jobSchedule != null)
            {
                await SchedulerService.ScheduleJob(scheduler, jobSchedule);

                await scheduler.Start();

                Console.WriteLine("Press any key to close the application");
                Console.ReadKey();

                await scheduler.Shutdown();
            }
        }

        private static IServiceProvider ConfigureServices()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json") // Add appropriate path if needed
                .Build();

            var connectionString = configuration["ConnectionString:SqlServer"];

            var service = new ServiceCollection();

            // Register services
            service.AddDbContext<AppDbContext>(options => 
                options.UseSqlServer(connectionString));
            service.AddSingleton<OmsJob>();
            service.AddSingleton(CreateJobScheduleService.CreateJobSchedule<OmsJob>(configuration));
            service.AddSingleton<IJobFactory, JobFactory>();
            service.AddSingleton(SchedulerService.CreateScheduler);

            return service.BuildServiceProvider();
        }
    }
}