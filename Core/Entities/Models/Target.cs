using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities.Models
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

        public string ProductName { get; set; }

        public byte[] Icon { get; set; }

        public DateTime UpdatedDate { get; set; }
    }
}
