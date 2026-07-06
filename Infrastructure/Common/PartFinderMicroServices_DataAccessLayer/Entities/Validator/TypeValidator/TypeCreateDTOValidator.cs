
using FluentValidation;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.TypeDTO;

public class TypeCreateDTOValidator : AbstractValidator<TypeCreateDTO>
{
    public TypeCreateDTOValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
       // RuleFor(x => x.Name_en).NotEmpty();
        RuleFor(x => x.SupplierId).GreaterThan(0);
    }
}
