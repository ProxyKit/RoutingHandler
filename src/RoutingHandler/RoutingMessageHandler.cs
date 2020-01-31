﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyKit.RoutingHandler
{
    /// <summary>
    ///     An <see cref="HttpMessageHandler"/> that acts like a router
    ///     between multiple handlers that represent different hosts.
    /// </summary>
    public class RoutingMessageHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, HostHandler> _hosts
            = new Dictionary<string, HostHandler>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        ///     Adds a handler for a given Origin.
        /// </summary>
        /// <param name="origin">The origin to whom requests are routed to.</param>
        /// <param name="handler">The handler for requests to the specified origin.</param>
        public void AddHandler(Origin origin, HttpMessageHandler handler)
        {
            var endpoint = new HostHandler(handler);
            var host = $"{origin.Host}:{origin.Port}";
            _hosts.Add(host, endpoint);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var host = $"{request.RequestUri.Host}:{request.RequestUri.Port}";
            var hostHandler = _hosts[host];

            return hostHandler.Send(request, cancellationToken);
        }

        private class HostHandler : DelegatingHandler
        {
            public HostHandler(HttpMessageHandler innerHandler)
                : base(innerHandler)
            { }

            internal Task<HttpResponseMessage> Send(HttpRequestMessage request,
                CancellationToken cancellationToken)
            {
                return SendAsync(request, cancellationToken);
            }
        }
    }

}
