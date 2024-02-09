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
using Rock.ViewModels.CheckIn;
using Rock.Web.Cache;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Primary entry point to the check-in system. This provides a single
    /// place to interface with check-in so that all logic is centralized
    /// and not duplicated.
    /// </summary>
    internal sealed class CheckInDirector
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
        /// <param name="checkinConfiguration">The checkin configuration.</param>
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
        private IQueryable<GroupMember> GetFamilyGroupMemberQuery()
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
        private IQueryable<Group> SearchForFamiliesByName( string searchTerm )
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
        private IQueryable<Group> SearchForFamiliesByPhoneNumber( string searchTerm, CheckInConfigurationData configuration )
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
        private IQueryable<Group> SearchForFamiliesByScannedId( string searchTerm )
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
        private IQueryable<Group> SearchForFamiliesByFamilyId( string searchTerm )
        {
            var searchFamilyIds = searchTerm.SplitDelimitedValues().AsIntegerList();

            return GetFamilyGroupMemberQuery()
                .Where( gm => searchFamilyIds.Contains( gm.GroupId ) )
                .Select( gm => gm.Group )
                .Distinct();
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

        #endregion
    }
}
