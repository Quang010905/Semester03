using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Semester03.Models.Entities;

public partial class AbcdmallContext : DbContext
{
    public AbcdmallContext()
    {
    }

    public AbcdmallContext(DbContextOptions<AbcdmallContext> options)
        : base(options)
    {
    }

    public virtual DbSet<TblCinema> TblCinemas { get; set; }

    public virtual DbSet<TblCoupon> TblCoupons { get; set; }

    public virtual DbSet<TblCouponUser> TblCouponUsers { get; set; }

    public virtual DbSet<TblCustomerComplaint> TblCustomerComplaints { get; set; }

    public virtual DbSet<TblEvent> TblEvents { get; set; }

    public virtual DbSet<TblEventBooking> TblEventBookings { get; set; }

    public virtual DbSet<TblEventBookingHistory> TblEventBookingHistories { get; set; }

    public virtual DbSet<TblMovie> TblMovies { get; set; }

    public virtual DbSet<TblNotification> TblNotifications { get; set; }

    public virtual DbSet<TblParkingLevel> TblParkingLevels { get; set; }

    public virtual DbSet<TblParkingSpot> TblParkingSpots { get; set; }

    public virtual DbSet<TblProduct> TblProducts { get; set; }

    public virtual DbSet<TblProductCategory> TblProductCategories { get; set; }

    public virtual DbSet<TblRole> TblRoles { get; set; }

    public virtual DbSet<TblScreen> TblScreens { get; set; }

    public virtual DbSet<TblSeat> TblSeats { get; set; }

    public virtual DbSet<TblShowtime> TblShowtimes { get; set; }

    public virtual DbSet<TblShowtimeSeat> TblShowtimeSeats { get; set; }

    public virtual DbSet<TblTenant> TblTenants { get; set; }

    public virtual DbSet<TblTenantPosition> TblTenantPositions { get; set; }

    public virtual DbSet<TblTenantPromotion> TblTenantPromotions { get; set; }

    public virtual DbSet<TblTenantType> TblTenantTypes { get; set; }

    public virtual DbSet<TblTicket> TblTickets { get; set; }

    public virtual DbSet<TblUser> TblUsers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=(local);Database=ABCDMall;uid=sa;pwd=123;Trusted_Connection=True;TrustServerCertificate=true;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TblCinema>(entity =>
        {
            entity.HasKey(e => e.CinemaId).HasName("PK__Tbl_Cine__89C6DAE1F923C028");

            entity.ToTable("Tbl_Cinema");

            entity.Property(e => e.CinemaId).HasColumnName("Cinema_ID");
            entity.Property(e => e.CinemaDescription)
                .HasMaxLength(1000)
                .HasColumnName("Cinema_Description");
            entity.Property(e => e.CinemaImg)
                .HasMaxLength(250)
                .HasColumnName("Cinema_Img");
            entity.Property(e => e.CinemaName)
                .HasMaxLength(200)
                .HasColumnName("Cinema_Name");
        });

        modelBuilder.Entity<TblCoupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("PK__Tbl_Coup__2A776BBC65038CFA");

            entity.ToTable("Tbl_Coupon");

            entity.HasIndex(e => e.CouponName, "UQ__Tbl_Coup__4AFC9B50FB11FAE1").IsUnique();

            entity.Property(e => e.CouponId).HasColumnName("Coupon_ID");
            entity.Property(e => e.CouponDescription)
                .HasMaxLength(400)
                .HasColumnName("Coupon_Description");
            entity.Property(e => e.CouponDiscountPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("Coupon_DiscountPercent");
            entity.Property(e => e.CouponIsActive)
                .HasDefaultValue(true)
                .HasColumnName("Coupon_IsActive");
            entity.Property(e => e.CouponMinimumPointsRequired).HasColumnName("Coupon_MinimumPointsRequired");
            entity.Property(e => e.CouponName)
                .HasMaxLength(100)
                .HasColumnName("Coupon_Name");
            entity.Property(e => e.CouponValidFrom).HasColumnName("Coupon_ValidFrom");
            entity.Property(e => e.CouponValidTo).HasColumnName("Coupon_ValidTo");
        });

        modelBuilder.Entity<TblCouponUser>(entity =>
        {
            entity.HasKey(e => e.CouponUserId).HasName("PK__Tbl_Coup__3B4F48E06FD682FE");

            entity.ToTable("Tbl_CouponUser");

            entity.Property(e => e.CouponUserId).HasColumnName("CouponUser_ID");
            entity.Property(e => e.CouponId).HasColumnName("Coupon_ID");
            entity.Property(e => e.UsersId).HasColumnName("Users_ID");

            entity.HasOne(d => d.Coupon).WithMany(p => p.TblCouponUsers)
                .HasForeignKey(d => d.CouponId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_CouponUser_Tbl_Coupon");

            entity.HasOne(d => d.Users).WithMany(p => p.TblCouponUsers)
                .HasForeignKey(d => d.UsersId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_CouponUser_Tbl_Users");
        });

        modelBuilder.Entity<TblCustomerComplaint>(entity =>
        {
            entity.HasKey(e => e.CustomerComplaintId).HasName("PK__Tbl_Cust__EF854084497929B1");

            entity.ToTable("Tbl_CustomerComplaint");

            entity.Property(e => e.CustomerComplaintId).HasColumnName("CustomerComplaint_ID");
            entity.Property(e => e.CustomerComplaintCreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("CustomerComplaint_CreatedAt");
            entity.Property(e => e.CustomerComplaintCustomerUserId).HasColumnName("CustomerComplaint_CustomerUserID");
            entity.Property(e => e.CustomerComplaintDescription)
                .HasMaxLength(2000)
                .HasColumnName("CustomerComplaint_Description");
            entity.Property(e => e.CustomerComplaintEventId).HasColumnName("CustomerComplaint_EventID");
            entity.Property(e => e.CustomerComplaintMovieId).HasColumnName("CustomerComplaint_MovieID");
            entity.Property(e => e.CustomerComplaintRate).HasColumnName("CustomerComplaint_Rate");
            entity.Property(e => e.CustomerComplaintStatus)
                .HasDefaultValue(0)
                .HasColumnName("CustomerComplaint_Status");
            entity.Property(e => e.CustomerComplaintTenantId).HasColumnName("CustomerComplaint_TenantID");

            entity.HasOne(d => d.CustomerComplaintCustomerUser).WithMany(p => p.TblCustomerComplaints)
                .HasForeignKey(d => d.CustomerComplaintCustomerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_CustomerComplaint_Tbl_Users");

            entity.HasOne(d => d.CustomerComplaintTenant).WithMany(p => p.TblCustomerComplaints)
                .HasForeignKey(d => d.CustomerComplaintTenantId)
                .HasConstraintName("FK_Tbl_CustomerComplaint_Tbl_Tenant");
        });

        modelBuilder.Entity<TblEvent>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Tbl_Even__FD6BEFE4A21B3DB1");

            entity.ToTable("Tbl_Event");

            entity.Property(e => e.EventId).HasColumnName("Event_ID");
            entity.Property(e => e.EventDescription)
                .HasMaxLength(1000)
                .HasColumnName("Event_Description");
            entity.Property(e => e.EventEnd).HasColumnName("Event_End");
            entity.Property(e => e.EventImg)
                .HasMaxLength(300)
                .HasColumnName("Event_Img");
            entity.Property(e => e.EventMaxSlot).HasColumnName("Event_MaxSlot");
            entity.Property(e => e.EventName)
                .HasMaxLength(300)
                .HasColumnName("Event_Name");
            entity.Property(e => e.EventStart).HasColumnName("Event_Start");
            entity.Property(e => e.EventStatus)
                .HasDefaultValue(1)
                .HasColumnName("Event_Status");
            entity.Property(e => e.EventTenantPositionId).HasColumnName("Event_TenantPositionID");
            entity.Property(e => e.EventUnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Event_Unit_Price");

            entity.HasOne(d => d.EventTenantPosition).WithMany(p => p.TblEvents)
                .HasForeignKey(d => d.EventTenantPositionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Event_Tbl_TenantPosition");
        });

        modelBuilder.Entity<TblEventBooking>(entity =>
        {
            entity.HasKey(e => e.EventBookingId).HasName("PK__Tbl_Even__B471E4EA6943D260");

            entity.ToTable("Tbl_EventBooking");

            entity.Property(e => e.EventBookingId).HasColumnName("EventBooking_ID");
            entity.Property(e => e.EventBookingCreatedDate)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("EventBooking_CreatedDate");
            entity.Property(e => e.EventBookingDate).HasColumnName("EventBooking_Date");
            entity.Property(e => e.EventBookingEventId).HasColumnName("EventBooking_EventID");
            entity.Property(e => e.EventBookingNotes).HasColumnName("EventBooking_Notes");
            entity.Property(e => e.EventBookingOrderGroup).HasColumnName("EventBooking_OrderGroup");
            entity.Property(e => e.EventBookingPaymentStatus)
                .HasDefaultValue(0)
                .HasColumnName("EventBooking_Payment_Status");
            entity.Property(e => e.EventBookingQuantity)
                .HasDefaultValue(1)
                .HasColumnName("EventBooking_Quantity");
            entity.Property(e => e.EventBookingStatus)
                .HasDefaultValue(1)
                .HasColumnName("EventBooking_Status");
            entity.Property(e => e.EventBookingTenantId).HasColumnName("EventBooking_TenantID");
            entity.Property(e => e.EventBookingTotalCost)
                .HasDefaultValue(0m)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("EventBooking_Total_Cost");
            entity.Property(e => e.EventBookingUnitPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("EventBooking_UnitPrice");
            entity.Property(e => e.EventBookingUserId).HasColumnName("EventBooking_UserID");

            entity.HasOne(d => d.EventBookingEvent).WithMany(p => p.TblEventBookings)
                .HasForeignKey(d => d.EventBookingEventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_EventBooking_Event");
        });

        modelBuilder.Entity<TblEventBookingHistory>(entity =>
        {
            entity.HasKey(e => e.EventBookingHistoryId).HasName("PK__Tbl_Even__F6344F95E6094BD9");

            entity.ToTable("Tbl_EventBookingHistory");

            entity.Property(e => e.EventBookingHistoryId).HasColumnName("EventBookingHistory_ID");
            entity.Property(e => e.EventBookingHistoryAction)
                .HasMaxLength(100)
                .HasColumnName("EventBookingHistory_Action");
            entity.Property(e => e.EventBookingHistoryBookingId).HasColumnName("EventBookingHistory_BookingID");
            entity.Property(e => e.EventBookingHistoryCreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("EventBookingHistory_CreatedAt");
            entity.Property(e => e.EventBookingHistoryDetails).HasColumnName("EventBookingHistory_Details");
            entity.Property(e => e.EventBookingHistoryEventId).HasColumnName("EventBookingHistory_EventID");
            entity.Property(e => e.EventBookingHistoryQuantity).HasColumnName("EventBookingHistory_Quantity");
            entity.Property(e => e.EventBookingHistoryRelatedDate).HasColumnName("EventBookingHistory_RelatedDate");
            entity.Property(e => e.EventBookingHistoryUserId).HasColumnName("EventBookingHistory_UserID");

            entity.HasOne(d => d.EventBookingHistoryBooking).WithMany(p => p.TblEventBookingHistories)
                .HasForeignKey(d => d.EventBookingHistoryBookingId)
                .HasConstraintName("FK_EventBookingHistory_EventBooking");

            entity.HasOne(d => d.EventBookingHistoryEvent).WithMany(p => p.TblEventBookingHistories)
                .HasForeignKey(d => d.EventBookingHistoryEventId)
                .HasConstraintName("FK_EventBookingHistory_Event");
        });

        modelBuilder.Entity<TblMovie>(entity =>
        {
            entity.HasKey(e => e.MovieId).HasName("PK__Tbl_Movi__7A8804058DCFCCB0");

            entity.ToTable("Tbl_Movie");

            entity.Property(e => e.MovieId).HasColumnName("Movie_ID");
            entity.Property(e => e.MovieDescription)
                .HasMaxLength(1000)
                .HasColumnName("Movie_Description");
            entity.Property(e => e.MovieDirector)
                .HasMaxLength(250)
                .HasColumnName("Movie_Director");
            entity.Property(e => e.MovieDurationMin).HasColumnName("Movie_Duration_Min");
            entity.Property(e => e.MovieEndDate).HasColumnName("Movie_EndDate");
            entity.Property(e => e.MovieGenre)
                .HasMaxLength(250)
                .HasColumnName("Movie_Genre");
            entity.Property(e => e.MovieImg)
                .HasMaxLength(250)
                .HasColumnName("Movie_Img");
            entity.Property(e => e.MovieRate).HasColumnName("Movie_Rate");
            entity.Property(e => e.MovieStartDate).HasColumnName("Movie_StartDate");
            entity.Property(e => e.MovieStatus)
                .HasDefaultValue(1)
                .HasColumnName("Movie_Status");
            entity.Property(e => e.MovieTitle)
                .HasMaxLength(300)
                .HasColumnName("Movie_Title");
        });

        modelBuilder.Entity<TblNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Tbl_Noti__8C1160B560F3E13B");

            entity.ToTable("Tbl_Notification");

            entity.Property(e => e.NotificationId).HasColumnName("Notification_ID");
            entity.Property(e => e.NotificationBody)
                .HasMaxLength(2000)
                .HasColumnName("Notification_Body");
            entity.Property(e => e.NotificationChannel)
                .HasMaxLength(50)
                .HasColumnName("Notification_Channel");
            entity.Property(e => e.NotificationCreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Notification_CreatedAt");
            entity.Property(e => e.NotificationIsRead)
                .HasDefaultValue(false)
                .HasColumnName("Notification_IsRead");
            entity.Property(e => e.NotificationTitle)
                .HasMaxLength(300)
                .HasColumnName("Notification_Title");
            entity.Property(e => e.NotificationUserId).HasColumnName("Notification_UserID");

            entity.HasOne(d => d.NotificationUser).WithMany(p => p.TblNotifications)
                .HasForeignKey(d => d.NotificationUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Notification_Tbl_Users");
        });

        modelBuilder.Entity<TblParkingLevel>(entity =>
        {
            entity.HasKey(e => e.LevelId).HasName("PK__Tbl_Park__C4322E604AB82A78");

            entity.ToTable("Tbl_ParkingLevel");

            entity.Property(e => e.LevelId).HasColumnName("Level_ID");
            entity.Property(e => e.LevelCapacity).HasColumnName("Level_Capacity");
            entity.Property(e => e.LevelName)
                .HasMaxLength(100)
                .HasColumnName("Level_Name");
        });

        modelBuilder.Entity<TblParkingSpot>(entity =>
        {
            entity.HasKey(e => e.ParkingSpotId).HasName("PK__Tbl_Park__B45C14DCF7C749A9");

            entity.ToTable("Tbl_ParkingSpot");

            entity.Property(e => e.ParkingSpotId).HasColumnName("ParkingSpot_ID");
            entity.Property(e => e.SpotCode)
                .HasMaxLength(50)
                .HasColumnName("Spot_Code");
            entity.Property(e => e.SpotCol).HasColumnName("Spot_Col");
            entity.Property(e => e.SpotLevelId).HasColumnName("Spot_LevelID");
            entity.Property(e => e.SpotRow)
                .HasMaxLength(5)
                .HasColumnName("Spot_Row");
            entity.Property(e => e.SpotStatus).HasColumnName("Spot_Status");

            entity.HasOne(d => d.SpotLevel).WithMany(p => p.TblParkingSpots)
                .HasForeignKey(d => d.SpotLevelId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_ParkingSpot_Tbl_ParkingLevel");
        });

        modelBuilder.Entity<TblProduct>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Tbl_Prod__9834FB9A90B1C99F");

            entity.ToTable("Tbl_Product");

            entity.Property(e => e.ProductId).HasColumnName("Product_ID");
            entity.Property(e => e.ProductCategoryId).HasColumnName("Product_CategoryID");
            entity.Property(e => e.ProductCreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Product_CreatedAt");
            entity.Property(e => e.ProductDescription)
                .HasMaxLength(1000)
                .HasColumnName("Product_Description");
            entity.Property(e => e.ProductImg)
                .HasMaxLength(300)
                .HasColumnName("Product_Img");
            entity.Property(e => e.ProductName)
                .HasMaxLength(300)
                .HasColumnName("Product_Name");
            entity.Property(e => e.ProductPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Product_Price");
            entity.Property(e => e.ProductStatus)
                .HasDefaultValue(1)
                .HasColumnName("Product_Status");

            entity.HasOne(d => d.ProductCategory).WithMany(p => p.TblProducts)
                .HasForeignKey(d => d.ProductCategoryId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Product_Tbl_ProductCategory");
        });

        modelBuilder.Entity<TblProductCategory>(entity =>
        {
            entity.HasKey(e => e.ProductCategoryId).HasName("PK__Tbl_Prod__4C1E45D93A0CFB25");

            entity.ToTable("Tbl_ProductCategory");

            entity.Property(e => e.ProductCategoryId).HasColumnName("ProductCategory_ID");
            entity.Property(e => e.ProductCategoryImg)
                .HasMaxLength(300)
                .HasColumnName("ProductCategory_Img");
            entity.Property(e => e.ProductCategoryName)
                .HasMaxLength(200)
                .HasColumnName("ProductCategory_Name");
            entity.Property(e => e.ProductCategoryStatus)
                .HasDefaultValue(1)
                .HasColumnName("ProductCategory_Status");
            entity.Property(e => e.ProductCategoryTenantId).HasColumnName("ProductCategory_TenantID");

            entity.HasOne(d => d.ProductCategoryTenant).WithMany(p => p.TblProductCategories)
                .HasForeignKey(d => d.ProductCategoryTenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_ProductCategory_Tbl_Tenant");
        });

        modelBuilder.Entity<TblRole>(entity =>
        {
            entity.HasKey(e => e.RolesId).HasName("PK__Tbl_Role__BB563FB7CD739449");

            entity.ToTable("Tbl_Roles");

            entity.Property(e => e.RolesId).HasColumnName("Roles_ID");
            entity.Property(e => e.RolesDescription)
                .HasMaxLength(400)
                .HasColumnName("Roles_Description");
            entity.Property(e => e.RolesName)
                .HasMaxLength(100)
                .HasColumnName("Roles_Name");
        });

        modelBuilder.Entity<TblScreen>(entity =>
        {
            entity.HasKey(e => e.ScreenId).HasName("PK__Tbl_Scre__1D3FB5CB91D69EE9");

            entity.ToTable("Tbl_Screen");

            entity.Property(e => e.ScreenId).HasColumnName("Screen_ID");
            entity.Property(e => e.ScreenCinemaId).HasColumnName("Screen_CinemaID");
            entity.Property(e => e.ScreenName)
                .HasMaxLength(100)
                .HasColumnName("Screen_Name");
            entity.Property(e => e.ScreenSeats).HasColumnName("Screen_Seats");

            entity.HasOne(d => d.ScreenCinema).WithMany(p => p.TblScreens)
                .HasForeignKey(d => d.ScreenCinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Screen_Tbl_Cinema");
        });

        modelBuilder.Entity<TblSeat>(entity =>
        {
            entity.HasKey(e => e.SeatId).HasName("PK__Tbl_Seat__8B2CE7B6AB36B7C5");

            entity.ToTable("Tbl_Seat");

            entity.Property(e => e.SeatId).HasColumnName("Seat_ID");
            entity.Property(e => e.SeatCol).HasColumnName("Seat_Col");
            entity.Property(e => e.SeatIsActive)
                .HasDefaultValue(true)
                .HasColumnName("Seat_IsActive");
            entity.Property(e => e.SeatLabel)
                .HasMaxLength(10)
                .HasColumnName("Seat_Label");
            entity.Property(e => e.SeatRow)
                .HasMaxLength(5)
                .HasColumnName("Seat_Row");
            entity.Property(e => e.SeatScreenId).HasColumnName("Seat_ScreenID");

            entity.HasOne(d => d.SeatScreen).WithMany(p => p.TblSeats)
                .HasForeignKey(d => d.SeatScreenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Seat_Tbl_Screen");
        });

        modelBuilder.Entity<TblShowtime>(entity =>
        {
            entity.HasKey(e => e.ShowtimeId).HasName("PK__Tbl_Show__7C7A908950BB5B62");

            entity.ToTable("Tbl_Showtime");

            entity.Property(e => e.ShowtimeId).HasColumnName("Showtime_ID");
            entity.Property(e => e.ShowtimeMovieId).HasColumnName("Showtime_MovieID");
            entity.Property(e => e.ShowtimePrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Showtime_Price");
            entity.Property(e => e.ShowtimeScreenId).HasColumnName("Showtime_ScreenID");
            entity.Property(e => e.ShowtimeStart).HasColumnName("Showtime_Start");

            entity.HasOne(d => d.ShowtimeMovie).WithMany(p => p.TblShowtimes)
                .HasForeignKey(d => d.ShowtimeMovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Showtime_Tbl_Movie");

            entity.HasOne(d => d.ShowtimeScreen).WithMany(p => p.TblShowtimes)
                .HasForeignKey(d => d.ShowtimeScreenId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Showtime_Tbl_Screen");
        });

        modelBuilder.Entity<TblShowtimeSeat>(entity =>
        {
            entity.HasKey(e => e.ShowtimeSeatId).HasName("PK__Tbl_Show__A216F9D2534AA03B");

            entity.ToTable("Tbl_ShowtimeSeat");

            entity.HasIndex(e => e.ShowtimeSeatSeatId, "IX_ShowtimeSeat_Seat");

            entity.HasIndex(e => new { e.ShowtimeSeatShowtimeId, e.ShowtimeSeatStatus }, "IX_ShowtimeSeat_Showtime_Status");

            entity.HasIndex(e => new { e.ShowtimeSeatShowtimeId, e.ShowtimeSeatSeatId }, "UQ_ShowtimeSeat_Showtime_Seat").IsUnique();

            entity.Property(e => e.ShowtimeSeatId).HasColumnName("ShowtimeSeat_ID");
            entity.Property(e => e.ShowtimeSeatReservedAt).HasColumnName("ShowtimeSeat_ReservedAt");
            entity.Property(e => e.ShowtimeSeatReservedByUserId).HasColumnName("ShowtimeSeat_ReservedByUserID");
            entity.Property(e => e.ShowtimeSeatSeatId).HasColumnName("ShowtimeSeat_SeatID");
            entity.Property(e => e.ShowtimeSeatShowtimeId).HasColumnName("ShowtimeSeat_ShowtimeID");
            entity.Property(e => e.ShowtimeSeatStatus)
                .HasMaxLength(20)
                .HasDefaultValue("available")
                .HasColumnName("ShowtimeSeat_Status");
            entity.Property(e => e.ShowtimeSeatUpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("ShowtimeSeat_UpdatedAt");

            entity.HasOne(d => d.ShowtimeSeatReservedByUser).WithMany(p => p.TblShowtimeSeats)
                .HasForeignKey(d => d.ShowtimeSeatReservedByUserId)
                .HasConstraintName("FK_Tbl_ShowtimeSeat_Tbl_Users");

            entity.HasOne(d => d.ShowtimeSeatSeat).WithMany(p => p.TblShowtimeSeats)
                .HasForeignKey(d => d.ShowtimeSeatSeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_ShowtimeSeat_Tbl_Seat");

            entity.HasOne(d => d.ShowtimeSeatShowtime).WithMany(p => p.TblShowtimeSeats)
                .HasForeignKey(d => d.ShowtimeSeatShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_ShowtimeSeat_Tbl_Showtime");
        });

        modelBuilder.Entity<TblTenant>(entity =>
        {
            entity.HasKey(e => e.TenantId).HasName("PK__Tbl_Tena__8E8F3472A1242C43");

            entity.ToTable("Tbl_Tenant");

            entity.Property(e => e.TenantId).HasColumnName("Tenant_ID");
            entity.Property(e => e.TenantCreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Tenant_CreatedAt");
            entity.Property(e => e.TenantDescription)
                .HasMaxLength(1000)
                .HasColumnName("Tenant_Description");
            entity.Property(e => e.TenantImg)
                .HasMaxLength(300)
                .HasColumnName("Tenant_Img");
            entity.Property(e => e.TenantName)
                .HasMaxLength(300)
                .HasColumnName("Tenant_Name");
            entity.Property(e => e.TenantStatus)
                .HasDefaultValue(1)
                .HasColumnName("Tenant_Status");
            entity.Property(e => e.TenantTypeId).HasColumnName("Tenant_TypeID");
            entity.Property(e => e.TenantUserId).HasColumnName("Tenant_UserID");

            entity.HasOne(d => d.TenantType).WithMany(p => p.TblTenants)
                .HasForeignKey(d => d.TenantTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Tenant_Tbl_TenantType");

            entity.HasOne(d => d.TenantUser).WithMany(p => p.TblTenants)
                .HasForeignKey(d => d.TenantUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Tenant_Tbl_Users");
        });

        modelBuilder.Entity<TblTenantPosition>(entity =>
        {
            entity.HasKey(e => e.TenantPositionId).HasName("PK__Tbl_Tena__3AD42952A8ADAEC7");

            entity.ToTable("Tbl_TenantPosition");

            entity.Property(e => e.TenantPositionId).HasColumnName("TenantPosition_ID");
            entity.Property(e => e.PositionLeaseEnd).HasColumnName("Position_LeaseEnd");
            entity.Property(e => e.PositionLeaseStart).HasColumnName("Position_LeaseStart");
            entity.Property(e => e.TenantPositionAreaM2)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("TenantPosition_Area_M2");
            entity.Property(e => e.TenantPositionAssignedCinemaId).HasColumnName("TenantPosition_AssignedCinemaID");
            entity.Property(e => e.TenantPositionAssignedTenantId).HasColumnName("TenantPosition_AssignedTenantID");
            entity.Property(e => e.TenantPositionFloor).HasColumnName("TenantPosition_Floor");
            entity.Property(e => e.TenantPositionLeftPct)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("TenantPosition_LeftPct");
            entity.Property(e => e.TenantPositionLocation)
                .HasMaxLength(250)
                .HasColumnName("TenantPosition_Location");
            entity.Property(e => e.TenantPositionRentPricePerM2)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TenantPosition_Rent_Price_Per_M2");
            entity.Property(e => e.TenantPositionStatus)
                .HasDefaultValue(0)
                .HasColumnName("TenantPosition_Status");
            entity.Property(e => e.TenantPositionTopPct)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("TenantPosition_TopPct");

            entity.HasOne(d => d.TenantPositionAssignedTenant).WithMany(p => p.TblTenantPositions)
                .HasForeignKey(d => d.TenantPositionAssignedTenantId)
                .HasConstraintName("FK_Tbl_TenantPosition_Tbl_Tenant");
        });

        modelBuilder.Entity<TblTenantPromotion>(entity =>
        {
            entity.HasKey(e => e.TenantPromotionId).HasName("PK__Tbl_Tena__C6E66251CEF2C28F");

            entity.ToTable("Tbl_TenantPromotion");

            entity.Property(e => e.TenantPromotionId).HasColumnName("TenantPromotion_ID");
            entity.Property(e => e.TenantPromotionDescription)
                .HasMaxLength(1000)
                .HasColumnName("TenantPromotion_Description");
            entity.Property(e => e.TenantPromotionDiscountAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TenantPromotion_DiscountAmount");
            entity.Property(e => e.TenantPromotionDiscountPercent)
                .HasColumnType("decimal(5, 2)")
                .HasColumnName("TenantPromotion_DiscountPercent");
            entity.Property(e => e.TenantPromotionEnd).HasColumnName("TenantPromotion_End");
            entity.Property(e => e.TenantPromotionImg)
                .HasMaxLength(300)
                .HasColumnName("TenantPromotion_Img");
            entity.Property(e => e.TenantPromotionMinBillAmount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TenantPromotion_MinBillAmount");
            entity.Property(e => e.TenantPromotionStart).HasColumnName("TenantPromotion_Start");
            entity.Property(e => e.TenantPromotionStatus)
                .HasDefaultValue(1)
                .HasColumnName("TenantPromotion_Status");
            entity.Property(e => e.TenantPromotionTenantId).HasColumnName("TenantPromotion_TenantID");
            entity.Property(e => e.TenantPromotionTitle)
                .HasMaxLength(300)
                .HasColumnName("TenantPromotion_Title");

            entity.HasOne(d => d.TenantPromotionTenant).WithMany(p => p.TblTenantPromotions)
                .HasForeignKey(d => d.TenantPromotionTenantId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_TenantPromotion_Tbl_Tenant");
        });

        modelBuilder.Entity<TblTenantType>(entity =>
        {
            entity.HasKey(e => e.TenantTypeId).HasName("PK__Tbl_Tena__7AEEA9C4B4D544FF");

            entity.ToTable("Tbl_TenantType");

            entity.Property(e => e.TenantTypeId).HasColumnName("TenantType_ID");
            entity.Property(e => e.TenantTypeName)
                .HasMaxLength(250)
                .HasColumnName("TenantType_Name");
            entity.Property(e => e.TenantTypeStatus)
                .HasDefaultValue(1)
                .HasColumnName("TenantType_Status");
        });

        modelBuilder.Entity<TblTicket>(entity =>
        {
            entity.HasKey(e => e.TicketId).HasName("PK__Tbl_Tick__ED7260D966576C49");

            entity.ToTable("Tbl_Ticket");

            entity.Property(e => e.TicketId).HasColumnName("Ticket_ID");
            entity.Property(e => e.TicketBuyerUserId).HasColumnName("Ticket_BuyerUserID");
            entity.Property(e => e.TicketCreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Ticket_CreatedAt");
            entity.Property(e => e.TicketPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Ticket_Price");
            entity.Property(e => e.TicketShowtimeSeatId).HasColumnName("Ticket_ShowtimeSeatID");
            entity.Property(e => e.TicketStatus)
                .HasMaxLength(50)
                .HasDefaultValue("sold")
                .HasColumnName("Ticket_Status");
            entity.Property(e => e.TicketUpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Ticket_UpdatedAt");

            entity.HasOne(d => d.TicketBuyerUser).WithMany(p => p.TblTickets)
                .HasForeignKey(d => d.TicketBuyerUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Ticket_Tbl_Users");

            entity.HasOne(d => d.TicketShowtimeSeat).WithMany(p => p.TblTickets)
                .HasForeignKey(d => d.TicketShowtimeSeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Ticket_Tbl_ShowtimeSeat");
        });

        modelBuilder.Entity<TblUser>(entity =>
        {
            entity.HasKey(e => e.UsersId).HasName("PK__Tbl_User__EB68290DE49CECD8");

            entity.ToTable("Tbl_Users");

            entity.HasIndex(e => e.UsersUsername, "UQ__Tbl_User__76886E48B9FF0D69").IsUnique();

            entity.HasIndex(e => e.UsersEmail, "UQ__Tbl_User__7F0D8B4243BCC6EF").IsUnique();

            entity.Property(e => e.UsersId).HasColumnName("Users_ID");
            entity.Property(e => e.UsersCreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Users_CreatedAt");
            entity.Property(e => e.UsersEmail)
                .HasMaxLength(200)
                .HasColumnName("Users_Email");
            entity.Property(e => e.UsersFullName)
                .HasMaxLength(200)
                .HasColumnName("Users_FullName");
            entity.Property(e => e.UsersPassword)
                .HasMaxLength(400)
                .HasColumnName("Users_Password");
            entity.Property(e => e.UsersPhone)
                .HasMaxLength(50)
                .HasColumnName("Users_Phone");
            entity.Property(e => e.UsersPoints)
                .HasDefaultValue(0)
                .HasColumnName("Users_Points");
            entity.Property(e => e.UsersRoleChangeReason).HasColumnName("Users_RoleChangeReason");
            entity.Property(e => e.UsersRoleId).HasColumnName("Users_RoleID");
            entity.Property(e => e.UsersStatus)
                .HasDefaultValue(1)
                .HasColumnName("Users_Status");
            entity.Property(e => e.UsersUpdatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Users_UpdatedAt");
            entity.Property(e => e.UsersUsername)
                .HasMaxLength(100)
                .HasColumnName("Users_Username");

            entity.HasOne(d => d.UsersRole).WithMany(p => p.TblUsers)
                .HasForeignKey(d => d.UsersRoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Users_Tbl_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
