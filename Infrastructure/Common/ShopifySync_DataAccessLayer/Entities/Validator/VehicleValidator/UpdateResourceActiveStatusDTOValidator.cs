using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.VehicleDTO;

namespace ShopifySync_DataAccessLayer.Entities.Validator.VehicleValidator
{
    public class UpdateResourceActiveStatusDTOValidator : AbstractValidator<UpdateResourceActiveStatusDTO>
    {
        public UpdateResourceActiveStatusDTOValidator()
        {
            RuleFor(x => x.ResourceId)
                .GreaterThan(0)
                .WithMessage("ResourceId must be greater than 0");

            RuleFor(x => x.ResourceType)
                .NotEmpty()
                .WithMessage("ResourceType is required")
                .Must(BeValidResourceType)
                .WithMessage("ResourceType must be one of: type, year, make, model");
        }

        private bool BeValidResourceType(string resourceType)
        {
            if (string.IsNullOrEmpty(resourceType))
                return false;

            var validTypes = new[] { "type", "year", "make", "model" };
            return validTypes.Contains(resourceType.ToLower());
        }
    }
}
