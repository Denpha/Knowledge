using FluentValidation;
using KMS.Application.DTOs.Knowledge;

namespace KMS.Application.Validators;

public class CreateArticleDtoValidator : AbstractValidator<CreateArticleDto>
{
    public CreateArticleDtoValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.");

        RuleFor(x => x.TitleEn)
            .MaximumLength(500).WithMessage("English title must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.TitleEn));

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MinimumLength(50).WithMessage("Content must be at least 50 characters.")
            .MaximumLength(10000).WithMessage("Content must not exceed 10,000 characters.");

        RuleFor(x => x.Summary)
            .NotEmpty().WithMessage("Summary is required.")
            .MaximumLength(500).WithMessage("Summary must not exceed 500 characters.");

        RuleFor(x => x.SummaryEn)
            .MaximumLength(500).WithMessage("English summary must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.SummaryEn));

        RuleFor(x => x.KeywordsEn)
            .MaximumLength(500).WithMessage("English keywords must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.KeywordsEn));

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category is required.");

        RuleFor(x => x.TagIds)
            .Must(ids => ids.Count <= 10).WithMessage("Maximum 10 tags allowed.");
    }
}

public class UpdateArticleDtoValidator : AbstractValidator<UpdateArticleDto>
{
    public UpdateArticleDtoValidator()
    {
        RuleFor(x => x.Title)
            .MaximumLength(500).WithMessage("Title must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Title));

        RuleFor(x => x.TitleEn)
            .MaximumLength(500).WithMessage("English title must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.TitleEn));

        RuleFor(x => x.Content)
            .MinimumLength(50).WithMessage("Content must be at least 50 characters.")
            .MaximumLength(10000).WithMessage("Content must not exceed 10,000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Content));

        RuleFor(x => x.Summary)
            .MaximumLength(500).WithMessage("Summary must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Summary));

        RuleFor(x => x.SummaryEn)
            .MaximumLength(500).WithMessage("English summary must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.SummaryEn));

        RuleFor(x => x.KeywordsEn)
            .MaximumLength(500).WithMessage("English keywords must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.KeywordsEn));

        RuleFor(x => x.TagIds)
            .Must(ids => ids == null || ids.Count <= 10).WithMessage("Maximum 10 tags allowed.")
            .When(x => x.TagIds != null);
    }
}