﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quicken.Core.Index.Entities.Models
{
    public class Alias
    {
        [Key]
        public int AliasId {get;set;}

        [Required]
        [ForeignKey("Target")]
        public int TargetId { get; set; }

        public virtual Target Target { get; set; }

        public string Text { get; set; }
    }
}
