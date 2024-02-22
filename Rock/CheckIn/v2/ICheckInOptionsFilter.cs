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
        #region Properties

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
        /// Gets or sets the person to filter the options for.
        /// </summary>
        /// <value>The person to filter the options for.</value>
        CheckInFamilyMemberItem Person { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the specified group is valid for check-in.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns><c>true</c> if the group is valid; otherwise, <c>false</c>.</returns>
        bool IsGroupValid( CheckInGroupItem group );

        /// <summary>
        /// Determines whether the specified location is valid for check-in.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns><c>true</c> if the location is valid; otherwise, <c>false</c>.</returns>
        bool IsLocationValid( CheckInLocationItem location );

        /// <summary>
        /// Determines whether the specified schedule is valid for check-in.
        /// </summary>
        /// <param name="schedule">The schedule.</param>
        /// <returns><c>true</c> if the schedule is valid; otherwise, <c>false</c>.</returns>
        bool IsScheduleValid( CheckInScheduleItem schedule );

        #endregion
    }
}
