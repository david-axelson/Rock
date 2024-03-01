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

namespace Rock.ViewModels.Rest.CheckIn
{
    /// <summary>
    /// The request parameters to use when requesting the options available
    /// for a single attendee.
    /// </summary>
    public class AttendeeOpportunitiesOptionsBag
    {
        /// <summary>
        /// Gets or sets the configuration template unique identifier.
        /// </summary>
        /// <value>The configuration template unique identifier.</value>
        public Guid ConfigurationTemplateGuid { get; set; }

        /// <summary>
        /// Gets or sets the area unique identifiers that will be used to
        /// determine which options are available for each family member.
        /// </summary>
        /// <value>The check-in area unique identifiers.</value>
        public List<Guid> AreaGuids { get; set; }

        /// <summary>
        /// Gets or sets the kiosk unique identifier.
        /// </summary>
        /// <value>The kiosk unique identifier.</value>
        public Guid KioskGuid { get; set; }

        /// <summary>
        /// Gets or sets the person unique identifier.
        /// </summary>
        /// <value>The person unique identifier.</value>
        public Guid PersonGuid { get; set; }

        /// <summary>
        /// Gets or sets the family unique identifier.
        /// </summary>
        /// <value>The family unique identifier.</value>
        public Guid? FamilyGuid { get; set; }
    }
}
