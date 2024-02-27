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
using Rock.Utility;
using Rock.ViewModels.CheckIn;
using Rock.Web.Cache;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Performs family search logic for the check-in system.
    /// </summary>
    internal class DefaultSearchProvider
    {
        #region Properties

        /// <summary>
        /// Gets or sets the check-in director.
        /// </summary>
        /// <value>The check-in director.</value>
        protected CheckInDirector Director { get; }

        /// <summary>
        /// Gets the check-in configuration data.
        /// </summary>
        /// <value>The check-in configuration data.</value>
        protected CheckInConfigurationData Configuration { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSearchProvider"/> class.
        /// </summary>
        /// <param name="director">The rock context to use when accessing the database.</param>
        /// <param name="configuration">The check-in configuration data.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="director"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="configuration"/> is <c>null</c>.</exception>
        public DefaultSearchProvider( CheckInDirector director, CheckInConfigurationData configuration )
        {
            if ( director == null )
            {
                throw new ArgumentNullException( nameof( director ) );
            }

            if ( configuration == null )
            {
                throw new ArgumentNullException( nameof( configuration ) );
            }

            Director = director;
            Configuration = configuration;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets a query to perform the basic family search based on the term
        /// and search type.
        /// </summary>
        /// <param name="searchTerm">The search term.</param>
        /// <param name="searchType">Type of the search.</param>
        /// <returns>A queryable for <see cref="Group"/> objects that match the search criteria.</returns>
        public virtual IQueryable<Group> GetFamilySearchQuery( string searchTerm, FamilySearchMode searchType )
        {
            switch ( searchType )
            {
                case FamilySearchMode.PhoneNumber:
                    if ( Configuration.FamilySearchType != FamilySearchMode.PhoneNumber && Configuration.FamilySearchType != FamilySearchMode.NameAndPhone )
                    {
                        throw new CheckInMessageException( "Searching by phone number is not allowed by the check-in configuration." );
                    }

                    return SearchForFamiliesByPhoneNumber( searchTerm );

                case FamilySearchMode.Name:
                    if ( Configuration.FamilySearchType != FamilySearchMode.Name && Configuration.FamilySearchType != FamilySearchMode.NameAndPhone )
                    {
                        throw new CheckInMessageException( "Searching by phone number is not allowed by the check-in configuration." );
                    }

                    return SearchForFamiliesByName( searchTerm );

                case FamilySearchMode.NameAndPhone:
                    if ( Configuration.FamilySearchType != FamilySearchMode.NameAndPhone )
                    {
                        throw new CheckInMessageException( "Searching by phone number is not allowed by the check-in configuration." );
                    }

                    return searchTerm.Any( c => char.IsLetter( c ) )
                        ? SearchForFamiliesByName( searchTerm )
                        : SearchForFamiliesByPhoneNumber( searchTerm);

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
        /// <returns>A queryable of family group identifiers to be included in the results.</returns>
        public virtual IQueryable<int> GetSortedFamilyIdSearchQuery( IQueryable<Group> familyQry, CampusCache sortByCampus )
        {
            var maxResults = Configuration.MaximumNumberOfResults ?? 100;
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
        /// <returns>A queryable of <see cref="GroupMember"/> objects.</returns>
        public virtual IQueryable<GroupMember> GetFamilyMemberSearchQuery( IQueryable<int> familyIdQry )
        {
            var familyMemberQry = GetFamilyGroupMemberQuery()
                .Where( gm => familyIdQry.Contains( gm.GroupId )
                    && !string.IsNullOrEmpty( gm.Person.NickName ) );

            if ( Configuration.IsInactivePersonExcluded )
            {
                var inactiveValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid(), Director.RockContext )?.Id;

                if ( inactiveValueId.HasValue )
                {
                    familyMemberQry = familyMemberQry
                        .Where( gm => gm.Person.RecordStatusValueId != inactiveValueId.Value );
                }
            }

            return familyMemberQry;
        }

        /// <summary>
        /// Gets the family search item bags from the queryable.
        /// </summary>
        /// <param name="familyMemberQry">The family member query.</param>
        /// <returns>A list of <see cref="FamilySearchItemBag"/> instances.</returns>
        public virtual List<FamilySearchItemBag> GetFamilySearchItemBags( IQueryable<GroupMember> familyMemberQry )
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
        /// Find all family members that match the specified family unique
        /// identifier for check-in. This normally includes immediate family
        /// members as well as people associated to the family with one of
        /// the configured "can check-in" known relationships.
        /// </summary>
        /// <param name="familyGuid">The family unique identifier.</param>
        /// <returns>A queryable that can be used to load all the group members associated with the family.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Check-in configuration data is not valid.</exception>
        public virtual IQueryable<GroupMember> GetFamilyMembersForFamilyQuery( Guid familyGuid )
        {
            using ( var activity = ObservabilityHelper.StartActivity( "Get Family Members Query" ) )
            {
                var familyMemberQry = GetImmediateFamilyMembersQuery( familyGuid, Configuration );
                var canCheckInFamilyMemberQry = GetCanCheckInFamilyMembersQuery( familyGuid, Configuration );

                return familyMemberQry.Union( canCheckInFamilyMemberQry );
            }
        }

        /// <summary>
        /// Gets a queryable for GroupMembers that is pre-filtered to the
        /// minimum requirements to be considered for check-in.
        /// </summary>
        /// <returns>A queryable of <see cref="GroupMember"/> objects.</returns>
        /// <exception cref="Exception">Family group type was not found in the database, please check your installation.</exception>
        /// <exception cref="Exception">Person record type was not found in the database, please check your installation.</exception>
        protected virtual IQueryable<GroupMember> GetFamilyGroupMemberQuery()
        {
            var familyGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid(), Director.RockContext )?.Id;
            var personRecordTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid(), Director.RockContext )?.Id;

            if ( !familyGroupTypeId.HasValue )
            {
                throw new Exception( "Family group type was not found in the database, please check your installation." );
            }

            if ( !personRecordTypeId.HasValue )
            {
                throw new Exception( "Person record type was not found in the database, please check your installation." );
            }

            return new GroupMemberService( Director.RockContext )
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
        protected virtual IQueryable<Group> SearchForFamiliesByName( string searchTerm )
        {
            var personIdQry = new PersonService( Director.RockContext )
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
        /// <returns>A queryable of family <see cref="Group"/> objects.</returns>
        protected virtual IQueryable<Group> SearchForFamiliesByPhoneNumber( string searchTerm )
        {
            var personRecordTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid(), Director.RockContext )?.Id;
            var numericSearchTerm = searchTerm.AsNumeric();

            if ( Configuration.MinimumPhoneNumberLength.HasValue && searchTerm.Length < Configuration.MinimumPhoneNumberLength.Value )
            {
                throw new CheckInMessageException( $"Search term must be at least {Configuration.MinimumPhoneNumberLength} digits." );
            }

            if ( Configuration.MaximumPhoneNumberLength.HasValue && searchTerm.Length > Configuration.MaximumPhoneNumberLength.Value )
            {
                throw new CheckInMessageException( $"Search term must be at most {Configuration.MaximumPhoneNumberLength} digits." );
            }

            if ( !personRecordTypeId.HasValue )
            {
                throw new Exception( "Person record type was not found in the database, please check your installation." );
            }

            var phoneQry = new PhoneNumberService( Director.RockContext )
                .Queryable()
                .AsNoTracking();

            if ( Configuration.PhoneSearchType == Enums.CheckIn.PhoneSearchMode.EndsWith )
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
        protected virtual IQueryable<Group> SearchForFamiliesByScannedId( string searchTerm )
        {
            var alternateIdValueId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid(), Director.RockContext )?.Id;

            if ( !alternateIdValueId.HasValue )
            {
                throw new Exception( "Alternate Id search type value was not found in the database, please check your installation." );
            }

            var personIdQry = new PersonSearchKeyService( Director.RockContext )
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
        protected virtual IQueryable<Group> SearchForFamiliesByFamilyId( string searchTerm )
        {
            var searchFamilyIds = searchTerm.SplitDelimitedValues().AsIntegerList();

            return GetFamilyGroupMemberQuery()
                .Where( gm => searchFamilyIds.Contains( gm.GroupId ) )
                .Select( gm => gm.Group )
                .Distinct();
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
        protected virtual IQueryable<GroupMember> GetImmediateFamilyMembersQuery( Guid familyGuid, CheckInConfigurationData configuration )
        {
            var groupMemberService = new GroupMemberService( Director.RockContext );
            var qry = groupMemberService.GetByGroupGuid( familyGuid ).AsNoTracking();

            if ( configuration.IsInactivePersonExcluded )
            {
                var personRecordStatusInactiveId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid(), Director.RockContext )?.Id;

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
        protected virtual IQueryable<GroupMember> GetCanCheckInFamilyMembersQuery( Guid familyGuid, CheckInConfigurationData configuration )
        {
            var knownRelationshipGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS.AsGuid(), Director.RockContext );
            int? personRecordStatusInactiveId = null;

            if ( knownRelationshipGroupType == null )
            {
                throw new Exception( "Known relationship group type was not found in the database, please check your installation." );
            }

            if ( configuration.IsInactivePersonExcluded )
            {
                personRecordStatusInactiveId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid(), Director.RockContext )?.Id;

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
            var groupMemberService = new GroupMemberService( Director.RockContext );
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

            canCheckInFamilyMemberQry = CheckInDirector.WhereContains( canCheckInFamilyMemberQry, canCheckInRoleIds, gm => gm.GroupRoleId );

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
