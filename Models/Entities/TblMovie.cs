using System.Collections.Generic;
using System;


namespace Semester03.Models.Entities
{
    public partial class TblMovie
    {
        public TblMovie()
        {
            TblShowtimes = new HashSet<TblShowtime>();
        }


        public int MovieId { get; set; }
        public string MovieTitle { get; set; }
        public string MovieGenre { get; set; }
        public string MovieDirector { get; set; }
        public string MovieImg { get; set; }
        public DateTime MovieStartDate { get; set; }
        public DateTime MovieEndDate { get; set; }
        public int MovieRate { get; set; }
        public int MovieDurationMin { get; set; }
        public string MovieDescription { get; set; }
        public int? MovieStatus { get; set; }


        public virtual ICollection<TblShowtime> TblShowtimes { get; set; }
    }
}