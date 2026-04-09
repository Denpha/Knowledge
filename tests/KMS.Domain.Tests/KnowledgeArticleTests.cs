using FluentAssertions;
using KMS.Domain.Entities.Knowledge;
using KMS.Domain.Enums;

namespace KMS.Domain.Tests;

public class KnowledgeArticleTests
{
    [Fact]
    public void NewArticle_DefaultStatus_ShouldBeDraft()
    {
        var article = new KnowledgeArticle();
        article.Status.Should().Be(ArticleStatus.Draft);
    }

    [Fact]
    public void NewArticle_DefaultVisibility_ShouldBeInternal()
    {
        var article = new KnowledgeArticle();
        article.Visibility.Should().Be(Visibility.Internal);
    }

    [Fact]
    public void NewArticle_DefaultViewCount_ShouldBeZero()
    {
        var article = new KnowledgeArticle();
        article.ViewCount.Should().Be(0);
    }

    [Fact]
    public void NewArticle_DefaultLikeCount_ShouldBeZero()
    {
        var article = new KnowledgeArticle();
        article.LikeCount.Should().Be(0);
    }

    [Fact]
    public void Article_PublishedAt_ShouldBeNullByDefault()
    {
        var article = new KnowledgeArticle();
        article.PublishedAt.Should().BeNull();
    }
}

public class ArticleStatusEnumTests
{
    [Theory]
    [InlineData(ArticleStatus.Draft, "ฉบับร่าง")]
    [InlineData(ArticleStatus.UnderReview, "รอการตรวจสอบ")]
    [InlineData(ArticleStatus.Published, "เผยแพร่แล้ว")]
    [InlineData(ArticleStatus.Archived, "เก็บถาวร")]
    public void ToDisplayString_ShouldReturnCorrectThai(ArticleStatus status, string expected)
    {
        status.ToDisplayString().Should().Be(expected);
    }

    [Fact]
    public void CanBePublishedDirectly_Draft_ShouldBeTrue()
    {
        ArticleStatus.Draft.CanBePublishedDirectly().Should().BeTrue();
    }

    [Fact]
    public void CanBePublishedDirectly_Archived_ShouldBeFalse()
    {
        ArticleStatus.Archived.CanBePublishedDirectly().Should().BeFalse();
    }

    [Fact]
    public void RequiresReview_Draft_ShouldBeTrue()
    {
        ArticleStatus.Draft.RequiresReview().Should().BeTrue();
    }

    [Fact]
    public void RequiresReview_Published_ShouldBeFalse()
    {
        ArticleStatus.Published.RequiresReview().Should().BeFalse();
    }
}

public class VisibilityEnumTests
{
    [Fact]
    public void IsAccessibleByPublic_Public_ShouldBeTrue()
    {
        Visibility.Public.IsAccessibleByPublic().Should().BeTrue();
    }

    [Fact]
    public void IsAccessibleByPublic_Internal_ShouldBeFalse()
    {
        Visibility.Internal.IsAccessibleByPublic().Should().BeFalse();
    }

    [Theory]
    [InlineData(Visibility.Public, true)]
    [InlineData(Visibility.Internal, true)]
    [InlineData(Visibility.Restricted, false)]
    public void IsAccessibleByInternal_ShouldMatchExpected(Visibility visibility, bool expected)
    {
        visibility.IsAccessibleByInternal().Should().Be(expected);
    }

    [Theory]
    [InlineData(Visibility.Public, "สาธารณะ")]
    [InlineData(Visibility.Internal, "ภายในองค์กร")]
    [InlineData(Visibility.Restricted, "จำกัดการเข้าถึง")]
    public void ToDisplayString_ShouldReturnCorrectThai(Visibility visibility, string expected)
    {
        visibility.ToDisplayString().Should().Be(expected);
    }
}
