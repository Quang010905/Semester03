using Semester03.Models.Entities;

public partial class TblShowtimeSeat
{
    public int ShowtimeSeatId { get; set; }
    public int ShowtimeSeatShowtimeId { get; set; }
    public int ShowtimeSeatSeatId { get; set; }
    public string? ShowtimeSeatStatus { get; set; }
    public int? ShowtimeSeatReservedByUserId { get; set; }
    public DateTime? ShowtimeSeatReservedAt { get; set; }
    public DateTime? ShowtimeSeatUpdatedAt { get; set; }

    // NEW: movie id convenience column
    public int? ShowtimeSeatMovieId { get; set; }

    public virtual TblSeat? ShowtimeSeatSeat { get; set; }
    public virtual TblShowtime? ShowtimeSeatShowtime { get; set; }
    public virtual TblUser? ShowtimeSeatReservedByUser { get; set; }

    // new navigation
    public virtual TblMovie? ShowtimeSeatMovie { get; set; }
}
