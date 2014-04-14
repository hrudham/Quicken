using Quicken.Core.Index.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quicken.Core.Index.Extensions;

namespace Quicken.Core.Index.Entities.Models
{
    public class Target
    {
        private ICollection<Alias> _Aliases;

        [Key]
        public int TargetId { get; set; }

        public virtual ICollection<Alias> Aliases
        {
            get 
            { 
                return this._Aliases ?? (this._Aliases = new List<Alias>()); 
            }
            set 
            {
                this._Aliases = value;
            } 
        }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Path { get; set; }

        public virtual IList<Term> Terms { get; set; }

        public string Description { get; set; }

        [Column(TypeName = "image")]
        [MaxLength]
        public byte[] Icon { get; set; }

        public DateTime UpdatedDate { get; set; }

        [Required]
        [DefaultValue(typeof(TargetType), "0")]
        public TargetType Platform { get; set; }

        /// <summary>
        /// Adds the alias.
        /// </summary>
        /// <param name="text">The name.</param>
        public void AddAlias(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                // Add both the text as it is, as well as in a normalized, non-diacritical 
                // form ("â" becomes "a" for example). Also only add aliases that do not 
                // exist already.
                var aliasTexts = new string[]
                {
                    text.ToUpperInvariant(),
                    text.RemoveDiacritics().ToUpperInvariant(),
                }
                .Distinct()
                .Except(
                    this.Aliases.Select(
                        alias => alias.Text));

                foreach (var aliasText in aliasTexts)
                {
                    this.Aliases.Add(
                        new Alias()
                        {
                            Text = aliasText
                        });
                }
            }
        }
    }
}
