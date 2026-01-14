using CleanArchitecture.OCR.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CleanArchitecture.OCR.Infrastructure;

/// <summary>
/// Uses GPT4All local OpenAI-compatible API to enhance OCR text
/// </summary>
public sealed class GPT4AllTextEnhancementService : ITextEnhancementService
{
    private readonly GPT4AllSettings _settings;
    private readonly ILogger<GPT4AllTextEnhancementService> _logger;
    private readonly HttpClient _httpClient;

    public GPT4AllTextEnhancementService(
        IOptions<GPT4AllSettings> settings,
        ILogger<GPT4AllTextEnhancementService> logger,
        HttpClient httpClient)
    {
        _settings = settings.Value;
        _logger = logger;
        _httpClient = httpClient;

        if (!string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        }
    }

    public async Task<string> EnhanceTextAsync(
        string rawOcrText,
        DocumentType documentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawOcrText))
            return rawOcrText;

        if (!_settings.Enabled)
            return rawOcrText;

        try
        {
            var prompt = BuildPrompt(rawOcrText, documentType);
            var result = await CallChatCompletionsAsync(prompt, cancellationToken);

            return string.IsNullOrWhiteSpace(result)
                ? rawOcrText
                : result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GPT4All enhancement failed. Returning original OCR text.");
            return rawOcrText;
        }
    }

    // -------------------- CORE API CALL --------------------

    private async Task<string> CallChatCompletionsAsync(string prompt, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.Model))
            throw new InvalidOperationException("GPT4All model name is not configured.");

        var request = new ChatCompletionRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new ChatMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ],
            MaxTokens = _settings.MaxTokens,
            Temperature = _settings.Temperature,
            TopP = _settings.TopP,
            Stream = false
        };

        using var response = await _httpClient.PostAsJsonAsync(
            "/v1/chat/completions",
            request,
            ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"GPT4All API failed ({response.StatusCode}): {error}");
        }

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(cancellationToken: ct);

        return result?.Choices?
            .FirstOrDefault()?
            .Message?
            .Content?
            .Trim() ?? string.Empty;
    }

    // -------------------- PROMPT --------------------

    private static string BuildPrompt(string text, DocumentType type)
    {
        var doc = type switch
        {
            DocumentType.Passport => "passport",
            DocumentType.EmiratesID => "Emirates ID",
            DocumentType.UAETradeLicense => "UAE trade license",
            _ => "document"
        };

        return $"""
        You are an OCR post-processor for {doc} documents.

        RULES:
        - Do NOT add new information
        - Do NOT infer missing values
        - Fix OCR errors only
        - Preserve numbers, dates, and IDs
        - Preserve MRZ lines EXACTLY
        - Return plain text only (no markdown)

        OCR TEXT:
        {text}
        """;
    }

    // -------------------- HEALTH CHECK --------------------

    public async Task<bool> IsServerAvailableAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/v1/models", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // -------------------- DTOs --------------------

    private sealed class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = default!;

        [JsonPropertyName("messages")]
        public ChatMessage[] Messages { get; set; } = default!;

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public double TopP { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = default!;

        [JsonPropertyName("content")]
        public string Content { get; set; } = default!;
    }


    private sealed class ChatCompletionResponse
    {
        public Choice[]? Choices { get; set; }

        public sealed class Choice
        {
            public ChatMessage? Message { get; set; }
        }
    }
}
