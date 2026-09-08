
using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.Supplier;

public class SupplierCreateDTOValidator : AbstractValidator<SupplierCreateDTO>
{
    public SupplierCreateDTOValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
    }
}
