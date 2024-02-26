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

using Rock.Model;
using Rock.ViewModels.CheckIn;
using Rock.Web.Cache;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Provides functionality for making default selections for a person. This
    /// is used when the AutoSelect feature is enabled and also configured to
    /// select the group/location/schedule.
    /// </summary>
    internal class DefaultOptionsSelector
    {
        /// <summary>
        /// Gets the default selection for the person. This uses recent
        /// attendance to try and put them in the same location they were in
        /// last time but will fall back to other methods if that is not
        /// available.
        /// </summary>
        /// <param name="person">The person to get the default selection for.</param>
        /// <returns>A new instance of <see cref="SelectedOptionsBag"/> or <c>null</c> if no defaults could be determined.</returns>
        public virtual SelectedOptionsBag GetDefaultSelectionForPerson( CheckInAttendeeItem person )
        {
            person.LastCheckIn = person.RecentAttendances.Max( a => ( DateTime? ) a.StartDateTime );

            var orderedRecentAttendance = person.RecentAttendances
                .Where( a => a.StartDateTime.Date == person.LastCheckIn.Value.Date )
                .OrderBy( a => NamedScheduleCache.Get( a.ScheduleGuid )?.StartTimeOfDay )
                .ThenByDescending( a => a.StartDateTime );

            var previousCheckIns = new List<RecentAttendanceItem>();

            // Sum down the previous check-ins so that we only have one per schedule.
            // This is ordered in such a way that the most recent attendance will
            // take precedence over older attendances.
            foreach ( var attendance in orderedRecentAttendance )
            {
                if ( !previousCheckIns.Any( i => i.ScheduleGuid == attendance.ScheduleGuid ) )
                {
                    previousCheckIns.Add( attendance );
                }
            }

            // First try to find a valid exact match against a previous check-in.
            if ( TryGetExactMatch( person, previousCheckIns, out var selectedOptions ) )
            {
                return selectedOptions;
            }

            // Next, try to find a matching group and then just take the first
            // available location and schedule.
            if ( TryGetBestMatchingGroup( person, previousCheckIns, out selectedOptions ) )
            {
                return selectedOptions;
            }

            // Finally just try to pick anything valid.
            if ( TryGetAnyValidSelection( person, out selectedOptions ) )
            {
                return selectedOptions;
            }

            return null;
        }

        /// <summary>
        /// Attempts to get an exact match from a previous check-in. This checks
        /// for exact matches to group, location and schedule.
        /// </summary>
        /// <param name="person">The person to be checked in.</param>
        /// <param name="previousCheckIns">The previous check-in records.</param>
        /// <param name="selectedOptions">On return contains an instance of <see cref="SelectedOptionsBag"/> or <c>null</c>.</param>
        /// <returns><c>true</c> if a match was found and <paramref name="selectedOptions"/> is valid, <c>false</c> otherwise.</returns>
        protected virtual bool TryGetExactMatch( CheckInAttendeeItem person, List<RecentAttendanceItem> previousCheckIns, out SelectedOptionsBag selectedOptions )
        {
            foreach ( var previousCheckIn in previousCheckIns )
            {
                var group = person.Options.Groups
                    .FirstOrDefault( g => g.Guid == previousCheckIn.GroupGuid );

                if ( group == null || !group.LocationGuids.Contains( previousCheckIn.LocationGuid ) )
                {
                    continue;
                }

                var area = person.Options.Areas
                    .FirstOrDefault( a => a.Guid == group.AreaGuid );

                if ( area == null )
                {
                    continue;
                }

                var location = person.Options.Locations
                    .FirstOrDefault( l => l.Guid == previousCheckIn.LocationGuid );

                if ( location == null || !location.ScheduleGuids.Contains( previousCheckIn.ScheduleGuid ) )
                {
                    continue;
                }

                var schedule = person.Options.Schedules
                    .FirstOrDefault( s => s.Guid == previousCheckIn.ScheduleGuid );

                if ( schedule == null )
                {
                    continue;
                }

                selectedOptions = GetSelectedOptions( area, group, location, schedule );

                return true;
            }

            selectedOptions = null;

            return false;
        }

        /// <summary>
        /// Attempts to get a loose match from a previous check-in. This checks
        /// for exact matches to group and will use any valid location and
        /// schedule currently supported for that group.
        /// </summary>
        /// <param name="person">The person to be checked in.</param>
        /// <param name="previousCheckIns">The previous check-in records.</param>
        /// <param name="selectedOptions">On return contains an instance of <see cref="SelectedOptionsBag"/> or <c>null</c>.</param>
        /// <returns><c>true</c> if a match was found and <paramref name="selectedOptions"/> is valid, <c>false</c> otherwise.</returns>
        protected virtual bool TryGetBestMatchingGroup( CheckInAttendeeItem person, List<RecentAttendanceItem> previousCheckIns, out SelectedOptionsBag selectedOptions )
        {
            foreach ( var previousCheckIn in previousCheckIns )
            {
                var group = person.Options.Groups
                    .FirstOrDefault( g => g.Guid == previousCheckIn.GroupGuid );

                if ( group == null )
                {
                    continue;
                }

                var area = person.Options.Areas
                    .FirstOrDefault( a => a.Guid == group.AreaGuid );

                if ( area == null )
                {
                    continue;
                }

                if ( TryGetFirstValidSelectionForGroup( area, group, person, out selectedOptions ) )
                {
                    return true;
                }
            }

            selectedOptions = null;

            return false;
        }

        /// <summary>
        /// Attempts to get any valid selection for the person. This is called
        /// as a last resort.
        /// </summary>
        /// <param name="person">The person to be checked in.</param>
        /// <param name="selectedOptions">On return contains an instance of <see cref="SelectedOptionsBag"/> or <c>null</c>.</param>
        /// <returns><c>true</c> if a match was found and <paramref name="selectedOptions"/> is valid, <c>false</c> otherwise.</returns>
        protected virtual bool TryGetAnyValidSelection( CheckInAttendeeItem person, out SelectedOptionsBag selectedOptions )
        {
            foreach ( var group in person.Options.Groups )
            {
                var area = person.Options.Areas
                    .FirstOrDefault( a => a.Guid == group.AreaGuid );

                if ( area == null )
                {
                    continue;
                }

                if ( TryGetFirstValidSelectionForGroup( area, group, person, out selectedOptions ) )
                {
                    return true;
                }
            }

            selectedOptions = null;

            return false;
        }

        /// <summary>
        /// Attempts to get the first valid location and schedule for the
        /// indicated area and group.
        /// </summary>
        /// <param name="area">The potential check-in area to be selected.</param>
        /// <param name="group">The potential check-in group to be selected.</param>
        /// <param name="person">The person to be checked in.</param>
        /// <param name="selectedOptions">On return contains an instance of <see cref="SelectedOptionsBag"/> or <c>null</c>.</param>
        /// <returns><c>true</c> if a match was found and <paramref name="selectedOptions"/> is valid, <c>false</c> otherwise.</returns>
        protected virtual bool TryGetFirstValidSelectionForGroup( CheckInAreaItem area, CheckInGroupItem group, CheckInAttendeeItem person, out SelectedOptionsBag selectedOptions )
        {
            foreach ( var locationGuid in group.LocationGuids )
            {
                var location = person.Options.Locations
                    .FirstOrDefault( l => l.Guid == locationGuid );

                if ( location == null )
                {
                    continue;
                }

                foreach ( var scheduleGuid in location.ScheduleGuids )
                {
                    var schedule = person.Options.Schedules
                        .FirstOrDefault( s => s.Guid == scheduleGuid );

                    if ( schedule == null )
                    {
                        continue;
                    }

                    selectedOptions = GetSelectedOptions( area, group, location, schedule );

                    return true;
                }
            }

            selectedOptions = null;

            return false;
        }

        /// <summary>
        /// This is a convenience method to get the <see cref="SelectedOptionsBag"/>
        /// from the given values.
        /// </summary>
        /// <param name="area">The selectedarea.</param>
        /// <param name="group">The selected group.</param>
        /// <param name="location">The selected location.</param>
        /// <param name="schedule">The selected schedule.</param>
        /// <returns>An instance of <see cref="SelectedOptionsBag"/>.</returns>
        protected SelectedOptionsBag GetSelectedOptions( CheckInAreaItem area, CheckInGroupItem group, CheckInLocationItem location, CheckInScheduleItem schedule )
        {
            return new SelectedOptionsBag
            {
                Area = new CheckInItemBag
                {
                    Guid = area.Guid,
                    Name = area.Name
                },
                Group = new CheckInItemBag
                {
                    Guid = group.Guid,
                    Name = group.Name
                },
                Location = new CheckInItemBag
                {
                    Guid = location.Guid,
                    Name = location.Name
                },
                Schedule = new CheckInItemBag
                {
                    Guid = schedule.Guid,
                    Name = schedule.Name
                }
            };
        }
    }
}
