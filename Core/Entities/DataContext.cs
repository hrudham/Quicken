using Quicken.Core.Index.Entities.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quicken.Core.Index.Entities
{
    internal class DataContext : DbContext
    {
        public DbSet<Target> Targets { get; set; }
        public DbSet<Term> Terms { get; set; }
        public DbSet<Alias> Aliases { get; set; }
    }
}
