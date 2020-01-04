using System;

namespace ProxyKit.RoutingHandler
{
    /// <summary>
    /// Represents the origin of web application. An origin consists of a host and a port. 
    /// </summary>
    public sealed class Origin
    {
        /// <summary>
        /// The origin host
        /// </summary>
        public string Host { get; }

        /// <summary>
        /// The origin port
        /// </summary>
        public int Port { get; }

        public static implicit operator Origin(string uri)
        {
            var parsed = new Uri(uri);
            return new Origin(parsed.Host, parsed.Port);
        }

        /// <summary>
        /// Create a new instance of <see cref="Origin"/>.
        /// </summary>
        /// <param name="host">The host name of the origin.</param>
        /// <param name="port">The port of the origin</param>
        public Origin(string host, int port)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
        }

        private bool Equals(Origin other)
        {
            return string.Equals(Host, other.Host, StringComparison.OrdinalIgnoreCase) && Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is Origin other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (StringComparer.OrdinalIgnoreCase.GetHashCode(Host) * 397) ^ (int)Port;
            }
        }

        public static bool operator ==(Origin left, Origin right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Origin left, Origin right)
        {
            return !Equals(left, right);
        }
    }
}
