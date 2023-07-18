using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

using TcServer.Storage.Core;
using TcServer.Core;
using TcServer.Core.Remote;
using TcServer.Services;

namespace TcServer.Storage
{
	public class CoreContext: DbContext
	{
		public DbSet<Account> Accounts { get; set; } = null!;
		public DbSet<Company> Companies { get; set; } = null!;
		public DbSet<Unit> Units { get; set; } = null!;
		public DbSet<WorkShift> WorkShifts { get; set; } = null!;
		public DbSet<Employee> Employees { get; set; } = null!;
		public DbSet<Device> Devices { get; set; } = null!;
		public DbSet<AtdRecord> AtdRecords { get; set; } = null!;
		public DbSet<Photo> Photos { get; set; } = null!;
		
		// stores info about whether each device is online or not
		// @todo load this on every startup
		public Dictionary<string, bool> DevicesStat { get; set; } = new();

		public CoreContext(DbContextOptions<CoreContext> options, IRemoteService remsvc) : base(options)
		{
			remSvc = remsvc;
			Database.EnsureCreated();
		}

		protected override void OnModelCreating(ModelBuilder mb)
		{
			base.OnModelCreating(mb);

			mb.Entity<Account>()
				.HasOne(a => a.Company)
				.WithOne(c => c.Account)
				.HasForeignKey<Company>(c => c.AccountId)
				.IsRequired(true);

			mb.Entity<Company>()
				.HasMany(c => c.WorkShifts)
				.WithOne(w => w.Company)
				.HasForeignKey(w => w.CompanyId)
				.IsRequired(true);

			mb.Entity<Company>()
				.HasMany(c => c.Units)
				.WithOne(u => u.Company)
				.HasForeignKey(u => u.CompanyId)
				.IsRequired(true);

			mb.Entity<Company>()
				.HasMany(c => c.Employees)
				.WithOne(u => u.Company)
				.HasForeignKey(u => u.CompanyId)
				.IsRequired(true);

			mb.Entity<Company>()
				.HasMany(c => c.Devices)
				.WithOne(d => d.Company)
				.HasForeignKey(d => d.CompanyId)
				.OnDelete(DeleteBehavior.Cascade);

			mb.Entity<Unit>()
				.HasMany(u => u.Employees)
				.WithOne(e => e.Unit)
				.HasForeignKey(e => e.UnitId)
				.OnDelete(DeleteBehavior.NoAction);

			mb.Entity<Employee>()
				.HasMany(e => e.AtdRecords)
				.WithOne(r => r.Employee)
				.HasForeignKey(r => r.EmployeeId)
				.IsRequired(true);
			
			mb.Entity<Employee>()
				.HasMany(e => e.Photos)
				.WithOne(p => p.Employee)
				.HasForeignKey(p => p.EmployeeId)
				.OnDelete(DeleteBehavior.Cascade);

			mb.Entity<Company>()
				.HasIndex(c => c.Name)
				.IsUnique();

			mb.Entity<Device>()
				.HasIndex(d => d.SerialNumber)
				.IsUnique();
			
			mb.Entity<Device>()
				.HasMany(d => d.Photos)
				.WithOne(p => p.Device)
				.HasForeignKey(p => p.DeviceId)
				.OnDelete(DeleteBehavior.NoAction);
			
			mb.Entity<Device>()
				.HasMany(d => d.Employees)
				.WithOne(p => p.Device)
				.HasForeignKey(p => p.DeviceId)
				.OnDelete(DeleteBehavior.NoAction);
		}

		public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			var emplsToRemove = ChangeTracker.Entries<Employee>()
				.Where(e => e.State == EntityState.Deleted)
				.Select(e => e.Entity);
			
			var tasks = new List<Task<DevResponseDTO?>>();
			foreach (var empl in emplsToRemove)
			{
				var dev = await Devices.FindAsync(empl.DeviceId);
				
				if (dev!.Password is null)
					continue; // @todo handle somehow
				
				tasks.Add (
					remSvc.RemoveEmployees (
						dev.Address, dev.Password,
						new List<string>() {
							// @todo group by device, remove with long list of ids
							empl.InnerCompId.ToString()
						}
					)
				);
			}
			await Task.WhenAll(tasks);
			return await base.SaveChangesAsync(cancellationToken);
		}
		
		public override int SaveChanges()
		{
			// @todo
			throw new Exception();
		}

		public async Task<Account?> AccByEmailAsync(string email)
		{
			return await Accounts.FirstOrDefaultAsync(o => o.Email == email);
		}
		
		protected IRemoteService remSvc = null!;
	}
}
