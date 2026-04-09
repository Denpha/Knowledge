using FluentValidation;
using KMS.Application.DTOs.Interaction;

namespace KMS.Application.Validators;

public class CreateCommentDtoValidator : AbstractValidator<CreateCommentDto>
{
    public CreateCommentDtoValidator()
    {
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MinimumLength(2).WithMessage("Content must be at least 2 characters.")
            .MaximumLength(2000).WithMessage("Content must not exceed 2000 characters.");

        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("Article ID is required.");

        RuleFor(x => x.ParentId)
            .Must((dto, parentId) => 
            {
                // If ParentId is provided, it must be a valid Guid
                if (parentId.HasValue && parentId.Value == Guid.Empty)
                {
                    return false;
                }
                return true;
            })
            .WithMessage("Invalid parent comment ID.");
    }
}

public class UpdateCommentDtoValidator : AbstractValidator<UpdateCommentDto>
{
    public UpdateCommentDtoValidator()
    {
        RuleFor(x => x.Content)
            .MinimumLength(2).WithMessage("Content must be at least 2 characters.")
            .MaximumLength(2000).WithMessage("Content must not exceed 2000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Content));
    }
}