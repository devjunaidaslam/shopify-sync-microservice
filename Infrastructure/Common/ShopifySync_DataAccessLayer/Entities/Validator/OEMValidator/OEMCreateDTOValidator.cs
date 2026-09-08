using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.OEMDTO;

public class OEMCreateDTOValidator : AbstractValidator<OEMCreateDTO>
{
    public OEMCreateDTOValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(255).WithMessage("Name cannot exceed 255 characters");
    }
}
