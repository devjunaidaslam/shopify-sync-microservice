
using FluentValidation;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.ModelDTO;

public class ModelCreateDTOValidator : AbstractValidator<ModelCreateDTO>
{
    public ModelCreateDTOValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.SupplierId).NotNull().WithMessage("SupplierId is required.");
       // RuleFor(x => x.ReferenceId).GreaterThan(0).When(x => x.ReferenceId.HasValue);
       // RuleFor(x => x.LocalName).MaximumLength(100);
    }
}
