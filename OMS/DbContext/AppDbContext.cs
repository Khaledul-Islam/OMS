﻿using System.Data;
using Microsoft.EntityFrameworkCore;

namespace OMS.DbContext;

public class AppDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public async Task ExecuteStoredProcedureAsync(string storedProcedureName)
    {
        await using var command = Database.GetDbConnection().CreateCommand();
        command.CommandText = storedProcedureName;
        command.CommandType = CommandType.StoredProcedure;

        if (command.Connection.State != ConnectionState.Open)
        {
            await command.Connection.OpenAsync();
        }

        await command.ExecuteNonQueryAsync();
    }
}