using Microsoft.EntityFrameworkCore;
using ITAssetManagement.Models;

namespace ITAssetManagement.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<PermissionRole> PermissionRoles { get; set; }
        public DbSet<UserItem> UserItems { get; set; }

        public DbSet<ITAsset> ITAssets { get; set; }
        public DbSet<AssetCategory> AssetCategories { get; set; }
        public DbSet<AssetStatus> AssetStatuses { get; set; }
        public DbSet<AssetLocation> AssetLocations { get; set; }

        public DbSet<Software> Softwares { get; set; }

        public DbSet<Request> Requests { get; set; }
        public DbSet<DepartmentRepresentative> DepartmentRepresentatives { get; set; }

        public DbSet<RequestDetail> RequestDetails { get; set; }
        public DbSet<RequestApproval> RequestApprovals { get; set; }
        public DbSet<RequestHistory> RequestHistories { get; set; }
        public DbSet<RequestType> RequestTypes { get; set; }
        public DbSet<RequestStatus> RequestStatuses { get; set; }
        public DbSet<RequestAsset> RequestAssets { get; set; }
        public DbSet<InventoryHistory> InventoryHistories { get; set; }

        public DbSet<License> Licenses { get; set; }
        public DbSet<LicenseInventoryHistory> LicenseInventoryHistories { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().ToTable("USERS");
            modelBuilder.Entity<Department>().ToTable("DEPARTMENT");
            modelBuilder.Entity<PermissionRole>().ToTable("PERMISSION_ROLE");
            modelBuilder.Entity<ITAsset>().ToTable("IT_ASSET");
            modelBuilder.Entity<License>().ToTable("LICENSE");
            modelBuilder.Entity<LicenseInventoryHistory>().ToTable("LICENSE_INVENTORY_HISTORY");

            // User → Department
            modelBuilder.Entity<User>()
                .HasOne(u => u.Department)
                .WithMany(d => d.Users)
                .HasForeignKey(u => u.PrimaryDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // User → Role
            modelBuilder.Entity<User>()
                .HasOne(u => u.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(u => u.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RequestAsset>()
               .HasOne(ra => ra.Request)
               .WithMany()
               .HasForeignKey(ra => ra.RequestId);

            modelBuilder.Entity<RequestAsset>()
                .HasOne(ra => ra.Asset)
                .WithMany()
                .HasForeignKey(ra => ra.AssetId);

            // License -> Department (Management Department)
            modelBuilder.Entity<License>()
                .HasOne(l => l.ManagementDepartment)
                .WithMany()
                .HasForeignKey(l => l.ManagementDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // License -> User (Manager)
            modelBuilder.Entity<License>()
                .HasOne(l => l.Manager)
                .WithMany()
                .HasForeignKey(l => l.ManagerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // License -> License (Parent/Child for Split)
            modelBuilder.Entity<License>()
                .HasOne(l => l.ParentLicense)
                .WithMany(l => l.ChildLicenses)
                .HasForeignKey(l => l.ParentLicenseId)
                .OnDelete(DeleteBehavior.Restrict);

            // LicenseInventoryHistory -> License
            modelBuilder.Entity<LicenseInventoryHistory>()
                .HasOne(h => h.License)
                .WithMany(l => l.InventoryHistories)
                .HasForeignKey(h => h.LicenseId)
                .OnDelete(DeleteBehavior.Cascade);

            // LicenseInventoryHistory -> User
            modelBuilder.Entity<LicenseInventoryHistory>()
                .HasOne(h => h.InventoryTaker)
                .WithMany()
                .HasForeignKey(h => h.InventoryTakerId)
                .OnDelete(DeleteBehavior.SetNull);
            }
        }
}