using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace TcServer.Utility
{
	public static class Password
	{
		public static int PwdSubkeyLen = 256 / 8; // HMACSHA256
		public static int PwdSaltLen = 128 / 8;

		public static byte[] Hash(string pwd)
		{
			byte[] salt = RandomNumberGenerator.GetBytes(PwdSaltLen);
			var subkey = KeyDerivation.Pbkdf2(pwd, salt, KeyDerivationPrf.HMACSHA256, 1000, PwdSubkeyLen);

			byte[] key = new byte[PwdSubkeyLen + PwdSaltLen];

			Buffer.BlockCopy(salt, 0, key, 0, PwdSaltLen);
			Buffer.BlockCopy(subkey, 0, key, PwdSaltLen, PwdSubkeyLen);

			return key;
		}

		public static bool Compare(string pwdRaw, byte[] pwdHashed)
		{
			if (pwdHashed.Length != PwdSaltLen + PwdSubkeyLen)
				return false;

			byte[] salt = new byte[PwdSaltLen];
			Buffer.BlockCopy(pwdHashed, 0, salt, 0, PwdSaltLen);

			byte[] subkey = new byte[PwdSubkeyLen];
			Buffer.BlockCopy(pwdHashed, PwdSaltLen, subkey, 0, PwdSubkeyLen);

			byte[] actualSubkey = KeyDerivation.Pbkdf2(pwdRaw, salt, KeyDerivationPrf.HMACSHA256, 1000, PwdSubkeyLen);
			
			return CryptographicOperations.FixedTimeEquals(subkey, actualSubkey);
		}
	}
}
