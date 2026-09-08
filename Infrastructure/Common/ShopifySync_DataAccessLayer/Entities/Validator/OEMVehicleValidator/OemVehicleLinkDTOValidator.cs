using FluentValidation;
using ShopifySync_DataAccessLayer.Entities.DTOs.OEMVehicleDTO;

namespace ShopifySync_DataAccessLayer.Entities.Validator.OEMVehicleValidator
{
    public class OemVehicleLinkDTOValidator : AbstractValidator<OemVehicleLinkDTO>
    {
        public OemVehicleLinkDTOValidator()
        {
            RuleFor(x => x.OEMId)
                .GreaterThan(0).WithMessage("OEMId must be greater than 0");

            RuleFor(x => x.VehicleId)
                .GreaterThan(0).WithMessage("VehicleId must be greater than 0");
        }
    }
}
