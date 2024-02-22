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
using System.Linq;

namespace Rock.CheckIn.v2.Filters
{
    /// <summary>
    /// Performs filtering of check-in options based on if the person has
    /// already checked into the schedule.
    /// </summary>
    internal class CheckInOptionsDuplicateCheckInFilter : CheckInOptionsFilter
    {
        #region Properties

        /// <summary>
        /// Gets the schedule unique identifiers this person is currently
        /// checked into today.
        /// </summary>
        /// <value>The checked in schedule unique identifiers.</value>
        private Lazy<HashSet<Guid>> CheckedInScheduleGuids { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInOptionsDuplicateCheckInFilter"/> class.
        /// </summary>
        public CheckInOptionsDuplicateCheckInFilter()
        {
            CheckedInScheduleGuids = new Lazy<HashSet<Guid>>( () =>
            {
                var today = RockDateTime.Today;
                var attendances = Person.RecentAttendances
                    .Where( a => a.StartDateTime.Date == today
                        && !a.EndDateTime.HasValue )
                    .Select( a => a.ScheduleGuid );

                return new HashSet<Guid>( attendances );
            }, true );
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override bool IsScheduleValid( CheckInScheduleItem schedule )
        {
            if ( !Configuration.IsDuplicateCheckInPrevented )
            {
                return true;
            }

            // Remove any schedules the attendee has already checked in for.
            return !CheckedInScheduleGuids.Value.Contains( schedule.Guid );
        }

        #endregion
    }
}
