using System;
using Microsoft.EntityFrameworkCore;

namespace Semester03.Models.Entities
{
    public partial class AbcdmallContext : DbContext
    {
        public AbcdmallContext()
        {
        }

        public AbcdmallContext(DbContextOptions<AbcdmallContext> options)
            : base(options)
        {
        }

        public virtual DbSet<TblRole> TblRoles { get; set; }
        public virtual DbSet<TblUser> TblUsers { get; set; }
        public virtual DbSet<TblTenantType> TblTenantTypes { get; set; }
        public virtual DbSet<TblTenant> TblTenants { get; set; }
        public virtual DbSet<TblTenantPosition> TblTenantPositions { get; set; }
        public virtual DbSet<TblProductCategory> TblProductCategories { get; set; }
        public virtual DbSet<TblProduct> TblProducts { get; set; }
        public virtual DbSet<TblCinema> TblCinemas { get; set; }
        public virtual DbSet<TblScreen> TblScreens { get; set; }
        public virtual DbSet<TblMovie> TblMovies { get; set; }
        public virtual DbSet<TblShowtime> TblShowtimes { get; set; }
        public virtual DbSet<TblSeat> TblSeats { get; set; }
        public virtual DbSet<TblShowtimeSeat> TblShowtimeSeats { get; set; }
        public virtual DbSet<TblTicket> TblTickets { get; set; }
        public virtual DbSet<TblCoupon> TblCoupons { get; set; }
        public virtual DbSet<TblCouponUser> TblCouponUsers { get; set; }
        public virtual DbSet<TblParkingSpot> TblParkingSpots { get; set; }
        public virtual DbSet<TblCustomerComplaint> TblCustomerComplaints { get; set; }
        public virtual DbSet<TblEvent> TblEvents { get; set; }
        public virtual DbSet<TblEventBooking> TblEventBookings { get; set; }
        public virtual DbSet<TblNotification> TblNotifications { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning Move connection string out of source for production
            => optionsBuilder.UseSqlServer("Server=(local);Database=ABCDMall;uid=sa;pwd=123456789;Trusted_Connection=True;TrustServerCertificate=true;");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Tbl_Roles
            modelBuilder.Entity<TblRole>(entity =>
            {
                entity.ToTable("Tbl_Roles");
                entity.HasKey(e => e.RolesId);
                entity.Property(e => e.RolesId).HasColumnName("Roles_ID");
                entity.Property(e => e.RolesName).HasColumnName("Roles_Name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.RolesDescription).HasColumnName("Roles_Description").HasMaxLength(400);
            });

            // Tbl_Users
            modelBuilder.Entity<TblUser>(entity =>
            {
                entity.ToTable("Tbl_Users");
                entity.HasKey(e => e.UsersId);
                entity.Property(e => e.UsersId).HasColumnName("Users_ID");
                entity.Property(e => e.UsersUsername).HasColumnName("Users_Username").HasMaxLength(100).IsRequired();
                entity.Property(e => e.UsersPassword).HasColumnName("Users_Password").HasMaxLength(400).IsRequired();
                entity.Property(e => e.UsersFullName).HasColumnName("Users_FullName").HasMaxLength(200).IsRequired();
                entity.Property(e => e.UsersEmail).HasColumnName("Users_Email").HasMaxLength(200).IsRequired();
                entity.Property(e => e.UsersPhone).HasColumnName("Users_Phone").HasMaxLength(50).IsRequired();
                entity.Property(e => e.UsersRoleId).HasColumnName("Users_RoleID").IsRequired();
                entity.Property(e => e.UsersPoints).HasColumnName("Users_Points");
                entity.Property(e => e.UsersCreatedAt).HasColumnName("Users_CreatedAt").HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.UsersUpdatedAt).HasColumnName("Users_UpdatedAt").HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(d => d.UsersRole)
                      .WithMany(p => p.TblUsers)
                      .HasForeignKey(d => d.UsersRoleId)
                      .HasConstraintName("FK_Tbl_Users_Tbl_Roles");
            });

            // TenantType
            modelBuilder.Entity<TblTenantType>(entity =>
            {
                entity.ToTable("Tbl_TenantType");
                entity.HasKey(e => e.TenantTypeId);
                entity.Property(e => e.TenantTypeId).HasColumnName("TenantType_ID");
                entity.Property(e => e.TenantTypeName).HasColumnName("TenantType_Name").HasMaxLength(250).IsRequired();
                entity.Property(e => e.TenantTypeStatus).HasColumnName("TenantType_Status").HasDefaultValue(1);
            });

            // Tenant
            modelBuilder.Entity<TblTenant>(entity =>
            {
                entity.ToTable("Tbl_Tenant");
                entity.HasKey(e => e.TenantId);
                entity.Property(e => e.TenantId).HasColumnName("Tenant_ID");
                entity.Property(e => e.TenantName).HasColumnName("Tenant_Name").HasMaxLength(300).IsRequired();
                entity.Property(e => e.TenantImg).HasColumnName("Tenant_Img").HasMaxLength(300).IsRequired();
                entity.Property(e => e.TenantTypeId).HasColumnName("Tenant_TypeID").IsRequired();
                entity.Property(e => e.TenantUserId).HasColumnName("Tenant_UserID").IsRequired();
                entity.Property(e => e.TenantDescription).HasColumnName("Tenant_Description").HasMaxLength(1000);
                entity.Property(e => e.TenantCreatedAt).HasColumnName("Tenant_CreatedAt").HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(d => d.TenantType)
                    .WithMany(p => p.TblTenants)
                    .HasForeignKey(d => d.TenantTypeId)
                    .HasConstraintName("FK_Tbl_Tenant_Tbl_TenantType");

                entity.HasOne(d => d.TenantUser)
                    .WithMany(p => p.TblTenants)
                    .HasForeignKey(d => d.TenantUserId)
                    .HasConstraintName("FK_Tbl_Tenant_Tbl_Users");
            });

            // TenantPosition
            modelBuilder.Entity<TblTenantPosition>(entity =>
            {
                entity.ToTable("Tbl_TenantPosition");
                entity.HasKey(e => e.TenantPositionId);
                entity.Property(e => e.TenantPositionId).HasColumnName("TenantPosition_ID");
                entity.Property(e => e.TenantPositionLocation).HasColumnName("TenantPosition_Location").HasMaxLength(250).IsRequired();
                entity.Property(e => e.TenantPositionFloor).HasColumnName("TenantPosition_Floor").IsRequired();
                entity.Property(e => e.TenantPositionAreaM2).HasColumnName("TenantPosition_Area_M2").HasColumnType("decimal(10,2)");
                entity.Property(e => e.TenantPositionRentPricePerM2).HasColumnName("TenantPosition_Rent_Price_Per_M2").HasColumnType("decimal(18,2)");
                entity.Property(e => e.TenantPositionStatus).HasColumnName("TenantPosition_Status").HasDefaultValue(0);
                entity.Property(e => e.TenantPositionAssignedTenantId).HasColumnName("TenantPosition_AssignedTenantID");

                entity.HasOne(d => d.TenantPositionAssignedTenant)
                      .WithMany(p => p.TblTenantPositions)
                      .HasForeignKey(d => d.TenantPositionAssignedTenantId)
                      .HasConstraintName("FK_Tbl_TenantPosition_Tbl_Tenant");
            });

            // ProductCategory
            modelBuilder.Entity<TblProductCategory>(entity =>
            {
                entity.ToTable("Tbl_ProductCategory");
                entity.HasKey(e => e.ProductCategoryId);
                entity.Property(e => e.ProductCategoryId).HasColumnName("ProductCategory_ID");
                entity.Property(e => e.ProductCategoryName).HasColumnName("ProductCategory_Name").HasMaxLength(200).IsRequired();
                entity.Property(e => e.ProductCategoryImg).HasColumnName("ProductCategory_Img").HasMaxLength(300).IsRequired();
                entity.Property(e => e.ProductCategoryStatus).HasColumnName("ProductCategory_Status").HasDefaultValue(1);
                entity.Property(e => e.ProductCategoryTenantId).HasColumnName("ProductCategory_TenantID").IsRequired();

                entity.HasOne(d => d.ProductCategoryTenant)
                      .WithMany(p => p.TblProductCategories)
                      .HasForeignKey(d => d.ProductCategoryTenantId)
                      .HasConstraintName("FK_Tbl_ProductCategory_Tbl_Tenant");
            });

            // Product
            modelBuilder.Entity<TblProduct>(entity =>
            {
                entity.ToTable("Tbl_Product");
                entity.HasKey(e => e.ProductId);
                entity.Property(e => e.ProductId).HasColumnName("Product_ID");
                entity.Property(e => e.ProductImg).HasColumnName("Product_Img").HasMaxLength(300).IsRequired();
                entity.Property(e => e.ProductCategoryId).HasColumnName("Product_CategoryID").IsRequired();
                entity.Property(e => e.ProductName).HasColumnName("Product_Name").HasMaxLength(300).IsRequired();
                entity.Property(e => e.ProductDescription).HasColumnName("Product_Description").HasMaxLength(1000).IsRequired();
                entity.Property(e => e.ProductPrice).HasColumnName("Product_Price").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.ProductStatus).HasColumnName("Product_Status").HasDefaultValue(1);
                entity.Property(e => e.ProductCreatedAt).HasColumnName("Product_CreatedAt").HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(d => d.ProductCategory)
                      .WithMany(p => p.TblProducts)
                      .HasForeignKey(d => d.ProductCategoryId)
                      .HasConstraintName("FK_Tbl_Product_Tbl_ProductCategory");
            });

            // Cinema
            modelBuilder.Entity<TblCinema>(entity =>
            {
                entity.ToTable("Tbl_Cinema");
                entity.HasKey(e => e.CinemaId);
                entity.Property(e => e.CinemaId).HasColumnName("Cinema_ID");
                entity.Property(e => e.CinemaName).HasColumnName("Cinema_Name").HasMaxLength(200).IsRequired();
                entity.Property(e => e.CinemaImg).HasColumnName("Cinema_Img").HasMaxLength(250);
                entity.Property(e => e.CinemaDescription).HasColumnName("Cinema_Description").HasMaxLength(1000);
            });

            // Screen
            modelBuilder.Entity<TblScreen>(entity =>
            {
                entity.ToTable("Tbl_Screen");
                entity.HasKey(e => e.ScreenId);
                entity.Property(e => e.ScreenId).HasColumnName("Screen_ID");
                entity.Property(e => e.ScreenCinemaId).HasColumnName("Screen_CinemaID").IsRequired();
                entity.Property(e => e.ScreenName).HasColumnName("Screen_Name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.ScreenSeats).HasColumnName("Screen_Seats").IsRequired();

                entity.HasOne(d => d.ScreenCinema)
                      .WithMany(p => p.TblScreens)
                      .HasForeignKey(d => d.ScreenCinemaId)
                      .HasConstraintName("FK_Tbl_Screen_Tbl_Cinema");
            });

            // Movie
            modelBuilder.Entity<TblMovie>(entity =>
            {
                entity.ToTable("Tbl_Movie");
                entity.HasKey(e => e.MovieId);
                entity.Property(e => e.MovieId).HasColumnName("Movie_ID");
                entity.Property(e => e.MovieTitle).HasColumnName("Movie_Title").HasMaxLength(300).IsRequired();
                entity.Property(e => e.MovieGenre).HasColumnName("Movie_Genre").HasMaxLength(250).IsRequired();
                entity.Property(e => e.MovieDirector).HasColumnName("Movie_Director").HasMaxLength(250).IsRequired();
                entity.Property(e => e.MovieImg).HasColumnName("Movie_Img").HasMaxLength(250).IsRequired();
                entity.Property(e => e.MovieStartDate).HasColumnName("Movie_StartDate").IsRequired();
                entity.Property(e => e.MovieEndDate).HasColumnName("Movie_EndDate").IsRequired();
                entity.Property(e => e.MovieRate).HasColumnName("Movie_Rate").IsRequired();
                entity.Property(e => e.MovieDurationMin).HasColumnName("Movie_Duration_Min").IsRequired();
                entity.Property(e => e.MovieDescription).HasColumnName("Movie_Description").HasMaxLength(1000);
                entity.Property(e => e.MovieStatus).HasColumnName("Movie_Status").HasDefaultValue(1);
            });

            // Showtime
            modelBuilder.Entity<TblShowtime>(entity =>
            {
                entity.ToTable("Tbl_Showtime");
                entity.HasKey(e => e.ShowtimeId);
                entity.Property(e => e.ShowtimeId).HasColumnName("Showtime_ID");
                entity.Property(e => e.ShowtimeScreenId).HasColumnName("Showtime_ScreenID").IsRequired();
                entity.Property(e => e.ShowtimeMovieId).HasColumnName("Showtime_MovieID").IsRequired();
                entity.Property(e => e.ShowtimeStart).HasColumnName("Showtime_Start").IsRequired();
                entity.Property(e => e.ShowtimePrice).HasColumnName("Showtime_Price").HasColumnType("decimal(18,2)").IsRequired();

                entity.HasOne(d => d.ShowtimeScreen)
                      .WithMany(p => p.TblShowtimes)
                      .HasForeignKey(d => d.ShowtimeScreenId)
                      .HasConstraintName("FK_Tbl_Showtime_Tbl_Screen");

                entity.HasOne(d => d.ShowtimeMovie)
                      .WithMany(p => p.TblShowtimes)
                      .HasForeignKey(d => d.ShowtimeMovieId)
                      .HasConstraintName("FK_Tbl_Showtime_Tbl_Movie");
            });

            // Seat
            modelBuilder.Entity<TblSeat>(entity =>
            {
                entity.ToTable("Tbl_Seat");
                entity.HasKey(e => e.SeatId);
                entity.Property(e => e.SeatId).HasColumnName("Seat_ID");
                entity.Property(e => e.SeatScreenId).HasColumnName("Seat_ScreenID").IsRequired();
                entity.Property(e => e.SeatLabel).HasColumnName("Seat_Label").HasMaxLength(10).IsRequired();
                entity.Property(e => e.SeatRow).HasColumnName("Seat_Row").HasMaxLength(5).IsRequired();
                entity.Property(e => e.SeatCol).HasColumnName("Seat_Col").IsRequired();
                entity.Property(e => e.SeatIsActive).HasColumnName("Seat_IsActive").HasDefaultValue(true);

                entity.HasOne(d => d.SeatScreen)
                      .WithMany(p => p.TblSeats)
                      .HasForeignKey(d => d.SeatScreenId)
                      .HasConstraintName("FK_Tbl_Seat_Tbl_Screen");
            });

            // ShowtimeSeat
            modelBuilder.Entity<TblShowtimeSeat>(entity =>
            {
                entity.ToTable("Tbl_ShowtimeSeat");
                entity.HasKey(e => e.ShowtimeSeatId);
                entity.Property(e => e.ShowtimeSeatId).HasColumnName("ShowtimeSeat_ID");
                entity.Property(e => e.ShowtimeSeatShowtimeId).HasColumnName("ShowtimeSeat_ShowtimeID").IsRequired();
                entity.Property(e => e.ShowtimeSeatSeatId).HasColumnName("ShowtimeSeat_SeatID").IsRequired();
                entity.Property(e => e.ShowtimeSeatStatus).HasColumnName("ShowtimeSeat_Status").HasMaxLength(20).HasDefaultValue("available");
                entity.Property(e => e.ShowtimeSeatReservedByUserId).HasColumnName("ShowtimeSeat_ReservedByUserID").IsRequired();
                entity.Property(e => e.ShowtimeSeatReservedAt).HasColumnName("ShowtimeSeat_ReservedAt").IsRequired();
                entity.Property(e => e.ShowtimeSeatUpdatedAt).HasColumnName("ShowtimeSeat_UpdatedAt").HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(d => d.ShowtimeSeatShowtime)
                      .WithMany()
                      .HasForeignKey(d => d.ShowtimeSeatShowtimeId)
                      .HasConstraintName("FK_Tbl_ShowtimeSeat_Tbl_Showtime");

                entity.HasOne(d => d.ShowtimeSeatSeat)
                      .WithMany()
                      .HasForeignKey(d => d.ShowtimeSeatSeatId)
                      .HasConstraintName("FK_Tbl_ShowtimeSeat_Tbl_Seat");

                entity.HasOne(d => d.ShowtimeSeatReservedByUser)
                      .WithMany(p => p.ReservedShowtimeSeats)
                      .HasForeignKey(d => d.ShowtimeSeatReservedByUserId)
                      .HasConstraintName("FK_Tbl_ShowtimeSeat_Tbl_Users");
            });

            // Ticket
            modelBuilder.Entity<TblTicket>(entity =>
            {
                entity.ToTable("Tbl_Ticket");
                entity.HasKey(e => e.TicketId);
                entity.Property(e => e.TicketId).HasColumnName("Ticket_ID");
                entity.Property(e => e.TicketShowtimeSeatId).HasColumnName("Ticket_ShowtimeSeatID").IsRequired();
                entity.Property(e => e.TicketBuyerUserId).HasColumnName("Ticket_BuyerUserID").IsRequired();
                entity.Property(e => e.TicketStatus).HasColumnName("Ticket_Status").HasMaxLength(50).HasDefaultValue("sold");
                entity.Property(e => e.TicketPrice).HasColumnName("Ticket_Price").HasColumnType("decimal(18,2)").IsRequired();
                entity.Property(e => e.TicketCreatedAt).HasColumnName("Ticket_CreatedAt").HasDefaultValueSql("(sysutcdatetime())");
                entity.Property(e => e.TicketUpdatedAt).HasColumnName("Ticket_UpdatedAt").HasDefaultValueSql("(sysutcdatetime())");

                // map buyer user
                entity.HasOne(d => d.TicketBuyerUser)
                      .WithMany(p => p.TblTickets)
                      .HasForeignKey(d => d.TicketBuyerUserId)
                      .HasConstraintName("FK_Tbl_Ticket_Tbl_Users");

                // Note: your DB script previously set an FK from Ticket_ShowtimeSeatID -> Tbl_Showtime (mismatch).
                // Here we try to map to Tbl_ShowtimeSeat (logical). If the DB has conflicting FK, EF may throw at runtime.
                entity.HasOne(d => d.TicketShowtimeSeat)
                      .WithMany(p => p.TblTickets)
                      .HasForeignKey(d => d.TicketShowtimeSeatId)
                      .HasConstraintName("FK_Tbl_Ticket_Tbl_ShowtimeSeat");
            });

            // Coupon / CouponUser
            modelBuilder.Entity<TblCoupon>(entity =>
            {
                entity.ToTable("Tbl_Coupon");
                entity.HasKey(e => e.CouponId);
                entity.Property(e => e.CouponId).HasColumnName("Coupon_ID");
                entity.Property(e => e.CouponName).HasColumnName("Coupon_Name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.CouponDescription).HasColumnName("Coupon_Description").HasMaxLength(400);
                entity.Property(e => e.CouponDiscountPercent).HasColumnName("Coupon_DiscountPercent").HasColumnType("decimal(5,2)");
                entity.Property(e => e.CouponValidFrom).HasColumnName("Coupon_ValidFrom");
                entity.Property(e => e.CouponValidTo).HasColumnName("Coupon_ValidTo");
                entity.Property(e => e.CouponIsActive).HasColumnName("Coupon_IsActive").HasDefaultValue(true);
            });

            modelBuilder.Entity<TblCouponUser>(entity =>
            {
                entity.ToTable("Tbl_CouponUser");
                entity.HasKey(e => e.CouponUserId);
                entity.Property(e => e.CouponUserId).HasColumnName("CouponUser_ID");
                entity.Property(e => e.CouponId).HasColumnName("Coupon_ID").IsRequired();
                entity.Property(e => e.UsersId).HasColumnName("Users_ID").IsRequired();

                entity.HasOne(d => d.Coupon).WithMany(p => p.TblCouponUsers)
                      .HasForeignKey(d => d.CouponId)
                      .HasConstraintName("FK_Tbl_CouponUser_Tbl_Coupon");

                entity.HasOne(d => d.Users).WithMany(p => p.TblCouponUsers)
                      .HasForeignKey(d => d.UsersId)
                      .HasConstraintName("FK_Tbl_CouponUser_Tbl_Users");
            });

            // ParkingSpot
            modelBuilder.Entity<TblParkingSpot>(entity =>
            {
                entity.ToTable("Tbl_ParkingSpot");
                entity.HasKey(e => e.ParkingSpotId);
                entity.Property(e => e.ParkingSpotId).HasColumnName("ParkingSpot_ID");
                entity.Property(e => e.ParkingSpotCode).HasColumnName("ParkingSpot_Code").HasMaxLength(50).IsRequired();
                entity.Property(e => e.ParkingSpotStatus).HasColumnName("ParkingSpot_Status").HasDefaultValue(0);
                entity.Property(e => e.ParkingSpotFloor).HasColumnName("ParkingSpot_Floor").HasMaxLength(250).IsRequired();
            });

            // CustomerComplaint
            modelBuilder.Entity<TblCustomerComplaint>(entity =>
            {
                entity.ToTable("Tbl_CustomerComplaint");
                entity.HasKey(e => e.CustomerComplaintId);
                entity.Property(e => e.CustomerComplaintId).HasColumnName("CustomerComplaint_ID");
                entity.Property(e => e.CustomerComplaintCustomerUserId).HasColumnName("CustomerComplaint_CustomerUserID").IsRequired();
                entity.Property(e => e.CustomerComplaintTenantId).HasColumnName("CustomerComplaint_TenantID");
                entity.Property(e => e.CustomerComplaintRate).HasColumnName("CustomerComplaint_Rate").IsRequired();
                entity.Property(e => e.CustomerComplaintDescription).HasColumnName("CustomerComplaint_Description").HasMaxLength(2000);
                entity.Property(e => e.CustomerComplaintStatus).HasColumnName("CustomerComplaint_Status").HasDefaultValue(0);
                entity.Property(e => e.CustomerComplaintCreatedAt).HasColumnName("CustomerComplaint_CreatedAt").HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(d => d.CustomerComplaintCustomerUser).WithMany(p => p.TblCustomerComplaints)
                      .HasForeignKey(d => d.CustomerComplaintCustomerUserId)
                      .HasConstraintName("FK_Tbl_CustomerComplaint_Tbl_Users");

                entity.HasOne(d => d.CustomerComplaintTenant).WithMany(p => p.TblCustomerComplaints)
                      .HasForeignKey(d => d.CustomerComplaintTenantId)
                      .HasConstraintName("FK_Tbl_CustomerComplaint_Tbl_Tenant");
            });

            // Event / EventBooking
            modelBuilder.Entity<TblEvent>(entity =>
            {
                entity.ToTable("Tbl_Event");
                entity.HasKey(e => e.EventId);
                entity.Property(e => e.EventId).HasColumnName("Event_ID");
                entity.Property(e => e.EventName).HasColumnName("Event_Name").HasMaxLength(300).IsRequired();
                entity.Property(e => e.EventImg).HasColumnName("Event_Img").HasMaxLength(300).IsRequired();
                entity.Property(e => e.EventDescription).HasColumnName("Event_Description").HasMaxLength(1000).IsRequired();
                entity.Property(e => e.EventStart).HasColumnName("Event_Start").IsRequired();
                entity.Property(e => e.EventEnd).HasColumnName("Event_End").IsRequired();
                entity.Property(e => e.EventStatus).HasColumnName("Event_Status").HasDefaultValue(1);
                entity.Property(e => e.EventMaxSlot).HasColumnName("Event_MaxSlot").IsRequired();
                entity.Property(e => e.EventTenantPositionId).HasColumnName("Event_TenantPositionID").IsRequired();

                entity.HasOne(d => d.EventTenantPosition).WithMany(p => p.TblEvents)
                      .HasForeignKey(d => d.EventTenantPositionId)
                      .HasConstraintName("FK_Tbl_Event_Tbl_TenantPosition");
            });

            modelBuilder.Entity<TblEventBooking>(entity =>
            {
                entity.ToTable("Tbl_EventBooking");
                entity.HasKey(e => e.EventBookingId);
                entity.Property(e => e.EventBookingId).HasColumnName("EventBooking_ID");
                entity.Property(e => e.EventBookingTenantId).HasColumnName("EventBooking_TenantID").IsRequired();
                entity.Property(e => e.EventBookingUserId).HasColumnName("EventBooking_UserID").IsRequired();
                entity.Property(e => e.EventBookingEventId).HasColumnName("EventBooking_EventID").IsRequired();
                entity.Property(e => e.EventBookingTotalCost).HasColumnName("EventBooking_Total_Cost").HasColumnType("decimal(18,2)");
                entity.Property(e => e.EventBookingPaymentStatus).HasColumnName("EventBooking_Payment_Status").HasDefaultValue(0);
                entity.Property(e => e.EventBookingNotes).HasColumnName("EventBooking_Notes");
                entity.Property(e => e.EventBookingCreatedDate).HasColumnName("EventBooking_CreatedDate").HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(d => d.EventBookingEvent).WithMany(p => p.TblEventBookings)
                      .HasForeignKey(d => d.EventBookingEventId)
                      .HasConstraintName("FK_Tbl_EventBooking_Tbl_Event");

                entity.HasOne(d => d.EventBookingTenant).WithMany(p => p.TblEventBookings)
                      .HasForeignKey(d => d.EventBookingTenantId)
                      .HasConstraintName("FK_Tbl_EventBooking_Tbl_Tenant");

                entity.HasOne(d => d.EventBookingUser).WithMany(p => p.TblEventBookings)
                      .HasForeignKey(d => d.EventBookingUserId)
                      .HasConstraintName("FK_Tbl_EventBooking_Tbl_Users");
            });

            // Notification
            modelBuilder.Entity<TblNotification>(entity =>
            {
                entity.ToTable("Tbl_Notification");
                entity.HasKey(e => e.NotificationId);
                entity.Property(e => e.NotificationId).HasColumnName("Notification_ID");
                entity.Property(e => e.NotificationUserId).HasColumnName("Notification_UserID").IsRequired();
                entity.Property(e => e.NotificationTitle).HasColumnName("Notification_Title").HasMaxLength(300).IsRequired();
                entity.Property(e => e.NotificationBody).HasColumnName("Notification_Body").HasMaxLength(2000).IsRequired();
                entity.Property(e => e.NotificationChannel).HasColumnName("Notification_Channel").HasMaxLength(50).HasDefaultValue("email");
                entity.Property(e => e.NotificationIsRead).HasColumnName("Notification_IsRead").HasDefaultValue(false);
                entity.Property(e => e.NotificationCreatedAt).HasColumnName("Notification_CreatedAt").HasDefaultValueSql("(sysutcdatetime())");

                entity.HasOne(d => d.NotificationUser).WithMany(p => p.TblNotifications)
                      .HasForeignKey(d => d.NotificationUserId)
                      .HasConstraintName("FK_Tbl_Notification_Tbl_Users");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
