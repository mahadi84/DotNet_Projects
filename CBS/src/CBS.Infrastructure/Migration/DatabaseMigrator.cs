using DbUp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CBS.Infrastructure.Migration;

public static class DatabaseMigrator
{
    public static void Migrate(string connectionString)
    {
        // গুরুত্বপূর্ণ: এখানে Assembly.GetExecutingAssembly() ব্যবহার করায় 
        // এটি Infrastructure লেয়ারের Embedded Scripts গুলো খুঁজে পাবে।
        var upgrader = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        if (upgrader.IsUpgradeRequired())
        {
            var result = upgrader.PerformUpgrade();
            //if (!result.Successful)
            //{
            //    throw new Exception($"Database Migration Failed: {result.Error}");
            //}
            //
            if (!result.Successful)
            {
                // explicitly print the error to the console before throwing
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("SQL Error: " + result.Error.Message);
                if (result.Error.InnerException != null)
                    Console.WriteLine("Inner Error: " + result.Error.InnerException.Message);
                Console.ResetColor();

                throw result.Error; // Throw the original error to preserve the full stack trace
            }
        }
    }
}
