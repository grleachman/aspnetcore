// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Quic;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Quic
{
    public class QuicConnectionFactory : IConnectionFactory
    {
        private QuicTransportContext _transportContext;

        public QuicConnectionFactory(IOptions<QuicTransportOptions> options, IHostApplicationLifetime lifetime, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var logger = loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Quic.Client");
            var trace = new QuicTrace(logger);

            _transportContext = new QuicTransportContext(lifetime, trace, options.Value);
        }

        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            if (!(endPoint is IPEndPoint ipEndPoint))
            {
                throw new NotSupportedException($"{endPoint} is not supported");
            }

            var sslOptions = new SslClientAuthenticationOptions();
            sslOptions.ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol(_transportContext.Options.Alpn) };
            var connection = new QuicConnection(QuicImplementationProviders.MsQuic, endPoint as IPEndPoint, sslOptions);

            await connection.ConnectAsync(cancellationToken);
            return new QuicConnectionContext(connection, _transportContext);
        }
    }
}