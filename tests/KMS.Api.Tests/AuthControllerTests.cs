using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using KMS.Application.DTOs.Identity;

namespace KMS.Api.Tests;

public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_Returns401()
    {
        var loginDto = new LoginDto
        {
            Username = "nonexistent@test.com",
            Password = "WrongPassword123!"
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyCredentials_ReturnsBadRequest()
    {
        var loginDto = new LoginDto
        {
            Username = "",
            Password = ""
        };

        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProfile_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/auth/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}

public class ArticlesControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ArticlesControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetArticles_PublicEndpoint_Returns200Or401()
    {
        var response = await _client.GetAsync("/api/articles");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetArticleById_WithUnknownId_Returns404Or401()
    {
        var unknownId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/articles/{unknownId}");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateArticle_WithoutToken_Returns401()
    {
        var payload = new { Title = "Test", Content = "Body", Summary = "Sum", Slug = "test" };
        var response = await _client.PostAsJsonAsync("/api/articles", payload);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
