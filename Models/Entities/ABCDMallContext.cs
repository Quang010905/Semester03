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

    public virtual DbSet<TblMovie> TblMovies { get; set; }

    public virtual DbSet<TblNotification> TblNotifications { get; set; }

    public virtual DbSet<TblParkingSpot> TblParkingSpots { get; set; }

    public virtual DbSet<TblProduct> TblProducts { get; set; }

    public virtual DbSet<TblProductCategory> TblProductCategories { get; set; }

    public virtual DbSet<TblRole> TblRoles { get; set; }

    public virtual DbSet<TblScreen> TblScreens { get; set; }

    public virtual DbSet<TblShowtime> TblShowtimes { get; set; }

    public virtual DbSet<TblTenant> TblTenants { get; set; }

    public virtual DbSet<TblTenantPosition> TblTenantPositions { get; set; }

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
            entity.HasKey(e => e.CinemaId).HasName("PK__Tbl_Cine__89C6DAE11E5270DA");

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
            entity.Property(e => e.CinemaStartDate)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Cinema_StartDate");
            entity.Property(e => e.CinemaTenantPositionId).HasColumnName("Cinema_TenantPosition_ID");
        });

        modelBuilder.Entity<TblCoupon>(entity =>
        {
            entity.HasKey(e => e.CouponId).HasName("PK__Tbl_Coup__2A776BBCA4441E74");

            entity.ToTable("Tbl_Coupon");

            entity.HasIndex(e => e.CouponName, "UQ__Tbl_Coup__4AFC9B506CB78890").IsUnique();

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
            entity.Property(e => e.CouponName)
                .HasMaxLength(100)
                .HasColumnName("Coupon_Name");
            entity.Property(e => e.CouponValidFrom).HasColumnName("Coupon_ValidFrom");
            entity.Property(e => e.CouponValidTo).HasColumnName("Coupon_ValidTo");
        });

        modelBuilder.Entity<TblCouponUser>(entity =>
        {
            entity.HasKey(e => e.CouponUserId).HasName("PK__Tbl_Coup__3B4F48E0EF878F96");

            entity.ToTable("Tbl_CouponUser");

            entity.Property(e => e.CouponUserId).HasColumnName("CouponUser_ID");
            entity.Property(e => e.CouponId).HasColumnName("Coupon_ID");
            entity.Property(e => e.UsersId).HasColumnName("Users_ID");

            entity.HasOne(d => d.Coupon).WithMany(p => p.TblCouponUsers)
                .HasForeignKey(d => d.CouponId)
                .HasConstraintName("FK_Tbl_CouponUser_Tbl_Coupon");

            entity.HasOne(d => d.Users).WithMany(p => p.TblCouponUsers)
                .HasForeignKey(d => d.UsersId)
                .HasConstraintName("FK_Tbl_CouponUser_Tbl_Users");
        });

        modelBuilder.Entity<TblCustomerComplaint>(entity =>
        {
            entity.HasKey(e => e.CustomerComplaintId).HasName("PK__Tbl_Cust__EF854084893D48F8");

            entity.ToTable("Tbl_CustomerComplaint");

            entity.Property(e => e.CustomerComplaintId).HasColumnName("CustomerComplaint_ID");
            entity.Property(e => e.CustomerComplaintCreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("CustomerComplaint_CreatedAt");
            entity.Property(e => e.CustomerComplaintCustomerUserId).HasColumnName("CustomerComplaint_CustomerUserID");
            entity.Property(e => e.CustomerComplaintDescription)
                .HasMaxLength(2000)
                .HasColumnName("CustomerComplaint_Description");
            entity.Property(e => e.CustomerComplaintStatus)
                .HasDefaultValue(1)
                .HasColumnName("CustomerComplaint_Status");
            entity.Property(e => e.CustomerComplaintTenantId).HasColumnName("CustomerComplaint_TenantID");

            entity.HasOne(d => d.CustomerComplaintCustomerUser).WithMany(p => p.TblCustomerComplaints)
                .HasForeignKey(d => d.CustomerComplaintCustomerUserId)
                .HasConstraintName("FK_Tbl_CustomerComplaint_Tbl_Users");

            entity.HasOne(d => d.CustomerComplaintTenant).WithMany(p => p.TblCustomerComplaints)
                .HasForeignKey(d => d.CustomerComplaintTenantId)
                .HasConstraintName("FK_Tbl_CustomerComplaint_Tbl_Tenant");
        });

        modelBuilder.Entity<TblEvent>(entity =>
        {
            entity.HasKey(e => e.EventId).HasName("PK__Tbl_Even__FD6BEFE47B019038");

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

            entity.HasOne(d => d.EventTenantPosition).WithMany(p => p.TblEvents)
                .HasForeignKey(d => d.EventTenantPositionId)
                .HasConstraintName("FK_Tbl_Event_Tbl_TenantPosition");
        });

        modelBuilder.Entity<TblEventBooking>(entity =>
        {
            entity.HasKey(e => e.EventBookingId).HasName("PK__Tbl_Even__B471E4EA0FAEF658");

            entity.ToTable("Tbl_EventBooking");

            entity.Property(e => e.EventBookingId).HasColumnName("EventBooking_ID");
            entity.Property(e => e.EventBookingCreatedDate)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("EventBooking_CreatedDate");
            entity.Property(e => e.EventBookingEventId).HasColumnName("EventBooking_EventID");
            entity.Property(e => e.EventBookingNotes).HasColumnName("EventBooking_Notes");
            entity.Property(e => e.EventBookingPaymentStatus)
                .HasDefaultValue(0)
                .HasColumnName("EventBooking_Payment_Status");
            entity.Property(e => e.EventBookingTenantId).HasColumnName("EventBooking_TenantID");
            entity.Property(e => e.EventBookingTotalCost)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("EventBooking_Total_Cost");
            entity.Property(e => e.EventBookingUserId).HasColumnName("EventBooking_UserID");

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

        modelBuilder.Entity<TblMovie>(entity =>
        {
            entity.HasKey(e => e.MovieId).HasName("PK__Tbl_Movi__7A8804057127C0EB");

            entity.ToTable("Tbl_Movie");

            entity.Property(e => e.MovieId).HasColumnName("Movie_ID");
            entity.Property(e => e.MovieDescription)
                .HasMaxLength(1000)
                .HasColumnName("Movie_Description");
            entity.Property(e => e.MovieDurationMin).HasColumnName("Movie_Duration_Min");
            entity.Property(e => e.MovieStatus)
                .HasDefaultValue(1)
                .HasColumnName("Movie_Status");
            entity.Property(e => e.MovieTitle)
                .HasMaxLength(300)
                .HasColumnName("Movie_Title");
        });

        modelBuilder.Entity<TblNotification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__Tbl_Noti__8C1160B5D09945BB");

            entity.ToTable("Tbl_Notification");

            entity.Property(e => e.NotificationId).HasColumnName("Notification_ID");
            entity.Property(e => e.NotificationBody)
                .HasMaxLength(2000)
                .HasColumnName("Notification_Body");
            entity.Property(e => e.NotificationChannel)
                .HasMaxLength(50)
                .HasDefaultValue("email")
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
                .HasConstraintName("FK_Tbl_Notification_Tbl_Users");
        });

        modelBuilder.Entity<TblParkingSpot>(entity =>
        {
            entity.HasKey(e => e.ParkingSpotId).HasName("PK__Tbl_Park__B45C14DC5BB3BDAA");

            entity.ToTable("Tbl_ParkingSpot");

            entity.Property(e => e.ParkingSpotId).HasColumnName("ParkingSpot_ID");
            entity.Property(e => e.ParkingSpotCode)
                .HasMaxLength(50)
                .HasColumnName("ParkingSpot_Code");
            entity.Property(e => e.ParkingSpotFloor)
                .HasMaxLength(250)
                .HasColumnName("ParkingSpot_Floor");
            entity.Property(e => e.ParkingSpotLevel)
                .HasMaxLength(50)
                .HasColumnName("ParkingSpot_Level");
            entity.Property(e => e.ParkingSpotStatus)
                .HasDefaultValue(1)
                .HasColumnName("ParkingSpot_Status");
        });

        modelBuilder.Entity<TblProduct>(entity =>
        {
            entity.HasKey(e => e.ProductId).HasName("PK__Tbl_Prod__9834FB9AC204A376");

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
                .HasConstraintName("FK_Tbl_Product_Tbl_ProductCategory");
        });

        modelBuilder.Entity<TblProductCategory>(entity =>
        {
            entity.HasKey(e => e.ProductCategoryId).HasName("PK__Tbl_Prod__4C1E45D9D3E0231E");

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
                .HasConstraintName("FK_Tbl_ProductCategory_Tbl_Tenant");
        });

        modelBuilder.Entity<TblRole>(entity =>
        {
            entity.HasKey(e => e.RolesId).HasName("PK__Tbl_Role__BB563FB78C34E985");

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
            entity.HasKey(e => e.ScreenId).HasName("PK__Tbl_Scre__1D3FB5CB4D4B2BCF");

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

        modelBuilder.Entity<TblShowtime>(entity =>
        {
            entity.HasKey(e => e.ShowtimeId).HasName("PK__Tbl_Show__7C7A908968FB578D");

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

        modelBuilder.Entity<TblTenant>(entity =>
        {
            entity.HasKey(e => e.TenantId).HasName("PK__Tbl_Tena__8E8F34723FC2471C");

            entity.ToTable("Tbl_Tenant");

            entity.Property(e => e.TenantId).HasColumnName("Tenant_ID");
            entity.Property(e => e.TenantCreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Tenant_CreatedAt");
            entity.Property(e => e.TenantImg)
                .HasMaxLength(300)
                .HasColumnName("Tenant_Img");
            entity.Property(e => e.TenantName)
                .HasMaxLength(300)
                .HasColumnName("Tenant_Name");
            entity.Property(e => e.TenantNotes)
                .HasMaxLength(1000)
                .HasColumnName("Tenant_Notes");
            entity.Property(e => e.TenantTypeId).HasColumnName("Tenant_TypeID");
            entity.Property(e => e.TenantUserId).HasColumnName("Tenant_UserID");

            entity.HasOne(d => d.TenantType).WithMany(p => p.TblTenants)
                .HasForeignKey(d => d.TenantTypeId)
                .HasConstraintName("FK_Tbl_Tenant_Tbl_TenantType");

            entity.HasOne(d => d.TenantUser).WithMany(p => p.TblTenants)
                .HasForeignKey(d => d.TenantUserId)
                .HasConstraintName("FK_Tbl_Tenant_Tbl_Users");
        });

        modelBuilder.Entity<TblTenantPosition>(entity =>
        {
            entity.HasKey(e => e.TenantPositionId).HasName("PK__Tbl_Tena__3AD429528D0E6BD9");

            entity.ToTable("Tbl_TenantPosition");

            entity.Property(e => e.TenantPositionId).HasColumnName("TenantPosition_ID");
            entity.Property(e => e.TenantPositionAreaM2)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("TenantPosition_Area_M2");
            entity.Property(e => e.TenantPositionAssignedTenantId).HasColumnName("TenantPosition_AssignedTenantID");
            entity.Property(e => e.TenantPositionFloor).HasColumnName("TenantPosition_Floor");
            entity.Property(e => e.TenantPositionLocation)
                .HasMaxLength(250)
                .HasColumnName("TenantPosition_Location");
            entity.Property(e => e.TenantPositionRentPricePerM2)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("TenantPosition_Rent_Price_Per_M2");
            entity.Property(e => e.TenantPositionStatus).HasColumnName("TenantPosition_Status");

            entity.HasOne(d => d.TenantPositionAssignedTenant).WithMany(p => p.TblTenantPositions)
                .HasForeignKey(d => d.TenantPositionAssignedTenantId)
                .HasConstraintName("FK_Tbl_TenantPosition_Tbl_Tenant");
        });

        modelBuilder.Entity<TblTenantType>(entity =>
        {
            entity.HasKey(e => e.TenantTypeId).HasName("PK__Tbl_Tena__7AEEA9C4BD288418");

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
            entity.HasKey(e => e.TicketId).HasName("PK__Tbl_Tick__ED7260D931C792B3");

            entity.ToTable("Tbl_Ticket");

            entity.Property(e => e.TicketId).HasColumnName("Ticket_ID");
            entity.Property(e => e.TicketBuyerUserId).HasColumnName("Ticket_BuyerUserID");
            entity.Property(e => e.TicketPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("Ticket_Price");
            entity.Property(e => e.TicketPurchasedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("Ticket_PurchasedAt");
            entity.Property(e => e.TicketSeat)
                .HasMaxLength(50)
                .HasColumnName("Ticket_Seat");
            entity.Property(e => e.TicketShowtimeId).HasColumnName("Ticket_ShowtimeID");
            entity.Property(e => e.TicketStatus)
                .HasMaxLength(50)
                .HasDefaultValue("sold")
                .HasColumnName("Ticket_Status");

            entity.HasOne(d => d.TicketBuyerUser).WithMany(p => p.TblTickets)
                .HasForeignKey(d => d.TicketBuyerUserId)
                .HasConstraintName("FK_Tbl_Ticket_Tbl_Users");

            entity.HasOne(d => d.TicketShowtime).WithMany(p => p.TblTickets)
                .HasForeignKey(d => d.TicketShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tbl_Ticket_Tbl_Showtime");
        });

        modelBuilder.Entity<TblUser>(entity =>
        {
            entity.HasKey(e => e.UsersId).HasName("PK__Tbl_User__EB68290DB443235C");

            entity.ToTable("Tbl_Users");

            entity.HasIndex(e => e.UsersUsername, "UQ__Tbl_User__76886E48FAC4F64D").IsUnique();

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
            entity.Property(e => e.UsersRoleId).HasColumnName("Users_RoleID");
            entity.Property(e => e.UsersUpdatedAt).HasColumnName("Users_UpdatedAt");
            entity.Property(e => e.UsersUsername)
                .HasMaxLength(100)
                .HasColumnName("Users_Username");

            entity.HasOne(d => d.UsersRole).WithMany(p => p.TblUsers)
                .HasForeignKey(d => d.UsersRoleId)
                .HasConstraintName("FK_Tbl_Users_Tbl_Roles");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
