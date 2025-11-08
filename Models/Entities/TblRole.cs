using System.Collections.Generic;


namespace Semester03.Models.Entities
{
    public partial class TblRole
    {
        public TblRole()
        {
            TblUsers = new HashSet<TblUser>();
        }


        public int RolesId { get; set; }
        public string RolesName { get; set; }
        public string RolesDescription { get; set; }


        public virtual ICollection<TblUser> TblUsers { get; set; }
    }
}