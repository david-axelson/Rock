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
using Rock.Observability;
using Rock.ViewModels.CheckIn;
using Rock.Web.Cache;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Provides logic for converting items from one type to another.
    /// </summary>
    internal class DefaultConversionProvider
    {
        #region Properties

        /// <summary>
        /// Gets or sets the check-in configuration in effect during filtering.
        /// </summary>
        /// <value>The check-in configuration.</value>
        protected CheckInConfigurationData Configuration => Coordinator.Configuration;

        /// <summary>
        /// Gets or sets the check-in coordinator.
        /// </summary>
        /// <value>The check-in coordinator.</value>
        protected DefaultCheckInCoordinator Coordinator { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultConversionProvider"/> class.
        /// </summary>
        /// <param name="coordinator">The check-in coordinator.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="coordinator"/> is <c>null</c>.</exception>
        public DefaultConversionProvider( DefaultCheckInCoordinator coordinator )
        {
            Coordinator = coordinator ?? throw new ArgumentNullException( nameof( coordinator ) );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Converts the family members into bags that represent the data
        /// required for check-in.
        /// </summary>
        /// <param name="familyGuid">The primary family unique identifier, this is used to resolve duplicates where a family member is also marked as can check-in.</param>
        /// <param name="groupMembers">The <see cref="GroupMember"/> objects to be converted to bags.</param>
        /// <returns>A collection of <see cref="FamilyMemberBag"/> objects.</returns>
        public List<FamilyMemberBag> GetFamilyMemberBags( Guid familyGuid, IEnumerable<GroupMember> groupMembers )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Family Member Bags" ) )
            {
                var familyMembers = new List<FamilyMemberBag>();

                // Get the group members along with the person record in memory.
                // Then sort by those that match the correct family first so that
                // any duplicates (non family members) can be skipped. This ensures
                // that a family member has precedence over the same person record
                // that is also flagged as "can check-in".
                //
                // Even though the logic between the two cases below is the same,
                // casting it to an IQueryable first will make sure the select
                // happens at the SQL level instead of in C# code.
                var members = groupMembers is IQueryable<GroupMember> groupMembersQry
                    ? groupMembersQry
                        .Select( gm => new
                        {
                            GroupGuid = gm.Person.PrimaryFamily != null ? gm.Person.PrimaryFamily.Guid : familyGuid,
                            RoleOrder = gm.GroupRole.Order,
                            gm.Person
                        } )
                        .ToList()
                        .OrderByDescending( gm => gm.GroupGuid == familyGuid )
                        .ThenBy( gm => gm.RoleOrder )
                    : groupMembers
                        .Select( gm => new
                        {
                            GroupGuid = gm.Person.PrimaryFamily != null ? gm.Person.PrimaryFamily.Guid : familyGuid,
                            RoleOrder = gm.GroupRole.Order,
                            gm.Person
                        } )
                        .ToList()
                        .OrderByDescending( gm => gm.GroupGuid == familyGuid )
                        .ThenBy( gm => gm.RoleOrder );

                foreach ( var member in members )
                {
                    // Skip any duplicates.
                    if ( familyMembers.Any( fm => fm.Guid == member.Person.Guid ) )
                    {
                        continue;
                    }

                    var familyMember = new FamilyMemberBag
                    {
                        Guid = member.Person.Guid,
                        IdKey = member.Person.IdKey,
                        FamilyGuid = member.GroupGuid,
                        FirstName = member.Person.FirstName,
                        NickName = member.Person.NickName,
                        LastName = member.Person.LastName,
                        FullName = member.Person.FullName,
                        PhotoUrl = member.Person.PhotoUrl,
                        BirthYear = member.Person.BirthYear,
                        BirthMonth = member.Person.BirthMonth,
                        BirthDay = member.Person.BirthDay,
                        BirthDate = member.Person.BirthYear.HasValue ? member.Person.BirthDate : null,
                        Age = member.Person.Age,
                        AgePrecise = member.Person.AgePrecise,
                        GradeOffset = member.Person.GradeOffset,
                        GradeFormatted = member.Person.GradeFormatted,
                        Gender = member.Person.Gender,
                        RoleOrder = member.RoleOrder
                    };

                    familyMembers.Add( familyMember );
                }

                return familyMembers;
            }
        }

        /// <summary>
        /// Gets the attendee item information for the family members. This also
        /// gathers all required information to later perform filtering on the
        /// attendees.
        /// </summary>
        /// <param name="familyMembers">The <see cref="FamilyMemberBag"/> to be used when constructing the <see cref="CheckInAttendeeItem"/> that willw rap it.</param>
        /// <param name="baseOptions">The <see cref="GroupMember"/> objects to be converted to bags.</param>
        /// <param name="recentAttendance">The recent attendance data for these family members.</param>
        /// <returns>A collection of <see cref="CheckInAttendeeItem"/> objects.</returns>
        public virtual List<CheckInAttendeeItem> GetAttendeeItems( IReadOnlyCollection<FamilyMemberBag> familyMembers, CheckInOptions baseOptions, IReadOnlyCollection<RecentAttendanceItem> recentAttendance )
        {
            return familyMembers
                .Select( fm =>
                {
                    var attendeeAttendances = recentAttendance
                        .Where( a => a.PersonGuid == fm.Guid )
                        .ToList();

                    return new CheckInAttendeeItem
                    {
                        Person = fm,
                        RecentAttendances = attendeeAttendances,
                        Options = baseOptions.Clone()
                    };
                } )
                .ToList();
        }

        /// <summary>
        /// Gets the attendance bag that represents all of the provided details.
        /// </summary>
        /// <param name="attendance">The source attendance record.</param>
        /// <param name="attendee">The attendee information for the attendance record.</param>
        /// <param name="area">The check-in area.</param>
        /// <param name="group">The check-in group.</param>
        /// <param name="location">The check-in location.</param>
        /// <param name="schedule">The check-in schedule.</param>
        /// <returns>A new instance of <see cref="AttendanceBag"/>.</returns>
        public virtual AttendanceBag GetAttendanceBag( RecentAttendanceItem attendance, CheckInAttendeeItem attendee, GroupTypeCache area, GroupCache group, NamedLocationCache location, NamedScheduleCache schedule )
        {
            var bag = new AttendanceBag
            {
                Guid = attendance.AttendanceGuid,
                PersonGuid = attendance.PersonGuid,
                NickName = attendee.Person.NickName,
                FirstName = attendee.Person.FirstName,
                LastName = attendee.Person.LastName,
                FullName = attendee.Person.FullName,
                Status = attendance.Status
            };

            if ( area != null )
            {
                bag.Area = new CheckInItemBag
                {
                    Guid = area.Guid,
                    Name = area.Name
                };
            }

            if ( group != null )
            {
                bag.Group = new CheckInItemBag
                {
                    Guid = group.Guid,
                    Name = group.Name
                };
            }

            if ( location != null )
            {
                bag.Location = new CheckInItemBag
                {
                    Guid = location.Guid,
                    Name = location.Name
                };
            }

            if ( schedule != null )
            {
                bag.Schedule = new CheckInItemBag
                {
                    Guid = schedule.Guid,
                    Name = schedule.Name
                };
            }

            return bag;
        }

        /// <summary>
        /// Gets the potential attendee bag from the attendee item.
        /// </summary>
        /// <param name="attendee">The attendee.</param>
        /// <returns>A new instance of <see cref="PotentialAttendeeBag"/>.</returns>
        public virtual PotentialAttendeeBag GetPotentialAttendeeBag( CheckInAttendeeItem attendee )
        {
            return new PotentialAttendeeBag
            {
                Person = attendee.Person,
                IsPreSelected = attendee.IsPreSelected,
                IsDisabled = attendee.IsDisabled,
                DisabledMessage = attendee.DisabledMessage,
                SelectedOptions = attendee.SelectedOptions
            };
        }

        #endregion
    }
}
