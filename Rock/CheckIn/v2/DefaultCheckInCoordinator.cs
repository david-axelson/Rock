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
using System.Data.Entity;
using System.Linq;

using Rock.Data;
using Rock.Enums.CheckIn;
using Rock.Model;
using Rock.Observability;
using Rock.ViewModels.CheckIn;
using Rock.Web.Cache;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// The check-in coordinator handles all logic related to a check-in session.
    /// </summary>
    internal class DefaultCheckInCoordinator
    {
        #region Properties

        /// <summary>
        /// Gets the check-in director managing this coordinator.
        /// </summary>
        /// <value>The check-in director.</value>
        public CheckInDirector Director { get; }

        /// <summary>
        /// The context to use when accessing the database.
        /// </summary>
        /// <value>The database context.</value>
        public RockContext RockContext => Director.RockContext;

        /// <summary>
        /// Gets the check-in configuration.
        /// </summary>
        /// <value>The check-in configuration.</value>
        public CheckInConfigurationData Configuration { get; }

        /// <summary>
        /// Gets the conversion provider to be used with this instance.
        /// </summary>
        /// <value>The conversion provider.</value>
        protected virtual DefaultConversionProvider ConversionProvider { get; }

        /// <summary>
        /// Gets the options filter provider to be used with this instance.
        /// </summary>
        /// <value>The options filter provider.</value>
        protected virtual DefaultOptionsFilterProvider OptionsFilterProvider { get; }

        /// <summary>
        /// Gets the options selection provider to be used with this instance.
        /// </summary>
        /// <value>The options selection provider.</value>
        protected virtual DefaultOptionsSelectionProvider OptionsSelectionProvider { get; }

        /// <summary>
        /// Gets the search provider to be used with this instance.
        /// </summary>
        /// <value>The search provider.</value>
        protected virtual DefaultSearchProvider SearchProvider { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultCheckInCoordinator"/> class.
        /// </summary>
        /// <param name="director">The director to get base information from.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="director"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="configuration"/> is <c>null</c>.</exception>
        public DefaultCheckInCoordinator( CheckInDirector director, CheckInConfigurationData configuration )
        {
            if ( director == null )
            {
                throw new ArgumentNullException( nameof( director ) );
            }

            if ( director == null )
            {
                throw new ArgumentNullException( nameof( director ) );
            }

            Director = director;
            Configuration = configuration;

            ConversionProvider = new DefaultConversionProvider( this );
            OptionsFilterProvider = new DefaultOptionsFilterProvider( this );
            OptionsSelectionProvider = new DefaultOptionsSelectionProvider( this );
            SearchProvider = new DefaultSearchProvider( this );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Searches for families that match the criteria for the configuration.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="searchType">Type of the search.</param>
        /// <returns>A collection of <see cref="FamilySearchItemBag"/> objects.</returns>
        public List<FamilySearchItemBag> SearchForFamilies( string searchTerm, FamilySearchMode searchType )
        {
            return SearchForFamilies( searchTerm, searchType, null );
        }

        /// <summary>
        /// Searches for families that match the criteria for the configuration.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="searchType">Type of the search.</param>
        /// <param name="sortByCampus">If provided, then results will be sorted by families matching this campus first.</param>
        /// <returns>A collection of <see cref="FamilySearchItemBag"/> objects.</returns>
        public List<FamilySearchItemBag> SearchForFamilies( string searchTerm, FamilySearchMode searchType, CampusCache sortByCampus )
        {
            if ( searchTerm.IsNullOrWhiteSpace() )
            {
                throw new CheckInMessageException( "Search term must not be empty." );
            }

            var familyQry = SearchProvider.GetFamilySearchQuery( searchTerm, searchType );
            var familyIdQry = SearchProvider.GetSortedFamilyIdSearchQuery( familyQry, sortByCampus );
            var familyMemberQry = SearchProvider.GetFamilyMemberSearchQuery( familyIdQry );

            return SearchProvider.GetFamilySearchItemBags( familyMemberQry );
        }

        /// <summary>
        /// Find all family members that match the specified family unique
        /// identifier for check-in. This normally includes immediate family
        /// members as well as people associated to the family with one of
        /// the configured "can check-in" known relationships.
        /// </summary>
        /// <param name="familyGuid">The family unique identifier.</param>
        /// <returns>A queryable that can be used to load all the group members associated with the family.</returns>
        public IQueryable<GroupMember> GetFamilyMembersForFamilyQuery( Guid familyGuid )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Family Members Query" ) )
            {
                return SearchProvider.GetFamilyMembersForFamilyQuery( familyGuid );
            }
        }

        /// <summary>
        /// Find the family member that matches the specified person unique
        /// identifier for check-in. If the family unique identifier is specified
        /// then it is used to sort the result so the GroupMember record
        /// associated with that family is the one used. If the family unique
        /// identifer is not specified or not found then the first family GroupMember
        /// record will be returned.
        /// </summary>
        /// <param name="personGuid">The person unique identifier.</param>
        /// <param name="familyGuid">The family unique identifier.</param>
        /// <returns>A queryable that can be used to load this person from the family.</returns>
        public IQueryable<GroupMember> GetPersonForFamilyQuery( Guid personGuid, Guid? familyGuid )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Person Query" ) )
            {
                return SearchProvider.GetPersonForFamilyQuery( personGuid, familyGuid );
            }
        }

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
                return ConversionProvider.GetFamilyMemberBags( familyGuid, groupMembers );
            }
        }

        /// <summary>
        /// Filters the check-in options for a single person.
        /// </summary>
        /// <param name="person">The person to use when filtering options.</param>
        public void FilterPersonOptions( CheckInAttendeeItem person )
        {
            OptionsFilterProvider.FilterPersonOptions( person );
            OptionsFilterProvider.RemoveEmptyOptions( person );
        }

        /// <summary>
        /// Gets the attendee item information for the family members. This also
        /// gathers all required information to later perform filtering on the
        /// attendees.
        /// </summary>
        /// <param name="familyMembers">The <see cref="FamilyMemberBag"/> to be used when constructing the <see cref="CheckInAttendeeItem"/> that willw rap it.</param>
        /// <param name="baseOptions">The <see cref="GroupMember"/> objects to be converted to bags.</param>
        /// <returns>A collection of <see cref="CheckInAttendeeItem"/> objects.</returns>
        public List<CheckInAttendeeItem> GetAttendeeItems( IReadOnlyCollection<FamilyMemberBag> familyMembers, CheckInOpportunities baseOptions )
        {
            var preSelectCutoff = RockDateTime.Today.AddDays( Math.Min( -1, 0 - Configuration.AutoSelectDaysBack ) );
            var recentAttendance = GetRecentAttendance( preSelectCutoff, familyMembers.Select( fm => fm.Guid ) );

            return ConversionProvider.GetAttendeeItems( familyMembers, baseOptions, recentAttendance );
        }

        /// <summary>
        /// Sets the default selections for the specified attendee. This will
        /// mark a person as pre-selected if they have recent attendance and
        /// it will also set the current selections if the check-in template
        /// is configured that way.
        /// </summary>
        /// <param name="attendee">The attendee to be checked in.</param>
        public void SetDefaultSelectionsForAttendee( CheckInAttendeeItem attendee )
        {
            using ( var activity = ObservabilityHelper.StartActivity( $"Set Defaults for {attendee.Person.NickName}" ) )
            {
                if ( Configuration.AutoSelect == AutoSelectMode.PeopleAndAreaGroupLocation )
                {
                    attendee.SelectedOptions = OptionsSelectionProvider.GetDefaultSelectionsForPerson( attendee );
                }

                attendee.IsPreSelected = Configuration.AutoSelectDaysBack > 0 && attendee.RecentAttendances.Count > 0;
            }
        }

        /// <summary>
        /// Gets the current attendance bags for the attendees. This means all
        /// the bags that represent attendance records for people that are
        /// considered to be currently checked in. This assumes the
        /// <see cref="CheckInAttendeeItem.RecentAttendances"/> property has
        /// been populated for each attendee.
        /// </summary>
        /// <param name="attendees">The attendees.</param>
        /// <returns>A list of attendance bags.</returns>
        public List<AttendanceBag> GetCurrentAttendanceBags( IReadOnlyCollection<CheckInAttendeeItem> attendees )
        {
            var checkedInAttendances = new List<AttendanceBag>();
            var today = RockDateTime.Today;

            foreach ( var attendee in attendees )
            {
                var activeAttendances = attendee.RecentAttendances
                    .Where( a => a.StartDateTime >= today
                        && !a.EndDateTime.HasValue )
                    .ToList();

                // We could get fancy and group things to try to improve
                // performance a tiny bit, but it would be extremely unsual
                // for a person to be checked into more than one thing so we
                // will just do a simple loop.
                foreach ( var attendance in activeAttendances )
                {
                    var area = GroupTypeCache.Get( attendance.GroupTypeGuid, RockContext );
                    var group = GroupCache.Get( attendance.GroupGuid, RockContext );
                    var location = NamedLocationCache.Get( attendance.LocationGuid, RockContext );
                    var schedule = NamedScheduleCache.Get( attendance.ScheduleGuid, RockContext );

                    if ( area == null || group == null || location == null || schedule == null )
                    {
                        continue;
                    }

                    var campusId = location.GetCampusIdForLocation();
                    var now = campusId.HasValue
                        ? CampusCache.Get( campusId.Value )?.CurrentDateTime ?? RockDateTime.Now
                        : RockDateTime.Now;

                    if ( !schedule.WasScheduleOrCheckInActiveForCheckOut( now ) )
                    {
                        continue;
                    }

                    checkedInAttendances.Add( ConversionProvider.GetAttendanceBag(
                        attendance,
                        attendee,
                        area,
                        group,
                        location,
                        schedule ) );
                }
            }

            return checkedInAttendances;
        }

        /// <summary>
        /// Gets the potential attendee bags from the set of attendee items.
        /// </summary>
        /// <param name="attendees">The attendees to be converted to bags.</param>
        /// <returns>A list of bags that represent the attendees.</returns>
        public List<PotentialAttendeeBag> GetPotentialAttendeeBags( IEnumerable<CheckInAttendeeItem> attendees )
        {
            return attendees
                .Select( a => ConversionProvider.GetPotentialAttendeeBag( a ) )
                .ToList();
        }

        /// <summary>
        /// Gets the potential attendee bags from the set of attendee items.
        /// </summary>
        /// <param name="opportunityCollection">The opportunity collection to be converted to a bag.</param>
        /// <returns>A list of bags that represent the attendees.</returns>
        public OpportunityCollectionBag GetOpportunityCollectionBag( CheckInOpportunities opportunityCollection )
        {
            return ConversionProvider.GetOpportunityCollectionBag( opportunityCollection );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the recent attendance for a set of people.
        /// </summary>
        /// <param name="cutoffDateTime">Attendance records must start on or after this date and time.</param>
        /// <param name="personGuids">The person unique identifiers to query the database for.</param>
        /// <returns>A collection of <see cref="RecentAttendanceItem"/> records.</returns>
        private List<RecentAttendanceItem> GetRecentAttendance( DateTime cutoffDateTime, IEnumerable<Guid> personGuids )
        {
            var attendanceService = new AttendanceService( RockContext );

            var personAttendanceQuery = attendanceService
                .Queryable().AsNoTracking()
                .Where( a => a.PersonAlias != null
                    && a.Occurrence.Group != null
                    && a.Occurrence.Schedule != null
                    && a.StartDateTime >= cutoffDateTime
                    && a.DidAttend.HasValue
                    && a.DidAttend.Value == true );

            // TODO: This should probably be changed to a raw SQL query for performance.
            // Because the list of personGuids will be changing constantly it
            // will still not be cached by EF.
            personAttendanceQuery = CheckInDirector.WhereContains( personAttendanceQuery, personGuids, aa => aa.PersonAlias.Person.Guid );

            return personAttendanceQuery
                .Select( a => new RecentAttendanceItem
                {
                    AttendanceId = a.Id,
                    AttendanceGuid = a.Guid,
                    Status = a.CheckInStatus,
                    StartDateTime = a.StartDateTime,
                    EndDateTime = a.EndDateTime,
                    PersonGuid = a.PersonAlias.Person.Guid,
                    GroupTypeGuid = a.Occurrence.Group.GroupType.Guid,
                    GroupGuid = a.Occurrence.Group.Guid,
                    LocationGuid = a.Occurrence.Location.Guid,
                    ScheduleGuid = a.Occurrence.Schedule.Guid
                } )
                .ToList();
        }

        #endregion
    }
}
