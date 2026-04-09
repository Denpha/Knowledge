namespace KMS.Domain.Enums;

/// <summary>
/// Text improvement types for AI writing assistant
/// </summary>
public enum ImprovementType
{
    /// <summary>
    /// Fix grammar, spelling, and punctuation errors
    /// </summary>
    Grammar = 1,
    
    /// <summary>
    /// Make text more concise and to the point
    /// </summary>
    Concise = 2,
    
    /// <summary>
    /// Make text more formal and professional
    /// </summary>
    Formal = 3,
    
    /// <summary>
    /// Expand on ideas with more details and examples
    /// </summary>
    Expand = 4,
    
    /// <summary>
    /// Simplify language for easier understanding
    /// </summary>
    Simplify = 5
}

public static class ImprovementTypeExtensions
{
    public static string ToDisplayString(this ImprovementType improvementType)
    {
        return improvementType switch
        {
            ImprovementType.Grammar => "แก้ไขไวยากรณ์และการสะกด",
            ImprovementType.Concise => "ทำให้กระชับขึ้น",
            ImprovementType.Formal => "ทำให้เป็นทางการมากขึ้น",
            ImprovementType.Expand => "เพิ่มรายละเอียด",
            ImprovementType.Simplify => "ทำให้เข้าใจง่ายขึ้น",
            _ => improvementType.ToString()
        };
    }

    public static string GetDescription(this ImprovementType improvementType)
    {
        return improvementType switch
        {
            ImprovementType.Grammar => "Fix grammar, spelling, and punctuation errors.",
            ImprovementType.Concise => "Make the text more concise and to the point.",
            ImprovementType.Formal => "Make the text more formal and professional.",
            ImprovementType.Expand => "Expand on the ideas with more details and examples.",
            ImprovementType.Simplify => "Simplify the language for easier understanding.",
            _ => improvementType.ToString()
        };
    }
}