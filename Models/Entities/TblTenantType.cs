using System.Collections.Generic;


namespace Semester03.Models.Entities
{
    public partial class TblTenantType
    {
        public TblTenantType()
        {
            TblTenants = new HashSet<TblTenant>();
        }


        public int TenantTypeId { get; set; }
        public string TenantTypeName { get; set; }
        public int? TenantTypeStatus { get; set; }


        public virtual ICollection<TblTenant> TblTenants { get; set; }
    }
}