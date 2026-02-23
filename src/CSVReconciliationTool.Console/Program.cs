using CSVReconciliationTool.App.Helpers;
using CSVReconciliationTool.App.Infrastructure;
using CSVReconciliationTool.App.Interfaces;
using CSVReconciliationTool.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal class Program
{
    private static async Task Main(string[] args)
    {
        if (args.Length is not 4)
        {
            Console.WriteLine($"Error: Expected 4 arguments, but received {args.Length}");
            Console.WriteLine("Usage: CSVReconciliationTool <config-path> <folderA> <folderB> <output-folder>");
            Environment.Exit(1);
        }

        try
        {
            var config = ConfigurationLoader.Load(args[0]);
            config.FolderA = Path.GetFullPath(args[1]);
            config.FolderB = Path.GetFullPath(args[2]);
            config.OutputFolder = Path.GetFullPath(args[3]);

            // Create output directory and setup log file
            Directory.CreateDirectory(config.OutputFolder);
            var logFilePath = Path.Combine(config.OutputFolder, "csv-reconciliation.log");

            var services = new ServiceCollection();
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddFile(logFilePath);
                builder.SetMinimumLevel(LogLevel.Information);
            });

            // Infrastructure services (with interfaces for testability)
            services.AddSingleton<ICsvService>(sp => new CsvService(
                config.Separator, 
                config.HasHeaderRow, 
                sp.GetRequiredService<ILogger<CsvService>>()));
            services.AddSingleton<IOutputWriter, OutputWriter>();

            // Business services
            services.AddSingleton<IMatchingService>(sp => new MatchingService(config.MatchingRule));

            // Helpers
            services.AddSingleton<SummaryReporter>();

            // Application services
            services.AddSingleton<IFilePairProcessor, FilePairProcessor>();
            services.AddSingleton<IReconciliationService, ReconciliationService>();

            var serviceProvider = services.BuildServiceProvider();
            var engine = serviceProvider.GetRequiredService<IReconciliationService>();

            var result = await engine.ReconcileAsync(config);
            await serviceProvider.DisposeAsync();

            if (result.FailedPairs > 0)
                Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}