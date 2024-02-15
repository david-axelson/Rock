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

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Contains a set of check-in options. This can be either all available
    /// options or the options available for single individual depending
    /// on the use case.
    /// </summary>
    internal class CheckInOptions
    {
        #region Properties

        /// <summary>
        /// Gets or sets the ability levels available to select from.
        /// </summary>
        /// <value>The list of ability levels.</value>
        public List<CheckInAbilityLevelItem> AbilityLevels { get; set; }

        /// <summary>
        /// Gets or sets the areas that are available for check-in.
        /// </summary>
        /// <value>The list of areas.</value>
        public List<CheckInAreaItem> Areas { get; set; }

        /// <summary>
        /// Gets or sets the groups that are available for check-in.
        /// </summary>
        /// <value>The list of groups.</value>
        public List<CheckInGroupItem> Groups { get; set; }

        /// <summary>
        /// Gets or sets the locations that are available for check-in.
        /// </summary>
        /// <value>The list of locations.</value>
        public List<CheckInLocationItem> Locations { get; set; }

        /// <summary>
        /// Gets or sets the schedules that are available for check-in.
        /// </summary>
        /// <value>The list of schedules.</value>
        public List<CheckInScheduleItem> Schedules { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Clones this instance. This creates an entirely new options instance
        /// as well as new instances of every object it contains. The new options
        /// can be modified at will without affecting the original. It seems
        /// like we are doing a lot, but this is insanely fast, clocking in at
        /// 6ns per call.
        /// </summary>
        /// <returns>A new instance of <see cref="CheckInOptions"/>.</returns>
        public CheckInOptions Clone()
        {
            var clonedOptions = new CheckInOptions
            {
                AbilityLevels = AbilityLevels
                    .Select( al => new CheckInAbilityLevelItem
                    {
                        Guid = al.Guid,
                        Name = al.Name
                    } )
                    .ToList(),
                Areas = Areas
                    .Select( a => new CheckInAreaItem
                    {
                        Guid = a.Guid,
                        Name = a.Name
                    } )
                    .ToList(),
                Groups = Groups
                    .Select( g => new CheckInGroupItem
                    {
                        Guid = g.Guid,
                        Name = g.Name,
                        AbilityLevelGuid = g.AbilityLevelGuid,
                        AreaGuid = g.AreaGuid,
                        CheckInData = g.CheckInData,
                        CheckInAreaData = g.CheckInAreaData,
                        LocationGuids = g.LocationGuids.ToList()
                    } )
                    .ToList(),
                Locations = Locations
                    .Select( l => new CheckInLocationItem
                    {
                        Guid = l.Guid,
                        Name = l.Name,
                        CurrentCount = l.CurrentCount,
                        Capacity = l.Capacity,
                        CurrentPersonGuids = new HashSet<Guid>( l.CurrentPersonGuids ),
                        ScheduleGuids = l.ScheduleGuids.ToList().ToList()
                    } )
                    .ToList(),
                Schedules = Schedules
                    .Select( s => new CheckInScheduleItem
                    {
                        Guid = s.Guid,
                        Name = s.Name
                    } )
                    .ToList()
            };

            return clonedOptions;
        }

        #endregion
    }
}
