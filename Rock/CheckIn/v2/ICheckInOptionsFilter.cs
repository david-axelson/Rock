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

using Rock.Data;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Provides functionality to filter check-in options.
    /// </summary>
    internal interface ICheckInOptionsFilter
    {
        /// <summary>
        /// Gets or sets the check-in configuration.
        /// </summary>
        /// <value>The check-in configuration.</value>
        CheckInConfigurationData Configuration { get; set; }

        /// <summary>
        /// Gets or sets the context to use if database access is needed.
        /// </summary>
        /// <value>The context to use if database access is needed.</value>
        RockContext RockContext { get; set; }

        /// <summary>
        /// Determines whether the specified group is valid for check-in.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns><c>true</c> if the group is valid; otherwise, <c>false</c>.</returns>
        bool IsGroupValid( CheckInGroupItem group );
    }
}
