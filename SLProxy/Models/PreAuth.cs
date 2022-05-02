using LiteNetLib.Utils;
using Org.BouncyCastle.Crypto;
using SLProxy.Cryptography;
using SLProxy.Enums;
using SLProxy.ServerList;
using System.Text;

namespace SLProxy.Models
{
    internal class PreAuth
    {
        public static PreAuth ReadPreAuth(NetDataReader reader, ref string failedOn)
        {
            PreAuth model = new PreAuth();
            failedOn = "Client Type";
            if (!reader.TryGetByte(out byte clientType)) return model;
            model.ClientType = (ClientType)clientType;

            failedOn = "Major Version";
            if (!reader.TryGetByte(out byte major)) return model;
            model.Major = major;

            failedOn = "Minor Version";
            if (!reader.TryGetByte(out byte minor)) return model;
            model.Minor = minor;

            failedOn = "Revision Version";
            if (!reader.TryGetByte(out byte revision)) return model;
            model.Revision = revision;

            failedOn = "Backward Compatibility";
            if (!reader.TryGetBool(out bool backwardCompatibility)) return model;
            model.BackwardCompatibility = backwardCompatibility;

            if (backwardCompatibility)
            {
                failedOn = "Backward Revision";
                if (!reader.TryGetByte(out byte backwardRevision)) return model;
                model.BackwardRevision = backwardRevision;
            }

            failedOn = "ChallengeID";
            if (!reader.TryGetInt(out int challengeid)) return model;
            model.ChallengeID = challengeid;

            if (challengeid != 0)
            {
                model.IsChallenge = true;
                failedOn = "ChallengeResponse";
                if (!reader.TryGetBytesWithLength(out byte[] challenge)) return model;
                model.ChallengeResponse = challenge;
            }

            failedOn = "UserID";
            if (!reader.TryGetString(out string userid)) return model;
            model.UserID = userid;

            failedOn = "Expiration";
            if (!reader.TryGetLong(out long expiration)) return model;
            model.Expiration = expiration;

            failedOn = "Flags";
            if (!reader.TryGetByte(out byte flags)) return model;
            model.Flags = (CentralAuthPreauthFlags)flags;

            failedOn = "Country";
            if (!reader.TryGetString(out string country)) return model;
            model.Country = country;

            failedOn = "Signature";
            if (!reader.TryGetBytesWithLength(out byte[] signature)) return model;
            model.Signature = signature;

            failedOn = "IP passthrough Check";
            if (reader.TryGetString(out _)) return model;

            failedOn = "Signature Check";
            if (!ECDSA.VerifyBytes($"{userid};{flags};{country};{expiration}", signature, ServerConsole.PublicKey)) return model;

            failedOn = "Expiration Check";
            if (DateTimeOffset.UtcNow.ToUnixTimeSeconds() > expiration) return model;

            model.IsValid = true;
            return model;
        }

        public bool IsValid { get; set; }
        public bool IsChallenge { get; set; }
        public ClientType ClientType { get; set; }
        public byte Major { get; set; }
        public byte Minor { get; set; }
        public byte Revision { get; set; }
        public bool BackwardCompatibility { get; set; }
        public byte BackwardRevision { get; set; }

        public int ChallengeID { get; set; }
        public byte[] ChallengeResponse { get; set; }

        public string UserID { get; set; } = "Unknown UserID";

        public long Expiration { get; set; }

        public CentralAuthPreauthFlags Flags { get; set; }

        public string Country { get; set; } = "Unknown Country";

        public byte[] Signature { get; set; } = new byte[0];

        public override string ToString()
        {
            return string.Concat(
                $"Client Type: {ClientType}",
                Environment.NewLine,
                $"Version: {Major}.{Minor}.{Revision}, Backward Compatibility: {(BackwardCompatibility ? "NO" : $"YES ( Revision {BackwardRevision} )")}",
                Environment.NewLine,
                $"Challenge ID: {ChallengeID}",
                Environment.NewLine,
                $"Challenge: {(ChallengeResponse != null ? Encoding.UTF8.GetString(ChallengeResponse) : "INVALID")}",
                Environment.NewLine,
                $"UserID: {UserID}",
                Environment.NewLine,
                $"Expiration: {Expiration}",
                Environment.NewLine,
                $"Flags: {Flags}",
                Environment.NewLine,
                $"Country: {Country}",
                Environment.NewLine,
                $"Signature length: {Signature.Length}");
        }
    }
}
