//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
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
using System.Linq;

using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Entities
{
    /// <summary>
    /// Attendance View Model
    /// </summary>
    public partial class AttendanceBag : EntityBagBase
    {
        /// <summary>
        /// Gets or sets the AttendanceCheckInSessionId.
        /// </summary>
        /// <value>
        /// The AttendanceCheckInSessionId.
        /// </value>
        public int? AttendanceCheckInSessionId { get; set; }

        /// <summary>
        /// Gets or sets the AttendanceCodeId.
        /// </summary>
        /// <value>
        /// The AttendanceCodeId.
        /// </value>
        public int? AttendanceCodeId { get; set; }

        /// <summary>
        /// Gets or sets the CampusId.
        /// </summary>
        /// <value>
        /// The CampusId.
        /// </value>
        public int? CampusId { get; set; }

        /// <summary>
        /// Gets or sets the CheckedInByPersonAliasId.
        /// </summary>
        /// <value>
        /// The CheckedInByPersonAliasId.
        /// </value>
        public int? CheckedInByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the CheckedOutByPersonAliasId.
        /// </summary>
        /// <value>
        /// The CheckedOutByPersonAliasId.
        /// </value>
        public int? CheckedOutByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the DeclineReasonValueId.
        /// </summary>
        /// <value>
        /// The DeclineReasonValueId.
        /// </value>
        public int? DeclineReasonValueId { get; set; }

        /// <summary>
        /// Gets or sets the DeviceId.
        /// </summary>
        /// <value>
        /// The DeviceId.
        /// </value>
        public int? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets the DidAttend.
        /// </summary>
        /// <value>
        /// The DidAttend.
        /// </value>
        public bool? DidAttend { get; set; }

        /// <summary>
        /// Gets or sets the EndDateTime.
        /// </summary>
        /// <value>
        /// The EndDateTime.
        /// </value>
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the IsFirstTime.
        /// </summary>
        /// <value>
        /// The IsFirstTime.
        /// </value>
        public bool? IsFirstTime { get; set; }

        /// <summary>
        /// Gets or sets the Note.
        /// </summary>
        /// <value>
        /// The Note.
        /// </value>
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the OccurrenceId.
        /// </summary>
        /// <value>
        /// The OccurrenceId.
        /// </value>
        public int OccurrenceId { get; set; }

        /// <summary>
        /// Gets or sets the PersonAliasId.
        /// </summary>
        /// <value>
        /// The PersonAliasId.
        /// </value>
        public int? PersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the PresentByPersonAliasId.
        /// </summary>
        /// <value>
        /// The PresentByPersonAliasId.
        /// </value>
        public int? PresentByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the PresentDateTime.
        /// </summary>
        /// <value>
        /// The PresentDateTime.
        /// </value>
        public DateTime? PresentDateTime { get; set; }

        /// <summary>
        /// Gets or sets the Processed.
        /// </summary>
        /// <value>
        /// The Processed.
        /// </value>
        public bool? Processed { get; set; }

        /// <summary>
        /// Gets or sets the QualifierValueId.
        /// </summary>
        /// <value>
        /// The QualifierValueId.
        /// </value>
        public int? QualifierValueId { get; set; }

        /// <summary>
        /// Gets or sets the RequestedToAttend.
        /// </summary>
        /// <value>
        /// The RequestedToAttend.
        /// </value>
        public bool? RequestedToAttend { get; set; }

        /// <summary>
        /// Gets or sets the RSVP.
        /// </summary>
        /// <value>
        /// The RSVP.
        /// </value>
        public int RSVP { get; set; }

        /// <summary>
        /// Gets or sets the RSVPDateTime.
        /// </summary>
        /// <value>
        /// The RSVPDateTime.
        /// </value>
        public DateTime? RSVPDateTime { get; set; }

        /// <summary>
        /// Gets or sets the ScheduleConfirmationSent.
        /// </summary>
        /// <value>
        /// The ScheduleConfirmationSent.
        /// </value>
        public bool? ScheduleConfirmationSent { get; set; }

        /// <summary>
        /// Gets or sets the ScheduledByPersonAliasId.
        /// </summary>
        /// <value>
        /// The ScheduledByPersonAliasId.
        /// </value>
        public int? ScheduledByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the ScheduledToAttend.
        /// </summary>
        /// <value>
        /// The ScheduledToAttend.
        /// </value>
        public bool? ScheduledToAttend { get; set; }

        /// <summary>
        /// Gets or sets the ScheduleReminderSent.
        /// </summary>
        /// <value>
        /// The ScheduleReminderSent.
        /// </value>
        public bool? ScheduleReminderSent { get; set; }

        /// <summary>
        /// Gets or sets the SearchResultGroupId.
        /// </summary>
        /// <value>
        /// The SearchResultGroupId.
        /// </value>
        public int? SearchResultGroupId { get; set; }

        /// <summary>
        /// Gets or sets the SearchTypeValueId.
        /// </summary>
        /// <value>
        /// The SearchTypeValueId.
        /// </value>
        public int? SearchTypeValueId { get; set; }

        /// <summary>
        /// Gets or sets the SearchValue.
        /// </summary>
        /// <value>
        /// The SearchValue.
        /// </value>
        public string SearchValue { get; set; }

        /// <summary>
        /// Gets or sets the StartDateTime.
        /// </summary>
        /// <value>
        /// The StartDateTime.
        /// </value>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the CreatedDateTime.
        /// </summary>
        /// <value>
        /// The CreatedDateTime.
        /// </value>
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the ModifiedDateTime.
        /// </summary>
        /// <value>
        /// The ModifiedDateTime.
        /// </value>
        public DateTime? ModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the CreatedByPersonAliasId.
        /// </summary>
        /// <value>
        /// The CreatedByPersonAliasId.
        /// </value>
        public int? CreatedByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the ModifiedByPersonAliasId.
        /// </summary>
        /// <value>
        /// The ModifiedByPersonAliasId.
        /// </value>
        public int? ModifiedByPersonAliasId { get; set; }

    }
}