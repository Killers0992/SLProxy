using System;
using System.Collections.Generic;
using System.Text;

namespace SLProxy.Models
{
	public readonly struct AuthenticatorPlayerObjects : IEquatable<AuthenticatorPlayerObjects>
	{
		public AuthenticatorPlayerObjects(AuthenticatorPlayerObject[] objects)
		{
			this.objects = objects;
		}

		public bool Equals(AuthenticatorPlayerObjects other)
		{
			return this.objects == other.objects;
		}

		public readonly AuthenticatorPlayerObject[] objects;
	}
}
