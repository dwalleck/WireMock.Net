// Copyright © WireMock.Net

using FluentAssertions;

namespace WireMock.Net.Aspire.Tests;

public class WireMockServerArgumentsTests
{
    [Fact]
    public void DefaultValues_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var args = new WireMockServerArguments();

        // Assert
        args.HttpPort.Should().BeNull();
        args.AdminUsername.Should().BeNull();
        args.AdminPassword.Should().BeNull();
        args.ReadStaticMappings.Should().BeFalse();
        args.WatchStaticMappings.Should().BeFalse();
        args.MappingsPath.Should().BeNull();
    }

    [Fact]
    public void HasBasicAuthentication_ShouldReturnTrue_WhenUsernameAndPasswordAreProvided()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            AdminUsername = "admin",
            AdminPassword = "password"
        };

        // Act & Assert
        args.HasBasicAuthentication.Should().BeTrue();
    }

    [Fact]
    public void HasBasicAuthentication_ShouldReturnFalse_WhenEitherUsernameOrPasswordIsNotProvided()
    {
        // Arrange
        var argsWithUsernameOnly = new WireMockServerArguments { AdminUsername = "admin" };
        var argsWithPasswordOnly = new WireMockServerArguments { AdminPassword = "password" };

        // Act & Assert
        argsWithUsernameOnly.HasBasicAuthentication.Should().BeFalse();
        argsWithPasswordOnly.HasBasicAuthentication.Should().BeFalse();
    }

    [Fact]
    public void GetArgs_WhenReadStaticMappingsIsTrue_ShouldContainReadStaticMappingsTrue()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            ReadStaticMappings = true
        };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--ReadStaticMappings", "true");
    }

    [Fact]
    public void GetArgs_WhenReadStaticMappingsIsFalse_ShouldNotContainReadStaticMappingsTrue()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            ReadStaticMappings = false
        };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().NotContain("--ReadStaticMappings", "true");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GetArgs_WhenWithWatchStaticMappingsIsTrue_ShouldContainWatchStaticMappingsTrue(bool readStaticMappings)
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            WatchStaticMappings = true,
            ReadStaticMappings = readStaticMappings
        };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--ReadStaticMappings", "true", "--WatchStaticMappings", "true", "--WatchStaticMappingsInSubdirectories", "true");
    }

    [Fact]
    public void GetArgs_WhenWithWatchStaticMappingsIsFalse_ShouldNotContainWatchStaticMappingsTrue()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            WatchStaticMappings = false
        };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().NotContain("--WatchStaticMappings", "true").And.NotContain("--WatchStaticMappingsInSubdirectories", "true");
    }

    [Fact]
    public void GetArgs_ShouldIncludeAuthenticationDetails_WhenAuthenticationIsRequired()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            AdminUsername = "admin",
            AdminPassword = "password"
        };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().Contain("--AdminUserName", "admin");
        commandLineArgs.Should().Contain("--AdminPassword", "password");
    }

    #region New Properties Tests

    [Fact]
    public void DefaultValues_NewProperties_ShouldBeSetCorrectly()
    {
        // Arrange & Act
        var args = new WireMockServerArguments();

        // Assert - Request Logging
        args.MaxRequestLogCount.Should().BeNull();
        args.RequestLogExpirationDuration.Should().BeNull();
        args.SaveUnmatchedRequests.Should().BeFalse();
        args.DoNotSaveDynamicResponseInLogEntry.Should().BeFalse();

        // Assert - Request Processing
        args.AllowPartialMapping.Should().BeFalse();
        args.AllowBodyForAllHttpMethods.Should().BeFalse();
        args.DisableJsonBodyParsing.Should().BeFalse();
        args.DisableRequestBodyDecompressing.Should().BeFalse();
        args.HandleRequestsSynchronously.Should().BeFalse();
        args.UseRegexExtended.Should().BeNull();

        // Assert - Server Configuration
        args.StartAdminInterface.Should().BeNull();
        args.AdminPath.Should().BeNull();
        args.StartTimeout.Should().BeNull();

        // Assert - Azure AD
        args.AdminAzureADTenant.Should().BeNull();
        args.AdminAzureADAudience.Should().BeNull();

        // Assert - HTTPS
        args.HttpsPort.Should().BeNull();
        args.UseHttps.Should().BeFalse();
    }

    [Fact]
    public void HasAzureADAuthentication_ShouldReturnTrue_WhenTenantAndAudienceAreProvided()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            AdminAzureADTenant = "my-tenant",
            AdminAzureADAudience = "my-audience"
        };

        // Act & Assert
        args.HasAzureADAuthentication.Should().BeTrue();
    }

    [Fact]
    public void HasAzureADAuthentication_ShouldReturnFalse_WhenEitherTenantOrAudienceIsNotProvided()
    {
        // Arrange
        var argsWithTenantOnly = new WireMockServerArguments { AdminAzureADTenant = "my-tenant" };
        var argsWithAudienceOnly = new WireMockServerArguments { AdminAzureADAudience = "my-audience" };

        // Act & Assert
        argsWithTenantOnly.HasAzureADAuthentication.Should().BeFalse();
        argsWithAudienceOnly.HasAzureADAuthentication.Should().BeFalse();
    }

    [Fact]
    public void UseHttps_ShouldReturnTrue_WhenHttpsPortIsProvided()
    {
        // Arrange
        var args = new WireMockServerArguments { HttpsPort = 443 };

        // Act & Assert
        args.UseHttps.Should().BeTrue();
    }

    [Fact]
    public void GetArgs_WhenMaxRequestLogCountIsSet_ShouldContainMaxRequestLogCount()
    {
        // Arrange
        var args = new WireMockServerArguments { MaxRequestLogCount = 100 };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--MaxRequestLogCount", "100");
    }

    [Fact]
    public void GetArgs_WhenRequestLogExpirationDurationIsSet_ShouldContainRequestLogExpirationDuration()
    {
        // Arrange
        var args = new WireMockServerArguments { RequestLogExpirationDuration = 24 };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--RequestLogExpirationDuration", "24");
    }

    [Fact]
    public void GetArgs_WhenSaveUnmatchedRequestsIsTrue_ShouldContainSaveUnmatchedRequests()
    {
        // Arrange
        var args = new WireMockServerArguments { SaveUnmatchedRequests = true };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--SaveUnmatchedRequests", "true");
    }

    [Fact]
    public void GetArgs_WhenDoNotSaveDynamicResponseInLogEntryIsTrue_ShouldContainFlag()
    {
        // Arrange
        var args = new WireMockServerArguments { DoNotSaveDynamicResponseInLogEntry = true };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--DoNotSaveDynamicResponseInLogEntry", "true");
    }

    [Fact]
    public void GetArgs_WhenAllowPartialMappingIsTrue_ShouldContainAllowPartialMapping()
    {
        // Arrange
        var args = new WireMockServerArguments { AllowPartialMapping = true };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--AllowPartialMapping", "true");
    }

    [Fact]
    public void GetArgs_WhenAllowBodyForAllHttpMethodsIsTrue_ShouldContainAllowBodyForAllHttpMethods()
    {
        // Arrange
        var args = new WireMockServerArguments { AllowBodyForAllHttpMethods = true };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--AllowBodyForAllHttpMethods", "true");
    }

    [Fact]
    public void GetArgs_WhenDisableJsonBodyParsingIsTrue_ShouldContainDisableJsonBodyParsing()
    {
        // Arrange
        var args = new WireMockServerArguments { DisableJsonBodyParsing = true };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--DisableJsonBodyParsing", "true");
    }

    [Fact]
    public void GetArgs_WhenDisableRequestBodyDecompressingIsTrue_ShouldContainFlag()
    {
        // Arrange
        var args = new WireMockServerArguments { DisableRequestBodyDecompressing = true };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--DisableRequestBodyDecompressing", "true");
    }

    [Fact]
    public void GetArgs_WhenHandleRequestsSynchronouslyIsTrue_ShouldContainHandleRequestsSynchronously()
    {
        // Arrange
        var args = new WireMockServerArguments { HandleRequestsSynchronously = true };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--HandleRequestsSynchronously", "true");
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void GetArgs_WhenUseRegexExtendedIsSet_ShouldContainUseRegexExtended(bool value, string expected)
    {
        // Arrange
        var args = new WireMockServerArguments { UseRegexExtended = value };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--UseRegexExtended", expected);
    }

    [Theory]
    [InlineData(true, "true")]
    [InlineData(false, "false")]
    public void GetArgs_WhenStartAdminInterfaceIsSet_ShouldContainStartAdminInterface(bool value, string expected)
    {
        // Arrange
        var args = new WireMockServerArguments { StartAdminInterface = value };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--StartAdminInterface", expected);
    }

    [Fact]
    public void GetArgs_WhenAdminPathIsSet_ShouldContainAdminPath()
    {
        // Arrange
        var args = new WireMockServerArguments { AdminPath = "/custom-admin" };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--AdminPath", "/custom-admin");
    }

    [Fact]
    public void GetArgs_WhenStartTimeoutIsSet_ShouldContainStartTimeout()
    {
        // Arrange
        var args = new WireMockServerArguments { StartTimeout = 30000 };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--StartTimeout", "30000");
    }

    [Fact]
    public void GetArgs_WhenAzureADAuthenticationIsSet_ShouldContainAzureADSettings()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            AdminAzureADTenant = "my-tenant",
            AdminAzureADAudience = "my-audience"
        };

        // Act
        var commandLineArgs = args.GetArgs();

        // Assert
        commandLineArgs.Should().ContainInOrder("--AdminAzureADTenant", "my-tenant");
        commandLineArgs.Should().ContainInOrder("--AdminAzureADAudience", "my-audience");
    }

    #endregion

    #region OpenAPI Configuration Tests

    [Fact]
    public void DefaultValues_OpenApiProperties_ShouldBeNull()
    {
        // Arrange & Act
        var args = new WireMockServerArguments();

        // Assert
        args.OpenApiFilePath.Should().BeNull();
        args.OpenApiDocument.Should().BeNull();
        args.OpenApiDocumentFactory.Should().BeNull();
        args.HasOpenApiConfiguration.Should().BeFalse();
    }

    [Fact]
    public void HasOpenApiConfiguration_ShouldReturnTrue_WhenFilePathIsSet()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            OpenApiFilePath = "/path/to/openapi.yaml"
        };

        // Act & Assert
        args.HasOpenApiConfiguration.Should().BeTrue();
    }

    [Fact]
    public void HasOpenApiConfiguration_ShouldReturnTrue_WhenDocumentIsSet()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            OpenApiDocument = "openapi: 3.0.0"
        };

        // Act & Assert
        args.HasOpenApiConfiguration.Should().BeTrue();
    }

    [Fact]
    public void HasOpenApiConfiguration_ShouldReturnTrue_WhenFactoryIsSet()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            OpenApiDocumentFactory = () => "openapi: 3.0.0"
        };

        // Act & Assert
        args.HasOpenApiConfiguration.Should().BeTrue();
    }

    [Fact]
    public void HasOpenApiConfiguration_ShouldReturnFalse_WhenNothingIsSet()
    {
        // Arrange
        var args = new WireMockServerArguments();

        // Act & Assert
        args.HasOpenApiConfiguration.Should().BeFalse();
    }

    [Fact]
    public void HasOpenApiConfiguration_ShouldReturnFalse_WhenEmptyStringsAreSet()
    {
        // Arrange
        var args = new WireMockServerArguments
        {
            OpenApiFilePath = "",
            OpenApiDocument = ""
        };

        // Act & Assert
        args.HasOpenApiConfiguration.Should().BeFalse();
    }

    #endregion
}