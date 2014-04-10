using Quicken.Core.Index.Enumerations;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quicken.Core.Index.Entities.Models
{
    public class Target
    {
        [Key]
        public int TargetId { get; set; }

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
    }
}
