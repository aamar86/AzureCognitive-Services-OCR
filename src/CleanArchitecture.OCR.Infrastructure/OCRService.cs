using Azure;
using Azure.AI.Vision.ImageAnalysis;
using CleanArchitecture.OCR.Application;
using Microsoft.Extensions.Options;

namespace CleanArchitecture.OCR.Infrastructure;

public class OCRService : IOCRService
{
    private readonly AzureOCRSettings _settings;

    public OCRService(IOptions<AzureOCRSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<string> ExtractTextAsync(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            throw new ArgumentException("Image path cannot be empty", nameof(imagePath));
        }

        if (!File.Exists(imagePath))
        {
            throw new FileNotFoundException($"Image file not found: {imagePath}");
        }

        if (string.IsNullOrWhiteSpace(_settings.Endpoint) || string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("Azure Cognitive Services endpoint and API key must be configured");
        }

        try
        {
            // Create Azure Computer Vision client
            var credential = new AzureKeyCredential(_settings.ApiKey);
            var client = new ImageAnalysisClient(new Uri(_settings.Endpoint), credential);

            // Read image file
            using var imageStream = File.OpenRead(imagePath);
            var imageData = BinaryData.FromStream(imageStream);

            // Analyze image for text (OCR)
            var result = await client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read,
                cancellationToken: default);

            // Extract text from results
            if (result?.Value?.Read?.Blocks != null && result.Value.Read.Blocks.Count > 0)
            {
                var extractedText = new System.Text.StringBuilder();
                
                foreach (var block in result.Value.Read.Blocks)
                {
                    foreach (var line in block.Lines)
                    {
                        extractedText.AppendLine(line.Text);
                    }
                }

                return extractedText.ToString().Trim();
            }

            return "No text detected in the image.";
        }
        catch (RequestFailedException ex)
        {
            throw new InvalidOperationException($"Azure Cognitive Services error: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error processing OCR: {ex.Message}", ex);
        }
    }
}

public class AzureOCRSettings
{
    public const string SectionName = "AzureOCR";
    
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
