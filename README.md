# CSV Reconciliation Tool

A multithreaded .NET 8 tool that compares CSV files between two folders and generates a reconciliation report showing matched and unmatched records. 
File pairs are processed concurrently using multithreading, streamed in chunks to handle large files and aggregated using worker threads.

## Installation

```bash
git clone https://github.com/endritgjocaj/CSVReconciliationTool.git
cd CSVReconciliationTool
dotnet restore
dotnet build
```

## Usage

```bash
dotnet run --project src/CSVReconciliationTool.Console -- <config-path> <folderA> <folderB> <output-folder>
```

Example:

```bash
dotnet run --project src/CSVReconciliationTool.Console -- samples/config-orders.json ./samples/folderA ./samples/folderB ./output
```

## Architecture

1. JSON config is parsed to determine matching fields and options
2. Both folders are scanned and files are paired by filename
3. CSV files are streamed in chunks of 1000 rows — never fully loaded into memory
4. Chunks are processed in parallel by worker threads using thread-safe collections
5. All file pairs run concurrently up to the configured parallelism limit
6. Output files and global summary are written on completion

## Output

```
output/
├── Orders/
│   ├── matched.csv
│   ├── only-in-folderA.csv
│   ├── only-in-folderB.csv
│   ├── reconcile-summary.json
│   └── errors.csv (only if malformed rows exist)
├── global-summary.json
└── csv-reconciliation.log
```

- `matched.csv` — records found in both files
- `only-in-folderA.csv` — records only in FolderA
- `only-in-folderB.csv` — records only in FolderB
- `reconcile-summary.json` — totals per file pair
- `errors.csv` — malformed rows that could not be parsed
- `global-summary.json` — aggregate totals across all file pairs
- `csv-reconciliation.log` — timestamped log, warnings and final summary

## Configuration

```json
{
  "folderA": "./samples/folderA",
  "folderB": "./samples/folderB",
  "outputFolder": "./output",
  "matchingRule": {
    "matchingFields": ["OrderId"],
    "caseSensitive": false,
    "trim": true
  },
  "degreeOfParallelism": 4,
  "separator": ",",
  "hasHeaderRow": true
}
```

- `folderA` / `folderB` - paths to the two input folders
- `outputFolder` - where output files will be saved
- `matchingRule.matchingFields` - columns used to match records (supports composite keys)
- `matchingRule.caseSensitive` - case-sensitive matching (default: false)
- `matchingRule.trim` - trim whitespace before comparing
- `degreeOfParallelism` - how many file pairs to process at once
- `separator` - CSV delimiter character
- `hasHeaderRow` - whether the first row is a header

## Testing

```bash
dotnet test
```

18 unit and integration tests covering:

- JSON config parsing
- Field normalization (case, trim)
- Small-file reconciliation correctness
- Integration test with 5 file pairs asserting expected output

## Error Handling

- Malformed rows (wrong column count) are logged with line number and written to `errors.csv`
- If a file exists in one folder but not the other, it is reported as missing and skipped
- If CSV headers differ between files, missing columns are filled with empty values in the output
- All errors and warnings are written to `csv-reconciliation.log` with timestamps