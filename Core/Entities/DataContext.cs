using Core.Entities.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    internal class DataContext : DbContext
    {
        public DbSet<Target> Targets { get; set; }
        public DbSet<Term> Terms { get; set; }
    }
}
