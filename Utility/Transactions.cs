using System.Transactions;

namespace TcServer.Utility
{
	public static class Transactions
	{
		public static TransactionScope DbAsyncScopeDefault()
		{
			return new TransactionScope
			(
				TransactionScopeOption.Required,
				new TransactionOptions {
					IsolationLevel = IsolationLevel.Serializable
				},
				TransactionScopeAsyncFlowOption.Enabled
			);
		}
		
		public static TransactionScope DbAsyncScopeRC()
		{
			return new TransactionScope
			(
				TransactionScopeOption.Required,
				new TransactionOptions {
					IsolationLevel = IsolationLevel.ReadCommitted
				},
				TransactionScopeAsyncFlowOption.Enabled
			);
		}
	}
}