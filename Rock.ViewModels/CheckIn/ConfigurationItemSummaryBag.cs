// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;

namespace Rock.ViewModels.CheckIn
{
    /// <summary>
    /// The summary information about a single check-in configuration.
    /// </summary>
    public class ConfigurationItemSummaryBag
    {
        /// <summary>
        /// Gets or sets the unique identifier of this check-in configuration.
        /// </summary>
        /// <value>The unique identifier.</value>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the name of this check-in configuration.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the icon CSS class defined on the check-in configuration.
        /// </summary>
        /// <value>The icon CSS class.</value>
        public string IconCssClass { get; set; }
    }
}
