using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.OEMDTO;

public class OEMUpdateDTOValidator : AbstractValidator<OEMUpdateDTO>
{
    public OEMUpdateDTOValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Id must be greater than 0");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");
    }
}
