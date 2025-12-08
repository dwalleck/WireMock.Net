// Copyright © WireMock.Net
//
// This example demonstrates the enhanced WireMock.Net.Aspire features:
// 1. Extended Configuration Options
// 2. HTTPS Endpoint Support
// 3. Fluent Configuration API
// 4. OpenAPI Specification Bootstrapping

using WireMock.Client.Builders;

var builder = DistributedApplication.CreateBuilder(args);

// =============================================================================
// EXAMPLE 1: Basic WireMock with Extended Configuration
// =============================================================================
// This example shows how to configure WireMock with enhanced settings
// for request logging, request processing, and server behavior.

var basicMock = builder
    .AddWireMock("basic-api")
    // Request Logging Configuration
    .WithMaxRequestLogCount(1000)              // Keep last 1000 requests in memory
    .WithRequestLogExpiration(24)              // Expire request logs after 24 hours
    .WithSaveUnmatchedRequests()               // Save requests that don't match any mapping
    // Request Processing Configuration
    .WithAllowPartialMapping()                 // Allow partial request matching
    .WithSynchronousRequestHandling()          // Handle requests synchronously
    // Server Configuration
    .WithAdminPath("/__admin")                 // Custom admin API path
    .WithApiMappingBuilder(BasicApiMappings.BuildAsync);

// =============================================================================
// EXAMPLE 2: WireMock with HTTPS Support
// =============================================================================
// This example shows how to configure WireMock with HTTPS endpoint
// for testing secure API communications.

var secureMock = builder
    .AddWireMock("secure-api", 9080)
    .WithHttpsEndpoint(9443)                   // Enable HTTPS on port 9443
    .WithMaxRequestLogCount(500)
    .WithApiMappingBuilder(SecureApiMappings.BuildAsync);

// =============================================================================
// EXAMPLE 3: WireMock with Azure AD Authentication
// =============================================================================
// This example shows how to configure WireMock with Azure AD authentication
// for the admin API (useful in enterprise scenarios).
// Note: Uncomment and replace with your actual Azure AD tenant and audience.

// var enterpriseMock = builder
//     .AddWireMock("enterprise-api")
//     .WithAzureADAuthentication(
//         tenant: "your-tenant-id",
//         audience: "your-api-audience")
//     .WithAdminInterface(true)
//     .WithApiMappingBuilder(EnterpriseApiMappings.BuildAsync);

// =============================================================================
// EXAMPLE 4: Full-Featured Configuration
// =============================================================================
// This example demonstrates combining multiple configuration options.

var fullFeaturedMock = builder
    .AddWireMock("full-api", 9090)
    // Request Logging
    .WithMaxRequestLogCount(2000)
    .WithRequestLogExpiration(48)
    .WithSaveUnmatchedRequests()
    // Request Processing
    .WithAllowPartialMapping()
    .WithAllowBodyForAllHttpMethods()          // Allow body in GET, DELETE, etc.
    .WithSynchronousRequestHandling()
    // Admin Configuration
    .WithAdminInterface(true)
    .WithAdminPath("/__wiremock")
    // Static Mappings (if you have mapping files)
    // .WithReadStaticMappings()
    // .WithWatchStaticMappings()
    .WithApiMappingBuilder(FullApiMappings.BuildAsync);

// =============================================================================
// EXAMPLE 5: WireMock with OpenAPI Specification
// =============================================================================
// This example shows how to bootstrap WireMock mappings from an OpenAPI spec.
// Supports OpenAPI 2.0 (Swagger), 3.0, 3.1, and RAML formats in JSON or YAML.

// Option A: From inline OpenAPI content
const string petStoreOpenApiSpec = """
    {
      "openapi": "3.0.0",
      "info": {
        "title": "Pet Store API",
        "version": "1.0.0"
      },
      "paths": {
        "/pets": {
          "get": {
            "summary": "List all pets",
            "operationId": "listPets",
            "responses": {
              "200": {
                "description": "A list of pets",
                "content": {
                  "application/json": {
                    "example": [
                      { "id": 1, "name": "Fluffy", "species": "Cat" },
                      { "id": 2, "name": "Buddy", "species": "Dog" }
                    ]
                  }
                }
              }
            }
          },
          "post": {
            "summary": "Create a pet",
            "operationId": "createPet",
            "responses": {
              "201": {
                "description": "Pet created",
                "content": {
                  "application/json": {
                    "example": { "id": 3, "name": "New Pet", "species": "Bird" }
                  }
                }
              }
            }
          }
        },
        "/pets/{petId}": {
          "get": {
            "summary": "Get a pet by ID",
            "operationId": "getPetById",
            "parameters": [
              {
                "name": "petId",
                "in": "path",
                "required": true,
                "schema": { "type": "integer" }
              }
            ],
            "responses": {
              "200": {
                "description": "A single pet",
                "content": {
                  "application/json": {
                    "example": { "id": 1, "name": "Fluffy", "species": "Cat" }
                  }
                }
              }
            }
          }
        }
      }
    }
    """;

var openApiMock = builder
    .AddWireMock("openapi-petstore")
    .WithOpenApiDocument(petStoreOpenApiSpec);

// Option B: From a file path (uncomment to use)
// var openApiFromFileMock = builder
//     .AddWireMock("openapi-from-file")
//     .WithOpenApiFile("./specs/openapi.yaml");

// Option C: From a factory function (useful for embedded resources)
// var openApiFromFactoryMock = builder
//     .AddWireMock("openapi-from-factory")
//     .WithOpenApiDocument(() =>
//     {
//         var assembly = Assembly.GetExecutingAssembly();
//         using var stream = assembly.GetManifestResourceStream("MyApp.openapi.yaml");
//         using var reader = new StreamReader(stream!);
//         return reader.ReadToEnd();
//     });

// Option D: Combine OpenAPI with additional custom mappings
// OpenAPI mappings are loaded first, then WithApiMappingBuilder can add/override
var openApiWithCustomMock = builder
    .AddWireMock("openapi-with-custom", 9095)
    .WithOpenApiDocument(petStoreOpenApiSpec)
    .WithApiMappingBuilder((mappingBuilder, ct) =>
    {
        // Add a custom health endpoint not in the OpenAPI spec
        mappingBuilder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/health"))
            .WithResponse(response => response
                .WithStatusCode(200)
                .WithBody("OK")));
        return Task.CompletedTask;
    });

builder.Build().Run();

// =============================================================================
// API Mapping Builders
// =============================================================================

/// <summary>
/// Basic API mappings for demonstration
/// </summary>
internal static class BasicApiMappings
{
    public static Task BuildAsync(AdminApiMappingBuilder builder, CancellationToken cancellationToken)
    {
        // Simple GET endpoint
        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/api/health"))
            .WithResponse(response => response
                .WithStatusCode(200)
                .WithBody("OK")));

        // JSON endpoint
        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/api/users"))
            .WithResponse(response => response
                .WithStatusCode(200)
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBodyAsJson(new[]
                {
                    new { Id = 1, Name = "Alice", Email = "alice@example.com" },
                    new { Id = 2, Name = "Bob", Email = "bob@example.com" }
                })));

        // POST endpoint
        builder.Given(b => b
            .WithRequest(request => request
                .UsingPost()
                .WithPath("/api/users"))
            .WithResponse(response => response
                .WithStatusCode(201)
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBodyAsJson(new { Id = 3, Message = "User created" })));

        return Task.CompletedTask;
    }
}

/// <summary>
/// Secure API mappings for HTTPS demonstration
/// </summary>
internal static class SecureApiMappings
{
    public static Task BuildAsync(AdminApiMappingBuilder builder, CancellationToken cancellationToken)
    {
        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/api/secure/token"))
            .WithResponse(response => response
                .WithStatusCode(200)
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBodyAsJson(new
                {
                    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
                    ExpiresIn = 3600
                })));

        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/api/secure/profile"))
            .WithResponse(response => response
                .WithStatusCode(200)
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBodyAsJson(new
                {
                    UserId = "user-123",
                    Name = "Secure User",
                    Role = "Admin"
                })));

        return Task.CompletedTask;
    }
}

/// <summary>
/// Full-featured API mappings demonstrating various response types
/// </summary>
internal static class FullApiMappings
{
    public static Task BuildAsync(AdminApiMappingBuilder builder, CancellationToken cancellationToken)
    {
        // Health check
        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/health"))
            .WithResponse(response => response
                .WithStatusCode(200)
                .WithBody("Healthy")));

        // Products API
        builder.Given(b => b
            .WithRequest(request => request
                .UsingGet()
                .WithPath("/api/products"))
            .WithResponse(response => response
                .WithStatusCode(200)
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBodyAsJson(new[]
                {
                    new { Id = 1, Name = "Widget", Price = 9.99, InStock = true },
                    new { Id = 2, Name = "Gadget", Price = 19.99, InStock = true },
                    new { Id = 3, Name = "Gizmo", Price = 29.99, InStock = false }
                })));

        // Create product
        builder.Given(b => b
            .WithRequest(request => request
                .UsingPost()
                .WithPath("/api/products"))
            .WithResponse(response => response
                .WithStatusCode(201)
                .WithHeaders(h => h
                    .Add("Content-Type", "application/json")
                    .Add("Location", "/api/products/4"))
                .WithBodyAsJson(new { Id = 4, Message = "Product created successfully" })));

        // Update product
        builder.Given(b => b
            .WithRequest(request => request
                .UsingPut()
                .WithPath("/api/products/*"))
            .WithResponse(response => response
                .WithStatusCode(200)
                .WithHeaders(h => h.Add("Content-Type", "application/json"))
                .WithBodyAsJson(new { Message = "Product updated successfully" })));

        // Delete product
        builder.Given(b => b
            .WithRequest(request => request
                .UsingDelete()
                .WithPath("/api/products/*"))
            .WithResponse(response => response
                .WithStatusCode(204)));

        return Task.CompletedTask;
    }
}
