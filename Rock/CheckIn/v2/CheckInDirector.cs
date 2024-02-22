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
        #region Fields

        /// <summary>
        /// The context to use when accessing the database.
        /// </summary>
        private readonly RockContext _rockContext;

        /// <summary>
        /// The default group filter types.
        /// </summary>
        private static readonly List<Type> _defaultGroupFilterTypes = new List<Type>
        {
            typeof( CheckInByAgeOptionsFilter ),
            typeof( CheckInByGradeOptionsFilter ),
            typeof( CheckInByGenderOptionsFilter ),
            typeof( CheckInByMembershipOptionsFilter ),
            typeof( CheckInByDataViewOptionsFilter )
        };

        /// <summary>
        /// The default location filter types.
        /// </summary>
        private static readonly List<Type> _defaultLocationFilterTypes = new List<Type>
        {
            typeof( CheckInThresholdOptionsFilter )
        };

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
            var configuration = checkinConfiguration?.GetCheckInConfiguration( _rockContext );

            if ( searchTerm.IsNullOrWhiteSpace() )
            {
                throw new CheckInMessageException( "Search term must not be empty." );
            }

            if ( configuration == null )
            {
                throw new ArgumentOutOfRangeException( nameof( checkinConfiguration ), "Check-in configuration data is not valid." );
            }

            var familySearch = CreateFamilySearch( configuration );
            var familyQry = familySearch.GetFamilySearchQuery( searchTerm, searchType );
            var familyIdQry = familySearch.GetSortedFamilyIdSearchQuery( familyQry, sortByCampus );
            var familyMemberQry = familySearch.GetFamilyMemberSearchQuery( familyIdQry );

            return GetFamilySearchItemBags( familyMemberQry );
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
            using ( var activity = ObservabilityHelper.StartActivity( "Get Family Members Query" ) )
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

                return CheckInOptions.Create( possibleAreas, kiosk, locations, _rockContext );
            }
        }

        /// <summary>
        /// Filters the check-in options for a single person.
        /// </summary>
        /// <param name="options">The options to be filtered.</param>
        /// <param name="person">The person to use when filtering options.</param>
        /// <param name="configuration">The check-inconfiguration.</param>
        public void FilterOptionsForPerson( CheckInOptions options, FamilyMemberBag person, CheckInConfigurationData configuration )
        {
            using ( var activity = ObservabilityHelper.StartActivity( $"Get Options For {person.NickName}" ) )
            {
                var groupFilters = GetGroupFilters( configuration, person );

                if ( groupFilters.Count > 0 )
                {
                    options.Groups = options.Groups
                        .Where( g => groupFilters.All( f => f.IsGroupValid( g ) ) )
                        .ToList();
                }

                var locationFilters = GetLocationFilters( configuration, person );

                if ( locationFilters.Count > 0 )
                {
                    options.Locations = options.Locations
                        .Where( l => locationFilters.All( f => f.IsLocationValid( l ) ) )
                        .ToList();
                }

                RemoveEmptyOptions( options, person, configuration );
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Creates the object that will handle family search logic.
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>An instance of <see cref="CheckInFamilySearch"/>.</returns>
        protected virtual CheckInFamilySearch CreateFamilySearch( CheckInConfigurationData configuration )
        {
            return new CheckInFamilySearch( _rockContext, configuration );
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
            options.RemoveEmptyOptions();
        }

        /// <summary>
        /// Gets the filter type definitions to use when filtering options for
        /// groups.
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A collection of <see cref="Type"/> objects.</returns>
        protected virtual IReadOnlyCollection<Type> GetGroupFilterTypes( CheckInConfigurationData configuration )
        {
            return _defaultGroupFilterTypes;
        }

        /// <summary>
        /// Gets the filter type definitions to use when filtering options for
        /// locations.
        /// </summary>
        /// <param name="configuration">The check-in configuration.</param>
        /// <returns>A collection of <see cref="Type"/> objects.</returns>
        protected virtual IReadOnlyCollection<Type> GetLocationFilterTypes( CheckInConfigurationData configuration )
        {
            return _defaultLocationFilterTypes;
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
            var checkinTemplateTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid(), _rockContext )?.Id;

            if ( !checkinTemplateTypeId.HasValue )
            {
                throw new Exception( "Check-in Template Purpose was not found in the database, please check your installation." );
            }

            return GroupTypeCache.All( _rockContext )
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

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets the family search item bags from the queryable.
        /// </summary>
        /// <param name="familyMemberQry">The family member query.</param>
        /// <returns>A list of <see cref="FamilySearchItemBag"/> instances.</returns>
        private List<FamilySearchItemBag> GetFamilySearchItemBags( IQueryable<GroupMember> familyMemberQry )
        {
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
                // values instead. Too many LINQ parameters and we hit a stack
                // overflow crash.
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
