using System.Collections.Generic;
using System;


namespace Semester03.Models.Entities
{
    public partial class TblCinema
    {
        public TblCinema()
        {
            TblScreens = new HashSet<TblScreen>();
        }


        public int CinemaId { get; set; }
        public string CinemaName { get; set; }
        public string CinemaImg { get; set; }
        public string CinemaDescription { get; set; }
        public DateTime? CinemaStartDate { get; set; }
        public int? CinemaTenantPositionId { get; set; }


        public virtual ICollection<TblScreen> TblScreens { get; set; }
    }
}