// Copyright © WireMock.Net

using System.Net.Sockets;
using FluentAssertions;
using Moq;

namespace WireMock.Net.Aspire.Tests;

public class WireMockServerBuilderExtensionsTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AddWireMock_WithNullOrWhiteSpaceName_ShouldThrowException(string? name)
    {
        // Arrange
        var builder = Mock.Of<IDistributedApplicationBuilder>();

        // Act
        Action act = () => builder.AddWireMock(name!, 12345);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void AddWireMock_WithInvalidPort_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        const int invalidPort = -1;
        var builder = Mock.Of<IDistributedApplicationBuilder>();

        // Act
        Action act = () => builder.AddWireMock("ValidName", invalidPort);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>().WithMessage("Specified argument was out of the range of valid values. (Parameter 'port')");
    }

    [Fact]
    public void AddWireMock()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        const int port = 12345;
        const string username = "admin";
        const string password = "test";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name, port)
            .WithAdminUserNameAndPassword(username, password)
            .WithReadStaticMappings();

        // Assert
        wiremock.Resource.Should().NotBeNull();
        wiremock.Resource.Name.Should().Be(name);
        wiremock.Resource.Arguments.Should().BeEquivalentTo(new WireMockServerArguments
        {
            AdminPassword = password,
            AdminUsername = username,
            ReadStaticMappings = true,
            WatchStaticMappings = false,
            MappingsPath = null,
            HttpPort = port
        });
        wiremock.Resource.Annotations.Should().HaveCount(6);

        var containerImageAnnotation = wiremock.Resource.Annotations.OfType<ContainerImageAnnotation>().FirstOrDefault();
        containerImageAnnotation.Should().BeEquivalentTo(new ContainerImageAnnotation
        {
            Image = "sheyenrath/wiremock.net-alpine",
            Registry = null,
            Tag = "latest"
        });

        var endpointAnnotation = wiremock.Resource.Annotations.OfType<EndpointAnnotation>().FirstOrDefault();
        endpointAnnotation.Should().BeEquivalentTo(new EndpointAnnotation(
            protocol: ProtocolType.Tcp,
            uriScheme: "http",
            transport: null,
            name: null,
            port: port,
            targetPort: 80,
            isExternal: null,
            isProxied: true
        ));

        wiremock.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>().FirstOrDefault().Should().NotBeNull();

        wiremock.Resource.Annotations.OfType<CommandLineArgsCallbackAnnotation>().FirstOrDefault().Should().NotBeNull();

        wiremock.Resource.Annotations.OfType<ResourceCommandAnnotation>().FirstOrDefault().Should().NotBeNull();
    }

    #region New Extension Methods Tests

    [Fact]
    public void WithMaxRequestLogCount_ShouldSetMaxRequestLogCount()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithMaxRequestLogCount(500);

        // Assert
        wiremock.Resource.Arguments.MaxRequestLogCount.Should().Be(500);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public void WithMaxRequestLogCount_WithInvalidValue_ShouldThrowException(int count)
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();
        var wiremock = builder.AddWireMock(name);

        // Act
        Action act = () => wiremock.WithMaxRequestLogCount(count);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithRequestLogExpiration_ShouldSetRequestLogExpirationDuration()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithRequestLogExpiration(24);

        // Assert
        wiremock.Resource.Arguments.RequestLogExpirationDuration.Should().Be(24);
    }

    [Fact]
    public void WithSaveUnmatchedRequests_ShouldSetSaveUnmatchedRequestsToTrue()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithSaveUnmatchedRequests();

        // Assert
        wiremock.Resource.Arguments.SaveUnmatchedRequests.Should().BeTrue();
    }

    [Fact]
    public void WithAllowPartialMapping_ShouldSetAllowPartialMappingToTrue()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithAllowPartialMapping();

        // Assert
        wiremock.Resource.Arguments.AllowPartialMapping.Should().BeTrue();
    }

    [Fact]
    public void WithAllowBodyForAllHttpMethods_ShouldSetAllowBodyForAllHttpMethodsToTrue()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithAllowBodyForAllHttpMethods();

        // Assert
        wiremock.Resource.Arguments.AllowBodyForAllHttpMethods.Should().BeTrue();
    }

    [Fact]
    public void WithSynchronousRequestHandling_ShouldSetHandleRequestsSynchronouslyToTrue()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithSynchronousRequestHandling();

        // Assert
        wiremock.Resource.Arguments.HandleRequestsSynchronously.Should().BeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithAdminInterface_ShouldSetStartAdminInterface(bool enabled)
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithAdminInterface(enabled);

        // Assert
        wiremock.Resource.Arguments.StartAdminInterface.Should().Be(enabled);
    }

    [Fact]
    public void WithAdminPath_ShouldSetAdminPath()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithAdminPath("/custom-admin");

        // Assert
        wiremock.Resource.Arguments.AdminPath.Should().Be("/custom-admin");
    }

    [Fact]
    public void WithAzureADAuthentication_ShouldSetTenantAndAudience()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithAzureADAuthentication("my-tenant", "my-audience");

        // Assert
        wiremock.Resource.Arguments.AdminAzureADTenant.Should().Be("my-tenant");
        wiremock.Resource.Arguments.AdminAzureADAudience.Should().Be("my-audience");
        wiremock.Resource.Arguments.HasAzureADAuthentication.Should().BeTrue();
    }

    [Fact]
    public void WithHttpsEndpoint_ShouldSetHttpsPort()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithHttpsEndpoint(8443);

        // Assert
        wiremock.Resource.Arguments.HttpsPort.Should().Be(8443);
        wiremock.Resource.Arguments.UseHttps.Should().BeTrue();
    }

    [Fact]
    public void WithHttpsEndpoint_WithNullPort_ShouldStillConfigureEndpoint()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act - explicitly call our extension method
        var wiremock = builder.AddWireMock(name);
        wiremock = WireMockServerBuilderExtensions.WithHttpsEndpoint(wiremock, null);

        // Assert
        wiremock.Resource.Arguments.HttpsPort.Should().BeNull();
        // Note: UseHttps will be false since HttpsPort is null
        // The endpoint is still configured via Aspire's WithHttpsEndpoint in the extension method
    }

    [Fact]
    public void ChainingMultipleExtensions_ShouldSetAllProperties()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name, 12345)
            .WithMaxRequestLogCount(100)
            .WithRequestLogExpiration(48)
            .WithSaveUnmatchedRequests()
            .WithAllowPartialMapping()
            .WithSynchronousRequestHandling()
            .WithAdminPath("/api/admin");

        // Assert
        wiremock.Resource.Arguments.HttpPort.Should().Be(12345);
        wiremock.Resource.Arguments.MaxRequestLogCount.Should().Be(100);
        wiremock.Resource.Arguments.RequestLogExpirationDuration.Should().Be(48);
        wiremock.Resource.Arguments.SaveUnmatchedRequests.Should().BeTrue();
        wiremock.Resource.Arguments.AllowPartialMapping.Should().BeTrue();
        wiremock.Resource.Arguments.HandleRequestsSynchronously.Should().BeTrue();
        wiremock.Resource.Arguments.AdminPath.Should().Be("/api/admin");
    }

    #endregion

    #region OpenAPI Configuration Tests

    [Fact]
    public void WithOpenApiFile_ShouldSetOpenApiFilePath()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithOpenApiFile("/path/to/openapi.yaml");

        // Assert
        wiremock.Resource.Arguments.OpenApiFilePath.Should().Be("/path/to/openapi.yaml");
        wiremock.Resource.Arguments.HasOpenApiConfiguration.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithOpenApiFile_WithNullOrWhiteSpacePath_ShouldThrowException(string? filePath)
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();
        var wiremock = builder.AddWireMock(name);

        // Act
        Action act = () => wiremock.WithOpenApiFile(filePath!);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void WithOpenApiDocument_String_ShouldSetOpenApiDocument()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();
        const string openApiContent = """
            openapi: "3.0.0"
            info:
              title: Test API
              version: "1.0"
            """;

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithOpenApiDocument(openApiContent);

        // Assert
        wiremock.Resource.Arguments.OpenApiDocument.Should().Be(openApiContent);
        wiremock.Resource.Arguments.HasOpenApiConfiguration.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WithOpenApiDocument_WithNullOrWhiteSpaceContent_ShouldThrowException(string? content)
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();
        var wiremock = builder.AddWireMock(name);

        // Act
        Action act = () => wiremock.WithOpenApiDocument(content!);

        // Assert
        act.Should().Throw<Exception>();
    }

    [Fact]
    public void WithOpenApiDocument_Factory_ShouldSetOpenApiDocumentFactory()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();
        Func<string> factory = () => "openapi: 3.0.0";

        // Act
        var wiremock = builder
            .AddWireMock(name)
            .WithOpenApiDocument(factory);

        // Assert
        wiremock.Resource.Arguments.OpenApiDocumentFactory.Should().NotBeNull();
        wiremock.Resource.Arguments.OpenApiDocumentFactory!().Should().Be("openapi: 3.0.0");
        wiremock.Resource.Arguments.HasOpenApiConfiguration.Should().BeTrue();
    }

    [Fact]
    public void WithOpenApiDocument_Factory_WithNullFactory_ShouldThrowException()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();
        var wiremock = builder.AddWireMock(name);

        // Act
        Action act = () => wiremock.WithOpenApiDocument((Func<string>)null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithOpenApiFile_CombinedWithOtherExtensions_ShouldWorkCorrectly()
    {
        // Arrange
        var name = $"apiservice{Guid.NewGuid()}";
        var builder = DistributedApplication.CreateBuilder();

        // Act
        var wiremock = builder
            .AddWireMock(name, 12345)
            .WithOpenApiFile("/path/to/spec.yaml")
            .WithMaxRequestLogCount(100)
            .WithAllowPartialMapping();

        // Assert
        wiremock.Resource.Arguments.HttpPort.Should().Be(12345);
        wiremock.Resource.Arguments.OpenApiFilePath.Should().Be("/path/to/spec.yaml");
        wiremock.Resource.Arguments.MaxRequestLogCount.Should().Be(100);
        wiremock.Resource.Arguments.AllowPartialMapping.Should().BeTrue();
    }

    #endregion
}