using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OMS.DbContext;
using Quartz;

public class OmsJob : IJob
{
    private readonly AppDbContext _dbContext;

    public OmsJob(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Execute(IJobExecutionContext context)
    {

        await InsertAndroidOrderIntoProcessTable();
        await TransferAndroidOrderToOMSProcessTable();
        await ProcessCodeConversion();
        Console.WriteLine("OmsJob executed successfully.");
    }

    private async Task InsertAndroidOrderIntoProcessTable()
    {
        using (var transaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                Console.WriteLine(@$"Started Execution ==InsertAndroidOrderIntoProcessTable== at {DateTime.Now}");
                await _dbContext.ExecuteStoredProcedureAsync("spInsertIntoProcessTable");
                Console.WriteLine($"Execution Completed ##InsertAndroidOrderIntoProcessTable## at {DateTime.Now}");
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error: {ex}");
            }

        }

    }

    private async Task TransferAndroidOrderToOMSProcessTable()
    {
        using (var transaction = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                Console.WriteLine(@$"Started Execution ==TransferAndroidOrderToOMSProcessTable== at {DateTime.Now}");
                await _dbContext.ExecuteStoredProcedureAsync("spInsertordertomobile");
                Console.WriteLine($"Execution Completed ##TransferAndroidOrderToOMSProcessTable## at {DateTime.Now}");
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error: {ex}");
            }

        }
    }

    private async Task ProcessCodeConversion()
    {
        using (var transA = await _dbContext.Database.BeginTransactionAsync())
        {
            try
            {
                Console.WriteLine(@$"Started Execution ==ProcessCodeConversion== at {DateTime.Now}");

                // Execute OMS_spUpdateStock
                await _dbContext.ExecuteStoredProcedureAsync("OMS_spUpdateStock");

                // Additional logic to check stock, you may adjust as needed
                bool stockCheckPassed = false;
                while (!stockCheckPassed)
                {
                    _dbContext.Database.OpenConnection(); // Ensure the connection is open
                    try
                    {
                        var stockCount = _dbContext.Database.SqlQuery<int>($"SELECT COUNT(*) FROM OMS_nblStock").FirstOrDefault();
                        stockCheckPassed = stockCount >= 1;
                    }
                    finally
                    {
                        _dbContext.Database.CloseConnection(); // Close the connection
                    }

                    if (!stockCheckPassed)
                    {
                        Console.WriteLine("Waiting for stock...");
                        await Task.Delay(50000);
                    }
                }

                // Begin transaction
                using (var transB = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        // Execute OMS_spUpdateProductBar
                        await _dbContext.ExecuteStoredProcedureAsync("OMS_spUpdateProductBar");

                        // Execute OMS_spRegionProductQuota
                        await _dbContext.ExecuteStoredProcedureAsync("OMS_spRegionProductQuota");

                        // Additional logic for processing orders
                        var status = 3; // Set your desired status value
                        var sqlQuery = $"SELECT * FROM OMS_OrderInfo_Process WHERE Status = {status}";

                        var pendingOrders = _dbContext.Set<OMS_OrderInfo_Process>()
                            .FromSqlRaw(sqlQuery)
                            .ToList();

                        foreach (var order in pendingOrders)
                        {
                            var orderId = new SqlParameter("@ParameterName1", order.OrderID);
                            // Add more parameters as needed

                            await _dbContext.ExecuteStoredProcedureWithParametersAsync("OMS_spConvertNewCodeSameStrength", orderId);

                            // Update OMS_OrderInfo_Process status
                            order.Status = 4;

                            // Update OMS_nblStock
                            await _dbContext.Database
                                .ExecuteSqlRawAsync("UPDATE OMS_nblStock SET QtyInHand=QtyInHand-ModifiedQty FROM OMS_OrderDetail_Process WHERE OMS_nblStock.ProdiD=OMS_OrderDetail_Process.prodid and OrderID={0} and OMS_nblStock.SCID=Branch", order.OrderID);
                        }

                        // Additional logic for updating OMS_OrderInfo_Process
                        await _dbContext.Database.ExecuteSqlRawAsync(@"
                        UPDATE OMS_OrderInfo_Process  
                        SET [LineCount]=a.cnt,
                            [SalesType]=CASE WHEN [SalesType]=1 THEN '01' ELSE '03' END,
                            BU=CASE 
                                WHEN BU='Sandoz' THEN '002' 
                                WHEN BU='Pharma' THEN '001'  
                                WHEN BU='AH' THEN '003' 
                                WHEN BU='ZPBL' THEN '004' 
                                WHEN BU='NHL' THEN '005' 
                                WHEN BU='GHL' THEN '006' 
                                WHEN BU='UBL' THEN '007' 
                                WHEN BU='DBL' THEN '011' 
                                WHEN BU='DBH' THEN '012' 
                                ELSE '008' 
                            END
                        FROM (
                            SELECT OrderID,CustID,Count(Distinct ProdID) cnt 
                            FROM OMS_OrderDetail_Process
                            GROUP BY OrderID,CustID) a 
                        WHERE OMS_OrderInfo_Process.OrderID=a.OrderID 
                            AND OMS_OrderInfo_Process.CustID=a.CustID 
                            AND OMS_OrderInfo_Process.Status=4");

                        // Additional logic for inserting into OMS_OrderInfo and OMS_OrderDetail
                        await _dbContext.Database.ExecuteSqlRawAsync("INSERT INTO OMS_OrderInfo SELECT * FROM OMS_OrderInfo_Process WHERE Status=4");
                        await _dbContext.Database.ExecuteSqlRawAsync("INSERT INTO OMS_OrderDetail SELECT * FROM OMS_OrderDetail_Process WHERE OrderID IN (SELECT OrderID FROM OMS_OrderInfo_Process WHERE Status=4)");

                        // Additional logic for deleting from OMS_OrderDetail_Process and OMS_OrderInfo_Process
                        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM OMS_OrderDetail_Process WHERE OrderID IN (SELECT OrderID FROM OMS_OrderInfo_Process WHERE Status=4)");
                        await _dbContext.Database.ExecuteSqlRawAsync("DELETE FROM OMS_OrderInfo_Process WHERE Status=4");

                        // Commit transaction
                        transB.Commit();

                        Console.WriteLine($"Execution Completed ##ProcessCodeConversion## at {DateTime.Now}");
                    }
                    catch (Exception ex)
                    {
                        // Rollback transaction on exception
                        transB.Rollback();
                        Console.WriteLine($"Error: {ex}");
                    }
                }

                transA.Commit();
            }
            catch (Exception ex)
            {
                transA.Rollback();
                Console.WriteLine($"Error: {ex}");
            }
        }


    }

}

public class OMS_OrderInfo_Process
{
    public int OrderID { get; set; }
    public string? CustID { get; set; }
    public string? MobileNo { get; set; }
    public string? CustPO { get; set; }
    public string? Branch { get; set; }
    public string? BU { get; set; }
    public string? RouteCode { get; set; }
    public string? RouteName { get; set; }
    public string? SalesType { get; set; }
    public int LineCount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public short Status { get; set; }
}
