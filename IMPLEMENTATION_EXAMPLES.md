# Implementation Examples for Security Improvements

This document provides concrete code examples for implementing the security improvements identified in the SECURITY_ANALYSIS.md report.

---

## Example 1: Configuration Management

### Create appsettings.json

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434/",
    "ModelId": "llama3.2:3b",
    "TimeoutSeconds": 30,
    "MaxRetries": 3
  },
  "IncidentProcessing": {
    "InputDirectory": "./incidents/",
    "MaxFileSizeBytes": 1048576,
    "AllowedExtensions": [ ".json" ]
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "System": "Warning"
    }
  }
}
```

### Configuration Classes

```csharp
// Configuration/OllamaOptions.cs
public class OllamaOptions
{
    public const string SectionName = "Ollama";
    
    public string Endpoint { get; set; } = "http://localhost:11434/";
    public string ModelId { get; set; } = "llama3.2:3b";
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetries { get; set; } = 3;
}

// Configuration/IncidentProcessingOptions.cs
public class IncidentProcessingOptions
{
    public const string SectionName = "IncidentProcessing";
    
    public string InputDirectory { get; set; } = "./incidents/";
    public long MaxFileSizeBytes { get; set; } = 1048576; // 1MB
    public string[] AllowedExtensions { get; set; } = { ".json" };
}
```

### Updated Program.cs with Configuration

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Add configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// Register configuration options
builder.Services.Configure<OllamaOptions>(
    builder.Configuration.GetSection(OllamaOptions.SectionName));
builder.Services.Configure<IncidentProcessingOptions>(
    builder.Configuration.GetSection(IncidentProcessingOptions.SectionName));

// Add services
builder.Services.AddSingleton<IIncidentService, IncidentService>();
builder.Services.AddSingleton<IAIAnalysisService, AIAnalysisService>();

var host = builder.Build();

// Get services and run
var incidentService = host.Services.GetRequiredService<IIncidentService>();
var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    await incidentService.ProcessIncidentAsync("../incident.json");
}
catch (Exception ex)
{
    logger.LogError(ex, "Fatal error processing incident");
    return 1;
}

return 0;
```

---

## Example 2: Strong-Typed Models with Validation

### Models/IncidentAlert.cs

```csharp
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace IncidentAgent.Models;

public class IncidentAlert
{
    [Required(ErrorMessage = "AlertId is required")]
    [StringLength(50, ErrorMessage = "AlertId must not exceed 50 characters")]
    public string AlertId { get; set; } = string.Empty;

    [Required(ErrorMessage = "AlertType is required")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AlertType AlertType { get; set; }

    [Required(ErrorMessage = "Severity is required")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Severity Severity { get; set; }

    [StringLength(2000, ErrorMessage = "Description must not exceed 2000 characters")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Timestamp is required")]
    public DateTime Timestamp { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }
}

public enum AlertType
{
    UnauthorizedAccess,
    DataExfiltration,
    MalwareDetection,
    PhishingAttempt,
    PrivilegeEscalation,
    AnomalousActivity,
    BruteForceAttempt,
    SuspiciousNetworkTraffic
}

public enum Severity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public class AnalysisResult
{
    public string IncidentId { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public string CriticalAction { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
```

---

## Example 3: Secure File Service with Validation

### Services/FileService.cs

```csharp
using System.Security;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using IncidentAgent.Models;
using IncidentAgent.Configuration;

namespace IncidentAgent.Services;

public interface IFileService
{
    Task<IncidentAlert> ReadIncidentAsync(string filePath);
}

public class FileService : IFileService
{
    private readonly IncidentProcessingOptions _options;
    private readonly ILogger<FileService> _logger;

    public FileService(
        IOptions<IncidentProcessingOptions> options,
        ILogger<FileService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<IncidentAlert> ReadIncidentAsync(string filePath)
    {
        try
        {
            // Validate file path
            var validatedPath = ValidateFilePath(filePath);
            
            _logger.LogInformation("Reading incident file from {Path}", validatedPath);

            // Validate file size
            ValidateFileSize(validatedPath);

            // Read and parse file
            var content = await File.ReadAllTextAsync(validatedPath);
            
            var incident = JsonSerializer.Deserialize<IncidentAlert>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true
            });

            if (incident == null)
            {
                throw new InvalidDataException("Failed to deserialize incident data");
            }

            // Validate model
            ValidateIncident(incident);

            _logger.LogInformation("Successfully read incident {AlertId}", incident.AlertId);
            
            return incident;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogError(ex, "Incident file not found: {Path}", filePath);
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON format in incident file");
            throw new InvalidDataException("Invalid incident file format", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading incident file");
            throw;
        }
    }

    private string ValidateFilePath(string filePath)
    {
        // Get full path
        var fullPath = Path.GetFullPath(filePath);

        // Check if file exists
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Incident file not found: {fullPath}");
        }

        // Validate extension
        var extension = Path.GetExtension(fullPath);
        if (!_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            throw new SecurityException($"File extension '{extension}' is not allowed");
        }

        // Optional: Validate file is within allowed directory
        var allowedDirectory = Path.GetFullPath(_options.InputDirectory);
        if (!fullPath.StartsWith(allowedDirectory, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("File {Path} is outside allowed directory {AllowedDir}", 
                fullPath, allowedDirectory);
            // Could throw exception here to enforce strict path validation
        }

        return fullPath;
    }

    private void ValidateFileSize(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        
        if (fileInfo.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File size ({fileInfo.Length} bytes) exceeds maximum allowed size ({_options.MaxFileSizeBytes} bytes)");
        }

        if (fileInfo.Length == 0)
        {
            throw new InvalidDataException("File is empty");
        }
    }

    private void ValidateIncident(IncidentAlert incident)
    {
        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(incident);
        
        if (!Validator.TryValidateObject(incident, context, validationResults, validateAllProperties: true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new ValidationException($"Incident validation failed: {errors}");
        }

        // Additional business logic validation
        if (incident.Timestamp > DateTime.UtcNow.AddHours(1))
        {
            throw new ValidationException("Incident timestamp cannot be in the future");
        }

        if (string.IsNullOrWhiteSpace(incident.Description))
        {
            _logger.LogWarning("Incident {AlertId} has no description", incident.AlertId);
        }
    }
}
```

---

## Example 4: AI Service with Retry Logic and Error Handling

### Services/AIAnalysisService.cs

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using IncidentAgent.Models;
using IncidentAgent.Configuration;

namespace IncidentAgent.Services;

public interface IAIAnalysisService
{
    Task<AnalysisResult> AnalyzeIncidentAsync(IncidentAlert incident);
}

public class AIAnalysisService : IAIAnalysisService
{
    private readonly OllamaOptions _options;
    private readonly ILogger<AIAnalysisService> _logger;
    private readonly IChatClient _chatClient;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;

    public AIAnalysisService(
        IOptions<OllamaOptions> options,
        ILogger<AIAnalysisService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _chatClient = new OllamaChatClient(_options.Endpoint, modelId: _options.ModelId);
        
        // Configure retry policy with exponential backoff
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                _options.MaxRetries,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} of {MaxRetries} after {Delay}s due to {ExceptionType}: {Message}",
                        retryCount, _options.MaxRetries, timeSpan.TotalSeconds,
                        exception.GetType().Name, exception.Message);
                });

        // Configure circuit breaker
        _circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                exceptionsAllowedBeforeBreaking: 5,
                durationOfBreak: TimeSpan.FromMinutes(1),
                onBreak: (exception, duration) =>
                {
                    _logger.LogError(
                        "Circuit breaker opened for {Duration}s due to {ExceptionType}",
                        duration.TotalSeconds, exception.GetType().Name);
                },
                onReset: () => _logger.LogInformation("Circuit breaker reset"),
                onHalfOpen: () => _logger.LogInformation("Circuit breaker half-open"));
    }

    public async Task<AnalysisResult> AnalyzeIncidentAsync(IncidentAlert incident)
    {
        var startTime = DateTime.UtcNow;
        
        try
        {
            _logger.LogInformation("Analyzing incident {AlertId} of type {AlertType} with severity {Severity}",
                incident.AlertId, incident.AlertType, incident.Severity);

            var result = await _retryPolicy.WrapAsync(_circuitBreakerPolicy)
                .ExecuteAsync(async () => await PerformAnalysisAsync(incident));

            var processingTime = DateTime.UtcNow - startTime;
            result.ProcessingTime = processingTime;
            
            _logger.LogInformation("Analysis completed for {AlertId} in {ProcessingTime}ms",
                incident.AlertId, processingTime.TotalMilliseconds);

            return result;
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex, "Circuit breaker is open - AI service unavailable");
            throw new ServiceUnavailableException("AI analysis service is temporarily unavailable", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to analyze incident {AlertId}", incident.AlertId);
            throw;
        }
    }

    private async Task<AnalysisResult> PerformAnalysisAsync(IncidentAlert incident)
    {
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, GetSystemPrompt()),
            new(ChatRole.User, BuildUserPrompt(incident))
        };

        var chatOptions = new ChatOptions
        {
            Temperature = 0.5f,
            MaxOutputTokens = 1000,
            StopSequences = new[] { "END_ANALYSIS" }
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));
        
        var response = await _chatClient.GetResponseAsync(messages, chatOptions, cts.Token);

        if (string.IsNullOrWhiteSpace(response.Text))
        {
            throw new InvalidOperationException("AI service returned empty response");
        }

        return ParseResponse(incident.AlertId, response.Text);
    }

    private string GetSystemPrompt()
    {
        return """
            You are a cybersecurity expert specializing in incident response and threat analysis.
            Your role is to analyze security incidents and provide actionable recommendations.
            
            Guidelines:
            - Provide clear, prioritized action items
            - Consider severity and urgency
            - Be specific and actionable
            - Identify the most critical action first
            - Focus on containment, eradication, and recovery
            
            Response format:
            1. List 3-5 specific recommendations
            2. Highlight ONE critical action that should be taken immediately
            """;
    }

    private string BuildUserPrompt(IncidentAlert incident)
    {
        return $"""
            Analyze this security incident and provide recommendations:
            
            Alert ID: {incident.AlertId}
            Type: {incident.AlertType}
            Severity: {incident.Severity}
            Description: {incident.Description ?? "No description provided"}
            Timestamp: {incident.Timestamp:yyyy-MM-dd HH:mm:ss UTC}
            
            Provide specific security recommendations based on this alert.
            Do not use markdown formatting.
            Highlight ONE critical action at the beginning.
            """;
    }

    private AnalysisResult ParseResponse(string incidentId, string responseText)
    {
        var result = new AnalysisResult
        {
            IncidentId = incidentId,
            AnalyzedAt = DateTime.UtcNow
        };

        // Simple parsing - can be enhanced with more sophisticated logic
        var lines = responseText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrWhiteSpace(trimmed))
            {
                if (string.IsNullOrEmpty(result.CriticalAction) && 
                    (trimmed.Contains("CRITICAL", StringComparison.OrdinalIgnoreCase) ||
                     trimmed.Contains("IMMEDIATE", StringComparison.OrdinalIgnoreCase)))
                {
                    result.CriticalAction = trimmed;
                }
                
                result.Recommendations.Add(trimmed);
            }
        }

        if (result.Recommendations.Count == 0)
        {
            throw new InvalidOperationException("No recommendations extracted from AI response");
        }

        return result;
    }
}

public class ServiceUnavailableException : Exception
{
    public ServiceUnavailableException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}
```

---

## Example 5: Main Application Service

### Services/IncidentService.cs

```csharp
using Microsoft.Extensions.Logging;
using IncidentAgent.Models;

namespace IncidentAgent.Services;

public interface IIncidentService
{
    Task<AnalysisResult> ProcessIncidentAsync(string filePath);
}

public class IncidentService : IIncidentService
{
    private readonly IFileService _fileService;
    private readonly IAIAnalysisService _aiService;
    private readonly ILogger<IncidentService> _logger;

    public IncidentService(
        IFileService fileService,
        IAIAnalysisService aiService,
        ILogger<IncidentService> logger)
    {
        _fileService = fileService;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<AnalysisResult> ProcessIncidentAsync(string filePath)
    {
        _logger.LogInformation("Starting incident processing for file: {FilePath}", filePath);

        try
        {
            // Read and validate incident
            var incident = await _fileService.ReadIncidentAsync(filePath);

            // Analyze with AI
            var result = await _aiService.AnalyzeIncidentAsync(incident);

            // Display results
            DisplayResults(result);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process incident from {FilePath}", filePath);
            throw;
        }
    }

    private void DisplayResults(AnalysisResult result)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("\n=== SECURITY ANALYSIS RESULTS ===\n");
        
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("⚠️  CRITICAL ACTION:");
        Console.WriteLine(result.CriticalAction);
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("\n📋 RECOMMENDATIONS:");
        for (int i = 0; i < result.Recommendations.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {result.Recommendations[i]}");
        }
        
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"\n⏱️  Analysis completed in {result.ProcessingTime.TotalSeconds:F2} seconds");
        Console.WriteLine($"📅 Analyzed at: {result.AnalyzedAt:yyyy-MM-dd HH:mm:ss UTC}");
        
        Console.ResetColor();
    }
}
```

---

## Example 6: Adding Serilog for Logging

### Update IncidentAgent.csproj

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI.Ollama" Version="9.6.0-preview.1.25310.2" />
  <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Hosting" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Options" Version="8.0.0" />
  <PackageReference Include="Polly" Version="8.0.0" />
  <PackageReference Include="Serilog" Version="3.1.0" />
  <PackageReference Include="Serilog.Extensions.Hosting" Version="8.0.0" />
  <PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
  <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
</ItemGroup>
```

### Configure Serilog in Program.cs

```csharp
using Serilog;
using Serilog.Events;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "IncidentAgent")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/incidentagent-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddSerilog();
    
    // ... rest of configuration
    
    var host = builder.Build();
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;
```

---

## Example 7: Unit Tests

### Create test project

```bash
dotnet new xunit -n IncidentAgent.Tests
cd IncidentAgent.Tests
dotnet add reference ../IncidentAgent.csproj
dotnet add package Moq
dotnet add package FluentAssertions
```

### Tests/FileServiceTests.cs

```csharp
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using IncidentAgent.Services;
using IncidentAgent.Configuration;
using IncidentAgent.Models;

namespace IncidentAgent.Tests;

public class FileServiceTests
{
    private readonly Mock<ILogger<FileService>> _loggerMock;
    private readonly IncidentProcessingOptions _options;
    private readonly FileService _sut;

    public FileServiceTests()
    {
        _loggerMock = new Mock<ILogger<FileService>>();
        _options = new IncidentProcessingOptions
        {
            InputDirectory = "./test-incidents/",
            MaxFileSizeBytes = 1024 * 1024,
            AllowedExtensions = new[] { ".json" }
        };
        
        var optionsMock = new Mock<IOptions<IncidentProcessingOptions>>();
        optionsMock.Setup(o => o.Value).Returns(_options);
        
        _sut = new FileService(optionsMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ReadIncidentAsync_ValidFile_ReturnsIncident()
    {
        // Arrange
        var testFilePath = CreateTestIncidentFile();

        // Act
        var result = await _sut.ReadIncidentAsync(testFilePath);

        // Assert
        result.Should().NotBeNull();
        result.AlertId.Should().Be("TEST-001");
        result.AlertType.Should().Be(AlertType.UnauthorizedAccess);
        result.Severity.Should().Be(Severity.High);
    }

    [Fact]
    public async Task ReadIncidentAsync_FileNotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _sut.ReadIncidentAsync("nonexistent.json"));
    }

    [Fact]
    public async Task ReadIncidentAsync_InvalidJson_ThrowsException()
    {
        // Arrange
        var testFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(testFile, "{ invalid json }");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidDataException>(
            () => _sut.ReadIncidentAsync(testFile));
        
        File.Delete(testFile);
    }

    private string CreateTestIncidentFile()
    {
        var testFile = Path.GetTempFileName();
        var json = """
        {
            "alertId": "TEST-001",
            "alertType": "UnauthorizedAccess",
            "severity": "High",
            "description": "Test incident",
            "timestamp": "2024-01-01T00:00:00Z"
        }
        """;
        File.WriteAllText(testFile, json);
        return testFile;
    }
}
```

---

## Example 8: Docker Support

### Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY ["IncidentAgent.csproj", "./"]
RUN dotnet restore

# Copy everything else and build
COPY . .
RUN dotnet build -c Release -o /app/build
RUN dotnet publish -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

# Create non-root user
RUN useradd -m -u 1000 incidentagent && \
    mkdir -p /app/logs /app/incidents && \
    chown -R incidentagent:incidentagent /app

USER incidentagent

COPY --from=build --chown=incidentagent:incidentagent /app/publish .

ENTRYPOINT ["dotnet", "IncidentAgent.dll"]
```

### docker-compose.yml

```yaml
version: '3.8'

services:
  incidentagent:
    build: .
    container_name: incidentagent
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - Ollama__Endpoint=http://ollama:11434/
    volumes:
      - ./incidents:/app/incidents:ro
      - ./logs:/app/logs
    depends_on:
      - ollama
    networks:
      - incidentagent-network

  ollama:
    image: ollama/ollama:latest
    container_name: ollama
    ports:
      - "11434:11434"
    volumes:
      - ollama-data:/root/.ollama
    networks:
      - incidentagent-network

networks:
  incidentagent-network:
    driver: bridge

volumes:
  ollama-data:
```

---

## Summary

These examples provide a solid foundation for implementing the security improvements identified in the analysis. Key improvements include:

1. ✅ **Configuration Management** - External configuration files
2. ✅ **Strong Typing** - Models with validation
3. ✅ **Secure File Handling** - Path validation and size limits
4. ✅ **Error Handling** - Try-catch blocks and retry logic
5. ✅ **Logging** - Structured logging with Serilog
6. ✅ **Dependency Injection** - Proper service architecture
7. ✅ **Unit Tests** - Test coverage for critical paths
8. ✅ **Containerization** - Docker support for deployment

Each example can be implemented incrementally, allowing for continuous improvement while maintaining functionality.
