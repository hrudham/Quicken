using System;
using System.Reflection;

namespace Quicken.UI.Extensions
{
    public static class ObjectExtensions
    {
        /// <summary>
        /// Gets the custom attribute.
        /// </summary>
        /// <typeparam name="TAttribute">The type of the attribute.</typeparam>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static TAttribute GetCustomAttribute<TAttribute>(this object value) where TAttribute : Attribute
        {
            return Attribute.GetCustomAttribute(
                value.GetType().GetField(value.ToString()), 
                typeof(TAttribute)) as TAttribute;
        }
    }
}
