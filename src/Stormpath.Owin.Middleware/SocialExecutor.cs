﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Stormpath.Configuration.Abstractions.Immutable;
using Stormpath.Owin.Abstractions;
using Stormpath.Owin.Middleware.Internal;
using Stormpath.SDK.Account;
using Stormpath.SDK.Application;
using Stormpath.SDK.Client;
using Stormpath.SDK.Error;
using Stormpath.SDK.Logging;
using Stormpath.SDK.Provider;

namespace Stormpath.Owin.Middleware
{
    internal sealed class SocialExecutor
    {
        private readonly IClient _client;
        private readonly StormpathConfiguration _configuration;
        private readonly HandlerConfiguration _handlers;
        private readonly ILogger _logger;

        public SocialExecutor(
            IClient client,
            StormpathConfiguration configuration,
            HandlerConfiguration handlers,
            ILogger logger)
        {
            _client = client;
            _configuration = configuration;
            _handlers = handlers;
            _logger = logger;
        }

        public static string CreateErrorUri(WebLoginRouteConfiguration loginRouteConfiguration, string stateToken)
        {
            var uri = $"{loginRouteConfiguration.Uri}?status=social_failed";

            if (!string.IsNullOrEmpty(stateToken))
            {
                uri += $"&{StringConstants.StateTokenName}={stateToken}";
            }

            return uri;
        }

        public async Task<ExternalLoginResult> LoginWithProviderRequestAsync(
            IOwinEnvironment environment,
            IProviderAccountRequest providerRequest,
            CancellationToken cancellationToken)
        {
            var application = await _client.GetApplicationAsync(_configuration.Application.Href, cancellationToken);

            var result = await application.GetAccountAsync(providerRequest, cancellationToken);

            return new ExternalLoginResult
            {
                Account = result.Account,
                IsNewAccount = result.IsNewAccount
            };
        }

        public async Task HandleLoginResultAsync(
            IOwinEnvironment environment,
            IApplication application,
            ExternalLoginResult loginResult,
            CancellationToken cancellationToken)
        {
            if (loginResult.Account == null)
            {
                throw new ArgumentNullException(nameof(loginResult.Account), "Login resulted in a null account");
            }

            var loginExecutor = new LoginExecutor(_client, _configuration, _handlers, _logger);
            var exchangeResult = await
                loginExecutor.TokenExchangeGrantAsync(environment, application, loginResult.Account, cancellationToken);

            if (exchangeResult == null)
            {
                throw new InvalidOperationException("The token exchange failed");
            }

            if (loginResult.IsNewAccount)
            {
                var registerExecutor = new RegisterExecutor(_client, _configuration, _handlers, _logger);
                await registerExecutor.HandlePostRegistrationAsync(environment, loginResult.Account, cancellationToken);
            }

            await loginExecutor.HandlePostLoginAsync(environment, exchangeResult, cancellationToken);
        }

        public async Task<bool> HandleRedirectAsync(
            IClient client,
            IOwinEnvironment environment,
            ExternalLoginResult loginResult,
            string nextUri,
            CancellationToken cancellationToken)
        {
            var loginExecutor = new LoginExecutor(_client, _configuration, _handlers, _logger);

            if (string.IsNullOrEmpty(nextUri))
            {
                nextUri = loginResult.IsNewAccount
                    ? _configuration.Web.Register.NextUri
                    : _configuration.Web.Login.NextUri;
            }

            return await loginExecutor.HandleRedirectAsync(environment, nextUri);
        }
    }
}
