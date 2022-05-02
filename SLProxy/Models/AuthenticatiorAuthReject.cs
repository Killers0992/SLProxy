using System;

namespace SLProxy.ServerList
{
	public readonly struct AuthenticatiorAuthReject : IEquatable<AuthenticatiorAuthReject>
	{
		public AuthenticatiorAuthReject(string id, string reason)
		{
			this.Id = id;
			this.Reason = reason;
		}

		public bool Equals(AuthenticatiorAuthReject other)
		{
			return string.Equals(this.Id, other.Id) && string.Equals(this.Reason, other.Reason);
		}

		public readonly string Id;
		public readonly string Reason;
	}
}
