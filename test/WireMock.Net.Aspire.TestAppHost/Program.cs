using WireMock.Net.Aspire.TestAppHost;

var builder = DistributedApplication.CreateBuilder(args);

var mappingsPath = Path.Combine(Directory.GetCurrentDirectory(), "WireMockMappings");

builder
    .AddWireMock("wiremock-service")
    .WithAdminUserNameAndPassword($"user-{Guid.NewGuid()}", $"pwd-{Guid.NewGuid()}")
    .WithMappingsPath(mappingsPath)
    .WithWatchStaticMappings()
    .WithApiMappingBuilder(WeatherForecastApiMock.BuildAsync);

// OpenAPI-configured WireMock for integration testing
const string openApiSpec = """
    {
      "openapi": "3.0.0",
      "info": {
        "title": "Pet Store API",
        "version": "1.0.0"
      },
      "paths": {
        "/api/pets": {
          "get": {
            "summary": "List all pets",
            "operationId": "listPets",
            "responses": {
              "200": {
                "description": "A list of pets",
                "content": {
                  "application/json": {
                    "example": [
                      { "id": 1, "name": "Fluffy" },
                      { "id": 2, "name": "Spot" }
                    ]
                  }
                }
              }
            }
          }
        }
      }
    }
    """;

builder
    .AddWireMock("wiremock-openapi")
    .WithOpenApiDocument(openApiSpec);

await builder
    .Build()
    .RunAsync();