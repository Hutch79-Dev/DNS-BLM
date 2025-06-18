using DNS_BLM.Infrastructure.Dtos;
using DNS_BLM.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Test;

public class MessageServiceTest
{
    private readonly MessageService messageService;
    private readonly Mock<ILogger<MessageService>> mockLogger;

    public MessageServiceTest()
    {
        mockLogger = new Mock<ILogger<MessageService>>();
        messageService = new MessageService(mockLogger.Object);
    }

    [Fact]
    public void AddResult_ThrowsArgumentNullException_ForNullResult()
    {
        Assert.Throws<ArgumentNullException>(() => messageService.AddResult(null));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddResult_ThrowsArgumentException_ForInvalidDomain(string domain)
    {
        var result = new ScanResult()
        {
            Domain = domain,
            ScannerName = "Scanner1",
            ScanResultUrl = "http://url.com",
            IsBlacklisted = false
        };

        
        var caughtException = (Exception)null;
        try
        {
            messageService.AddResult(result);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        Assert.NotNull(caughtException);
        Assert.True(caughtException is ArgumentNullException || caughtException is ArgumentException);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddResult_ThrowsArgumentException_ForInvalidScannerName(string scannerName)
    {
        var result = new ScanResult()
        {
            Domain = "example.ch",
            ScannerName = scannerName,
            ScanResultUrl = "http://url.com",
            IsBlacklisted = false
        };
        
        var caughtException = (Exception)null;
        try
        {
            messageService.AddResult(result);
        }
        catch (Exception ex)
        {
            caughtException = ex;
        }

        Assert.NotNull(caughtException);
        Assert.True(caughtException is ArgumentNullException || caughtException is ArgumentException);
    }

    [Fact]
    public void GetResults_ReturnsNull_WhenNoBlacklistedResults()
    {
        var scanResult1 = new ScanResult() { Domain = "example.ch", ScannerName = "Scanner1", ScanResultUrl = "http://url.com", IsBlacklisted = false };
        var scanResult2 = new ScanResult() { Domain = "example.swiss", ScannerName = "Scanner2", ScanResultUrl = "http://url.com", IsBlacklisted = false };

        messageService.AddResult(scanResult1);
        messageService.AddResult(scanResult2);

        var result = messageService.GetResults();

        Assert.Null(result);
    }

    [Fact]
    public void GetResults_ReturnsFormattedString_WhenBlacklistedResultsExist()
    {
        var scanResult1 = new ScanResult() { Domain = "example.ch", ScannerName = "Scanner1", ScanResultUrl = "http://url.com", IsBlacklisted = true };
        var scanResult2 = new ScanResult() { Domain = "example.swiss", ScannerName = "Scanner2", ScanResultUrl = "http://url.com", IsBlacklisted = false };
        var scanResult3 = new ScanResult() { Domain = "example.net", ScannerName = "Scanner3", ScanResultUrl = "http://url.com", IsBlacklisted = true };

        messageService.AddResult(scanResult1);
        messageService.AddResult(scanResult2);
        messageService.AddResult(scanResult3);

        var expected = """
                       example.ch
                       | Scanner1: Listed
                       | URL: http://url.com

                       example.net
                       | Scanner3: Listed
                       | URL: http://url.com
                       
                       """;

        var result = messageService.GetResults();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Clear_ResetsInternalState()
    {
        var scanResult1 = new ScanResult() { Domain = "example.ch", ScannerName = "Scanner1", ScanResultUrl = "http://url.com", IsBlacklisted = true };
        messageService.AddResult(scanResult1);

        Assert.NotNull(messageService.GetResults());

        messageService.Clear();

        Assert.Null(messageService.GetResults());
    }
}