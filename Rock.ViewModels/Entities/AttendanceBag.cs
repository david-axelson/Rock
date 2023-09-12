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
        /// Gets or sets the Rock.Model.AttendanceCheckInSession identifier.
        /// </summary>
        /// <value>
        /// The attendance check in session identifier.
        /// </value>
        public int? AttendanceCheckInSessionId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the Rock.Model.AttendanceCode that is associated with this Rock.Model.Attendance entity.
        /// </summary>
        /// <value>
        /// A System.Int32 representing the Id of the Rock.Model.AttendanceCode that is associated with this Rock.Model.Attendance entity.
        /// </value>
        public int? AttendanceCodeId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the Rock.Model.Campus that the individual attended/checked in to.
        /// </summary>
        /// <value>
        /// A System.Int32 representing the Id of the Rock.Model.Campus that was checked in to.
        /// </value>
        public int? CampusId { get; set; }

        /// <summary>
        /// Gets or sets the person who was identified as the person doing the check-in.
        /// </summary>
        /// <value>
        /// The person alias identifier of person doing check-in.
        /// </value>
        public int? CheckedInByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the person that checked-out the Rock.Model.PersonAlias person attended.
        /// </summary>
        /// <value>
        /// The person that checked-out the Rock.Model.Attendance.PersonAlias person attended.
        /// </value>
        public int? CheckedOutByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the Reason that the Rock.Model.Attendance.PersonAlias person declined to attend
        /// </summary>
        /// <value>
        /// The decline reason value identifier.
        /// </value>
        public int? DeclineReasonValueId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the Rock.Model.Device that was used (the device where the person checked in from).
        /// </summary>
        /// <value>
        /// A System.Int32 representing the Id of the Rock.Model.Device that was used.
        /// </value>
        public int? DeviceId { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if the person attended.
        /// </summary>
        /// <value>
        /// A System.Boolean indicating if the person attended. This value will be true if they did attend, otherwise false.
        /// </value>
        public bool? DidAttend { get; set; }

        /// <summary>
        /// Gets or sets the date and time that person checked out.
        /// </summary>
        /// <value>
        /// A System.DateTime representing the date and time that person checked out.
        /// </value>
        public DateTime? EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets if this first time that this person has ever checked into anything
        /// </summary>
        /// <value>
        /// If this attendance is the first time the person has attended anything
        /// </value>
        public bool? IsFirstTime { get; set; }

        /// <summary>
        /// Gets or sets the note.
        /// </summary>
        /// <value>
        /// A System.String representing the note.
        /// </value>
        public string Note { get; set; }

        /// <summary>
        /// Gets or sets the Id of the Rock.Model.AttendanceOccurrence that the attendance is for.
        /// </summary>
        /// <value>
        /// A System.Int32 representing the Id of the AttendanceOccurrence that the attendance is for.
        /// </value>
        public int OccurrenceId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the Rock.Model.Person that attended/checked in to the Rock.Model.Group
        /// </summary>
        /// <value>
        /// A System.Int32 representing the Id of the Rock.Model.Person who attended/checked in.
        /// </value>
        public int? PersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the person that presented the Rock.Model.PersonAlias person attended.
        /// </summary>
        /// <value>
        /// The person that presented the Rock.Model.Attendance.PersonAlias person attended.
        /// </value>
        public int? PresentByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the present date and time.
        /// </summary>
        /// <value>
        /// A System.DateTime representing the present date and time.
        /// </value>
        public DateTime? PresentDateTime { get; set; }

        /// <summary>
        /// Gets or sets the processed.
        /// </summary>
        /// <value>
        /// The processed.
        /// </value>
        public bool? Processed { get; set; }

        /// <summary>
        /// Gets or sets the qualifier value id.  Qualifier can be used to
        /// "qualify" attendance records.  There are not any system values
        /// for this particular defined type
        /// </summary>
        /// <value>
        /// The qualifier value id.
        /// </value>
        public int? QualifierValueId { get; set; }

        /// <summary>
        /// Gets or sets if the Rock.Model.Attendance.PersonAlias person has been requested to attend.
        /// </summary>
        /// <value>
        /// The requested to attend.
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
        /// Gets or sets the Rock.Model.Attendance.RSVP date time.
        /// </summary>
        /// <value>
        /// The RSVP date time.
        /// </value>
        public DateTime? RSVPDateTime { get; set; }

        /// <summary>
        /// Gets or sets if a schedule confirmation has been sent to the Rock.Model.Attendance.PersonAlias person
        /// </summary>
        /// <value>
        /// The schedule confirmation sent.
        /// </value>
        public bool? ScheduleConfirmationSent { get; set; }

        /// <summary>
        /// Gets or sets the person that scheduled the Rock.Model.Attendance.PersonAlias person to attend
        /// </summary>
        /// <value>
        /// The scheduled by person alias identifier.
        /// </value>
        public int? ScheduledByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets if the Rock.Model.Attendance.PersonAlias person is scheduled (confirmed) to attend.
        /// </summary>
        /// <value>
        /// The scheduled to attend.
        /// </value>
        public bool? ScheduledToAttend { get; set; }

        /// <summary>
        /// Gets or sets if a schedule reminder has been sent to the Rock.Model.Attendance.PersonAlias person
        /// </summary>
        /// <value>
        /// The schedule reminder sent.
        /// </value>
        public bool? ScheduleReminderSent { get; set; }

        /// <summary>
        /// Gets or sets the Id of the Rock.Model.Group (family) that was selected after searching.
        /// </summary>
        /// <value>
        /// A System.Int32 representing the Id of the Rock.Model.Group (family) that was selected.
        /// </value>
        public int? SearchResultGroupId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the Check-in Search Type Rock.Model.DefinedValue that was used to search for the person/family.
        /// </summary>
        /// <value>
        /// A System.Int32 representing the Id of the Check-in Search Type Rock.Model.DefinedValue that was used to search for the person/family.
        /// </value>
        public int? SearchTypeValueId { get; set; }

        /// <summary>
        /// Gets or sets the value that was entered when searching for family during check-in.
        /// </summary>
        /// <value>
        /// The search value entered.
        /// </value>
        public string SearchValue { get; set; }

        /// <summary>
        /// Gets or sets the date and time that person checked in
        /// </summary>
        /// <value>
        /// A System.DateTime representing the date and time that person checked in
        /// </value>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the created date time.
        /// </summary>
        /// <value>
        /// The created date time.
        /// </value>
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the modified date time.
        /// </summary>
        /// <value>
        /// The modified date time.
        /// </value>
        public DateTime? ModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the created by person alias identifier.
        /// </summary>
        /// <value>
        /// The created by person alias identifier.
        /// </value>
        public int? CreatedByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the modified by person alias identifier.
        /// </summary>
        /// <value>
        /// The modified by person alias identifier.
        /// </value>
        public int? ModifiedByPersonAliasId { get; set; }

    }
}