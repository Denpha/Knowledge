using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using KMS.Application.Interfaces;
using KMS.Application.Services;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Enums;
using KMS.Domain.Interfaces;

namespace KMS.Domain.Tests;

public class ArticleServiceTests
{
    private readonly Mock<IArticleRepository> _articleRepoMock = new();
    private readonly Mock<IRepository<ArticleVersion>> _versionRepoMock = new();
    private readonly Mock<IRepository<Category>> _categoryRepoMock = new();
    private readonly Mock<IRepository<Tag>> _tagRepoMock = new();
    private readonly Mock<IRepository<KMS.Domain.Entities.Interaction.ArticleReaction>> _reactionRepoMock = new();
    private readonly Mock<IPublishWorkflowService> _publishWorkflowMock = new();
    private readonly Mock<INotificationService> _notificationMock = new();
    private readonly Mock<IDistributedCache> _cacheMock = new();

    private ArticleService CreateService() => new ArticleService(
        _articleRepoMock.Object,
        _versionRepoMock.Object,
        _categoryRepoMock.Object,
        _tagRepoMock.Object,
        _reactionRepoMock.Object,
        _publishWorkflowMock.Object,
        _notificationMock.Object,
        _cacheMock.Object);

    [Fact]
    public async Task GetByIdAsync_ArticleNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        _articleRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((KnowledgeArticle?)null);

        var service = CreateService();
        var result = await service.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ArticleExists_ReturnsMappedDto()
    {
        var id = Guid.NewGuid();
        var article = new KnowledgeArticle
        {
            Id = id,
            Title = "Test Article",
            Slug = "test-article",
            Content = "Content",
            Summary = "Summary",
            Status = ArticleStatus.Published,
            Category = new Category { Id = Guid.NewGuid(), Name = "Test Cat" },
            Author = new KMS.Domain.Entities.Identity.AppUser { Id = Guid.NewGuid(), FullNameTh = "Author" }
        };

        _articleRepoMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(article);

        var service = CreateService();
        var result = await service.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Test Article");
        result.Slug.Should().Be("test-article");
    }

    [Fact]
    public async Task GetBySlugAsync_SlugNotFound_ReturnsNull()
    {
        _articleRepoMock.Setup(r => r.GetBySlugAsync("missing-slug", default)).ReturnsAsync((KnowledgeArticle?)null);

        var service = CreateService();
        var result = await service.GetBySlugAsync("missing-slug");

        result.Should().BeNull();
    }

    [Fact]
    public async Task CheckSlugAvailabilityAsync_SlugNotExists_ReturnsTrue()
    {
        _articleRepoMock.Setup(r => r.GetBySlugAsync("new-unique-slug", default)).ReturnsAsync((KnowledgeArticle?)null);

        var service = CreateService();
        var result = await service.CheckSlugAvailabilityAsync("new-unique-slug");

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckSlugAvailabilityAsync_SlugExists_ReturnsFalse()
    {
        var existing = new KnowledgeArticle { Id = Guid.NewGuid(), Slug = "existing-slug" };
        _articleRepoMock.Setup(r => r.GetBySlugAsync("existing-slug", default)).ReturnsAsync(existing);

        var service = CreateService();
        var result = await service.CheckSlugAvailabilityAsync("existing-slug");

        result.Should().BeFalse();
    }
}
