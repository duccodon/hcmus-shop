using hcmus_shop.Contracts.Services;
using hcmus_shop.Models.Common;
using hcmus_shop.Services.Products.Dto;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace hcmus_shop.Services.Products
{
    public class ProductImportService : IProductImportService
    {
        private readonly IBrandService _brandService;
        private readonly ICategoryService _categoryService;
        private readonly IProductService _productService;

        public ProductImportService(
            IBrandService brandService,
            ICategoryService categoryService,
            IProductService productService)
        {
            _brandService = brandService;
            _categoryService = categoryService;
            _productService = productService;
        }

        public async Task<Result<ProductImportSummary>> ImportAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return Result<ProductImportSummary>.Failure("Excel file not found.");
                }

                var rows = ReadRows(filePath);
                if (rows.Count == 0)
                {
                    return Result<ProductImportSummary>.Failure("The Excel file is empty.");
                }

                var brandsResult = await _brandService.GetAllAsync();
                if (!brandsResult.IsSuccess || brandsResult.Value is null)
                {
                    return Result<ProductImportSummary>.Failure(brandsResult.Error ?? "Failed to load brands.");
                }

                var categoriesResult = await _categoryService.GetAllAsync();
                if (!categoriesResult.IsSuccess || categoriesResult.Value is null)
                {
                    return Result<ProductImportSummary>.Failure(categoriesResult.Error ?? "Failed to load categories.");
                }

                var brandMap = brandsResult.Value.ToDictionary(brand => brand.Name, brand => brand.BrandId, StringComparer.OrdinalIgnoreCase);
                var categoryMap = categoriesResult.Value.ToDictionary(category => category.Name, category => category.CategoryId, StringComparer.OrdinalIgnoreCase);

                var summary = new ProductImportSummary();
                var rowNumber = 1;

                foreach (var row in rows)
                {
                    rowNumber++;

                    try
                    {
                        var brandId = await ResolveBrandIdAsync(row.Brand, brandMap);
                        var categoryIds = await ResolveCategoryIdsAsync(row.Categories, categoryMap);

                        if (row.ImageUrls.Count < 3)
                        {
                            throw new InvalidOperationException("At least 3 image URLs are required.");
                        }

                        var createResult = await _productService.CreateAsync(new CreateProductInput
                        {
                            Sku = row.Sku,
                            Name = row.Name,
                            BrandId = brandId,
                            ImportPrice = row.ImportPrice,
                            SellingPrice = row.SellingPrice,
                            StockQuantity = row.StockQuantity,
                            WarrantyMonths = row.WarrantyMonths,
                            Description = row.Description,
                            Specifications = row.Specifications,
                            CategoryIds = categoryIds,
                            ImageUrls = row.ImageUrls
                        });

                        if (!createResult.IsSuccess)
                        {
                            throw new InvalidOperationException(createResult.Error ?? "Failed to create product.");
                        }

                        summary.ImportedCount++;
                    }
                    catch (Exception ex)
                    {
                        summary.FailedCount++;
                        summary.Messages.Add($"Row {rowNumber}: {ex.Message}");
                    }
                }

                if (summary.ImportedCount == 0 && summary.FailedCount > 0)
                {
                    return Result<ProductImportSummary>.Failure(string.Join(Environment.NewLine, summary.Messages.Take(5)));
                }

                if (summary.Messages.Count == 0)
                {
                    summary.Messages.Add($"Imported {summary.ImportedCount} products successfully.");
                }

                return Result<ProductImportSummary>.Success(summary);
            }
            catch (Exception ex)
            {
                return Result<ProductImportSummary>.Failure(ex.Message);
            }
        }

        private async Task<int> ResolveBrandIdAsync(string brandName, Dictionary<string, int> brandMap)
        {
            if (brandMap.TryGetValue(brandName, out var existingId))
            {
                return existingId;
            }

            var createResult = await _brandService.CreateAsync(brandName);
            if (!createResult.IsSuccess || createResult.Value is null)
            {
                throw new InvalidOperationException(createResult.Error ?? $"Failed to create brand '{brandName}'.");
            }

            brandMap[brandName] = createResult.Value.BrandId;
            return createResult.Value.BrandId;
        }

        private async Task<List<int>> ResolveCategoryIdsAsync(List<string> categoryNames, Dictionary<string, int> categoryMap)
        {
            var ids = new List<int>();
            foreach (var categoryName in categoryNames)
            {
                if (categoryMap.TryGetValue(categoryName, out var existingId))
                {
                    ids.Add(existingId);
                    continue;
                }

                var createResult = await _categoryService.CreateAsync(categoryName);
                if (!createResult.IsSuccess || createResult.Value is null)
                {
                    throw new InvalidOperationException(createResult.Error ?? $"Failed to create category '{categoryName}'.");
                }

                categoryMap[categoryName] = createResult.Value.CategoryId;
                ids.Add(createResult.Value.CategoryId);
            }

            return ids;
        }

        private static List<ImportedProductRow> ReadRows(string filePath)
        {
            using var archive = ZipFile.OpenRead(filePath);
            var sharedStrings = ReadSharedStrings(archive);
            var sheetEntry = archive.GetEntry("xl/worksheets/sheet1.xml")
                ?? throw new InvalidOperationException("The workbook does not contain sheet1.");

            using var stream = sheetEntry.Open();
            var document = XDocument.Load(stream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            var rows = document.Descendants(ns + "row").ToList();
            if (rows.Count == 0)
            {
                return [];
            }

            var headers = ReadHeaderRow(rows[0], ns, sharedStrings);
            var result = new List<ImportedProductRow>();
            foreach (var row in rows.Skip(1))
            {
                var values = ReadRowValues(row, ns, sharedStrings);
                if (values.Values.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                result.Add(ParseRow(headers, values));
            }

            return result;
        }

        private static Dictionary<int, string> ReadHeaderRow(XElement row, XNamespace ns, List<string> sharedStrings)
        {
            var values = ReadRowValues(row, ns, sharedStrings);
            return values.ToDictionary(
                pair => pair.Key,
                pair => NormalizeHeader(pair.Value),
                EqualityComparer<int>.Default);
        }

        private static Dictionary<int, string> ReadRowValues(XElement row, XNamespace ns, List<string> sharedStrings)
        {
            var values = new Dictionary<int, string>();
            foreach (var cell in row.Elements(ns + "c"))
            {
                var reference = cell.Attribute("r")?.Value ?? string.Empty;
                var columnIndex = GetColumnIndex(reference);
                values[columnIndex] = GetCellValue(cell, ns, sharedStrings);
            }

            return values;
        }

        private static ImportedProductRow ParseRow(Dictionary<int, string> headers, Dictionary<int, string> values)
        {
            string GetValue(string header)
            {
                var match = headers.FirstOrDefault(pair => pair.Value == header);
                return match.Equals(default(KeyValuePair<int, string>))
                    ? string.Empty
                    : values.TryGetValue(match.Key, out var value) ? value.Trim() : string.Empty;
            }

            return new ImportedProductRow
            {
                Sku = RequireValue(GetValue("sku"), "SKU is required."),
                Name = RequireValue(GetValue("name"), "Name is required."),
                Brand = RequireValue(GetValue("brand"), "Brand is required."),
                ImportPrice = ParseDouble(GetValue("importprice"), "ImportPrice is invalid."),
                SellingPrice = ParseDouble(GetValue("sellingprice"), "SellingPrice is invalid."),
                StockQuantity = ParseInt(GetValue("stockquantity"), 0),
                WarrantyMonths = ParseInt(GetValue("warrantymonths"), 12),
                Description = EmptyToNull(GetValue("description")),
                Specifications = EmptyToNull(GetValue("specifications")),
                Categories = SplitMultiValue(GetValue("categories")),
                ImageUrls = SplitMultiValue(GetValue("imageurls"))
            };
        }

        private static List<string> ReadSharedStrings(ZipArchive archive)
        {
            var entry = archive.GetEntry("xl/sharedStrings.xml");
            if (entry is null)
            {
                return [];
            }

            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

            return document.Descendants(ns + "si")
                .Select(item => string.Concat(item.Descendants(ns + "t").Select(text => text.Value)))
                .ToList();
        }

        private static string GetCellValue(XElement cell, XNamespace ns, List<string> sharedStrings)
        {
            var type = cell.Attribute("t")?.Value;
            var value = cell.Element(ns + "v")?.Value ?? string.Empty;
            var inline = cell.Element(ns + "is")?.Element(ns + "t")?.Value;

            if (!string.IsNullOrWhiteSpace(inline))
            {
                return inline;
            }

            if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(value, out var sharedIndex)
                && sharedIndex >= 0
                && sharedIndex < sharedStrings.Count)
            {
                return sharedStrings[sharedIndex];
            }

            return value;
        }

        private static int GetColumnIndex(string cellReference)
        {
            var letters = new string(cellReference.TakeWhile(char.IsLetter).ToArray());
            var sum = 0;
            foreach (var ch in letters)
            {
                sum *= 26;
                sum += char.ToUpperInvariant(ch) - 'A' + 1;
            }

            return Math.Max(sum - 1, 0);
        }

        private static string NormalizeHeader(string header)
        {
            return new string(header.Trim().ToLowerInvariant().Where(ch => !char.IsWhiteSpace(ch) && ch != '_' && ch != '-').ToArray());
        }

        private static string RequireValue(string value, string error)
        {
            return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException(error);
        }

        private static double ParseDouble(string value, string error)
        {
            if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
            {
                throw new InvalidOperationException(error);
            }

            return parsed;
        }

        private static int ParseInt(string value, int defaultValue)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : defaultValue;
        }

        private static string? EmptyToNull(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        private static List<string> SplitMultiValue(string value)
        {
            return value
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(item => item.Trim())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private sealed class ImportedProductRow
        {
            public string Sku { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Brand { get; set; } = string.Empty;
            public double ImportPrice { get; set; }
            public double SellingPrice { get; set; }
            public int StockQuantity { get; set; }
            public int WarrantyMonths { get; set; }
            public string? Description { get; set; }
            public string? Specifications { get; set; }
            public List<string> Categories { get; set; } = new();
            public List<string> ImageUrls { get; set; } = new();
        }
    }
}
