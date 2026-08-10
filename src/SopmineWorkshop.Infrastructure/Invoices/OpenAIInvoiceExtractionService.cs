using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using SopmineWorkshop.Domain.Common.Results;
using SopmineWorkshop.Domain.Invoices;

namespace SopmineWorkshop.Infrastructure.Invoices;

public sealed class OpenAIInvoiceExtractionService(
    IConfiguration configuration,
    ILogger<OpenAIInvoiceExtractionService> logger) : IInvoiceExtractionService
{
    private const int MaxInlineImageSizeInBytes = 8 * 1024 * 1024;
    private const string DefaultBaseUrl = "https://api.openai.com/v1/chat/completions";
    private const string DefaultModel = "gpt-4o-mini";

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IConfiguration _configuration = configuration;
    private readonly ILogger<OpenAIInvoiceExtractionService> _logger = logger;

    public async Task<Result<InvoiceExtractionDto>> ExtractFromImageAsync(
        byte[] imageBytes,
        string contentType,
        string? fileName,
        CancellationToken ct = default)
    {
        if (imageBytes is null || imageBytes.Length == 0)
        {
            return InvoiceExtractionErrors.ImageRequired;
        }

        if (string.IsNullOrWhiteSpace(contentType) ||
            !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return InvoiceExtractionErrors.ImageTypeInvalid;
        }

        if (imageBytes.Length > MaxInlineImageSizeInBytes)
        {
            return InvoiceExtractionErrors.ImageTooLarge;
        }

        var settings = GetSettings();

        if (settings is null)
        {
            _logger.LogWarning("OpenAI invoice extraction is not configured.");
            return InvoiceExtractionErrors.ServiceNotConfigured;
        }

        var requestUri = BuildRequestUri(settings);
        var requestBody = CreateRequestBody(
            settings.Model,
            Convert.ToBase64String(imageBytes),
            contentType);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody, JsonOptions),
            Encoding.UTF8,
            "application/json");

        try
        {
            using var response = await HttpClient.SendAsync(request, ct);
            var responseBody = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OpenAI invoice extraction failed. StatusCode: {StatusCode}, FileName: {FileName}, Response: {Response}",
                    response.StatusCode,
                    fileName,
                    responseBody);

                return MapOpenAIError(response.StatusCode, responseBody);
            }

            return ParseOpenAIResponse(responseBody);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI invoice extraction failed unexpectedly. FileName: {FileName}", fileName);
            return InvoiceExtractionErrors.ServiceUnavailable;
        }
    }

    private OpenAISettings? GetSettings()
    {
        var section = _configuration.GetSection("OpenAI");
        var apiKey = section["ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            apiKey = _configuration["OPENAI_API_KEY"];
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return null;
        }

        var model = section["Model"];
        var baseUrl = section["BaseUrl"];

        return new OpenAISettings(
            apiKey.Trim(),
            string.IsNullOrWhiteSpace(model) ? DefaultModel : model.Trim(),
            string.IsNullOrWhiteSpace(baseUrl) ? DefaultBaseUrl : baseUrl.Trim());
    }

    private static Uri BuildRequestUri(OpenAISettings settings)
    {
        return new Uri(settings.BaseUrl.Trim());
    }

    private static object CreateRequestBody(string model, string base64Image, string contentType)
    {
        return new
        {
            model,
            temperature = 0,
            response_format = new
            {
                type = "json_object"
            },
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = CreatePrompt()
                },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "text",
                            text = "Extract the supplier invoice data from the attached image."
                        },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:{contentType};base64,{base64Image}",
                                detail = "high"
                            }
                        }
                    }
                }
            }
        };
    }

    private static string CreatePrompt()
    {
        return """
Extract data from this supplier invoice image for an Achat form.
Return only valid JSON with this exact shape:
{
  "supplierName": "seller/supplier legal name or null",
  "supplierICE": "seller/supplier ICE number or null",
  "supplierAddress": "seller/supplier address from header/footer/stamp or null",
  "supplierCity": "seller/supplier city or null",
  "supplierPhone": "seller/supplier phone or null",
  "supplierEmail": "seller/supplier email or null",
  "supplierWebsite": "seller/supplier website or null",
  "date": "yyyy-MM-dd or null",
  "reference": "invoice reference or null",
  "totalHT": 0,
  "totalTVA": 0,
  "totalTTC": 0,
  "lineItems": [
    {
      "productReference": "article code/reference or null",
      "product": "product designation or null",
      "productFamily": null,
      "productUnit": "unit printed on the line, like PCE, KG, M, L, or null",
      "quantity": 0,
      "unitPriceHT": 0,
      "unitPriceTTC": 0,
      "tva": 20,
      "amountHT": 0,
      "amountTTC": 0,
      "priceIncludesTax": false
    }
  ]
}
Rules:
- Do not invent missing values. Use null when a value is not visible.
- Use decimal numbers with a dot as separator.
- This is a Moroccan supplier invoice for SOPMINE. SOPMINE in a boxed customer/client area is the buyer, not the supplier. Ignore buyer/customer ICE when filling supplier fields.
- The supplier is the seller printed in the logo/header/footer/stamp, for example BIG SANY DISTRIBUTION or A.t.l PLAST. Supplier address, city, phone, email, website, ICE, RC, IF, CNSS and patent information normally appear in the footer or stamp and belong to the supplier unless they are inside the customer box.
- If the invoice has a customer/buyer box labeled SOPMINE, STE SOPMINE, or SOPMINE details, do not copy those values into supplier fields.
- If the table header says P.U. TTC, Prix TTC, Montant TTC, set priceIncludesTax true and fill TTC fields. Also calculate HT fields when TVA is visible.
- If the table header says P.U NET HT, P.U HT, Prix HT, Montant HT, set priceIncludesTax false and fill HT fields. Also calculate TTC fields when TVA is visible.
- Extract invoice-level totals from summary boxes: HT, TVA, TTC.
- For TVA, prefer the line TVA column. Otherwise use the invoice-level TVA rate when visible.
- Keep product labels exactly as printed when possible.
- Do not invent productFamily; keep it null unless the invoice clearly prints a family/category.
- Extract productUnit only when a unit column or unit text is visible on the article line.
- Use a separate productReference only when there is a clear Code Article / reference column or a clear article code. Otherwise keep the full text in product.
""";
    }

    private Result<InvoiceExtractionDto> ParseOpenAIResponse(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return InvoiceExtractionErrors.EmptyResponse;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var text = ReadFirstTextPart(document.RootElement);

            if (string.IsNullOrWhiteSpace(text))
            {
                _logger.LogWarning("OpenAI invoice extraction returned no text part. Response: {Response}", responseBody);
                return InvoiceExtractionErrors.EmptyResponse;
            }

            return ParseExtractionJson(text);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "OpenAI invoice extraction returned invalid JSON envelope.");
            return InvoiceExtractionErrors.InvalidResponse;
        }
    }

    private static string? ReadFirstTextPart(JsonElement root)
    {
        if (!TryGetProperty(root, "choices", out var choices) ||
            choices.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var choice in choices.EnumerateArray())
        {
            if (!TryGetProperty(choice, "message", out var message))
            {
                continue;
            }

            var text = ReadString(message, "content");

            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return null;
    }

    private static Result<InvoiceExtractionDto> ParseExtractionJson(string rawText)
    {
        try
        {
            var json = ExtractJsonPayload(rawText);

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var lines = ReadLines(root);
            var totalHT = ReadDecimal(root, "totalHT", "subtotal", "subtotalHT", "montantHT", "amountHT")
                ?? lines.Sum(line => line.AmountHT);
            var totalTVA = ReadDecimal(root, "totalTVA", "taxTotal", "tvaTotal", "montantTVA", "amountTVA")
                ?? lines.Sum(line => line.AmountTTC - line.AmountHT);
            var totalTTC = ReadDecimal(root, "totalTTC", "total", "totalAmount", "amountTotal", "montantTTC")
                ?? totalHT + totalTVA;

            var extraction = new InvoiceExtractionDto
            {
                SupplierName = ReadString(root, "supplierName", "supplier", "fournisseur", "fournisseurNom") ?? string.Empty,
                SupplierICE = ReadString(root, "supplierICE", "supplierIce", "ice", "fournisseurICE"),
                SupplierAddress = ReadString(root, "supplierAddress", "address", "adresse", "fournisseurAdresse"),
                SupplierCity = ReadString(root, "supplierCity", "city", "ville", "fournisseurVille"),
                SupplierPhone = ReadString(root, "supplierPhone", "phone", "telephone", "tel", "fournisseurTelephone"),
                SupplierEmail = ReadString(root, "supplierEmail", "email", "mail", "fournisseurEmail"),
                SupplierWebsite = ReadString(root, "supplierWebsite", "website", "site", "web", "fournisseurSite"),
                Date = ReadDate(root, "date", "invoiceDate", "documentDate"),
                Reference = ReadString(root, "reference", "invoiceReference", "documentReference", "numero") ?? string.Empty,
                TotalHT = Math.Round(totalHT, 2, MidpointRounding.AwayFromZero),
                TotalTVA = Math.Round(totalTVA, 2, MidpointRounding.AwayFromZero),
                TotalTTC = Math.Round(totalTTC, 2, MidpointRounding.AwayFromZero),
                Total = Math.Round(totalTTC, 2, MidpointRounding.AwayFromZero),
                Lines = lines
            };

            if (!HasDetectedData(extraction))
            {
                return InvoiceExtractionErrors.NoDataFound;
            }

            return extraction;
        }
        catch (JsonException)
        {
            return InvoiceExtractionErrors.InvalidResponse;
        }
    }

    private static string ExtractJsonPayload(string rawText)
    {
        var text = rawText.Trim();

        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineBreak = text.IndexOf('\n');
            var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);

            if (firstLineBreak >= 0 && lastFence > firstLineBreak)
            {
                text = text[(firstLineBreak + 1)..lastFence].Trim();
            }
        }

        var firstObject = text.IndexOf('{');
        var lastObject = text.LastIndexOf('}');

        if (firstObject >= 0 && lastObject > firstObject)
        {
            text = text[firstObject..(lastObject + 1)];
        }

        return text;
    }

    private static List<InvoiceExtractionLineDto> ReadLines(JsonElement root)
    {
        if (!TryGetProperty(root, "lineItems", out var linesElement) &&
            !TryGetProperty(root, "lines", out linesElement) &&
            !TryGetProperty(root, "items", out linesElement))
        {
            return [];
        }

        if (linesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var lines = new List<InvoiceExtractionLineDto>();

        foreach (var item in linesElement.EnumerateArray())
        {
            var productReference = ReadString(item, "productReference", "reference", "ref") ?? string.Empty;
            var product = ReadString(item, "product", "productName", "designation", "description") ?? string.Empty;
            var productFamily = ReadString(item, "productFamily", "family", "famille") ?? string.Empty;
            var productUnit = ReadString(item, "productUnit", "unit", "unite", "uniteMesure", "measurementUnit") ?? string.Empty;
            var quantity = ReadDecimal(item, "quantity", "qty", "qte") ?? 0;
            var tva = ReadDecimal(item, "tva", "TVA", "taxRate", "vatRate", "tauxTVA") ?? 0;
            var priceIncludesTax = ReadBool(item, "priceIncludesTax", "isTTC", "ttc");
            var visibleUnitPrice = ReadDecimal(item, "unitPrice", "price", "unit_price", "pu", "prixUnitaire");
            var unitPriceHT = ReadDecimal(item, "unitPriceHT", "priceHT", "unitPriceBeforeTax", "prixUnitaireHT");
            var unitPriceTTC = ReadDecimal(item, "unitPriceTTC", "priceTTC", "unitPriceIncludingTax", "prixUnitaireTTC");
            var amountHT = ReadDecimal(item, "amountHT", "lineAmountHT", "montantHT");
            var amountTTC = ReadDecimal(item, "amountTTC", "lineAmountTTC", "montantTTC");

            if (!unitPriceHT.HasValue && amountHT.HasValue && quantity > 0)
            {
                unitPriceHT = amountHT.Value / quantity;
            }

            if (!unitPriceTTC.HasValue && amountTTC.HasValue && quantity > 0)
            {
                unitPriceTTC = amountTTC.Value / quantity;
            }

            if (!unitPriceHT.HasValue && visibleUnitPrice.HasValue && !priceIncludesTax)
            {
                unitPriceHT = visibleUnitPrice.Value;
            }

            if (!unitPriceTTC.HasValue && visibleUnitPrice.HasValue && priceIncludesTax)
            {
                unitPriceTTC = visibleUnitPrice.Value;
            }

            if (!unitPriceHT.HasValue && unitPriceTTC.HasValue)
            {
                unitPriceHT = ConvertTtcToHt(unitPriceTTC.Value, tva);
            }

            if (!unitPriceTTC.HasValue && unitPriceHT.HasValue)
            {
                unitPriceTTC = ConvertHtToTtc(unitPriceHT.Value, tva);
            }

            var safeUnitPriceHT = unitPriceHT ?? 0;
            var safeUnitPriceTTC = unitPriceTTC ?? 0;
            var safeAmountHT = amountHT ?? quantity * safeUnitPriceHT;
            var safeAmountTTC = amountTTC ?? quantity * safeUnitPriceTTC;

            if (string.IsNullOrWhiteSpace(productReference) &&
                string.IsNullOrWhiteSpace(product) &&
                quantity == 0 &&
                safeUnitPriceHT == 0 &&
                safeUnitPriceTTC == 0)
            {
                continue;
            }

            lines.Add(new InvoiceExtractionLineDto
            {
                ProductReference = productReference,
                Product = product,
                ProductFamily = productFamily,
                ProductUnit = productUnit,
                Quantity = quantity,
                Price = safeUnitPriceHT,
                UnitPrice = safeUnitPriceHT,
                UnitPriceHT = safeUnitPriceHT,
                UnitPriceTTC = safeUnitPriceTTC,
                TVA = tva,
                AmountHT = Math.Round(safeAmountHT, 2, MidpointRounding.AwayFromZero),
                AmountTTC = Math.Round(safeAmountTTC, 2, MidpointRounding.AwayFromZero),
                PriceIncludesTax = priceIncludesTax
            });
        }

        return lines;
    }

    private static DateTime? ReadDate(JsonElement element, params string[] propertyNames)
    {
        var value = ReadString(element, propertyNames);

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] formats =
        [
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "d/M/yyyy",
            "dd-MM-yyyy",
            "d-M-yyyy",
            "yyyy/MM/dd"
        ];

        if (DateTime.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var exactDate))
        {
            return exactDate.Date;
        }

        if (DateTime.TryParse(
                value,
                CultureInfo.GetCultureInfo("fr-FR"),
                DateTimeStyles.AssumeLocal,
                out var parsedDate))
        {
            return parsedDate.Date;
        }

        return null;
    }

    private static decimal? ReadDecimal(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(element, propertyName, out var property))
            {
                continue;
            }

            var value = ToDecimal(property);

            if (value.HasValue)
            {
                return value.Value;
            }
        }

        return null;
    }

    private static bool ReadBool(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(element, propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.True)
            {
                return true;
            }

            if (property.ValueKind == JsonValueKind.False)
            {
                return false;
            }

            if (property.ValueKind == JsonValueKind.String &&
                bool.TryParse(property.GetString(), out var value))
            {
                return value;
            }
        }

        return false;
    }

    private static decimal ConvertTtcToHt(decimal amount, decimal tva)
    {
        var divisor = 1 + tva / 100;

        return divisor <= 0 ? amount : amount / divisor;
    }

    private static decimal ConvertHtToTtc(decimal amount, decimal tva)
    {
        return amount * (1 + tva / 100);
    }

    private static decimal? ToDecimal(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var number))
        {
            return number;
        }

        if (element.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var value = element.GetString();

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        value = value
            .Trim()
            .Replace("%", string.Empty)
            .Replace("Dhs", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("DH", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("MAD", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty);

        if (value.Contains(',') && value.Contains('.'))
        {
            value = value.LastIndexOf(',') > value.LastIndexOf('.')
                ? value.Replace(".", string.Empty).Replace(',', '.')
                : value.Replace(",", string.Empty);
        }
        else
        {
            value = value.Replace(',', '.');
        }

        return decimal.TryParse(
            value,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture,
            out var parsedNumber)
            ? parsedNumber
            : null;
    }

    private static string? ReadString(JsonElement element, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (!TryGetProperty(element, propertyName, out var property))
            {
                continue;
            }

            if (property.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (property.ValueKind == JsonValueKind.String)
            {
                var value = property.GetString()?.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }

            if (property.ValueKind == JsonValueKind.Number)
            {
                return property.GetRawText();
            }
        }

        return null;
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var item in element.EnumerateObject())
            {
                if (string.Equals(item.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = item.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private static bool HasDetectedData(InvoiceExtractionDto extraction)
    {
        return !string.IsNullOrWhiteSpace(extraction.SupplierName)
            || !string.IsNullOrWhiteSpace(extraction.SupplierICE)
            || !string.IsNullOrWhiteSpace(extraction.SupplierAddress)
            || !string.IsNullOrWhiteSpace(extraction.SupplierCity)
            || !string.IsNullOrWhiteSpace(extraction.SupplierPhone)
            || !string.IsNullOrWhiteSpace(extraction.SupplierEmail)
            || !string.IsNullOrWhiteSpace(extraction.SupplierWebsite)
            || !string.IsNullOrWhiteSpace(extraction.Reference)
            || extraction.Date.HasValue
            || extraction.Lines.Count > 0;
    }

    private static Error MapOpenAIError(HttpStatusCode statusCode, string responseBody)
    {
        if (statusCode == HttpStatusCode.TooManyRequests ||
            ContainsAny(responseBody, "RESOURCE_EXHAUSTED", "quota exceeded", "Quota exceeded", "rate_limit_exceeded"))
        {
            return InvoiceExtractionErrors.QuotaExceeded;
        }

        if (statusCode == HttpStatusCode.Unauthorized ||
            statusCode == HttpStatusCode.Forbidden ||
            ContainsAny(responseBody, "invalid_api_key", "Incorrect API key", "permission denied", "PERMISSION_DENIED"))
        {
            return InvoiceExtractionErrors.InvalidApiKey;
        }

        return InvoiceExtractionErrors.ServiceUnavailable;
    }

    private static bool ContainsAny(string source, params string[] values)
    {
        foreach (var value in values)
        {
            if (source.Contains(value, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record OpenAISettings(string ApiKey, string Model, string BaseUrl);
}
