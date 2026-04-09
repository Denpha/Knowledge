namespace KMS.Domain.Enums;

/// <summary>
/// Publishing modes for Publish-First workflow
/// </summary>
public enum PublishMode
{
    /// <summary>
    /// Direct publish (no review required)
    /// </summary>
    Direct = 1,
    
    /// <summary>
    /// Requires review before publishing
    /// </summary>
    ReviewRequired = 2,
    
    /// <summary>
    /// Auto-publish based on role and content
    /// </summary>
    Auto = 3
}

public static class PublishModeExtensions
{
    public static string ToDisplayString(this PublishMode publishMode)
    {
        return publishMode switch
        {
            PublishMode.Direct => "เผยแพร่โดยตรง",
            PublishMode.ReviewRequired => "ต้องตรวจสอบก่อนเผยแพร่",
            PublishMode.Auto => "เผยแพร่อัตโนมัติ",
            _ => publishMode.ToString()
        };
    }

    public static bool RequiresReview(this PublishMode publishMode)
    {
        return publishMode == PublishMode.ReviewRequired;
    }

    public static bool CanPublishDirectly(this PublishMode publishMode)
    {
        return publishMode == PublishMode.Direct || publishMode == PublishMode.Auto;
    }
}