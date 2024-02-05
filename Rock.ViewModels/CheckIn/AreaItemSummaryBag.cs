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
using System.Collections.Generic;

namespace Rock.ViewModels.CheckIn
{
    /// <summary>
    /// The summary information about a single check-in area.
    /// </summary>
    public class AreaItemSummaryBag
    {
        /// <summary>
        /// Gets or sets the unique identifier of this check-in area.
        /// </summary>
        /// <value>The unique identifier.</value>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the name of this check-in area.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the configuration unique identifiers that are
        /// considered primary for this area. A primary configuration is
        /// one that this area is a descendant of.
        /// </summary>
        /// <value>The primary configuration unique identifiers.</value>
        public List<Guid> PrimaryConfigurationGuids { get; set; }
    }
}
