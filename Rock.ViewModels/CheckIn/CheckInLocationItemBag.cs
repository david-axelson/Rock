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
    /// Defines a single location that can be used during check-in.
    /// </summary>
    public class CheckInLocationItemBag : CheckInItemBag
    {
        /// <summary>
        /// Gets or sets the maximum capacity of the location.
        /// </summary>
        /// <value>The maximum capacity; or <c>null</c> if not available.</value>
        public int? Capacity { get; set; }

        /// <summary>
        /// Gets or sets the number of available spots in the location.
        /// </summary>
        /// <value>The number of available spots; or <c>null</c> if not available.</value>
        public int? Available { get; set; }

        /// <summary>
        /// Gets or sets the schedule unique identifiers that this location
        /// is valid for.
        /// </summary>
        /// <value>The schedule unique identifiers.</value>
        public List<Guid> ScheduleGuids { get; set; }

        /// <summary>
        /// Gets or sets the group unique identifiers that this location
        /// is valid for.
        /// </summary>
        /// <value>The group unique identifiers.</value>
        public List<Guid> GroupGuids { get; set; }
    }
}
