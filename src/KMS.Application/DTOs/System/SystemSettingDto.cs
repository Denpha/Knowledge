namespace KMS.Application.DTOs.System;

public class SystemSettingDto : BaseDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string DataType { get; set; } = "string";
    public bool IsEncrypted { get; set; }
    public bool IsSystem { get; set; }
}

public class CreateSystemSettingDto : CreateDto
{
    public string Key { get; set; } = string.Empty;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string DataType { get; set; } = "string";
    public bool IsEncrypted { get; set; }
    public bool IsSystem { get; set; }
}

public class UpdateSystemSettingDto : UpdateDto
{
    public string? Value { get; set; }
    public string? Description { get; set; }
    public string? DataType { get; set; }
    public bool? IsEncrypted { get; set; }
    public bool? IsSystem { get; set; }
}