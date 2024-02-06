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

using Rock.Enums.CheckIn;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// This provides the server check-in configuration data for a single
    /// check-in template group type. This should not be sent down to clients
    /// as it contains additional data they should not see.
    /// </summary>
    internal class CheckInConfigurationData
    {
        /// <summary>
        /// Gets or sets the type of the family search configured for
        /// the configuration.
        /// </summary>
        /// <value>The type of the family search.</value>
        public FamilySearchType FamilySearchType { get; set; }

        /// <summary>
        /// Gets or sets the minimum length of the phone number during
        /// family search.
        /// </summary>
        /// <value>The minimum length of the phone number.</value>
        public int? MinimumPhoneNumberLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum length of the phone number during
        /// family search.
        /// </summary>
        /// <value>The maximum length of the phone number.</value>
        public int? MaximumPhoneNumberLength { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of family search results.
        /// </summary>
        /// <value>The maximum number of family search results.</value>
        public int? MaximumNumberOfResults { get; set; }

        /// <summary>
        /// Gets or sets the type of the phone search used in family search.
        /// </summary>
        /// <value>The type of the phone search used in family search.</value>
        public Enums.CheckIn.PhoneSearchType PhoneSearchType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether inactive people should
        /// be excluded from the check-in process.
        /// </summary>
        /// <value><c>true</c> if inactive people should be excluded; otherwise, <c>false</c>.</value>
        public bool PreventInactivePeople { get; set; }
    }
}
