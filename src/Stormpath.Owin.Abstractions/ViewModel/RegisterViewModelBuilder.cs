﻿// <copyright file="RegisterViewModelBuilder.cs" company="Stormpath, Inc.">
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

using System.Linq;
using Stormpath.Configuration.Abstractions.Immutable;

namespace Stormpath.Owin.Abstractions.ViewModel
{
    public sealed class RegisterViewModelBuilder
    {
        private readonly WebRegisterRouteConfiguration registerRouteConfiguration;

        public RegisterViewModelBuilder(WebRegisterRouteConfiguration registerRouteConfiguration)
        {
            this.registerRouteConfiguration = registerRouteConfiguration;
        }

        public RegisterViewModel Build()
        {
            var result = new RegisterViewModel();

            var fieldViewModelBuilder = new FormFieldViewModelBuilder(
                registerRouteConfiguration.Form.FieldOrder,
                registerRouteConfiguration.Form.Fields,
                Stormpath.Configuration.Abstractions.Default.Configuration.Web.Register.Form.Fields);
            result.Form.Fields = fieldViewModelBuilder.Build().ToArray();

            return result;
        }
    }
}
