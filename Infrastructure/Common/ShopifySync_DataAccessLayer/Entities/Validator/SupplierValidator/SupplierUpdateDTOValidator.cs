
using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.Supplier;

public class SupplierUpdateDTOValidator : AbstractValidator<SupplierUpdateDTO>
{
    public SupplierUpdateDTOValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
    }
}
