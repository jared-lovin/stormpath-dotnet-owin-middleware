﻿// <copyright file="LoginRoute.cs" company="Stormpath, Inc.">
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
using System.Threading;
using System.Threading.Tasks;
using Stormpath.Owin.Abstractions;
using Stormpath.Owin.Abstractions.ViewModel;
using Stormpath.Owin.Middleware.Internal;
using Stormpath.Owin.Middleware.Model;
using Stormpath.Owin.Middleware.Model.Error;
using Stormpath.SDK.Account;
using Stormpath.SDK.Client;
using Stormpath.SDK.Error;
using Stormpath.SDK.Oauth;

namespace Stormpath.Owin.Middleware.Route
{
    public class LoginRoute : AbstractRoute
    {
        protected override async Task<bool> GetHtmlAsync(IOwinEnvironment context, IClient client, CancellationToken cancellationToken)
        {
            var queryString = QueryStringParser.Parse(context.Request.QueryString, _logger);

            return await RenderLoginViewAsync(context, cancellationToken, queryString, null);
        }

        private async Task<bool> RenderLoginViewAsync(
            IOwinEnvironment context,
            CancellationToken cancellationToken,
            IDictionary<string, string[]> queryString,
            IDictionary<string, string[]> previousFormData,
            string[] errors = null)
        {
            var viewModelBuilder = new ExtendedLoginViewModelBuilder(
                _configuration.Web,
                _configuration.Providers,
                ChangePasswordRoute.ShouldBeEnabled(_configuration),
                VerifyEmailRoute.ShouldBeEnabled(_configuration),
                queryString,
                previousFormData,
                errors);
            var loginViewModel = viewModelBuilder.Build();

            Cookies.AddTempCookieToResponse(
                context,
                Csrf.OauthStateTokenCookieName,
                loginViewModel.OauthStateToken,
                TimeSpan.FromMinutes(5),
                _logger);

            await RenderViewAsync(context, _configuration.Web.Login.View, loginViewModel, cancellationToken);
            return true;
        }

        private async Task<IOauthGrantAuthenticationResult> HandleLogin(
            IOwinEnvironment environment,
            IClient client,
            string login,
            string password,
            Func<PreLoginContext, CancellationToken, Task> preLoginHandler,
            Func<PostLoginContext, CancellationToken, Task> postLoginHandler,
            CancellationToken cancellationToken)
        {
            var application = await client.GetApplicationAsync(_configuration.Application.Href, cancellationToken);

            var preLoginHandlerContext = new PreLoginContext(environment)
            {
                Login = login
            };

            await preLoginHandler(preLoginHandlerContext, cancellationToken);

            var passwordGrantRequest = OauthRequests.NewPasswordGrantRequest()
                .SetLogin(login)
                .SetPassword(password);

            if (preLoginHandlerContext.AccountStore != null)
            {
                passwordGrantRequest.SetAccountStore(preLoginHandlerContext.AccountStore);
            }

            var passwordGrantAuthenticator = application.NewPasswordGrantAuthenticator();
            var grantResult = await passwordGrantAuthenticator
                .AuthenticateAsync(passwordGrantRequest.Build(), cancellationToken);

            var accessToken = await grantResult.GetAccessTokenAsync(cancellationToken);
            var account = await accessToken.GetAccountAsync(cancellationToken);

            var postLoginHandlerContext = new PostLoginContext(environment, account);
            await postLoginHandler(postLoginHandlerContext, cancellationToken);

            return grantResult;
        }

        protected override async Task<bool> PostHtmlAsync(IOwinEnvironment context, IClient client, ContentType bodyContentType, CancellationToken cancellationToken)
        {
            var queryString = QueryStringParser.Parse(context.Request.QueryString, _logger);

            var body = await context.Request.GetBodyAsStringAsync(cancellationToken);
            var model = PostBodyParser.ToModel<LoginPostModel>(body, bodyContentType, _logger);
            var formData = FormContentParser.Parse(body, _logger);

            bool missingLoginOrPassword = string.IsNullOrEmpty(model.Login) || string.IsNullOrEmpty(model.Password);
            if (missingLoginOrPassword)
            {
                return await RenderLoginViewAsync(
                    context,
                    cancellationToken,
                    queryString,
                    formData,
                    errors: new[] { "The login and password fields are required." });
            }

            try
            {
                var grantResult = await HandleLogin(
                    context,
                    client,
                    model.Login,
                    model.Password,
                    _handlers.PreLoginHandler,
                    _handlers.PostLoginHandler,
                    cancellationToken);

                Cookies.AddTokenCookiesToResponse(context, client, grantResult, _configuration, _logger);
            }
            catch (ResourceException rex)
            {
                return await RenderLoginViewAsync(
                    context,
                    cancellationToken,
                    queryString,
                    formData,
                    errors: new[] { rex.Message });
            }

            var nextUriFromQueryString = queryString.GetString("next");

            var parsedNextUri = string.IsNullOrEmpty(nextUriFromQueryString)
                ? new Uri(_configuration.Web.Login.NextUri, UriKind.Relative)
                : new Uri(nextUriFromQueryString, UriKind.RelativeOrAbsolute);

            // Ensure this is a relative URI
            var nextLocation = parsedNextUri.IsAbsoluteUri
                ? parsedNextUri.PathAndQuery
                : parsedNextUri.OriginalString;
            
            return await HttpResponse.Redirect(context, nextLocation);
        }

        protected override Task<bool> GetJsonAsync(IOwinEnvironment context, IClient client, CancellationToken cancellationToken)
        {
            var viewModelBuilder = new LoginViewModelBuilder(_configuration.Web.Login);
            var loginViewModel = viewModelBuilder.Build();

            return JsonResponse.Ok(context, loginViewModel);
        }

        protected override async Task<bool> PostJsonAsync(IOwinEnvironment context, IClient client, ContentType bodyContentType, CancellationToken cancellationToken)
        {
            var model = await PostBodyParser.ToModel<LoginPostModel>(context, bodyContentType, _logger, cancellationToken);

            bool missingLoginOrPassword = string.IsNullOrEmpty(model.Login) || string.IsNullOrEmpty(model.Password);
            if (missingLoginOrPassword)
            {
                return await Error.Create(context, new BadRequest("Missing login or password."), cancellationToken);
            }

            var grantResult = await HandleLogin(
                context,
                client,
                model.Login,
                model.Password,
                _handlers.PreLoginHandler,
                _handlers.PostLoginHandler,
                cancellationToken);
            // Errors will be caught up in AbstractRouteMiddleware

            Cookies.AddTokenCookiesToResponse(context, client, grantResult, _configuration, _logger);

            var token = await grantResult.GetAccessTokenAsync(cancellationToken);
            var account = await token.GetAccountAsync(cancellationToken);

            var sanitizer = new ResponseSanitizer<IAccount>();
            var responseModel = new
            {
                account = sanitizer.Sanitize(account)
            };

            return await JsonResponse.Ok(context, responseModel);
        }
    }
}
