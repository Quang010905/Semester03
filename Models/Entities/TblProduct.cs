using System;


namespace Semester03.Models.Entities
{
    public partial class TblProduct
    {
        public int ProductId { get; set; }
        public string ProductImg { get; set; }
        public int ProductCategoryId { get; set; }
        public string ProductName { get; set; }
        public string ProductDescription { get; set; }
        public decimal ProductPrice { get; set; }
        public int? ProductStatus { get; set; }
        public DateTime? ProductCreatedAt { get; set; }


        public virtual TblProductCategory ProductCategory { get; set; }
    }
}