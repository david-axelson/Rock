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
using System.Data.Entity;
using System.Linq;

using Rock.Data;
using Rock.Enums.CheckIn;
using Rock.Model;
using Rock.Web.Cache;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Performs family search logic for the check-in system.
    /// </summary>
    internal class CheckInFamilySearch
    {
        #region Properties

        /// <summary>
        /// The context to use when accessing the database.
        /// </summary>
        protected RockContext RockContext { get; }

        /// <summary>
        /// Gets the check-in configuration data.
        /// </summary>
        /// <value>The check-in configuration data.</value>
        protected CheckInConfigurationData Configuration { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInFamilySearch"/> class.
        /// </summary>
        /// <param name="rockContext">The rock context to use when accessing the database.</param>
        /// <param name="configuration">The check-in configuration data.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="rockContext"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="configuration"/> is <c>null</c>.</exception>
        public CheckInFamilySearch( RockContext rockContext, CheckInConfigurationData configuration )
        {
            if ( rockContext == null )
            {
                throw new ArgumentNullException( nameof( rockContext ) );
            }

            if ( configuration == null )
            {
                throw new ArgumentNullException( nameof( configuration ) );
            }

            RockContext = rockContext;
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
                var inactiveValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid(), RockContext )?.Id;

                if ( inactiveValueId.HasValue )
                {
                    familyMemberQry = familyMemberQry
                        .Where( gm => gm.Person.RecordStatusValueId != inactiveValueId.Value );
                }
            }

            return familyMemberQry;
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
            var familyGroupTypeId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuid(), RockContext )?.Id;
            var personRecordTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid(), RockContext )?.Id;

            if ( !familyGroupTypeId.HasValue )
            {
                throw new Exception( "Family group type was not found in the database, please check your installation." );
            }

            if ( !personRecordTypeId.HasValue )
            {
                throw new Exception( "Person record type was not found in the database, please check your installation." );
            }

            return new GroupMemberService( RockContext )
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
            var personIdQry = new PersonService( RockContext )
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
            var personRecordTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid(), RockContext )?.Id;
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

            var phoneQry = new PhoneNumberService( RockContext )
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
            var alternateIdValueId = DefinedValueCache.Get( SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid(), RockContext )?.Id;

            if ( !alternateIdValueId.HasValue )
            {
                throw new Exception( "Alternate Id search type value was not found in the database, please check your installation." );
            }

            var personIdQry = new PersonSearchKeyService( RockContext )
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

        #endregion
    }
}
