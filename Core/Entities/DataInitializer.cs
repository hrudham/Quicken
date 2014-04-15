using Quicken.Core.Index.Entities.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quicken.Core.Index.Entities
{
    internal class DataInitializer : DropCreateDatabaseIfModelChanges<DataContext>
    {
        /// <summary>
        /// Seeds the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        protected override void Seed(DataContext context)
        {
            base.Seed(context);

            context.Database.ExecuteSqlCommand(
                CreatIndexSql("Text", typeof(Alias)));
        }

        /// <summary>
        /// Creats the index SQL.
        /// </summary>
        /// <param name="field">The field.</param>
        /// <param name="table">The table.</param>
        /// <returns></returns>
        private string CreatIndexSql(string field, Type table)
        {
            return String.Format("CREATE INDEX IX_{0} ON {1} ({0})", field, table.Name);
        }   
    }
}
