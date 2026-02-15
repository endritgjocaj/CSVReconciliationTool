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

            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Information));

            // Infrastructure services (with interfaces for testability)
            services.AddSingleton<ICsvService>(sp => new CsvService(config.Separator, config.HasHeaderRow));
            services.AddSingleton<IOutputWriter, OutputWriter>();

            // Business services
            services.AddSingleton<IMatchingService>(sp => new MatchingService(config.MatchingRule));
            services.AddSingleton<RecordCategorizer>();

            // Helpers
            services.AddSingleton<SummaryReporter>();

            // Application services
            services.AddSingleton<FilePairProcessor>();
            services.AddSingleton<IReconciliationService, ReconciliationService>();

            var serviceProvider = services.BuildServiceProvider();
            var engine = serviceProvider.GetRequiredService<IReconciliationService>();

            var result = await engine.ReconcileAsync(config);

            if (result.FailedPairs > 0)
                Environment.Exit(1);

            await serviceProvider.DisposeAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}