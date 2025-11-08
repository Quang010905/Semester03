using System.Collections.Generic;


namespace Semester03.Models.Entities
{
    public partial class TblProductCategory
    {
        public TblProductCategory()
        {
            TblProducts = new HashSet<TblProduct>();
        }


        public int ProductCategoryId { get; set; }
        public string ProductCategoryName { get; set; }
        public string ProductCategoryImg { get; set; }
        public int? ProductCategoryStatus { get; set; }
        public int ProductCategoryTenantId { get; set; }


        public virtual TblTenant ProductCategoryTenant { get; set; }
        public virtual ICollection<TblProduct> TblProducts { get; set; }
    }
}