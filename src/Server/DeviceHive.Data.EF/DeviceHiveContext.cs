using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using DeviceHive.Data.Model;

namespace DeviceHive.Data.EF
{
    /// <summary>
    /// Represents data access context for DeviceHive model
    /// </summary>
    internal class DeviceHiveContext : DbContext
    {
        #region Public Properties

        /// <summary>
        /// Gets DbSet for users
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// Gets DbSet for user/network associations
        /// </summary>
        public DbSet<UserNetwork> UserNetworks { get; set; }

        /// <summary>
        /// Gets DbSet for access keys
        /// </summary>
        public DbSet<AccessKey> AccessKeys { get; set; }

        /// <summary>
        /// Gets DbSet for access key permissions
        /// </summary>
        public DbSet<AccessKeyPermission> AccessKeyPermissions { get; set; }

        /// <summary>
        /// Gets DbSet for networks
        /// </summary>
        public DbSet<Network> Networks { get; set; }

        /// <summary>
        /// Gets DbSet for device classes
        /// </summary>
        public DbSet<DeviceClass> DeviceClasses { get; set; }

        /// <summary>
        /// Gets DbSet for equipments
        /// </summary>
        public DbSet<Equipment> Equipments { get; set; }

        /// <summary>
        /// Gets DbSet for devices
        /// </summary>
        public DbSet<Device> Devices { get; set; }

        /// <summary>
        /// Gets DbSet for device notifications
        /// </summary>
        public DbSet<DeviceNotification> DeviceNotifications { get; set; }

        /// <summary>
        /// Gets DbSet for device commands
        /// </summary>
        public DbSet<DeviceCommand> DeviceCommands { get; set; }

        /// <summary>
        /// Gets DbSet for device equipments
        /// </summary>
        public DbSet<DeviceEquipment> DeviceEquipments { get; set; }

        /// <summary>
        /// Gets DbSet for OAuth clients
        /// </summary>
        public DbSet<OAuthClient> OAuthClients { get; set; }

        /// <summary>
        /// Gets DbSet for OAuth grants
        /// </summary>
        public DbSet<OAuthGrant> OAuthGrants { get; set; }

        #endregion

        #region Protected Methods

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            modelBuilder.Entity<AccessKey>().HasMany(e => e.Permissions).WithRequired().HasForeignKey(e => e.AccessKeyID).WillCascadeOnDelete(true);
            modelBuilder.Entity<UserNetwork>().HasRequired(e => e.User).WithMany().HasForeignKey(e => e.UserID).WillCascadeOnDelete(true);
            modelBuilder.Entity<UserNetwork>().HasRequired(e => e.Network).WithMany().HasForeignKey(e => e.NetworkID).WillCascadeOnDelete(true);
            modelBuilder.Entity<DeviceClass>().HasMany(e => e.Equipment).WithRequired().HasForeignKey(e => e.DeviceClassID).WillCascadeOnDelete(true);
            modelBuilder.Entity<Device>().HasOptional(e => e.Network).WithMany().HasForeignKey(e => e.NetworkID).WillCascadeOnDelete(true);
            modelBuilder.Entity<Device>().HasRequired(e => e.DeviceClass).WithMany().HasForeignKey(e => e.DeviceClassID).WillCascadeOnDelete(true);
            modelBuilder.Entity<DeviceNotification>().Property(e => e.Timestamp).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            modelBuilder.Entity<DeviceNotification>().HasRequired(e => e.Device).WithMany().HasForeignKey(e => e.DeviceID).WillCascadeOnDelete(true);
            modelBuilder.Entity<DeviceCommand>().Property(e => e.Timestamp).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            modelBuilder.Entity<DeviceCommand>().HasRequired(e => e.Device).WithMany().HasForeignKey(e => e.DeviceID).WillCascadeOnDelete(true);
            modelBuilder.Entity<DeviceEquipment>().HasRequired(e => e.Device).WithMany().HasForeignKey(e => e.DeviceID).WillCascadeOnDelete(true);
            modelBuilder.Entity<OAuthGrant>().Property(e => e.Timestamp).HasDatabaseGeneratedOption(DatabaseGeneratedOption.Computed);
            modelBuilder.Entity<OAuthGrant>().HasRequired(e => e.Client).WithMany().HasForeignKey(e => e.ClientID).WillCascadeOnDelete(true);
            modelBuilder.Entity<OAuthGrant>().HasRequired(e => e.AccessKey).WithMany().HasForeignKey(e => e.AccessKeyID).WillCascadeOnDelete(false);
        }
        #endregion
    }
}
