using ClosedXML.Excel;
using System.Net.Http.Json;
using System.Text;
using Microsoft.JSInterop;

namespace LeloPage.Services
{
    public class YahooConverterService
    {
        private static readonly string[] CsvHeaders = new[]
        {
            "Symbol", "Current Price", "Date", "Time", "Change", "Open", "High", "Low", "Volume",
            "Trade Date", "Purchase Price", "Quantity", "Commission", "High Limit", "Low Limit", "Comment", "Transaction Type"
        };

        private readonly HttpClient _http;
        private readonly IConfiguration _config;
        private readonly IJSRuntime _js;

        public YahooConverterService(HttpClient http, IConfiguration config, IJSRuntime js)
        {
            _http = http;
            _config = config;
            _js = js;
        }

        private string ApiUrl => (_config["ApiUrl"] ?? "https://lelopage.eu/api") + "/ticker_map.php";

        public async Task<(string Message, byte[]? CsvData, string FileName)> ConvertAsync(Stream fileStream, string originalFileName)
        {
            try
            {
                if (!originalFileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    return ("Selected file is not .xlsx", null, "");

                using var workbook = new XLWorkbook(fileStream);
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null)
                    return ("No worksheet found in the workbook.", null, "");

                var usedRange = worksheet.RangeUsed();
                if (usedRange == null)
                    return ("No data found in the worksheet.", null, "");

                // Build header map (column name -> column index)
                var headerRow = worksheet.Row(1);
                var headerMap = new Dictionary<string, int>();
                foreach (var cell in headerRow.CellsUsed())
                {
                    var headerValue = cell.Value;
                    var header = headerValue.IsBlank ? "" : headerValue.ToString().Trim();
                    if (!string.IsNullOrEmpty(header) && !headerMap.ContainsKey(header))
                    {
                        headerMap[header] = cell.Address.ColumnNumber;
                    }
                }

                var dateTimeColIndex = FindDateTimeColumn(headerMap);
                var csvLines = new List<string>();
                
                // Add CSV headers
                csvLines.Add(string.Join(",", CsvHeaders.Select(h => $"\"{h}\"")));

                var tickerCache = new Dictionary<string, string?>();
                var matched = 0;

                // Process data rows
                var lastRowNum = usedRange.LastRowUsed().RowNumber();
                for (int rowNum = 2; rowNum <= lastRowNum; rowNum++)
                {
                    var row = worksheet.Row(rowNum);

                    // Get symbol from column C (3)
                    var symbolCell = worksheet.Cell(rowNum, 3);
                    var symbolValue = symbolCell.Value;
                    var symbolFromC = symbolValue.IsBlank ? "" : symbolValue.ToString().Trim();
                    
                    if (string.IsNullOrEmpty(symbolFromC))
                        continue;

                    // Get transaction type from column E (5)
                    var typeCell = worksheet.Cell(rowNum, 5);
                    var typeValue = typeCell.Value;
                    var transactionType = typeValue.IsBlank ? "" : typeValue.ToString().Trim();

                    if (string.IsNullOrEmpty(transactionType) || !transactionType.Equals("BUY", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var originalTicker = ExtractTickerFromSymbol(symbolFromC);
                    if (string.IsNullOrEmpty(originalTicker))
                        continue;

                    var resolvedTicker = await ResolveTickerAsync(originalTicker, tickerCache);
                    if (string.IsNullOrEmpty(resolvedTicker))
                        continue;

                    var outVals = new List<string>();
                    foreach (var colName in CsvHeaders)
                    {
                        string cellText = colName switch
                        {
                            "Symbol" => resolvedTicker,
                            "Transaction Type" => transactionType,
                            _ => GetCellText(worksheet, rowNum, colName, headerMap, dateTimeColIndex)
                        };

                        cellText = (cellText ?? "").Replace("\"", "\"\"");
                        outVals.Add($"\"{cellText}\"");
                    }

                    csvLines.Add(string.Join(",", outVals));
                    matched++;
                }

                if (matched == 0)
                    return ("No matching rows (0) - no CSV generated.", null, "");

                var csvContent = string.Join("\n", csvLines);
                var csvBytes = Encoding.UTF8.GetBytes(csvContent);
                var outputFileName = Path.ChangeExtension(originalFileName, ".csv");

                return ($"Converted '{originalFileName}' -> '{outputFileName}' ({matched} rows)", csvBytes, outputFileName);
            }
            catch (OperationCanceledException)
            {
                return ("Conversion canceled.", null, "");
            }
            catch (Exception ex)
            {
                return ($"Conversion failed: {ex.Message}", null, "");
            }
        }

        private int? FindDateTimeColumn(Dictionary<string, int> headerMap)
        {
            var candidates = new[] { "DateTime", "Date/Time", "Date Time", "DateTimeUTC", "Timestamp" };
            foreach (var candidate in candidates)
            {
                if (headerMap.TryGetValue(candidate, out var colIndex))
                    return colIndex;
            }
            return null;
        }

        private string GetCellText(IXLWorksheet worksheet, int rowNum, string colName, Dictionary<string, int> headerMap, int? dateTimeColIndex)
        {
            if (headerMap.TryGetValue(colName, out var colIdx))
            {
                var cell = worksheet.Cell(rowNum, colIdx);
                var value = cell.Value;
                
                if (value.IsBlank)
                    return "";

                if (cell.DataType == XLDataType.DateTime)
                {
                    try
                    {
                        var dateTime = (DateTime)value;
                        return colName == "Date" ? dateTime.ToString("yyyy-MM-dd") : dateTime.ToString("HH:mm:ss");
                    }
                    catch
                    {
                        return value.ToString();
                    }
                }

                return value.ToString();
            }

            if ((colName == "Date" || colName == "Time") && dateTimeColIndex.HasValue)
            {
                var cell = worksheet.Cell(rowNum, dateTimeColIndex.Value);
                var value = cell.Value;
                
                if (value.IsBlank)
                    return "";

                if (cell.DataType == XLDataType.DateTime)
                {
                    try
                    {
                        var dateTime = (DateTime)value;
                        return colName == "Date" ? dateTime.ToString("yyyy-MM-dd") : dateTime.ToString("HH:mm:ss");
                    }
                    catch
                    {
                        return "";
                    }
                }

                if (DateTime.TryParse(value.ToString(), out var parsedDt))
                {
                    return colName == "Date" ? parsedDt.ToString("yyyy-MM-dd") : parsedDt.ToString("HH:mm:ss");
                }
            }

            return "";
        }

        private string ExtractTickerFromSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return "";

            var dotIndex = symbol.IndexOf('.');
            return dotIndex > 0 ? symbol.Substring(0, dotIndex) : symbol;
        }

        private async Task<string?> ResolveTickerAsync(string ticker, Dictionary<string, string?> cache)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                return null;

            var normalizedTicker = ticker.Trim();
            if (cache.TryGetValue(normalizedTicker, out var cachedTicker))
                return cachedTicker;

            try
            {
                var mappedTicker = await LookupTickerInDatabaseAsync(normalizedTicker);
                if (!string.IsNullOrWhiteSpace(mappedTicker))
                {
                    cache[normalizedTicker] = mappedTicker;
                    return mappedTicker;
                }

                var userInput = await _js.InvokeAsync<string?>("showYahooTickerPrompt", $"Can you provide ticker for {normalizedTicker}  in yahoo?");
                if (string.Equals(userInput, "__CLOSE_CONVERSION__", StringComparison.Ordinal))
                    throw new OperationCanceledException();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    cache[normalizedTicker] = null;
                    return null;
                }

                var cleaned = userInput.Trim();
                if (string.IsNullOrWhiteSpace(cleaned))
                {
                    cache[normalizedTicker] = null;
                    return null;
                }

                var saved = await SaveTickerMappingAsync(normalizedTicker, cleaned);
                if (!saved)
                {
                    cache[normalizedTicker] = null;
                    return null;
                }

                cache[normalizedTicker] = cleaned;
                return cleaned;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ticker resolution error: {ex.Message}");
                cache[normalizedTicker] = null;
                return null;
            }
        }

        private async Task<string?> LookupTickerInDatabaseAsync(string usedTicker)
        {
            try
            {
                var response = await _http.GetAsync($"{ApiUrl}?usedTicker={Uri.EscapeDataString(usedTicker)}");
                if (!response.IsSuccessStatusCode)
                    return null;

                var payload = await response.Content.ReadFromJsonAsync<TickerMappingResponse>();
                return payload?.YahooTicker;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ticker lookup error: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> SaveTickerMappingAsync(string usedTicker, string yahooTicker)
        {
            try
            {
                var response = await _http.PostAsJsonAsync(ApiUrl, new
                {
                    usedTicker,
                    yahooTicker
                });

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ticker save error: {ex.Message}");
                return false;
            }
        }

        private sealed class TickerMappingResponse
        {
            public string? UsedTicker { get; set; }
            public string? YahooTicker { get; set; }
        }
    }
}
