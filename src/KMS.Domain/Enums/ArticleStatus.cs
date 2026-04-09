namespace KMS.Domain.Enums;

public enum ArticleStatus
{
    Draft = 1,
    UnderReview = 2,
    Published = 3,
    Archived = 4
}

public static class ArticleStatusExtensions
{
    public static string ToDisplayString(this ArticleStatus status)
    {
        return status switch
        {
            ArticleStatus.Draft => "ฉบับร่าง",
            ArticleStatus.UnderReview => "รอการตรวจสอบ",
            ArticleStatus.Published => "เผยแพร่แล้ว",
            ArticleStatus.Archived => "เก็บถาวร",
            _ => status.ToString()
        };
    }
    
    public static bool CanBePublishedDirectly(this ArticleStatus status)
    {
        return status == ArticleStatus.Draft || status == ArticleStatus.UnderReview;
    }
    
    public static bool RequiresReview(this ArticleStatus status)
    {
        return status == ArticleStatus.Draft;
    }
}