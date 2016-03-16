﻿// <copyright file="DefaultOwinRequest.cs" company="Stormpath, Inc.">
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stormpath.Owin.Middleware.Internal;

namespace Stormpath.Owin.Middleware.Owin
{
    public sealed class DefaultOwinRequest : IOwinRequest
    {
        private readonly IDictionary<string, object> environment;

        public DefaultOwinRequest(IDictionary<string, object> owinEnvironment)
        {
            this.environment = owinEnvironment;
        }

        public Stream Body
            => environment.Get<Stream>(OwinKeys.RequestBody);

        public IDictionary<string, string[]> Headers
            => environment.Get<IDictionary<string, string[]>>(OwinKeys.RequestHeaders);

        public string Method
            => environment.Get<string>(OwinKeys.RequestMethod);

        public string Path
            => environment.Get<string>(OwinKeys.RequestPath);

        public string PathBase
            => environment.Get<string>(OwinKeys.RequestPathBase);
        
        public string QueryString
            => environment.Get<string>(OwinKeys.RequestQueryString);

        public async Task<T> GetBodyAsAsync<T>(CancellationToken cancellationToken)
            where T : new()
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bodyAsString = string.Empty;
            using (var reader = new StreamReader(this.Body))
            {
                bodyAsString = await reader.ReadToEndAsync();
            }

            return Serializer.Deserialize<T>(bodyAsString);
        }
    }
}
