using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.TemporaryVehicleDTO;

namespace ShopifySync_DataAccessLayer.Entities.Validator.TempVehicleValidator
{
    public class BulkUpdateVehicleAttributeDTOValidator : AbstractValidator<BulkUpdateVehicleAttributeDTO>
    {
        public BulkUpdateVehicleAttributeDTOValidator()
        {
            RuleFor(x => x.TempVehicleImportId)
                .GreaterThan(0)
                .WithMessage("TempVehicleImportId must be greater than 0");

            RuleFor(x => x.AttributeType)
                .NotEmpty()
                .WithMessage("AttributeType is required")
                .Must(BeValidAttributeType)
                .WithMessage("AttributeType must be one of: type, make, year, model");

            RuleFor(x => x.AttributeValue)
                .NotEmpty()
                .WithMessage("AttributeValue is required")
                .MaximumLength(255)
                .WithMessage("AttributeValue cannot exceed 255 characters");

            RuleFor(x => x.NewAttributeId)
                .GreaterThan(0)
                .When(x => x.NewAttributeId.HasValue)
                .WithMessage("NewAttributeId must be greater than 0 when provided");

            RuleFor(x => x)
                .Must(HaveEitherIdOrDefault)
                .WithMessage("Either NewAttributeId must be provided or SetAsDefault must be true, but not both");
        }

        private bool BeValidAttributeType(string attributeType)
        {
            if (string.IsNullOrEmpty(attributeType))
                return false;

            var validTypes = new[] { "type", "make", "year", "model" };
            return validTypes.Contains(attributeType.ToLower());
        }

        private bool HaveEitherIdOrDefault(BulkUpdateVehicleAttributeDTO dto)
        {
            // Either NewAttributeId is provided OR SetAsDefault is true, but not both
            return (dto.NewAttributeId.HasValue && !dto.SetAsDefault) || 
                   (!dto.NewAttributeId.HasValue && dto.SetAsDefault);
        }
    }
}
