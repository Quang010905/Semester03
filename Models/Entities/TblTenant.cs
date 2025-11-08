using System;
using System.Collections.Generic;

namespace Semester03.Models.Entities
{
    public partial class TblTenant
    {
        public TblTenant()
        {
            TblTenantPositions = new HashSet<TblTenantPosition>();
            TblCustomerComplaints = new HashSet<TblCustomerComplaint>();
            TblEventBookings = new HashSet<TblEventBooking>();
            TblProductCategories = new HashSet<TblProductCategory>();
        }

        public int TenantId { get; set; }
        public string TenantName { get; set; } = null!;
        public string TenantImg { get; set; } = null!;
        public int TenantTypeId { get; set; }
        public int TenantUserId { get; set; }

        // DB column name: Tenant_Description
        public string? TenantDescription { get; set; }    // <-- fixes TenantDescription missing

        public DateTime? TenantCreatedAt { get; set; }

        // navigation
        public virtual TblTenantType? TenantType { get; set; }
        public virtual TblUser? TenantUser { get; set; }

        public virtual ICollection<TblTenantPosition> TblTenantPositions { get; set; } // <-- fixes TblTenantPositions missing
        public virtual ICollection<TblProductCategory> TblProductCategories { get; set; }
        public virtual ICollection<TblCustomerComplaint> TblCustomerComplaints { get; set; }
        public virtual ICollection<TblEventBooking> TblEventBookings { get; set; }
    }
}
