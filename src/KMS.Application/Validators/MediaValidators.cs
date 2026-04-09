using FluentValidation;
using KMS.Application.DTOs.Media;

namespace KMS.Application.Validators;

public class CreateMediaItemDtoValidator : AbstractValidator<CreateMediaItemDto>
{
    public CreateMediaItemDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.AltText)
            .MaximumLength(500).WithMessage("Alt text must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.AltText));

        RuleFor(x => x.CollectionName)
            .MaximumLength(100).WithMessage("Collection name must not exceed 100 characters.")
            .When(x => !string.IsNullOrEmpty(x.CollectionName));

        RuleFor(x => x.EntityType)
            .MaximumLength(50).WithMessage("Entity type must not exceed 50 characters.")
            .When(x => !string.IsNullOrEmpty(x.EntityType));

        RuleFor(x => x.EntityId)
            .NotEmpty().WithMessage("Entity ID is required when entity type is specified.")
            .When(x => !string.IsNullOrEmpty(x.EntityType));
    }
}

public class UpdateMediaItemDtoValidator : AbstractValidator<UpdateMediaItemDto>
{
    public UpdateMediaItemDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.AltText)
            .MaximumLength(500).WithMessage("Alt text must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.AltText));
    }
}