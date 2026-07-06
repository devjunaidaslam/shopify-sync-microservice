
using FluentValidation;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.VehicleDTO;

public class VehicleCreateDTOValidator : AbstractValidator<VehicleCreateDTO>
{
    public VehicleCreateDTOValidator()
    {
        RuleFor(x => x.TypeId).NotNull().GreaterThan(0);
        RuleFor(x => x.YearId).NotNull().GreaterThan(0);
        RuleFor(x => x.MakeId).NotNull().GreaterThan(0);
        RuleFor(x => x.ModelId).NotNull().GreaterThan(0);
    }
}
