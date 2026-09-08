
using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.TypeDTO;

public class TypeUpdateDTOValidator : AbstractValidator<TypeUpdateDTO>
{
    public TypeUpdateDTOValidator()
    {
        RuleFor(x => x.VehicleTypesId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
       // RuleFor(x => x.Name_en).NotEmpty();
        RuleFor(x => x.SupplierId).GreaterThan(0);
    }
}
