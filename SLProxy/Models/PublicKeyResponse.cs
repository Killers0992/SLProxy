using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SLProxy.Models
{
	public readonly struct PublicKeyResponse : IEquatable<PublicKeyResponse>
	{
		public readonly string key;
		public readonly string signature;
		public readonly string credits;

		public PublicKeyResponse(string key, string signature, string credits)
		{
			this.key = key;
			this.signature = signature;
			this.credits = credits;
		}

		public bool Equals(PublicKeyResponse other)
		{
			return key == other.key && signature == other.signature && credits == other.credits;
		}

		public override bool Equals(object obj)
		{
			return obj is PublicKeyResponse other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((key != null ? key.GetHashCode() : 0) * 397) ^ (signature != null ? signature.GetHashCode() : 0) ^
					   (credits != null ? credits.GetHashCode() : 0);
			}
		}

		public static bool operator ==(PublicKeyResponse left, PublicKeyResponse right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(PublicKeyResponse left, PublicKeyResponse right)
		{
			return !left.Equals(right);
		}
	}
}
