using Semester03.Areas.Admin.Models;
using Semester03.Models.Entities;
using System.ComponentModel;

namespace Semester03.Models.Repositories
{
    public class TenantTypeRepository
    {
        private static TenantTypeRepository _instance = null;
        private TenantTypeRepository() { }
        public static TenantTypeRepository Instance
        {
            get
            {
                _instance = _instance ?? new TenantTypeRepository();
                return _instance;

            }
        }
        public List<TenantType> GetAll()
        {
            var ls = new List<TenantType>();    

            try
            {
                var ct = new AbcdmallContext();
                ls = ct.TblTenantTypes.Select(x => new TenantType
                {
                    Id = x.TenantTypeId,
                    Name = x.TenantTypeName,
                    Status = x.TenantTypeStatus ?? 0
                }).ToList();
            }   
            catch (Exception)
            {

                throw;
            }
            return ls;
        }
    }
}
