
using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.TemporaryVehicleDTO;

public class TemporaryVehicleUpdateDTOValidator : AbstractValidator<TemporaryVehicleUpdateDTO>
{
    public TemporaryVehicleUpdateDTOValidator()
    {
        RuleFor(x => x.TempVehicleImportId).GreaterThan(0);
        RuleFor(x => x.type).NotEmpty();
        RuleFor(x => x.year).GreaterThan(0);
        RuleFor(x => x.make).NotEmpty();
        RuleFor(x => x.model).NotEmpty();
    }
}
