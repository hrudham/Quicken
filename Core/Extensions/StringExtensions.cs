﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Quicken.Core.Index.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Removes the diacritics.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <remarks>Source: http://stackoverflow.com/questions/249087/how-do-i-remove-diacritics-accents-from-a-string-in-net#answer-249126</remarks>
        public static string RemoveDiacritics(this string source)
        {
            var normalizedString = source.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Gets the abbreviation.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <returns></returns>
        public static string GetAbbreviation(this string source)
        {
            return source.GetAbbreviation(false);
        }

        /// <summary>
        /// Gets the abbreviation.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="abbreviateNumbers">if set to <c>true</c> [abbreviate numbers].</param>
        /// <returns></returns>
        public static string GetAbbreviation(this string source, bool abbreviateNumbers)
        {
            var numberRegex = new Regex("\\d+(\\.\\d+)?");

            var words = source.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var resultStringBuilder = new StringBuilder();

            foreach(var word in words)
            {
                string abbrChararcter = word.Substring(0, 1);

                if (!abbreviateNumbers && numberRegex.IsMatch(word))
                {
                    abbrChararcter = word;
                }

                resultStringBuilder.Append(abbrChararcter);
            }

            return resultStringBuilder.ToString();
        }
    }
}
