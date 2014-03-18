using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quicken.UI.Extensions;

namespace Quicken.UI.Extensions
{
    public static class EnumerationExtensions
    {
        public static string GetDisplayName(this Enum value)
        {
            var attribute = value.GetCustomAttribute<DisplayAttribute>();
            return attribute == null ? value.ToString() : attribute.Name;
        }
    }
}
