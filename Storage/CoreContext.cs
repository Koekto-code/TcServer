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
				.IsRequired(false)
				.OnDelete(DeleteBehavior.ClientCascade);

			mb.Entity<Company>()
				.HasMany(c => c.WorkShifts)
				.WithOne(w => w.Company)
				.HasForeignKey(w => w.CompanyId)
				.IsRequired()
				.OnDelete(DeleteBehavior.ClientCascade);

			mb.Entity<Company>()
				.HasMany(c => c.Units)
				.WithOne(u => u.Company)
				.HasForeignKey(u => u.CompanyId)
				.IsRequired()
				.OnDelete(DeleteBehavior.ClientCascade);

			mb.Entity<Company>()
				.HasMany(c => c.Employees)
				.WithOne(u => u.Company)
				.HasForeignKey(u => u.CompanyId)
				.IsRequired()
				.OnDelete(DeleteBehavior.NoAction);

			mb.Entity<Company>()
				.HasMany(c => c.Devices)
				.WithOne(d => d.Company)
				.HasForeignKey(d => d.CompanyId)
				.IsRequired(false)
				.OnDelete(DeleteBehavior.ClientCascade);

			mb.Entity<Unit>()
				.HasMany(u => u.Employees)
				.WithOne(e => e.Unit)
				.HasForeignKey(e => e.UnitId)
				.IsRequired()
				.OnDelete(DeleteBehavior.ClientCascade);

			mb.Entity<Employee>()
				.HasMany(e => e.AtdRecords)
				.WithOne(r => r.Employee)
				.HasForeignKey(r => r.EmployeeId)
				.IsRequired()
				.OnDelete(DeleteBehavior.ClientCascade);
			
			mb.Entity<Employee>()
				.HasMany(e => e.Photos)
				.WithOne(p => p.Employee)
				.HasForeignKey(p => p.EmployeeId)
				.IsRequired(false)
				.OnDelete(DeleteBehavior.ClientSetNull);
			
			mb.Entity<Device>()
				.HasMany(d => d.Photos)
				.WithOne(p => p.Device)
				.HasForeignKey(p => p.DeviceId)
				.IsRequired()
				.OnDelete(DeleteBehavior.ClientCascade);
			
			mb.Entity<Company>()
				.HasIndex(c => c.Name)
				.IsUnique();

			mb.Entity<Device>()
				.HasIndex(d => d.SerialNumber)
				.IsUnique();
		}

		public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			var accsToRemove = ChangeTracker.Entries<Account>()
				.Where(e => e.State == EntityState.Deleted)
				.Select(e => e.Entity)
				.ToList();
			
			foreach (var acc in accsToRemove)
				await Entry(acc).Reference(a => a.Company).LoadAsync();
			
			var compsToRemove = ChangeTracker.Entries<Company>()
				.Where(e => e.State == EntityState.Deleted)
				.Select(e => e.Entity)
				.ToList();
			
			foreach (var comp in compsToRemove)
			{
				await Entry(comp).Collection(c => c.Employees).LoadAsync();
				await Entry(comp).Collection(c => c.Units).LoadAsync();
				await Entry(comp).Collection(c => c.Devices).LoadAsync();
				await Entry(comp).Collection(c => c.WorkShifts).LoadAsync();
			}
			
			var unitsToRemove = ChangeTracker.Entries<Unit>()
				.Where(e => e.State == EntityState.Deleted)
				.Select(e => e.Entity)
				.ToList();
			
			foreach (var unit in unitsToRemove)
				await Entry(unit).Collection(u => u.Employees).LoadAsync();
			
			var devicesToRemove = ChangeTracker.Entries<Device>()
				.Where(e => e.State == EntityState.Deleted)
				.Select(e => e.Entity)
				.ToList();
			
			foreach (var dev in devicesToRemove)
				await Entry(dev).Collection(d => d.Photos).LoadAsync();
			
			var emplsToRemove = ChangeTracker.Entries<Employee>()
				.Where(e => e.State == EntityState.Deleted)
				.Select(e => e.Entity)
				.ToList();
			
			// maps CompanyId to list of employees' IDs of this company that are to be deleted
			var ceMapping = new Dictionary<int, List<string>>();
			
			foreach (var empl in emplsToRemove)
			{
				if (!ceMapping.ContainsKey(empl.CompanyId))
					ceMapping[empl.CompanyId] = new();
				
				await Entry(empl).Collection(e => e.AtdRecords).LoadAsync();
				await Entry(empl).Collection(e => e.Photos).LoadAsync();
				ceMapping[empl.CompanyId].Add(empl.InnerCompId.ToString());
			}
			
			foreach (var c in ceMapping)
			{
				if (c.Value.Count == 0)
					continue;
				
				var devices = await Devices
					.Where(d => d.CompanyId == c.Key)
					.Where(d => d.Password != null)
					.ToListAsync();
				
				foreach (var dev in devices)
				{
					Func<Task<object?>> task = async () => {
						return await remSvc.RemoveEmployees (
							dev.Address, dev.Password!, c.Value
						) as object;
					};
					remoteTasks.Add(task());
				}
			}
			return await base.SaveChangesAsync(cancellationToken);
		}
		
		public async Task<List<object?>> SaveRemoteChangesAsync()
		{
			var res = await Task.WhenAll(remoteTasks);
			remoteTasks.Clear();
			return res.ToList();
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
		
		protected readonly IRemoteService remSvc = null!;
		
		protected List<Task<object?>> remoteTasks = new();
	}
}
