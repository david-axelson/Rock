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
using System.Linq.Expressions;

using Rock.Data;
using Rock.Enums.CheckIn;
using Rock.Model;
using Rock.Observability;
using Rock.Utility;
using Rock.ViewModels.CheckIn;
using Rock.Web.Cache;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Primary entry point to the check-in system. This provides a single
    /// place to interface with check-in so that all logic is centralized
    /// and not duplicated.
    /// </summary>
    internal class CheckInDirector
    {
        #region Properties

        /// <summary>
        /// The context to use when accessing the database.
        /// </summary>
        /// <value>The database context.</value>
        public RockContext RockContext { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInDirector"/> class.
        /// </summary>
        /// <param name="rockContext">The rock context to use when accessing the database.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="rockContext"/> is <c>null</c>.</exception>
        public CheckInDirector( RockContext rockContext )
        {
            if ( rockContext == null )
            {
                throw new ArgumentNullException( nameof( rockContext ) );
            }

            RockContext = rockContext;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the configuration summary bags for all valid check-in configurations.
        /// </summary>
        /// <returns>A colleciton of <see cref="ConfigurationItemSummaryBag"/> objects.</returns>
        public List<ConfigurationItemSummaryBag> GetConfigurationSummaries()
        {
            return GetConfigurationTemplates()
                .OrderBy( t => t.Name )
                .Select( t => new ConfigurationItemSummaryBag
                {
                    Guid = t.Guid,
                    Name = t.Name,
                    IconCssClass = t.IconCssClass
                } )
                .ToList();
        }

        /// <summary>
        /// Gets the check in area summary bags for all valid check-in areas. If
        /// a <paramref name="kiosk"/> or <paramref name="checkinConfiguration"/> are
        /// provided then they will be used to filter the results to only areas
        /// valid for those items.
        /// </summary>
        /// <param name="kiosk">The optional kiosk to filter the results for.</param>
        /// <param name="checkinConfiguration">The optional configuration to filter the results for.</param>
        /// <returns>A collection of <see cref="AreaItemSummaryBag"/> objects.</returns>
        public List<AreaItemSummaryBag> GetCheckInAreaSummaries( DeviceCache kiosk, GroupTypeCache checkinConfiguration )
        {
            var areas = new Dictionary<Guid, AreaItemSummaryBag>();
            List<GroupTypeCache> configurations;
            HashSet<int> kioskGroupTypeIds = null;

            // If the caller specified a configuration, then we return areas for
            // only that primary configuration. Otherwise we include areas from
            // all configurations.
            if ( checkinConfiguration != null )
            {
                configurations = new List<GroupTypeCache> { checkinConfiguration };
            }
            else
            {
                configurations = GetConfigurationTemplates().ToList();
            }

            if ( kiosk != null )
            {
                kioskGroupTypeIds = new HashSet<int>( GetKioskAreas( kiosk ).Select( gt => gt.Id ) );
            }

            // Go through each configuration and get all areas that belong to
            // it. Then either add them to the list of areas or update the
            // primary configuration guids of the existing area.
            foreach ( var cfg in configurations )
            {
                foreach ( var areaGroupType in cfg.GetDescendentGroupTypes() )
                {
                    // Only include group types that actually take attendance.
                    if ( !areaGroupType.TakesAttendance )
                    {
                        continue;
                    }

                    // If a kiosk was specified, limit the results to areas
                    // that are valid for the kiosk.
                    if ( kioskGroupTypeIds != null && !kioskGroupTypeIds.Contains( areaGroupType.Id ) )
                    {
                        continue;
                    }

                    if ( areas.TryGetValue( areaGroupType.Guid, out var area ) )
                    {
                        area.PrimaryConfigurationGuids.Add( cfg.Guid );
                    }
                    else
                    {
                        areas.Add( areaGroupType.Guid, new AreaItemSummaryBag
                        {
                            Guid = areaGroupType.Guid,
                            Name = areaGroupType.Name,
                            PrimaryConfigurationGuids = new List<Guid> { cfg.Guid }
                        } );
                    }
                }
            }

            return new List<AreaItemSummaryBag>( areas.Values );
        }

        /// <summary>
        /// Searches for families that match the criteria for the configuration.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="searchType">Type of the search.</param>
        /// <param name="checkinConfiguration">The checkin configuration.</param>
        /// <returns>A collection of <see cref="FamilySearchItemBag"/> objects.</returns>
        public List<FamilySearchItemBag> SearchForFamilies( string searchTerm, FamilySearchMode searchType, GroupTypeCache checkinConfiguration )
        {
            return SearchForFamilies( searchTerm, searchType, checkinConfiguration, null );
        }

        /// <summary>
        /// Searches for families that match the criteria for the configuration.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="searchType">Type of the search.</param>
        /// <param name="checkinConfiguration">The check-in configuration.</param>
        /// <param name="sortByCampus">If provided, then results will be sorted by families matching this campus first.</param>
        /// <returns>A collection of <see cref="FamilySearchItemBag"/> objects.</returns>
        public List<FamilySearchItemBag> SearchForFamilies( string searchTerm, FamilySearchMode searchType, GroupTypeCache checkinConfiguration, CampusCache sortByCampus )
        {
            var configuration = checkinConfiguration?.GetCheckInConfiguration( RockContext );

            if ( searchTerm.IsNullOrWhiteSpace() )
            {
                throw new CheckInMessageException( "Search term must not be empty." );
            }

            if ( configuration == null )
            {
                throw new ArgumentOutOfRangeException( nameof( checkinConfiguration ), "Check-in configuration data is not valid." );
            }

            var searchProvider = CreateSearchProvider( configuration );
            var familyQry = searchProvider.GetFamilySearchQuery( searchTerm, searchType );
            var familyIdQry = searchProvider.GetSortedFamilyIdSearchQuery( familyQry, sortByCampus );
            var familyMemberQry = searchProvider.GetFamilyMemberSearchQuery( familyIdQry );

            return searchProvider.GetFamilySearchItemBags( familyMemberQry );
        }

        /// <summary>
        /// Find all family members that match the specified family unique
        /// identifier for check-in. This normally includes immediate family
        /// members as well as people associated to the family with one of
        /// the configured "can check-in" known relationships.
        /// </summary>
        /// <param name="familyGuid">The family unique identifier.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A queryable that can be used to load all the group members associated with the family.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Check-in configuration data is not valid.</exception>
        public IQueryable<GroupMember> GetFamilyMembersForFamilyQuery( Guid familyGuid, CheckInConfigurationData configuration )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Family Members Query" ) )
            {
                var searchProvider = CreateSearchProvider( configuration );

                return searchProvider.GetFamilyMembersForFamilyQuery( familyGuid );
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
        /// Filters the check-in options for a single person.
        /// </summary>
        /// <param name="person">The person to use when filtering options.</param>
        /// <param name="configuration">The check-in configuration data.</param>
        public virtual void FilterPersonOptions( CheckInAttendeeItem person, CheckInConfigurationData configuration )
        {
            var filter = CreateOptionsFilterProvider( configuration );

            filter.FilterPersonOptions( person );
            filter.RemoveEmptyOptions( person );
        }

        /// <summary>
        /// Gets the attendee item information for the family members. This also
        /// gathers all required information to later perform filtering on the
        /// attendees.
        /// </summary>
        /// <param name="familyMembers">The <see cref="FamilyMemberBag"/> to be used when constructing the <see cref="CheckInAttendeeItem"/> that willw rap it.</param>
        /// <param name="baseOptions">The <see cref="GroupMember"/> objects to be converted to bags.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A collection of <see cref="CheckInAttendeeItem"/> objects.</returns>
        public List<CheckInAttendeeItem> GetAttendeeItems( IReadOnlyCollection<FamilyMemberBag> familyMembers, CheckInOptions baseOptions, CheckInConfigurationData configuration )
        {
            var preSelectCutoff = RockDateTime.Today.AddDays( Math.Min( -1, 0 - configuration.AutoSelectDaysBack ) );
            var recentAttendance = GetRecentAttendance( preSelectCutoff, familyMembers.Select( fm => fm.Guid ) );

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
        /// <para>
        /// Gets all the check-in options that are possible for the kiosk or
        /// locations. 
        /// </para>
        /// <para>
        /// If you provide an array of locations they will be used, otherwise
        /// the locations of the kiosk will be used. If you provide a kiosk
        /// then it will be used to determine the current timestamp when
        /// checking if locations are open or not.
        /// </para>
        /// </summary>
        /// <param name="possibleAreas">The possible areas that are to be considered when generating the options.</param>
        /// <param name="kiosk">The optional kiosk to use.</param>
        /// <param name="locations">The list of locations to use.</param>
        /// <returns>An instance of <see cref="CheckInOptions"/> that describes the available options.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="possibleAreas"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException">kiosk - Kiosk must be specified unless locations are specified.</exception>
        public CheckInOptions GetAllCheckInOptions( IReadOnlyCollection<GroupTypeCache> possibleAreas, DeviceCache kiosk, IReadOnlyCollection<NamedLocationCache> locations )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get All Options" ) )
            {
                if ( kiosk == null && locations == null )
                {
                    throw new ArgumentNullException( nameof( kiosk ), "Kiosk must be specified unless locations are specified." );
                }

                if ( possibleAreas == null )
                {
                    throw new ArgumentNullException( nameof( possibleAreas ) );
                }

                return CheckInOptions.Create( possibleAreas, kiosk, locations, RockContext );
            }
        }

        /// <summary>
        /// Sets the default selections for the specified attendee. This will
        /// mark a person as pre-selected if they have recent attendance and
        /// it will also set the current selections if the check-in template
        /// is configured that way.
        /// </summary>
        /// <param name="attendee">The attendee to be checked in.</param>
        /// <param name="configuration">The check-in configuration.</param>
        public void SetDefaultSelectionsForAttendee( CheckInAttendeeItem attendee, CheckInConfigurationData configuration )
        {
            using ( var activity = ObservabilityHelper.StartActivity( $"Set Defaults for {attendee.Person.NickName}" ) )
            {
                if ( configuration.AutoSelect == AutoSelectMode.PeopleAndAreaGroupLocation )
                {
                    var optionsSelector = CreateOptionsSelectionProvider( configuration );

                    attendee.SelectedOptions = optionsSelector.GetDefaultSelectionForPerson( attendee );
                }

                attendee.IsPreSelected = configuration.AutoSelectDaysBack > 0 && attendee.RecentAttendances.Count > 0;
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
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A list of attendance bags.</returns>
        public List<AttendanceBag> GetCurrentAttendanceBags( IReadOnlyCollection<CheckInAttendeeItem> attendees, CheckInConfigurationData configuration )
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
                    var location = NamedLocationCache.Get( attendance.LocationGuid, RockContext );
                    var schedule = NamedScheduleCache.Get( attendance.ScheduleGuid, RockContext );
                    var group = GroupCache.Get( attendance.GroupGuid, RockContext );

                    if ( location == null || schedule == null || group == null )
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

                    checkedInAttendances.Add( new AttendanceBag
                    {
                        Guid = attendance.AttendanceGuid,
                        PersonGuid = attendance.PersonGuid,
                        NickName = attendee.Person.NickName,
                        FirstName = attendee.Person.FirstName,
                        LastName = attendee.Person.LastName,
                        FullName = attendee.Person.FullName,
                        Status = attendance.Status,
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
                    } );
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
            // TODO: This per-item guts should be an extension method.
            return attendees
                .Select( a => new PotentialAttendeeBag
                {
                    Person = a.Person,
                    IsPreSelected = a.IsPreSelected,
                    IsDisabled = a.IsDisabled,
                    DisabledMessage = a.DisabledMessage,
                    SelectedOptions = a.SelectedOptions
                } )
                .ToList();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates the object that will handle search logic.
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>An instance of <see cref="DefaultSearchProvider"/>.</returns>
        protected virtual DefaultSearchProvider CreateSearchProvider( CheckInConfigurationData configuration )
        {
            return new DefaultSearchProvider( RockContext, configuration );
        }

        /// <summary>
        /// Creates the object that will handle person options logic.
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>An instance of <see cref="DefaultOptionsFilterProvider"/>.</returns>
        protected virtual DefaultOptionsFilterProvider CreateOptionsFilterProvider( CheckInConfigurationData configuration )
        {
            return new DefaultOptionsFilterProvider( configuration, this );
        }

        /// <summary>
        /// Creates the object that will handle making default selections for
        /// people. This is used when check-in is configured for full auto
        /// mode (AutoBack mode is set to select group/location/schedule).
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>An instance of <see cref="DefaultOptionsSelectionProvider"/>.</returns>
        protected virtual DefaultOptionsSelectionProvider CreateOptionsSelectionProvider( CheckInConfigurationData configuration )
        {
            return new DefaultOptionsSelectionProvider();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the configuration group types that are defined in the system.
        /// </summary>
        /// <returns>An enumeration of <see cref="GroupTypeCache"/> objects.</returns>
        /// <exception cref="Exception">Check-in Template Purpose was not found in the database, please check your installation.</exception>
        internal IEnumerable<GroupTypeCache> GetConfigurationTemplates()
        {
            var checkinTemplateTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid(), RockContext )?.Id;

            if ( !checkinTemplateTypeId.HasValue )
            {
                throw new Exception( "Check-in Template Purpose was not found in the database, please check your installation." );
            }

            return GroupTypeCache.All( RockContext )
                .Where( t => t.GroupTypePurposeValueId.HasValue && t.GroupTypePurposeValueId == checkinTemplateTypeId.Value );
        }

        /// <summary>
        /// Gets the group type areas that are valid for the kiosk device. Only group
        /// types associated via group and location to the kiosk will be returned.
        /// </summary>
        /// <param name="kiosk">The kiosk device.</param>
        /// <returns>An enumeration of <see cref="GroupTypeCache" /> objects.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="kiosk"/> is <c>null</c>.</exception>
        internal IEnumerable<GroupTypeCache> GetKioskAreas( DeviceCache kiosk )
        {
            if ( kiosk == null )
            {
                throw new ArgumentNullException( nameof( kiosk ) );
            }

            // Get all locations for the device.
            var locationIds = new HashSet<int>( kiosk.GetAllLocationIds() );

            // Get all the group locations associated with those locations.
            var groupLocations = locationIds
                .SelectMany( id => GroupLocationCache.AllForLocationId( id, RockContext ) )
                .DistinctBy( glc => glc.Id )
                .ToList();

            // Get the distinct group types for those group locations that have
            // attendance enabled.
            return groupLocations
                .Select( gl => GroupCache.Get( gl.GroupId, RockContext )?.GroupTypeId )
                .Where( id => id.HasValue )
                .Distinct()
                .Select( id => GroupTypeCache.Get( id.Value, RockContext ) )
                .Where( gt => gt != null && gt.TakesAttendance )
                .ToList();
        }

        /// <summary>
        /// <para>
        /// Adds a where clause that can replicates a Contains() call on the
        /// values. If your LINQ statement has a real Contains() call then it
        /// will not be cached by EF - meaning EF will generate the SQL each
        /// time instead of using a cached SQL statement. This is very costly at
        /// about 15-20ms or more each time this happens.
        /// </para>
        /// <para>
        /// This method will do the same but generate individual x == 1 OR x == 2
        /// statements - which do get translated to an IN statement in SQL.
        /// </para>
        /// <para>
        /// Because the EF cache will be no good if any of the values in the
        /// clause change, this method is only helpful if <paramref name="values"/>
        /// is fairly consistent. If it is going to change with nearly every
        /// query then this does not provide any performance improvement.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of queryable.</typeparam>
        /// <typeparam name="V">The type of the value to be checked.</typeparam>
        /// <param name="source">The source queryable.</param>
        /// <param name="values">The values that <paramref name="expression"/> must match one of.</param>
        /// <param name="expression">The expression to the property.</param>
        /// <returns>A new queryable with the updated where clause.</returns>
        internal static IQueryable<T> WhereContains<T, V>( IQueryable<T> source, IEnumerable<V> values, Expression<Func<T, V>> expression )
        {
            Expression<Func<T, bool>> predicate = null;
            var parameter = expression.Parameters[0];

            foreach ( var value in values )
            {
                var equalExpr = Expression.Equal( expression.Body, Expression.Constant( value ) );
                var lambdaExpr = Expression.Lambda<Func<T, bool>>( equalExpr, parameter );

                predicate = predicate != null
                    ? predicate.Or( lambdaExpr )
                    : lambdaExpr;
            }

            if ( predicate != null )
            {
                return source.Where( predicate );
            }
            else
            {
                return source.Where( a => false );
            }
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
            personAttendanceQuery = WhereContains( personAttendanceQuery, personGuids, aa => aa.PersonAlias.Person.Guid );

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
