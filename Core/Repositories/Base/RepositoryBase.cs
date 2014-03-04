using Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repositories.Base
{
    internal abstract class RepositoryBase
    {
        #region Fields

        private readonly DataContext _dataContext; 

        #endregion

        #region Properties

        /// <summary>
        /// Gets the strongly typed OpenLeaseContext for the repository
        /// </summary>
        protected DataContext DataContext
        {
            get
            {
                return this._dataContext;
            }
        } 

        #endregion

        #region Constructor

        /// <summary>
        /// The base constructor for the repository to be implemented by all repository implementations
        /// </summary>
        /// <param name="principleProvider">The principle provider.</param>
        protected RepositoryBase()
        {
            this._dataContext = new DataContext();
        }

        #endregion
    }
}
