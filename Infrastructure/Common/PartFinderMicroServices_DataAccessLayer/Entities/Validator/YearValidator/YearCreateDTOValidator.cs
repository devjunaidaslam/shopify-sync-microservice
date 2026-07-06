using FluentValidation;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.MakeDTO;
using PartFinderMicroServices_DataAccessLayer.Entities.DTOs.YearDTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_DataAccessLayer.Entities.Validator.YearValidator
{
    public class YearCreateDTOValidator : AbstractValidator<YearCreateDTO>
    {
        public YearCreateDTOValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(100).WithMessage("Name must not exceed 100 characters.");

            RuleFor(x => x.SupplierId)
                .NotNull().WithMessage("SupplierId is required.")
                .GreaterThan(0).WithMessage("SupplierId must be greater than 0.");

            //RuleFor(x => x.ReferenceId)
            //    .GreaterThan(1).When(x => x.ReferenceId.HasValue).WithMessage("ReferenceId must be greater than 0 if provided.");

            //RuleFor(x => x.LocalName)
            //    .MaximumLength(100).WithMessage("LocalName must not exceed 100 characters.")
            //    .When(x => !string.IsNullOrEmpty(x.LocalName));
        }
    }
}
