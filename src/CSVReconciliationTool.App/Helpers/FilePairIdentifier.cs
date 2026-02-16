namespace CSVReconciliationTool.App.Helpers;

/// <summary>
/// Identifies the paths of the CSV files that will be compared.
/// </summary>
public static class FilePairIdentifier
{
    public static Dictionary<string, Tuple<string?, string?>> IdentifyFilePairs(
        string folderAPath,
        string folderBPath,
        List<string> folderAFiles,
        List<string> folderBFiles)
    {
        var pairs = new Dictionary<string, Tuple<string?, string?>>();

        // Process all files from FolderA
        foreach (var file in folderAFiles)
        {
            // For each file in FolderA, create a pair with matching file in FolderB (if exists)
            // eg. "Employees.csv" -> "Employees", pair = ("folderA/Employees.csv", "folderB/Employees.csv")
            // If it does not in FolderB -> pair = ("folderA/Products.csv", null)
            var baseName = Path.GetFileNameWithoutExtension(file);
            var pathA = Path.Combine(folderAPath, file);
            var pathB = folderBFiles.Contains(file) ? Path.Combine(folderBPath, file) : null;
            pairs[baseName] = new Tuple<string?, string?>(pathA, pathB);
            // eg. "employees", ("folderA/employees.csv", "folderB/employees.csv")
        }

        // Add remaining files from FolderB that weren't already paired
        // eg. If "Customer.csv" exists only in FolderB -> pair = (null, "folderB/Customer.csv")
        foreach (var file in folderBFiles)
        {
            var baseName = Path.GetFileNameWithoutExtension(file);
            if (!pairs.ContainsKey(baseName))
            {
                var pathB = Path.Combine(folderBPath, file);
                pairs[baseName] = new Tuple<string?, string?>(null, pathB);
            }
        }

        return pairs;
    }
}
