﻿// <copyright file="AbstractMiddlewareController.cs" company="Stormpath, Inc.">
// Copyright (c) 2016 Stormpath, Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stormpath.Owin.Middleware.Internal;
using Stormpath.Owin.Middleware.Model.Error;
using Stormpath.Owin.Middleware.Owin;
using Stormpath.Configuration.Abstractions;
using Stormpath.SDK.Client;
using Stormpath.SDK.Logging;

namespace Stormpath.Owin.Middleware.Route
{
    using Common;
    using SDK.Error;

    public abstract class AbstractRouteMiddleware
    {
        private readonly IClient _client;

        protected readonly ILogger _logger;
        protected readonly StormpathConfiguration _configuration;

        public AbstractRouteMiddleware(
            StormpathConfiguration configuration,
            ILogger logger,
            IClient client)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException(nameof(configuration));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            _logger = logger;
            _configuration = configuration;
            _client = client;
        }

        public async Task<bool> Invoke(IOwinEnvironment owinContext)
        {
            if (!HasSupportedAccept(owinContext))
            {
                return false;
            }

            _logger.Info($"Stormpath middleware handling request {owinContext.Request.Path}");

            var acceptHeader = owinContext.Request.Headers.GetString("Accept");
            var contentNegotiationResult = ContentNegotiation.Negotiate(acceptHeader, _configuration.Web.Produces);

            try
            {
                return await Dispatch(owinContext, _client, contentNegotiationResult, owinContext.CancellationToken);
            }
            catch (ResourceException rex)
            {
                if (contentNegotiationResult.Preferred == ContentType.Json)
                {
                    // Sanitize Stormpath API errors
                    await Error.CreateFromApiError(owinContext, rex, owinContext.CancellationToken);
                    return true;
                }
                else
                {
                    // todo
                    throw;
                }
            }
            catch (Exception ex)
            {
                if (contentNegotiationResult.Preferred == ContentType.Json)
                {
                    // Sanitize framework-level errors
                    await Error.Create(owinContext, 400, ex.Message, owinContext.CancellationToken);
                    return true;
                }
                else
                {
                    // todo
                    throw;
                }
            }
        }

        private bool HasSupportedAccept(IOwinEnvironment context)
        {
            // if any Accept matches web.produces, true
            // else false
            // todo
            return true;
        }

        private Task<bool> Dispatch(IOwinEnvironment context, IClient scopedClient, ContentNegotiationResult contentNegotiationResult, CancellationToken cancellationToken)
        {
            var method = context.Request.Method;

            if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                return Get(context, scopedClient, contentNegotiationResult, cancellationToken);
            }

            if (method.Equals("POST", StringComparison.OrdinalIgnoreCase))
            {
                return Post(context, scopedClient, contentNegotiationResult, cancellationToken);
            }

            // Do nothing and pass on to next middleware.
            return Task.FromResult(false);
        }

        protected virtual Task<bool> Get(IOwinEnvironment context, IClient client, ContentNegotiationResult contentNegotiationResult, CancellationToken cancellationToken)
        {
            if (contentNegotiationResult.Preferred == ContentType.Json)
            {
                return GetJson(context, client, cancellationToken);
            }

            if (contentNegotiationResult.Preferred == ContentType.Html)
            {
                return GetHtml(context, client, cancellationToken);
            }

            // Do nothing and pass on to next middleware.
            return Task.FromResult(false);
        }

        protected virtual Task<bool> Post(IOwinEnvironment context, IClient client, ContentNegotiationResult contentNegotiationResult, CancellationToken cancellationToken)
        {
            if (contentNegotiationResult.Preferred == ContentType.Json)
            {
                return PostJson(context, client, cancellationToken);
            }

            if (contentNegotiationResult.Preferred == ContentType.Html)
            {
                return PostHtml(context, client, cancellationToken);
            }

            // Do nothing and pass on to next middleware.
            return Task.FromResult(false);
        }

        protected virtual Task<bool> GetJson(IOwinEnvironment context, IClient client, CancellationToken cancellationToken)
        {
            // Do nothing and pass on to next middleware by default.
            return Task.FromResult(false);
        }

        protected virtual Task<bool> GetHtml(IOwinEnvironment context, IClient client, CancellationToken cancellationToken)
        {
            // Do nothing and pass on to next middleware by default.
            return Task.FromResult(false);
        }

        protected virtual Task<bool> PostJson(IOwinEnvironment context, IClient client, CancellationToken cancellationToken)
        {
            // Do nothing and pass on to next middleware by default.
            return Task.FromResult(false);
        }

        protected virtual Task<bool> PostHtml(IOwinEnvironment context, IClient client, CancellationToken cancellationToken)
        {
            // Do nothing and pass on to next middleware by default.
            return Task.FromResult(false);
        }
    }
}
