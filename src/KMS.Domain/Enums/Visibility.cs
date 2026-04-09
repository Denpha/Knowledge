namespace KMS.Domain.Enums;

public enum Visibility
{
    Public = 1,
    Internal = 2,
    Restricted = 3
}

public static class VisibilityExtensions
{
    public static string ToDisplayString(this Visibility visibility)
    {
        return visibility switch
        {
            Visibility.Public => "สาธารณะ",
            Visibility.Internal => "ภายในองค์กร",
            Visibility.Restricted => "จำกัดการเข้าถึง",
            _ => visibility.ToString()
        };
    }
    
    public static bool IsAccessibleByPublic(this Visibility visibility)
    {
        return visibility == Visibility.Public;
    }
    
    public static bool IsAccessibleByInternal(this Visibility visibility)
    {
        return visibility == Visibility.Public || visibility == Visibility.Internal;
    }
}