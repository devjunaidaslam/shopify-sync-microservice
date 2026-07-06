using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartFinderMicroServices_BusinessLogicLayer.Infrastructure
{
    public static class EnumExtensions
    {
        // public static string GetEnumDisplayName<T>(T value) where T : struct
        // {
        //     var memberInfo = value.GetType().GetMember(value.ToString()).FirstOrDefault();
        //     if (memberInfo == null)
        //         return value.ToString();

        //     var displayAttribute = memberInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
        //                                       .OfType<DisplayAttribute>().FirstOrDefault();
        //     return displayAttribute?.Name ?? value.ToString();
        // }

        public static string GetEnumDisplayName(this Enum value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Enum value cannot be null");
            }

            var memberInfo = value.GetType().GetMember(value.ToString());
            if (memberInfo.Length > 0)
            {
                var attributes = memberInfo[0].GetCustomAttributes(typeof(DisplayAttribute), false);
                if (attributes.Length > 0)
                {
                    return ((DisplayAttribute)attributes[0]).Name ?? value.ToString();
                }
            }

            return value.ToString();
        }
    }
}
