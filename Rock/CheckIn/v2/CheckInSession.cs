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
    /// The check-in session handles all logic related to the process of checking
    /// in one or more attendees.
    /// </summary>
    internal class CheckInSession
    {
        #region Properties

        /// <summary>
        /// Gets the check-in director managing this session.
        /// </summary>
        /// <value>The check-in director.</value>
        public CheckInDirector Director { get; }

        /// <summary>
        /// The context to use when accessing the database.
        /// </summary>
        /// <value>The database context.</value>
        public RockContext RockContext => Director.RockContext;

        /// <summary>
        /// Gets the check-in template configuration.
        /// </summary>
        /// <value>The check-in template configuration.</value>
        public TemplateConfigurationData TemplateConfiguration { get; }

        /// <summary>
        /// <para>
        /// Gets the attendees that have been loaded as part of this session.
        /// This is set after calling one of the LoadAttendees methods.
        /// </para>
        /// <para>
        /// This property does not persist between API calls since a new session
        /// object is created each time. So the list of attendees would not be
        /// available when, for example, saving attendance.
        /// </para>
        /// </summary>
        /// <value>The attendees.</value>
        public IReadOnlyList<Attendee> Attendees { get; private set; }

        /// <summary>
        /// Gets the conversion provider to be used with this instance.
        /// </summary>
        /// <value>The conversion provider.</value>
        protected virtual DefaultConversionProvider ConversionProvider { get; }

        /// <summary>
        /// Gets the opportunity filter provider to be used with this instance.
        /// </summary>
        /// <value>The opportunity filter provider.</value>
        protected virtual DefaultOpportunityFilterProvider OpportunityFilterProvider { get; }

        /// <summary>
        /// Gets the selection provider to be used with this instance.
        /// </summary>
        /// <value>The selection provider.</value>
        protected virtual DefaultSelectionProvider SelectionProvider { get; }

        /// <summary>
        /// Gets the search provider to be used with this instance.
        /// </summary>
        /// <value>The search provider.</value>
        protected virtual DefaultSearchProvider SearchProvider { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInSession"/> class.
        /// </summary>
        /// <param name="director">The director to get base information from.</param>
        /// <param name="templateConfiguration">The check-in template configuration.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="director"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="templateConfiguration"/> is <c>null</c>.</exception>
        public CheckInSession( CheckInDirector director, TemplateConfigurationData templateConfiguration )
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
            TemplateConfiguration = templateConfiguration;

            ConversionProvider = new DefaultConversionProvider( this );
            OpportunityFilterProvider = new DefaultOpportunityFilterProvider( this );
            SelectionProvider = new DefaultSelectionProvider( this );
            SearchProvider = new DefaultSearchProvider( this );
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Searches for families that match the criteria for the configuration
        /// template.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="searchType">Type of the search.</param>
        /// <returns>A collection of <see cref="FamilyBag"/> objects.</returns>
        public List<FamilyBag> SearchForFamilies( string searchTerm, FamilySearchMode searchType )
        {
            return SearchForFamilies( searchTerm, searchType, null );
        }

        /// <summary>
        /// Searches for families that match the criteria for the configuration
        /// template.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="searchType">Type of the search.</param>
        /// <param name="sortByCampus">If provided, then results will be sorted by families matching this campus first.</param>
        /// <returns>A collection of <see cref="FamilyBag"/> objects.</returns>
        public List<FamilyBag> SearchForFamilies( string searchTerm, FamilySearchMode searchType, CampusCache sortByCampus )
        {
            if ( searchTerm.IsNullOrWhiteSpace() )
            {
                throw new CheckInMessageException( "Search term must not be empty." );
            }

            using ( var activity = ObservabilityHelper.StartActivity( "Search for Families" ) )
            {
                activity?.AddTag( "rock.checkin.search_provider", SearchProvider.GetType().FullName );

                var familyQry = SearchProvider.GetFamilySearchQuery( searchTerm, searchType );
                var familyIdQry = SearchProvider.GetSortedFamilyIdSearchQuery( familyQry, sortByCampus );
                var familyMemberQry = SearchProvider.GetFamilyMemberSearchQuery( familyIdQry );

                return SearchProvider.GetFamilySearchItemBags( familyMemberQry );
            }
        }

        /// <summary>
        /// Loads the attendee information for the specified family. This will
        /// populate the <see cref="Attendees"/> property and perform all
        /// filtering and default selections.
        /// </summary>
        /// <param name="familyGuid">The family unique identifier to load.</param>
        /// <param name="possibleAreas">The possible areas that are to be considered when generating the opportunities.</param>
        /// <param name="kiosk">The optional kiosk to use.</param>
        /// <param name="locations">The list of locations to use.</param>
        public void LoadAndPrepareAttendeesForFamily( Guid familyGuid, IReadOnlyCollection<GroupTypeCache> possibleAreas, DeviceCache kiosk, IReadOnlyCollection<NamedLocationCache> locations )
        {
            var opportunities = Director.GetAllOpportunities( possibleAreas, kiosk, locations );
            var groupMemberQry = GetGroupMembersQueryForFamily( familyGuid );
            var people = GetPersonBags( familyGuid, groupMemberQry );

            LoadAttendees( people, opportunities );
            PrepareAttendees();
        }

        /// <summary>
        /// Loads the attendee information for the specified family. This will
        /// populate the <see cref="Attendees"/> property and perform all
        /// filtering and default selections.
        /// </summary>
        /// <param name="personGuid"></param>
        /// <param name="familyGuid">The family unique identifier to load.</param>
        /// <param name="possibleAreas">The possible areas that are to be considered when generating the opportunities.</param>
        /// <param name="kiosk">The optional kiosk to use.</param>
        /// <param name="locations">The list of locations to use.</param>
        public void LoadAndPrepareAttendeesForPerson( Guid personGuid, Guid? familyGuid, IReadOnlyCollection<GroupTypeCache> possibleAreas, DeviceCache kiosk, IReadOnlyCollection<NamedLocationCache> locations )
        {
            var checkInOpportunities = Director.GetAllOpportunities( possibleAreas, kiosk, locations );
            var familyMembersQry = GetGroupMemberQueryForPerson( personGuid, familyGuid );
            var people = GetPersonBags( Guid.Empty, familyMembersQry );

            LoadAttendees( people, checkInOpportunities );
            PrepareAttendees();
        }

        /// <summary>
        /// Find all group members that match the specified family unique
        /// identifier for check-in. This normally includes immediate family
        /// members as well as people associated to the family with one of
        /// the configured "can check-in" known relationships.
        /// </summary>
        /// <param name="familyGuid">The family unique identifier.</param>
        /// <returns>A queryable that can be used to load all the group members associated with the family.</returns>
        public IQueryable<GroupMember> GetGroupMembersQueryForFamily( Guid familyGuid )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Group Members Query For Family" ) )
            {
                activity?.AddTag( "rock.checkin.search_provider", SearchProvider.GetType().FullName );

                return SearchProvider.GetGroupMembersForFamilyQuery( familyGuid );
            }
        }

        /// <summary>
        /// Find the group member that matches the specified person unique
        /// identifier for check-in. If the family unique identifier is specified
        /// then it is used to sort the result so the GroupMember record
        /// associated with that family is the one used. If the family unique
        /// identifer is not specified or not found then the first family GroupMember
        /// record will be returned.
        /// </summary>
        /// <param name="personGuid">The person unique identifier.</param>
        /// <param name="familyGuid">The family unique identifier used to sort the records.</param>
        /// <returns>A queryable that can be used to load this person.</returns>
        public IQueryable<GroupMember> GetGroupMemberQueryForPerson( Guid personGuid, Guid? familyGuid )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Group Member Query For Person" ) )
            {
                activity?.AddTag( "rock.checkin.search_provider", SearchProvider.GetType().FullName );

                return SearchProvider.GetPersonForFamilyQuery( personGuid, familyGuid );
            }
        }

        /// <summary>
        /// Converts the group members into bags that represent the people
        /// for check-in.
        /// </summary>
        /// <param name="familyGuid">The primary family unique identifier, this is used to resolve duplicates where a family member is also marked as can check-in.</param>
        /// <param name="groupMembers">The <see cref="GroupMember"/> objects to be converted to bags.</param>
        /// <returns>A collection of <see cref="PersonBag"/> objects.</returns>
        public List<PersonBag> GetPersonBags( Guid familyGuid, IEnumerable<GroupMember> groupMembers )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Person Bags" ) )
            {
                activity?.AddTag( "rock.checkin.conversion_provider", ConversionProvider.GetType().FullName );

                return ConversionProvider.GetFamilyMemberBags( familyGuid, groupMembers );
            }
        }

        /// <summary>
        /// Filters the check-in opportunities for a single person.
        /// </summary>
        /// <param name="person">The person whose opportunities will be filtered.</param>
        public void FilterPersonOpportunities( Attendee person )
        {
            using ( var activity = ObservabilityHelper.StartActivity( $"Filter Opportunities For {person.Person.NickName}" ) )
            {
                activity?.AddTag( "rock.checkin.opportunity_filter_provider", OpportunityFilterProvider.GetType().FullName );

                OpportunityFilterProvider.FilterPersonOpportunities( person );
                OpportunityFilterProvider.RemoveEmptyOpportunities( person );
            }
        }

        /// <summary>
        /// Loads the attendee information for the family members. This also
        /// gathers all required information to later perform filtering on the
        /// attendees.
        /// </summary>
        /// <param name="people">The <see cref="PersonBag"/> objects to be used when constructing the <see cref="Attendee"/> objects that will wrap them.</param>
        /// <param name="baseOpportunities">The opportunity collection to clone onto each attendee.</param>
        public void LoadAttendees( IReadOnlyCollection<PersonBag> people, OpportunityCollection baseOpportunities )
        {
            using ( var activity = ObservabilityHelper.StartActivity( $"Get Attendee Items" ) )
            {
                activity?.AddTag( "rock.checkin.conversion_provider", ConversionProvider.GetType().FullName );

                var preSelectCutoff = RockDateTime.Today.AddDays( Math.Min( -1, 0 - TemplateConfiguration.AutoSelectDaysBack ) );
                var recentAttendance = GetRecentAttendance( preSelectCutoff, people.Select( fm => fm.Guid ) );

                var attendees = ConversionProvider.GetAttendeeItems( people, baseOpportunities, recentAttendance );

                Attendees = attendees;
            }
        }

        /// <summary>
        /// Prepares all of the <see cref="Attendees"/> by filtering and
        /// applying all default selections.
        /// </summary>
        public void PrepareAttendees()
        {
            foreach ( var attendee in Attendees )
            {
                FilterPersonOpportunities( attendee );
                SetDefaultSelectionsForAttendee( attendee );
            }
        }

        /// <summary>
        /// Sets the default selections for the specified attendee. This will
        /// mark a person as pre-selected if they have recent attendance and
        /// it will also set the current selections if the check-in template
        /// is configured that way.
        /// </summary>
        /// <param name="attendee">The attendee to be checked in.</param>
        public void SetDefaultSelectionsForAttendee( Attendee attendee )
        {
            using ( var activity = ObservabilityHelper.StartActivity( $"Set Defaults for {attendee.Person.NickName}" ) )
            {
                activity?.AddTag( "rock.checkin.selection_provider", SelectionProvider.GetType().FullName );

                if ( TemplateConfiguration.AutoSelect == AutoSelectMode.PeopleAndAreaGroupLocation )
                {
                    attendee.SelectedOpportunities = SelectionProvider.GetDefaultSelectionsForPerson( attendee );
                }

                attendee.IsPreSelected = TemplateConfiguration.AutoSelectDaysBack > 0 && attendee.RecentAttendances.Count > 0;
            }
        }

        /// <summary>
        /// Gets the current attendance bags for the attendees. This means all
        /// the bags that represent attendance records for people that are
        /// considered to be currently checked in. This assumes the
        /// <see cref="Attendee.RecentAttendances"/> property has
        /// been populated for each attendee.
        /// </summary>
        /// <returns>A list of attendance bags.</returns>
        public List<AttendanceBag> GetCurrentAttendanceBags()
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Current Attendance Bags" ) )
            {
                activity?.AddTag( "rock.checkin.conversion_provider", ConversionProvider.GetType().FullName );

                var checkedInAttendances = new List<AttendanceBag>();
                var today = RockDateTime.Today;

                foreach ( var attendee in Attendees )
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
        }

        /// <summary>
        /// Gets the attendee bags from the <see cref="Attendees"/> loaded in
        /// this session.
        /// </summary>
        /// <returns>A list of bags that represent the attendees.</returns>
        public List<AttendeeBag> GetAttendeeBags()
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Attendee Bags" ) )
            {
                activity?.AddTag( "rock.checkin.conversion_provider", ConversionProvider.GetType().FullName );

                return Attendees
                    .Select( a => ConversionProvider.GetAttendeeBag( a ) )
                    .ToList();
            }
        }

        /// <summary>
        /// Gets the potential attendee bags from the set of attendee items.
        /// </summary>
        /// <param name="opportunityCollection">The opportunity collection to be converted to a bag.</param>
        /// <returns>A list of bags that represent the attendees.</returns>
        public OpportunityCollectionBag GetOpportunityCollectionBag( OpportunityCollection opportunityCollection )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Opportunity Collection Bag" ) )
            {
                activity?.AddTag( "rock.checkin.conversion_provider", ConversionProvider.GetType().FullName );

                return ConversionProvider.GetOpportunityCollectionBag( opportunityCollection );
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the recent attendance for a set of people.
        /// </summary>
        /// <param name="cutoffDateTime">Attendance records must start on or after this date and time.</param>
        /// <param name="personGuids">The person unique identifiers to query the database for.</param>
        /// <returns>A collection of <see cref="RecentAttendance"/> records.</returns>
        private List<RecentAttendance> GetRecentAttendance( DateTime cutoffDateTime, IEnumerable<Guid> personGuids )
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
                .Select( a => new RecentAttendance
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
