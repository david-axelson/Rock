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
using System.Runtime.Serialization;

using Rock.Data;
using Rock.Model;

namespace Rock.Web.Cache
{
    /// <summary>
    /// Information about a named group location. This is only intended for
    /// use with <see cref="Rock.Model.GroupLocation"/> objects with named
    /// locations.
    /// </summary>
    [Serializable]
    [DataContract]
    public class NamedGroupLocationCache : ModelCache<NamedGroupLocationCache, Rock.Model.GroupLocation>
    {
        #region Fields

        /// <summary>
        /// <c>true</c> if this is for a named location.
        /// </summary>
        private bool _isNamedLocation;

        #endregion

        #region Properties

        /// <inheritdoc/>
        public override TimeSpan? Lifespan
        {
            // If this isn't for a named location, use a short lifetime of 10 minutes.
            get => _isNamedLocation ? base.Lifespan : new TimeSpan( 0, 10, 0 );
        }

        /// <inheritdoc cref="Rock.Model.GroupLocation.GroupId"/>
        [DataMember]
        public int GroupId { get; private set; }

        /// <inheritdoc cref="Rock.Model.GroupLocation.LocationId"/>
        [DataMember]
        public int LocationId { get; private set; }

        /// <inheritdoc cref="Rock.Model.GroupLocation.GroupLocationTypeValueId"/>
        [DataMember]
        public int? GroupLocationTypeValueId { get; private set; }

        /// <inheritdoc cref="Rock.Model.GroupLocation.IsMailingLocation"/>
        [DataMember]
        public bool IsMailingLocation { get; private set; }

        /// <inheritdoc cref="Rock.Model.GroupLocation.IsMappedLocation"/>
        [DataMember]
        public bool IsMappedLocation { get; private set; }

        /// <inheritdoc cref="Rock.Model.GroupLocation.GroupMemberPersonAliasId"/>
        [DataMember]
        public int? GroupMemberPersonAliasId { get; private set; }

        /// <inheritdoc cref="Rock.Model.GroupLocation.Order"/>
        [DataMember( IsRequired = true )]
        public int Order { get; private set; }

        /// <summary>
        /// Gets or sets a collection containing the <see cref="Rock.Model.Schedule" />
        /// identifiers that are associated with this instance.
        /// </summary>
        /// <value>
        /// A collection of <see cref="Rock.Model.Schedule"/> identifiers.
        /// </value>
        public List<int> ScheduleIds { get; private set; }

        /// <inheritdoc cref="Rock.Model.GroupLocation.Location" />
        public NamedLocationCache Location => NamedLocationCache.Get( LocationId );

        /// <inheritdoc cref="Rock.Model.GroupLocation.GroupLocationTypeValue" />
        public DefinedValueCache GroupLocationTypeValue => GroupLocationTypeValueId.HasValue ? DefinedValueCache.Get( GroupLocationTypeValueId.Value ) : null;

        /// <summary>
        /// Gets or sets a collection containing the <see cref="NamedScheduleCache" />
        /// objects that are associated with this instance.
        /// </summary>
        /// <value>
        /// A collection of <see cref="NamedScheduleCache"/> objects.
        /// </value>
        public List<NamedScheduleCache> Schedules => ScheduleIds.Select( NamedScheduleCache.Get ).Where( s => s != null ).ToList();

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// This will return a collection containing all <see cref="NamedGroupLocationCache"/>
        /// items. This will only include items that have a named location.
        /// </summary>
        /// <returns>All <see cref="NamedGroupLocationCache"/> objects.</returns>
        public static new List<NamedGroupLocationCache> All()
        {
            return All( null );
        }

        /// <summary>
        /// This will return a collection containing all <see cref="NamedGroupLocationCache"/>
        /// items. This will only include items that have a named location.
        /// </summary>
        /// <returns>All <see cref="NamedGroupLocationCache"/> objects.</returns>
        public static new List<NamedGroupLocationCache> All( RockContext rockContext )
        {
            var cachedKeys = GetOrAddKeys( () =>
            {
                if ( rockContext != null )
                {
                    return QueryDbForAllNamedIdsWithContext( rockContext );
                }
                else
                {
                    using ( var queryRockContext = new RockContext() )
                    {
                        return QueryDbForAllNamedIdsWithContext( queryRockContext );
                    }
                }
            } );

            if ( cachedKeys == null )
            {
                return new List<NamedGroupLocationCache>();
            }

            return GetMany( cachedKeys.ToList().AsIntegerList(), rockContext ).ToList();
        }

        /// <summary>
        /// Queries the database for all named identifers with context.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns>A collection of group locations.</returns>
        private static List<string> QueryDbForAllNamedIdsWithContext( RockContext rockContext )
        {
            var service = new GroupLocationService( rockContext );

            return service.Queryable()
                .AsNoTracking()
                .Include( gl => gl.Location )
                .Include( gl => gl.Schedules )
                .Where( gl => gl.Location.Name != null && gl.Location.Name != "" )
                .Select( i => i.Id )
                .ToList()
                .ConvertAll( i => i.ToString() );
        }

        /// <inheritdoc/>
        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            if ( !( entity is GroupLocation groupLocation ) )
            {
                return;
            }

            GroupId = groupLocation.GroupId;
            LocationId = groupLocation.LocationId;
            GroupLocationTypeValueId = groupLocation.GroupLocationTypeValueId;
            IsMailingLocation = groupLocation.IsMailingLocation;
            IsMappedLocation = groupLocation.IsMappedLocation;
            GroupMemberPersonAliasId = groupLocation.GroupMemberPersonAliasId;
            Order = groupLocation.Order;
            ScheduleIds = groupLocation.Schedules.Select( s => s.Id ).ToList();

            _isNamedLocation = groupLocation.Location.Name.IsNotNullOrWhiteSpace();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Group {GroupId} at {Location.ToStringSafe()}";
        }

        #endregion Public Methods
    }
}
