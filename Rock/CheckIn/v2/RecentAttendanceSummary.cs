﻿// <copyright>
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

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// A representation of a recent attendance record for a person.
    /// </summary>
    internal class RecentAttendanceSummary
    {
        /// <summary>
        /// Gets or sets the Attendance identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public int AttendanceId { get; set; }

        /// <summary>
        /// Gets or sets the start date time.
        /// </summary>
        /// <value>
        /// The start date time.
        /// </value>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the end date time.
        /// </summary>
        /// <value>
        /// The end date time.
        /// </value>
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the person unique identifier.
        /// </summary>
        /// <value>
        /// The person unique identifier.
        /// </value>
        public Guid PersonGuid { get; set; }

        /// <summary>
        /// Gets or sets the group type unique identifier.
        /// </summary>
        /// <value>
        /// The group type unique identifier.
        /// </value>
        public Guid GroupTypeGuid { get; set; }

        /// <summary>
        /// Gets or sets the group unique identifier.
        /// </summary>
        /// <value>
        /// The group unique identifier.
        /// </value>
        public Guid GroupGuid { get; set; }

        /// <summary>
        /// Gets or sets the location unique identifier.
        /// </summary>
        /// <value>
        /// The location unique identifier.
        /// </value>
        public Guid LocationGuid { get; set; }

        /// <summary>
        /// Gets or sets the schedule unique identifier.
        /// </summary>
        /// <value>
        /// The schedule unique identifier.
        /// </value>
        public Guid ScheduleGuid { get; set; }
    }
}
