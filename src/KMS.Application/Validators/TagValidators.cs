using FluentValidation;
using KMS.Application.DTOs.Knowledge;

namespace KMS.Application.Validators;

public class CreateTagDtoValidator : AbstractValidator<CreateTagDto>
{
    public CreateTagDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name is required.")
            .MaximumLength(100).WithMessage("Tag name must not exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9ก-๙\s\-_]+$").WithMessage("Tag name can only contain letters, numbers, spaces, hyphens, and underscores.");
    }
}

public class UpdateTagDtoValidator : AbstractValidator<UpdateTagDto>
{
    public UpdateTagDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tag name cannot be empty.")
            .MaximumLength(100).WithMessage("Tag name must not exceed 100 characters.")
            .Matches(@"^[a-zA-Z0-9ก-๙\s\-_]+$").WithMessage("Tag name can only contain letters, numbers, spaces, hyphens, and underscores.")
            .When(x => !string.IsNullOrEmpty(x.Name));
    }
}