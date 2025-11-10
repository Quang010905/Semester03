using Semester03.Areas.Admin.Models;
using Semester03.Models.Entities;
using System.ComponentModel;
using System.Linq;

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

        // Lấy TenantType theo tên
        public TenantType? GetTenantTypeByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            try
            {
                using var ct = new AbcdmallContext();
                return ct.TblTenantTypes
                         .Where(t => t.TenantTypeName == name)
                         .Select(x => new TenantType
                         {
                             Id = x.TenantTypeId,
                             Name = x.TenantTypeName,
                             Status = x.TenantTypeStatus ?? 0
                         })
                         .FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }

    public class TenantType
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int Status { get; set; }
    }
}
