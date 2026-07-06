
using FluentValidation;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TemporaryVehicleDTO;

public class TemporaryVehicleCreateDTOValidator : AbstractValidator<TemporaryVehicleCreateDTO>
{
    public TemporaryVehicleCreateDTOValidator()
    {
        RuleFor(x => x.type).NotEmpty();
        RuleFor(x => x.year).GreaterThan(0);
        RuleFor(x => x.make).NotEmpty();
        RuleFor(x => x.model).NotEmpty();
    }
}
