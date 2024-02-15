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

using Rock.CheckIn.v2.Filters;
using Rock.Data;
using Rock.Enums.CheckIn;
using Rock.Model;
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
        #region Fields

        /// <summary>
        /// The context to use when accessing the database.
        /// </summary>
        private readonly RockContext _rockContext;

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

            _rockContext = rockContext;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the configuration summary bags for all valid check-in configurations.
        /// </summary>
        /// <returns>A colleciton of <see cref="ConfigurationItemSummaryBag"/> objects.</returns>
        public List<ConfigurationItemSummaryBag> GetConfigurationSummaries()
        {
            return GetConfigurationGroupTypes()
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
        /// a <paramref name="kiosk"/> or <paramref name="configuration"/> are
        /// provided then they will be used to filter the results to only areas
        /// valid for those items.
        /// </summary>
        /// <param name="kiosk">The optional kiosk to filter the results for.</param>
        /// <param name="configuration">The optional configuration to filter the results for.</param>
        /// <returns>A collection of <see cref="AreaItemSummaryBag"/> objects.</returns>
        public List<AreaItemSummaryBag> GetCheckInAreaSummaries( DeviceCache kiosk, GroupTypeCache configuration )
        {
            var areas = new Dictionary<Guid, AreaItemSummaryBag>();
            List<GroupTypeCache> configurations;
            HashSet<int> kioskGroupTypeIds = null;

            // If the caller specified a configuration, then we return areas for
            // only that primary configuration. Otherwise we include areas from
            // all configurations.
            if ( configuration != null )
            {
                configurations = new List<GroupTypeCache> { configuration };
            }
            else
            {
                configurations = GetConfigurationGroupTypes().ToList();
            }

            if ( kiosk != null )
            {
                kioskGroupTypeIds = new HashSet<int>( GetKioskAreaGroupTypes( kiosk ).Select( gt => gt.Id ) );
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
            var configuration = checkinConfiguration?.GetCheckInConfiguration( _rockContext );

            if ( searchTerm.IsNullOrWhiteSpace() )
            {
                throw new CheckInMessageException( "Search term must not be empty." );
            }

            if ( configuration == null )
            {
                throw new ArgumentOutOfRangeException( nameof( checkinConfiguration ), "Check-in configuration data is not valid." );
            }

            var familyQry = GetFamilySearchQuery( searchTerm, searchType, configuration );
            var familyIdQry = GetSortedFamilyIdSearchQuery( familyQry, sortByCampus, configuration );
            var familyMemberQry = GetFamilyMemberSearchQuery( familyIdQry, configuration );

            // Pull just the information we need from the database.
            var familyMembers = familyMemberQry
                .Select( gm => new
                {
                    GroupGuid = gm.Group.Guid,
                    GroupName = gm.Group.Name,
                    CampusGuid = gm.Group.Campus.Guid,
                    RoleOrder = gm.GroupRole.Order,
                    gm.Person.Guid,
                    gm.Person.Id,
                    gm.Person.BirthYear,
                    gm.Person.BirthMonth,
                    gm.Person.BirthDay,
                    gm.Person.Gender,
                    gm.Person.NickName,
                    gm.Person.LastName
                } )
                .ToList();

            // Convert the raw database data into the bags that are understood
            // by different elements of the check-in system.
            var families = familyMembers
                .GroupBy( fm => fm.GroupGuid )
                .Select( family =>
                {
                    var firstMember = family.First();

                    return new FamilySearchItemBag
                    {
                        Guid = firstMember.GroupGuid,
                        Name = firstMember.GroupName,
                        CampusGuid = firstMember.CampusGuid,
                        Members = family
                            .OrderBy( fm => fm.RoleOrder )
                            .ThenBy( fm => fm.BirthYear )
                            .ThenBy( fm => fm.BirthMonth )
                            .ThenBy( fm => fm.BirthDay )
                            .ThenBy( fm => fm.Gender )
                            .ThenBy( fm => fm.NickName )
                            .Select( fm => new FamilyMemberSearchItemBag
                            {
                                Guid = fm.Guid,
                                IdKey = IdHasher.Instance.GetHash( fm.Id ),
                                NickName = fm.NickName,
                                LastName = fm.LastName,
                                RoleOrder = fm.RoleOrder,
                                Gender = fm.Gender,
                                BirthYear = fm.BirthYear,
                                BirthMonth = fm.BirthMonth,
                                BirthDay = fm.BirthDay,
                                BirthDate = fm.BirthYear.HasValue && fm.BirthMonth.HasValue && fm.BirthDay.HasValue
                                    ? new DateTimeOffset( new DateTime( fm.BirthYear.Value, fm.BirthMonth.Value, fm.BirthDay.Value ) )
                                    : ( DateTimeOffset? ) null
                            } )
                            .ToList()
                    };
                } )
                .ToList();

            return families;
        }

        /// <summary>
        /// Find all family members that match the specified family unique
        /// identifier for check-in. This normally includes immediate family
        /// members as well as people associated to the family with one of
        /// the configured "can check-in" known relationships.
        /// </summary>
        /// <param name="familyGuid">The family unique identifier.</param>
        /// <param name="checkinConfiguration">The check-in configuration.</param>
        /// <returns>A queryable that can be used to load all the group members associated with the family.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Check-in configuration data is not valid.</exception>
        public IQueryable<GroupMember> GetFamilyMembersForCheckInQuery( Guid familyGuid, GroupTypeCache checkinConfiguration )
        {
            var configuration = checkinConfiguration?.GetCheckInConfiguration( _rockContext );

            if ( configuration == null )
            {
                throw new ArgumentOutOfRangeException( nameof( checkinConfiguration ), "Check-in configuration data is not valid." );
            }

            var familyMemberQry = GetImmediateFamilyMembersQuery( familyGuid, configuration );
            var canCheckInFamilyMemberQry = GetCanCheckInFamilyMembersQuery( familyGuid, configuration );

            return familyMemberQry.Union( canCheckInFamilyMemberQry );
        }

        /// <summary>
        /// Converts the family members into bags that represent the data
        /// required for check-in.
        /// </summary>
        /// <param name="familyGuid">The primary family unique identifier, this is used to resolve duplicates where a family member is also marked as can check-in.</param>
        /// <param name="groupMembers">The <see cref="GroupMember"/> objects to be converted to bags.</param>
        /// <returns>A collection of <see cref="FamilyMemberBag"/> objects.</returns>
        public List<FamilyMemberBag> GetFamilyMemberBags( Guid familyGuid, IQueryable<GroupMember> groupMembers )
        {
            var familyMembers = new List<FamilyMemberBag>();

            // Get the group members along with the person record in memory.
            // Then sort by those that match the correct family first so that
            // any duplicates (non family members) can be skipped. This ensures
            // that a family member has precedence over the same person record
            // that is also flagged as "can check-in".
            var members = groupMembers
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
                    IdKey = IdHasher.Instance.GetHash( member.Person.Id ),
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
        /// <exception cref="System.ArgumentNullException">kiosk - Kiosk must be specified unless locations are specified.</exception>
        public CheckInOptions GetAllCheckInOptions( IReadOnlyCollection<GroupTypeCache> possibleAreas, DeviceCache kiosk, IReadOnlyCollection<NamedLocationCache> locations )
        {
            if ( kiosk == null && locations == null )
            {
                throw new ArgumentNullException( nameof( kiosk ), "Kiosk must be specified unless locations are specified." );
            }

            // Get the primary campus for this kiosk.
            var kioskCampusId = kiosk?.GetCampusId();
            var kioskCampus = kioskCampusId.HasValue ? CampusCache.Get( kioskCampusId.Value, _rockContext ) : null;

            // Get the current timestamp as well as today's date for filtering
            // in later logic.
            var now = kioskCampus?.CurrentDateTime ?? RockDateTime.Now;
            var today = now.Date;

            // Get all areas that don't have exclusions for today.
            var activeAreas = possibleAreas
                .Where( a => !a.GroupScheduleExclusions.Any( e => today >= e.Start && today <= e.End ) )
                .ToList();

            // Get all area identifiers as a HashSet for faster lookups.
            var activeAreaIds = new HashSet<int>( activeAreas.Select( a => a.Id ) );

            // Get the active locations to work with.
            if ( locations != null )
            {
                locations = locations
                    .Where( nlc => nlc.IsActive )
                    .ToList();
            }
            else
            {
                // Get all locations for the kiosk that are active.
                locations = kiosk.GetAllLocationIds()
                    .Select( id => NamedLocationCache.Get( id ) )
                    .Where( nlc => nlc != null && nlc.IsActive )
                    .ToList();
            }

            // Get all the group locations for these locations. This also
            // filters down to only groups in an active area.
            var groupLocations = locations
                .SelectMany( l => GroupLocationCache.AllForLocationId( l.Id ) )
                .DistinctBy( glc => glc.Id )
                .Where( glc => activeAreaIds.Contains( GroupCache.Get( glc.GroupId, _rockContext )?.GroupTypeId ?? 0 ) )
                .ToList();

            // Get all the schedules that are active.
            var activeSchedules = groupLocations
                .SelectMany( gl => gl.ScheduleIds )
                .Distinct()
                .Select( id => NamedScheduleCache.Get( id, _rockContext ) )
                .Where( s => s != null
                    && s.IsActive
                    && s.WasCheckInActive( now ) )
                .ToList();

            // Get just the schedule identifiers in a hash set for faster lookups.
            var activeScheduleIds = new HashSet<int>( activeSchedules.Select( s => s.Id ) );

            // Get just the group locations with active schedules.
            var activeGroupLocations = groupLocations
                .Where( gl => gl.ScheduleIds.Any( sid => activeScheduleIds.Contains( sid ) ) )
                .ToList();

            // Load all the counts for any locations that are still up for
            // consideration.
            var locationIdsForCount = activeGroupLocations
                .Select( gl => gl.LocationId )
                .Distinct()
                .ToList();
            var locationCounts = GetCountsForLocations( locationIdsForCount, now );

            // Construct the initial options bag.
            var options = new CheckInOptions
            {
                AbilityLevels = DefinedTypeCache.Get( SystemGuid.DefinedType.PERSON_ABILITY_LEVEL_TYPE.AsGuid(), _rockContext )
                    ?.DefinedValues
                    .Select( dv => new CheckInAbilityLevelItem
                    {
                        Guid = dv.Guid,
                        Name = dv.Value
                    } )
                    .ToList(),
                Areas = activeAreas
                    .Select( a => new CheckInAreaItem
                    {
                        Guid = a.Guid,
                        Name = a.Name
                    } )
                    .ToList(),
                Groups = new List<CheckInGroupItem>(),
                Locations = new List<CheckInLocationItem>(),
                Schedules = activeSchedules
                    .Select( s => new CheckInScheduleItem
                    {
                        Guid = s.Guid,
                        Name = s.Name
                    } )
                    .ToList()
            };

            var locationIdsOverCapacity = new HashSet<int>();

            // Add in all the locations to the options bag.
            foreach ( var grp in activeGroupLocations.GroupBy( gl => gl.LocationId ) )
            {
                var location = NamedLocationCache.Get( grp.Key, _rockContext );
                var locationScheduleIds = new HashSet<int>( grp.SelectMany( gl => gl.ScheduleIds ).Distinct() );
                var attendeeGuids = locationCounts.GetValueOrDefault( location.Guid, new HashSet<Guid>() );

                // Check if this room is at all valid. If it is over the firm
                // threshold then not even an override is allowed.
                var isThresholdExceeded = location.FirmRoomThreshold.HasValue
                    && attendeeGuids.Count > location.FirmRoomThreshold.Value;

                if ( isThresholdExceeded )
                {
                    locationIdsOverCapacity.Add( location.Id );

                    continue;
                }

                options.Locations.Add( new CheckInLocationItem
                {
                    Guid = location.Guid,
                    Name = location.Name,
                    CurrentCount = attendeeGuids.Count,
                    Capacity = location.SoftRoomThreshold,
                    CurrentPersonGuids = attendeeGuids,
                    ScheduleGuids = activeSchedules.Where( s => locationScheduleIds.Contains( s.Id ) ).Select( s => s.Guid ).ToList()
                } );
            }

            // Add in all the Groups to the options bag.
            var activeGroupLocationsUnderCapacity = activeGroupLocations
                .Where( gl => !locationIdsOverCapacity.Contains( gl.LocationId ) );

            foreach ( var grp in activeGroupLocationsUnderCapacity.GroupBy( gl => gl.GroupId ) )
            {
                var group = GroupCache.Get( grp.Key, _rockContext );
                var groupType = group?.GroupType;

                if ( groupType == null )
                {
                    continue;
                }

                options.Groups.Add( new CheckInGroupItem
                {
                    Guid = group.Guid,
                    Name = group.Name,
                    AbilityLevelGuid = null,
                    AreaGuid = groupType.Guid,
                    CheckInData = group.GetCheckInData( _rockContext ),
                    CheckInAreaData = groupType.GetCheckInAreaData( _rockContext ),
                    LocationGuids = grp.OrderBy( gl => gl.Order )
                        .Select( gl => NamedLocationCache.Get( gl.LocationId ) )
                        .Where( l => l != null )
                        .Select( l => l.Guid )
                        .ToList()
                } );
            }

            return options;
        }

        /// <summary>
        /// Filters the check-in options for a single person.
        /// </summary>
        /// <param name="options">The options to be filtered.</param>
        /// <param name="person">The person to use when filtering options.</param>
        /// <param name="configuration">The check-inconfiguration.</param>
        public void FilterOptionsForPerson( CheckInOptions options, FamilyMemberBag person, CheckInConfigurationData configuration )
        {
            var groupFilters = GetGroupFilters( configuration, person );

            options.Groups = options.Groups
                .Where( g => groupFilters.All( f => f.IsGroupValid( g ) ) )
                .ToList();

            var locationFilters = GetLocationFilters( configuration, person );

            options.Locations = options.Locations
                .Where( l => locationFilters.All( f => f.IsLocationValid( l ) ) )
                .ToList();

            RemoveEmptyOptions( options, person, configuration );
        }

        /// <summary>
        /// Removes any option items that are "empty". Meaning, if a group has
        /// no locations then it can't be available as a choice so it will be
        /// removed.
        /// </summary>
        /// <param name="options">The options to be cleaned up.</param>
        /// <param name="person">The person involved in the check-in, may be <c>null</c>.</param>
        /// <param name="configuration">The check-in configuration.</param>
        protected virtual void RemoveEmptyOptions( CheckInOptions options, FamilyMemberBag person, CheckInConfigurationData configuration )
        {
            // Start at the "bottom" and work our way up. So first remove all
            // locations without schedules.
            var allScheduleGuids = new HashSet<Guid>( options.Schedules.Select( s => s.Guid ) );
            var allReferencedLocationGuids = new HashSet<Guid>( options.Groups.SelectMany( g => g.LocationGuids ) );

            foreach ( var location in options.Locations )
            {
                location.ScheduleGuids = location.ScheduleGuids
                    .Where( scheduleGuid => allScheduleGuids.Contains( scheduleGuid ) )
                    .ToList();
            }

            options.Locations = options.Locations
                .Where( l => l.ScheduleGuids.Count > 0
                    && allReferencedLocationGuids.Contains( l.Guid ) )
                .ToList();

            // Next remove all groups without locations.
            var allLocationGuids = new HashSet<Guid>( options.Locations.Select( l => l.Guid ) );

            foreach ( var group in options.Groups )
            {
                group.LocationGuids = group.LocationGuids
                    .Where( locationGuid => allLocationGuids.Contains( locationGuid ) )
                    .ToList();
            }

            options.Groups = options.Groups
                .Where( g => g.LocationGuids.Count > 0 )
                .ToList();

            // Finally remove all areas without groups.
            var allReferencedAreaGuids = new HashSet<Guid>( options.Groups.Select( g => g.AreaGuid ) );

            options.Areas = options.Areas
                .Where( a => allReferencedAreaGuids.Contains( a.Guid ) )
                .ToList();
        }

        /// <summary>
        /// Gets the filters to use when filtering options for a specific group.
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <param name="person">The person to filter options for.</param>
        /// <returns>A list of <see cref="ICheckInOptionsFilter"/> objects that will perform filtering logic.</returns>
        private List<ICheckInOptionsGroupFilter> GetGroupFilters( CheckInConfigurationData configuration, FamilyMemberBag person )
        {
            var types = GetGroupFilterTypes( configuration );

            return GetOptionsFilters<ICheckInOptionsGroupFilter>( types, configuration, person );
        }

        /// <summary>
        /// Gets the filter type definitions to use when filtering options for
        /// groups.
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A collection of <see cref="Type"/> objects.</returns>
        protected virtual IReadOnlyCollection<Type> GetGroupFilterTypes( CheckInConfigurationData configuration )
        {
            return new[]
            {
                typeof( CheckInByAgeOptionsFilter ),
                typeof( CheckInByGradeOptionsFilter ),
                typeof( CheckInByGenderOptionsFilter ),
                typeof( CheckInByMembershipOptionsFilter ),
                typeof( CheckInByDataViewOptionsFilter )
            };
        }

        /// <summary>
        /// Gets the filters to use when filtering options for a specific location.
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <param name="person">The person to filter options for.</param>
        /// <returns>A list of <see cref="ICheckInOptionsFilter"/> objects that will perform filtering logic.</returns>
        private List<ICheckInOptionsLocationFilter> GetLocationFilters( CheckInConfigurationData configuration, FamilyMemberBag person )
        {
            var types = GetLocationFilterTypes( configuration );

            return GetOptionsFilters<ICheckInOptionsLocationFilter>( types, configuration, person );
        }

        /// <summary>
        /// Gets the filter type definitions to use when filtering options for
        /// locations.
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A collection of <see cref="Type"/> objects.</returns>
        protected virtual IReadOnlyCollection<Type> GetLocationFilterTypes( CheckInConfigurationData configuration )
        {
            return new[]
            {
                typeof( CheckInThresholdOptionsFilter )
            };
        }

        /// <summary>
        /// Gets the options filters specified by the types. This filters will
        /// be properly initialized before returning.
        /// </summary>
        /// <typeparam name="T">The expected type that the filters must conform to.</typeparam>
        /// <param name="filterTypes">The filter types.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <param name="person">The person to filter for.</param>
        /// <returns>A collection of filter instances.</returns>
        private List<T> GetOptionsFilters<T>( IReadOnlyCollection<Type> filterTypes, CheckInConfigurationData configuration, FamilyMemberBag person )
        {
            var expectedType = typeof( T );

            return filterTypes
                .Where( t => expectedType.IsAssignableFrom( t ) )
                .Select( t =>
                {
                    var filter = ( T ) Activator.CreateInstance( t );

                    if ( filter is ICheckInOptionsFilter optionsFilter )
                    {
                        optionsFilter.Configuration = configuration;
                        optionsFilter.RockContext = _rockContext;
                    }

                    if ( filter is ICheckInPersonOptionsFilter personFilter )
                    {
                        personFilter.Person = person;
                    }

                    return filter;
                } )
                .ToList();
        }

        /// <summary>
        /// Clones the options. This creates an entirely new options bag as well
        /// as new instances of every object it contains. The new options bag
        /// can be modified at will without affecting the original bag. It seems
        /// like we are doing a lot, but this is insanely fast, clocking in at
        /// 6ns per call.
        /// </summary>
        /// <remarks>TODO: This should be an extension method.</remarks>
        /// <param name="options">The options to be cloned.</param>
        /// <returns>A new instance of <see cref="CheckInOptions"/>.</returns>
        public CheckInOptions CloneOptions( CheckInOptions options )
        {
            var clonedOptions = new CheckInOptions
            {
                AbilityLevels = options.AbilityLevels
                    .Select( al => new CheckInAbilityLevelItem
                    {
                        Guid = al.Guid,
                        Name = al.Name
                    } )
                    .ToList(),
                Areas = options.Areas
                    .Select( a => new CheckInAreaItem
                    {
                        Guid = a.Guid,
                        Name = a.Name
                    } )
                    .ToList(),
                Groups = options.Groups
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
                Locations = options.Locations
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
                Schedules = options.Schedules
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

        #region Internal Methods

        /// <summary>
        /// Gets the configuration group types that are defined in the system.
        /// </summary>
        /// <returns>An enumeration of <see cref="GroupTypeCache"/> objects.</returns>
        /// <exception cref="Exception">Check-in Template Purpose was not found in the database, please check your installation.</exception>
        internal IEnumerable<GroupTypeCache> GetConfigurationGroupTypes()
        {
            var checkinTemplateTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid(), _rockContext )?.Id;

            if ( !checkinTemplateTypeId.HasValue )
            {
                throw new Exception( "Check-in Template Purpose was not found in the database, please check your installation." );
            }

            return GroupTypeCache.All( _rockContext )
                .Where( t => t.GroupTypePurposeValueId.HasValue && t.GroupTypePurposeValueId == checkinTemplateTypeId.Value );
        }

        /// <summary>
        /// Gets the group types that are valid for the kiosk device. Only group
        /// types associated via group and location to the kiosk will be returned.
        /// </summary>
        /// <param name="kiosk">The kiosk device.</param>
        /// <returns>An enumeration of <see cref="GroupTypeCache" /> objects.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="kiosk"/> is <c>null</c>.</exception>
        internal IEnumerable<GroupTypeCache> GetKioskAreaGroupTypes( DeviceCache kiosk )
        {
            if ( kiosk == null )
            {
                throw new ArgumentNullException( nameof( kiosk ) );
            }

            // Get all locations for the device.
            var locationIds = new HashSet<int>( kiosk.GetAllLocationIds() );

            // Get all the group locations associated with those locations.
            var groupLocations = locationIds
                .SelectMany( id => GroupLocationCache.AllForLocationId( id, _rockContext ) )
                .DistinctBy( glc => glc.Id )
                .ToList();

            // Get the distinct group types for those group locations that have
            // attendance enabled.
            return groupLocations
                .Select( gl => GroupCache.Get( gl.GroupId, _rockContext )?.GroupTypeId )
                .Where( id => id.HasValue )
                .Distinct()
                .Select( id => GroupTypeCache.Get( id.Value, _rockContext ) )
                .Where( gt => gt != null && gt.TakesAttendance )
                .ToList();
        }

        /// <summary>
        /// Gets a queryable for GroupMembers that is pre-filtered to the
        /// minimum requirements to be considered for check-in.
        /// </summary>
        /// <returns>A queryable of <see cref="GroupMember"/> objects.</returns>
        /// <exception cref="Exception">Family group type was not found in the database, please check your installation.</exception>
        /// <exception cref="Exception">Person record type was not found in the database, please check your installation.</exception>
        internal IQueryable<GroupMember> GetFamilyGroupMemberQuery()
        {
            var familyGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid(), _rockContext )?.Id;
            var personRecordTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid(), _rockContext )?.Id;

            if ( !familyGroupTypeId.HasValue )
            {
                throw new Exception( "Family group type was not found in the database, please check your installation." );
            }

            if ( !personRecordTypeId.HasValue )
            {
                throw new Exception( "Person record type was not found in the database, please check your installation." );
            }

            return new GroupMemberService( _rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( gm => gm.GroupTypeId == familyGroupTypeId.Value
                    && !gm.Person.IsDeceased
                    && gm.Person.RecordTypeValueId == personRecordTypeId.Value );
        }

        /// <summary>
        /// Searches for families by full name, last name first.
        /// </summary>
        /// <param name="searchTerm">The family name to search for.</param>
        /// <returns>A queryable of family <see cref="Group"/> objects.</returns>
        internal IQueryable<Group> SearchForFamiliesByName( string searchTerm )
        {
            var personIdQry = new PersonService( _rockContext )
                .GetByFullName( searchTerm, false )
                .AsNoTracking()
                .Select( p => p.Id );

            return GetFamilyGroupMemberQuery()
                .Where( gm => personIdQry.Contains( gm.PersonId ) )
                .Select( gm => gm.Group )
                .Distinct();
        }

        /// <summary>
        /// Searches for families by phone number.
        /// </summary>
        /// <param name="searchTerm">The phone number to search for.</param>
        /// <param name="configuration">The check-in configuration data for this search.</param>
        /// <returns>A queryable of family <see cref="Group"/> objects.</returns>
        internal IQueryable<Group> SearchForFamiliesByPhoneNumber( string searchTerm, CheckInConfigurationData configuration )
        {
            var personRecordTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid(), _rockContext )?.Id;
            var numericSearchTerm = searchTerm.AsNumeric();

            if ( configuration.MinimumPhoneNumberLength.HasValue && searchTerm.Length < configuration.MinimumPhoneNumberLength.Value )
            {
                throw new CheckInMessageException( $"Search term must be at least {configuration.MinimumPhoneNumberLength} digits." );
            }

            if ( configuration.MaximumPhoneNumberLength.HasValue && searchTerm.Length > configuration.MaximumPhoneNumberLength.Value )
            {
                throw new CheckInMessageException( $"Search term must be at most {configuration.MaximumPhoneNumberLength} digits." );
            }

            if ( !personRecordTypeId.HasValue )
            {
                throw new Exception( "Person record type was not found in the database, please check your installation." );
            }

            var phoneQry = new PhoneNumberService( _rockContext )
                .Queryable()
                .AsNoTracking();

            if ( configuration.PhoneSearchType == Enums.CheckIn.PhoneSearchMode.EndsWith )
            {
                var charSearchTerm = numericSearchTerm.ToCharArray();

                Array.Reverse( charSearchTerm );

                var reversedSearchTerm = new string( charSearchTerm );

                phoneQry = phoneQry
                    .Where( pn => pn.NumberReversed.StartsWith( reversedSearchTerm ) );
            }
            else
            {
                phoneQry = phoneQry
                    .Where( pn => pn.Number.Contains( numericSearchTerm ) );
            }

            var personIdQry = phoneQry.Select( pn => pn.PersonId );

            return GetFamilyGroupMemberQuery()
                .Where( gm => personIdQry.Contains( gm.PersonId ) )
                .Select( gm => gm.Group );
        }

        /// <summary>
        /// Searches for families by a scanned identifier.
        /// </summary>
        /// <param name="searchTerm">The scanned identifier to search for.</param>
        /// <returns>A queryable of family <see cref="Group"/> objects.</returns>
        internal IQueryable<Group> SearchForFamiliesByScannedId( string searchTerm )
        {
            var alternateIdValueId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid(), _rockContext )?.Id;

            if ( !alternateIdValueId.HasValue )
            {
                throw new Exception( "Alternate Id search type value was not found in the database, please check your installation." );
            }

            var personIdQry = new PersonSearchKeyService( _rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( psk => psk.SearchTypeValueId == alternateIdValueId.Value
                    && psk.SearchValue == searchTerm )
                .Select( psk => psk.PersonAlias.PersonId );

            return GetFamilyGroupMemberQuery()
                .Where( gm => personIdQry.Contains( gm.PersonId ) )
                .Select( gm => gm.Group )
                .Distinct();
        }

        /// <summary>
        /// Searches for families by one or more family identifiers.
        /// </summary>
        /// <param name="searchTerm">The family identifer to search for as a delimited list of integer identifiers.</param>
        /// <returns>A queryable of family <see cref="Group"/> objects.</returns>
        internal IQueryable<Group> SearchForFamiliesByFamilyId( string searchTerm )
        {
            var searchFamilyIds = searchTerm.SplitDelimitedValues().AsIntegerList();

            return GetFamilyGroupMemberQuery()
                .Where( gm => searchFamilyIds.Contains( gm.GroupId ) )
                .Select( gm => gm.Group )
                .Distinct();
        }

        /// <summary>
        /// Gets the counts for all the locations in one query.
        /// </summary>
        /// <param name="locationIds">The location identifiers.</param>
        /// <param name="now">The current timestamp to use for attendance calculation.</param>
        /// <returns>
        /// A dictionary of location unique identifier keys and the unique
        /// identifiers of the people in the location. No value will be available
        /// if there are not any attendance records for the location.
        /// </returns>
        internal Dictionary<Guid, HashSet<Guid>> GetCountsForLocations( IReadOnlyCollection<int> locationIds, DateTime now )
        {
            var attendanceService = new AttendanceService( _rockContext );
            var todayDate = now.Date;

            var attendances = attendanceService.Queryable()
                .Where( a =>
                    a.Occurrence.OccurrenceDate == todayDate
                    && a.Occurrence.LocationId.HasValue
                    && a.Occurrence.GroupId.HasValue
                    && a.Occurrence.ScheduleId.HasValue
                    && locationIds.Contains( a.Occurrence.LocationId.Value )
                    && a.PersonAliasId.HasValue
                    && a.DidAttend.HasValue
                    && a.DidAttend.Value
                    && !a.EndDateTime.HasValue )
                .Select( a => new
                {
                    LocationGuid = a.Occurrence.Location.Guid,
                    ScheduleId = a.Occurrence.ScheduleId.Value,
                    a.CampusId,
                    a.StartDateTime,
                    a.EndDateTime,
                    PersonGuid = a.PersonAlias.Person.Guid
                } )
                .ToList();

            // We now have all the attendance records for these locations that
            // have check-in today but not yet checked out. Now we need to
            // filter out any that have schedules where check-in is no longer
            // active.

            var activeAttendances = attendances
                .GroupBy( a => new { a.ScheduleId, a.CampusId } )
                .SelectMany( grp =>
                {
                    // The vast majority of attendance records for a single
                    // location should have the same schedule and campus.
                    var scheduleCache = NamedScheduleCache.Get( grp.Key.ScheduleId, _rockContext );
                    var campusCache = grp.Key.CampusId.HasValue
                        ? CampusCache.Get( grp.Key.CampusId.Value, _rockContext )
                        : null;

                    return grp.Where( a => Attendance.CalculateIsCurrentlyCheckedIn( a.StartDateTime, a.EndDateTime, campusCache, scheduleCache ) );
                } );

            return activeAttendances
                .GroupBy( a => a.LocationGuid )
                .ToDictionary( grp => grp.Key, grp => new HashSet<Guid>( grp.Select( a => a.PersonGuid ) ) );
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets a query to perform the basic family search based on the term
        /// and search type.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="searchType">Type of the search.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A queryable for <see cref="Group"/> objects that match the search criteria.</returns>
        private IQueryable<Group> GetFamilySearchQuery( string searchTerm, FamilySearchMode searchType, CheckInConfigurationData configuration )
        {
            switch ( searchType )
            {
                case FamilySearchMode.PhoneNumber:
                    if ( configuration.FamilySearchType != FamilySearchMode.PhoneNumber && configuration.FamilySearchType != FamilySearchMode.NameAndPhone )
                    {
                        throw new CheckInMessageException( "Searching by phone number is not allowed by the check-in configuration." );
                    }

                    return SearchForFamiliesByPhoneNumber( searchTerm, configuration );

                case FamilySearchMode.Name:
                    if ( configuration.FamilySearchType != FamilySearchMode.Name && configuration.FamilySearchType != FamilySearchMode.NameAndPhone )
                    {
                        throw new CheckInMessageException( "Searching by phone number is not allowed by the check-in configuration." );
                    }

                    return SearchForFamiliesByName( searchTerm );

                case FamilySearchMode.NameAndPhone:
                    if ( configuration.FamilySearchType != FamilySearchMode.NameAndPhone )
                    {
                        throw new CheckInMessageException( "Searching by phone number is not allowed by the check-in configuration." );
                    }

                    return searchTerm.Any( c => char.IsLetter( c ) )
                        ? SearchForFamiliesByName( searchTerm )
                        : SearchForFamiliesByPhoneNumber( searchTerm, configuration );

                case FamilySearchMode.ScannedId:
                    return SearchForFamiliesByScannedId( searchTerm );

                case FamilySearchMode.FamilyId:
                    return SearchForFamiliesByFamilyId( searchTerm );

                default:
                    throw new ArgumentOutOfRangeException( nameof( searchType ), "Invalid search type specified." );
            }
        }

        /// <summary>
        /// Gets the sorted family identifier search query. This is used during
        /// the family search process to apply the correct sorting and maximum
        /// result limits to the query.
        /// </summary>
        /// <param name="familyQry">The family query to be sorted and limited..</param>
        /// <param name="sortByCampus">The campus to use when sorting the results.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A queryable of family group identifiers to be included in the results.</returns>
        private IQueryable<int> GetSortedFamilyIdSearchQuery( IQueryable<Group> familyQry, CampusCache sortByCampus, CheckInConfigurationData configuration )
        {
            var maxResults = configuration.MaximumNumberOfResults ?? 100;
            IQueryable<int> familyIdQry;

            // Handle sorting of the results. We either sort by campus or just
            // take the results as-is.
            if ( sortByCampus != null )
            {
                familyIdQry = familyQry
                    .Select( g => new
                    {
                        g.Id,
                        g.CampusId
                    } )
                    .Distinct()
                    .OrderByDescending( g => g.CampusId.HasValue && g.CampusId.Value == sortByCampus.Id )
                    .Select( g => g.Id );
            }
            else
            {
                familyIdQry = familyQry.Select( g => g.Id ).Distinct();
            }

            // Limit the results.
            if ( maxResults > 0 )
            {
                familyIdQry = familyIdQry.Take( maxResults );
            }

            return familyIdQry;
        }

        /// <summary>
        /// Gets the family member query that contains all the family members
        /// are valid for check-in and a member of one of the specified families.
        /// </summary>
        /// <param name="familyIdQry">The family identifier query specifying which families to include.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A queryable of <see cref="GroupMember"/> objects.</returns>
        private IQueryable<GroupMember> GetFamilyMemberSearchQuery( IQueryable<int> familyIdQry, CheckInConfigurationData configuration )
        {
            var familyMemberQry = GetFamilyGroupMemberQuery()
                .Where( gm => familyIdQry.Contains( gm.GroupId )
                    && !string.IsNullOrEmpty( gm.Person.NickName ) );

            if ( configuration.IsInactivePersonExcluded )
            {
                var inactiveValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid(), _rockContext )?.Id;

                if ( inactiveValueId.HasValue )
                {
                    familyMemberQry = familyMemberQry
                        .Where( gm => gm.Person.RecordStatusValueId != inactiveValueId.Value );
                }
            }

            return familyMemberQry;
        }

        /// <summary>
        /// Gets a queryable that will return all family members that are
        /// part of the specified family. Only <see cref="GroupMember"/>
        /// records that are part of the <see cref="Group"/> specified by
        /// <paramref name="familyGuid"/> will be returned.
        /// </summary>
        /// <param name="familyGuid">The unique identifier of the family.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A queryable of matching <see cref="GroupMember"/> objects.</returns>
        /// <exception cref="Exception">Inactive person record status was not found in the database, please check your installation.</exception>
        private IQueryable<GroupMember> GetImmediateFamilyMembersQuery( Guid familyGuid, CheckInConfigurationData configuration )
        {
            var groupMemberService = new GroupMemberService( _rockContext );
            var qry = groupMemberService.GetByGroupGuid( familyGuid ).AsNoTracking();

            if ( configuration.IsInactivePersonExcluded )
            {
                var personRecordStatusInactiveId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid(), _rockContext )?.Id;

                if ( !personRecordStatusInactiveId.HasValue )
                {
                    throw new Exception( "Inactive person record status was not found in the database, please check your installation." );
                }

                qry = qry.Where( m => m.Person.RecordStatusValueId != personRecordStatusInactiveId.Value );
            }

            return qry;
        }

        /// <summary>
        /// Gets a queryable that will return any group member records with
        /// a valid relationship to any member of the family. This uses the
        /// allowed can check-in roles defined on the configuration.
        /// </summary>
        /// <param name="familyGuid">The family unique identifier.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A queryable of matching <see cref="GroupMember"/> objects.</returns>
        /// <exception cref="Exception">Known relationship group type was not found in the database, please check your installation.</exception>
        /// <exception cref="Exception">Inactive person record status was not found in the database, please check your installation.</exception>
        /// <exception cref="Exception">Known relationship owner role was not found in the database, please check your installation.</exception>
        private IQueryable<GroupMember> GetCanCheckInFamilyMembersQuery( Guid familyGuid, CheckInConfigurationData configuration )
        {
            var knownRelationshipGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid(), _rockContext );
            int? personRecordStatusInactiveId = null;

            if ( knownRelationshipGroupType == null )
            {
                throw new Exception( "Known relationship group type was not found in the database, please check your installation." );
            }

            if ( configuration.IsInactivePersonExcluded )
            {
                personRecordStatusInactiveId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid(), _rockContext )?.Id;

                if ( !personRecordStatusInactiveId.HasValue )
                {
                    throw new Exception( "Inactive person record status was not found in the database, please check your installation." );
                }
            }

            var knownRelationshipsOwnerGuid = SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER.AsGuid();
            var ownerRole = knownRelationshipGroupType.Roles.FirstOrDefault( r => r.Guid == knownRelationshipsOwnerGuid );

            if ( ownerRole == null )
            {
                throw new Exception( "Known relationship owner role was not found in the database, please check your installation." );
            }

            var familyMemberPersonIdQry = GetImmediateFamilyMembersQuery( familyGuid, configuration )
                .Select( fm => fm.PersonId );
            var groupMemberService = new GroupMemberService( _rockContext );
            var canCheckInRoleIds = knownRelationshipGroupType.Roles
                .Where( r => configuration.CanCheckInKnownRelationshipRoleGuids.Contains( r.Guid ) )
                .Select( r => r.Id )
                .ToList();

            // Get the Known Relationship group ids for each member of the family.
            var relationshipGroupIdQry = groupMemberService
                .Queryable()
                .AsNoTracking()
                .Where( g => g.GroupRoleId == ownerRole.Id
                    && familyMemberPersonIdQry.Contains( g.PersonId ) )
                .Select( g => g.GroupId );

            // Get anyone in any of those groups that has a role flagged as "can check-in".
            var canCheckInFamilyMemberQry = groupMemberService
                .Queryable()
                .AsNoTracking()
                .Where( gm => relationshipGroupIdQry.Contains( gm.GroupId ) );

            /*  02-12-2024 DSH

              Build LINQ expression 'canCheckInRoleIds.Contains( gm.GroupRoleId )'
              manually. If EF sees a List<>.Contains() call then it won't re-use
              the cache and has to re-create the SQL statement each time. This
              costs about 15-20ms. Considering this query will otherwise take
              only about 2-3ms, that is a lot of overhead.
           */
            Expression<Func<GroupMember, bool>> predicate = null;
            var gmParameter = Expression.Parameter( typeof( GroupMember ), "gm" );
            foreach ( var roleId in canCheckInRoleIds )
            {
                // Don't use LinqPredicateBuilder as that will cause a SQL
                // parameter to be generated for each comparison. Since we
                // are not in control of how many items are in the list we
                // could run out of parameters. So build it with constant
                // values instead.
                var propExpr = Expression.Property( gmParameter, nameof( GroupMember.GroupRoleId ) );
                var equalExpr = Expression.Equal( propExpr, Expression.Constant( roleId ) );
                var expression = Expression.Lambda<Func<GroupMember, bool>>( equalExpr, gmParameter );

                predicate = predicate != null
                    ? predicate.Or( expression )
                    : expression;
            }

            // If we had any canCheckInRoleIds then append the predicate.
            if ( predicate != null )
            {
                canCheckInFamilyMemberQry = canCheckInFamilyMemberQry.Where( predicate );
            }

            // If check-in does not allow inactive people then add that
            // check now.
            if ( configuration.IsInactivePersonExcluded )
            {
                canCheckInFamilyMemberQry = canCheckInFamilyMemberQry
                    .Where( gm => gm.Person.RecordStatusReasonValueId != personRecordStatusInactiveId.Value );
            }

            return canCheckInFamilyMemberQry;
        }

        #endregion
    }
}
