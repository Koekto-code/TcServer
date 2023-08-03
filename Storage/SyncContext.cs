using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

using TcServer.Storage.Core;
using TcServer.Core;
using TcServer.Core.Remote;
using TcServer.Services;

namespace TcServer.Storage
{
	public class SyncContext: CoreContext
	{
		public SyncContext(DbContextOptions<CoreContext> options, IRemoteService remsvc):
			base(options, remsvc) {}
	}
}