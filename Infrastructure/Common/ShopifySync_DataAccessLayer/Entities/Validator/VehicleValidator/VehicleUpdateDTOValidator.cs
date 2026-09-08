
using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.VehicleDTO;

public class VehicleUpdateDTOValidator : AbstractValidator<VehicleUpdateDTO>
{
    public VehicleUpdateDTOValidator()
    {
        RuleFor(x => x.VehicleId).GreaterThan(0);
        RuleFor(x => x.TypeId).NotNull().GreaterThan(0);
        RuleFor(x => x.YearId).NotNull().GreaterThan(0);
        RuleFor(x => x.MakeId).NotNull().GreaterThan(0);
        RuleFor(x => x.ModelId).NotNull().GreaterThan(0);
    }
}
