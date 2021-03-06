﻿// <copyright file="ForgotPasswordViewModelBuilder.cs" company="Stormpath, Inc.">
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

using System.Collections.Generic;
using Stormpath.Configuration.Abstractions.Immutable;

namespace Stormpath.Owin.Abstractions.ViewModel
{
    public sealed class ForgotPasswordViewModelBuilder
    {
        private readonly WebConfiguration _webConfiguration;
        private readonly IDictionary<string, string[]> _queryString;

        public ForgotPasswordViewModelBuilder(
            WebConfiguration webConfiguration,
            IDictionary<string, string[]> queryString)
        {
            _webConfiguration = webConfiguration;
            _queryString = queryString;
        }

        public ForgotPasswordViewModel Build()
        {
            var result = new ForgotPasswordViewModel();

            // status parameter from queryString
            result.Status = _queryString.GetString("status");

            // Copy values from configuration
            result.ForgotPasswordUri = _webConfiguration.ForgotPassword.Uri;
            result.LoginEnabled = _webConfiguration.Login.Enabled;
            result.LoginUri = _webConfiguration.Login.Uri;

            return result;
        }
    }
}
