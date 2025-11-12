using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using Semester03.Areas.Admin.Models;
using Semester03.Models.Entities;

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

        public TenantType FindById(int Id)
        {
            var tenantType = new TenantType();
            try
            {
                using var db = new AbcdmallContext();
                int idItem = Id;
                var q = db.TblTenantTypes
                          .Where(t => t.TenantTypeId == idItem)
                          .Select(t => new TenantType
                          {
                              Id = idItem,
                              Name = t.TenantTypeName,
                              Status = t.TenantTypeStatus ?? 0
                          })
                          .FirstOrDefault();
                if (q != null)
                {
                    tenantType = q;
                }
            }
            catch (Exception)
            {
                throw;
            }

            return tenantType;
        }

        public List<TenantType> GetAll()
        {
            var ls = new List<TenantType>();
            try
            {
                using var ct = new AbcdmallContext();
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

        public void Add(TenantType entity)
        {
            try
            {
                using var db = new AbcdmallContext();
                var item = new TblTenantType
                {
                    TenantTypeName = entity.Name,
                    TenantTypeStatus = entity.Status
                };
                db.TblTenantTypes.Add(item);
                db.SaveChanges();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public bool Delete(int Id)
        {
            try
            {
                using var db = new AbcdmallContext();
                var item = db.TblTenantTypes.SingleOrDefault(t => t.TenantTypeId == Id);
                if (item != null)
                {
                    db.TblTenantTypes.Remove(item);
                    var res = db.SaveChanges();
                    if (res > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return false;
        }

        public bool Update(TenantType entity)
        {
            try
            {
                using var db = new AbcdmallContext();
                var q = db.TblTenantTypes.SingleOrDefault(t => t.TenantTypeId == entity.Id);
                if (q != null)
                {
                    q.TenantTypeName = entity.Name;
                    q.TenantTypeStatus = entity.Status;
                    int res = db.SaveChanges();
                    return res > 0;
                }
            }
            catch (Exception)
            {
                throw;
            }
            return false;
        }

        public bool checkTenantTypeName(string Name, int? excludeId = null)
        {
            try
            {
                using var db = new AbcdmallContext();
                string normalizedInput = NormalizeName(Name);
                var allTenantTypeNames = db.TblTenantTypes
                                           .Where(t => !excludeId.HasValue || t.TenantTypeId != excludeId.Value)
                                           .Select(c => c.TenantTypeName)
                                           .ToList();

                return allTenantTypeNames.Any(dbName => NormalizeName(dbName) == normalizedInput);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public string NormalizeName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Bỏ khoảng trắng và chuyển về thường
            return new string(input.Where(c => !char.IsWhiteSpace(c)).ToArray()).ToLowerInvariant();
        }

        public string NormalizeSearch(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            string lower = input.ToLowerInvariant();
            string normalized = lower.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();
            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return new string(sb.ToString()
                .Where(c => !char.IsWhiteSpace(c))
                .ToArray());
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
        public string Name { get; set; } = string.Empty;
        public int Status { get; set; }
    }
}
