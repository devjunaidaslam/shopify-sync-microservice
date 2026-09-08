
using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.ModelDTO;

public class ModelUpdateDTOValidator : AbstractValidator<ModelUpdateDTO>
{
    public ModelUpdateDTOValidator()
    {
        RuleFor(x => x.VehicleModelId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.SupplierId).NotNull();
     //   RuleFor(x => x.ReferenceId).GreaterThan(0).When(x => x.ReferenceId.HasValue);
     //   RuleFor(x => x.LocalName).MaximumLength(100);
    }
}
