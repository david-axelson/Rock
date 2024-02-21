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
using System.Runtime.Serialization;

using Rock.Data;
using Rock.Model;

namespace Rock.Web.Cache
{
    /// <summary>
    /// Information about a DataView that is cached by Rock. 
    /// </summary>
    [Serializable]
    [DataContract]
    public class DataViewCache : ModelCache<DataViewCache, DataView>
    {
        #region Fields

        /// <summary>
        /// The persisted entity id values that have been cached for the
        /// DataView. This will be <c>null</c> until the values are loaded
        /// from the database or if the DataView is not persisted.
        /// </summary>
        private IReadOnlyCollection<int> _persistedEntityIds;

        #endregion

        #region Properties

        /// <inheritdoc cref="DataView.IsSystem"/>
        [DataMember]
        public bool IsSystem { get; private set; }

        /// <inheritdoc cref="DataView.Name"/>
        [DataMember]
        public string Name { get; private set; }

        /// <inheritdoc cref="DataView.Description"/>
        [DataMember]
        public string Description { get; private set; }

        /// <inheritdoc cref="DataView.CategoryId"/>
        [DataMember]
        public int? CategoryId { get; private set; }

        /// <inheritdoc cref="DataView.EntityTypeId"/>
        [DataMember]
        public int? EntityTypeId { get; private set; }

        /// <inheritdoc cref="DataView.DataViewFilterId"/>
        [DataMember]
        public int? DataViewFilterId { get; private set; }

        /// <inheritdoc cref="DataView.TransformEntityTypeId"/>
        [DataMember]
        public int? TransformEntityTypeId { get; private set; }

        /// <inheritdoc cref="DataView.PersistedScheduleIntervalMinutes"/>
        [DataMember]
        public int? PersistedScheduleIntervalMinutes { get; private set; }

        /// <inheritdoc cref="DataView.PersistedLastRefreshDateTime"/>
        [DataMember]
        public DateTime? PersistedLastRefreshDateTime { get; private set; }

        /// <inheritdoc cref="DataView.IncludeDeceased"/>
        [DataMember]
        public bool IncludeDeceased { get; private set; }

        /// <inheritdoc cref="DataView.PersistedLastRunDurationMilliseconds"/>
        [DataMember]
        public int? PersistedLastRunDurationMilliseconds { get; private set; }

        /// <inheritdoc cref="DataView.LastRunDateTime"/>
        [DataMember]
        public DateTime? LastRunDateTime { get; private set; }

        /// <inheritdoc cref="DataView.RunCount"/>
        [DataMember]
        public int? RunCount { get; private set; }

        /// <inheritdoc cref="DataView.TimeToRunDurationMilliseconds"/>
        [DataMember]
        public double? TimeToRunDurationMilliseconds { get; private set; }

        /// <inheritdoc cref="DataView.RunCountLastRefreshDateTime"/>
        [DataMember]
        public DateTime? RunCountLastRefreshDateTime { get; private set; }

        /// <inheritdoc cref="DataView.DisableUseOfReadOnlyContext"/>
        [DataMember]
        public bool DisableUseOfReadOnlyContext { get; private set; }

        /// <inheritdoc cref="DataView.PersistedScheduleId"/>
        [DataMember]
        public int? PersistedScheduleId { get; private set; }

        /// <inheritdoc cref="DataView.IconCssClass"/>
        [DataMember]
        public string IconCssClass { get; private set; }

        /// <inheritdoc cref="DataView.HighlightColor"/>
        [DataMember]
        public string HighlightColor { get; private set; }

        #endregion

        #region Navigation Properties

        /// <inheritdoc cref="DataView.Category"/>
        public CategoryCache Category => CategoryId.HasValue ? CategoryCache.Get( CategoryId.Value ) : null;

        /// <inheritdoc cref="DataView.EntityType"/>
        public EntityTypeCache EntityType => EntityTypeId.HasValue ? EntityTypeCache.Get( EntityTypeId.Value ) : null;

        /// <inheritdoc cref="DataView.TransformEntityType"/>
        public EntityTypeCache TransformEntityType => TransformEntityTypeId.HasValue ? EntityTypeCache.Get( TransformEntityTypeId.Value ) : null;

        /*
            2024-02-20 - DSH

            We intentionally don't include the DataViewFilter navigation property
            because it would be hard to determine when one of them was changed.
        */

        /// <inheritdoc cref="DataView.PersistedSchedule"/>
        public NamedScheduleCache PersistedSchedule => PersistedScheduleId.HasValue ? NamedScheduleCache.Get( PersistedScheduleId.Value ) : null;

        #endregion

        #region Methods

        /// <summary>
        /// Copies from model.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            if ( !( entity is DataView dataView ) )
            {
                return;
            }

            IsSystem = dataView.IsSystem;
            Name = dataView.Name;
            Description = dataView.Description;
            CategoryId = dataView.CategoryId;
            EntityTypeId = dataView.EntityTypeId;
            DataViewFilterId = dataView.DataViewFilterId;
            TransformEntityTypeId = dataView.TransformEntityTypeId;
            PersistedScheduleIntervalMinutes = dataView.PersistedScheduleIntervalMinutes;
            PersistedLastRefreshDateTime = dataView.PersistedLastRefreshDateTime;
            IncludeDeceased = dataView.IncludeDeceased;
            PersistedLastRunDurationMilliseconds = dataView.PersistedLastRunDurationMilliseconds;
            LastRunDateTime = dataView.LastRunDateTime;
            RunCount = dataView.RunCount;
            TimeToRunDurationMilliseconds = dataView.TimeToRunDurationMilliseconds;
            RunCountLastRefreshDateTime = dataView.RunCountLastRefreshDateTime;
            DisableUseOfReadOnlyContext = dataView.DisableUseOfReadOnlyContext;
            PersistedScheduleId = dataView.PersistedScheduleId;
            IconCssClass = dataView.IconCssClass;
            HighlightColor = dataView.HighlightColor;
        }

        /// <summary>
        /// Returns true if this DataView is configured to be Persisted.
        /// </summary>
        /// <returns><c>true</c> if this instance is persisted; otherwise, <c>false</c>.</returns>
        public bool IsPersisted()
        {
            return this.PersistedScheduleIntervalMinutes.HasValue || this.PersistedScheduleId.HasValue;
        }

        /// <summary>
        /// Gets the queryable that is generated by the DataView filters. This
        /// will automatically use the persisted values if they are configured
        /// and available. A new <see cref="RockContext"/> will be created to
        /// access the database.
        /// </summary>
        /// <returns>A queryable that contains the entities returned by the filters or <c>null</c> if the DataView is not valid.</returns>
        public IQueryable<IEntity> GetQueryable()
        {
            return GetQueryable( new RockContext() );
        }

        /// <summary>
        /// Gets the queryable that is generated by the DataView filters. This
        /// will automatically use the persisted values if they are configured
        /// and available.
        /// </summary>
        /// <param name="rockContext">The rock context to attach the query to.</param>
        /// <returns>A queryable that contains the entities returned by the filters or <c>null</c> if the DataView is not valid.</returns>
        public IQueryable<IEntity> GetQueryable( RockContext rockContext )
        {
            if ( IsPersisted() && PersistedLastRefreshDateTime.HasValue )
            {
                var entityType = EntityType?.GetEntityType();

                if ( entityType == null )
                {
                    return null;
                }

                var entityIdQry = rockContext.Set<DataViewPersistedValue>()
                    .Where( pv => pv.DataViewId == Id )
                    .Select( pv => pv.EntityId );

                var qry = Reflection.GetQueryableForEntityType( entityType, rockContext );

                return qry.Where( a => entityIdQry.Contains( a.Id ) );
            }
            else
            {
                var dataView = new DataViewService( rockContext ).Get( Id );

                // Shouldn't normally happen, but it's possible for the DataView
                // to be deleted while the cache object is currently being
                // accessed by some other code.
                if ( dataView == null )
                {
                    return null;
                }

                var getQueryArgs = new DataViewGetQueryArgs
                {
                    DatabaseTimeoutSeconds = 30
                };

                return dataView.GetQuery( getQueryArgs );
            }
        }

        /// <summary>
        /// Gets the entity identifiers represented by the DataView filters.
        /// This will automatically use the persisted values if they are
        /// configured and available. A new <see cref="RockContext"/> will be
        /// created to query the database with.
        /// </summary>
        /// <returns>A read-only collection of identifiers or <c>null</c> if the DataView is not valid.</returns>
        public IReadOnlyCollection<int> GetEntityIds()
        {
            RockContext rockContext = null;

            var ids = GetEntityIds( () =>
            {
                rockContext = new RockContext();

                return rockContext;
            } );

            rockContext?.Dispose();

            return ids;
        }

        /// <summary>
        /// Gets the entity identifiers represented by the DataView filters.
        /// This will automatically use the persisted values if they are
        /// configured and available.
        /// </summary>
        /// <param name="rockContext">The rock context to attach the query to.</param>
        /// <returns>A read-only collection of identifiers or <c>null</c> if the DataView is not valid.</returns>
        /// <exception cref="System.ArgumentNullException">rockContext</exception>
        public IReadOnlyCollection<int> GetEntityIds( RockContext rockContext )
        {
            if ( rockContext == null )
            {
                throw new ArgumentNullException( nameof( rockContext ) );
            }

            return GetEntityIds( () => rockContext );
        }

        /// <summary>
        /// Gets the entity identifiers represented by the DataView filters.
        /// This will automatically use the persisted values if they are
        /// configured and available.
        /// </summary>
        /// <param name="rockContextFactory">The factory that will give us the rock context if we need to query the database.</param>
        /// <returns>A read-only collection of identifiers or <c>null</c> if the DataView is not valid.</returns>
        private IReadOnlyCollection<int> GetEntityIds( Func<RockContext> rockContextFactory )
        {
            if ( IsPersisted() && PersistedLastRefreshDateTime.HasValue )
            {
                if ( _persistedEntityIds != null )
                {
                    return _persistedEntityIds;
                }

                var entityType = EntityType?.GetEntityType();

                if ( entityType == null )
                {
                    return null;
                }

                var rockContext = rockContextFactory();

                var idQry = rockContext.Set<DataViewPersistedValue>()
                    .Where( pv => pv.DataViewId == Id )
                    .Select( pv => pv.EntityId );

                _persistedEntityIds = new HashSet<int>( idQry );

                return _persistedEntityIds;
            }
            else
            {
                var rockContext = rockContextFactory();
                var dataView = new DataViewService( rockContext ).Get( Id );

                // Shouldn't normally happen, but it's possible for the DataView
                // to be deleted while the cache object is currently being
                // accessed by some other code.
                if ( dataView == null )
                {
                    return null;
                }

                var getQueryArgs = new DataViewGetQueryArgs
                {
                    DatabaseTimeoutSeconds = 30
                };

                return dataView.GetQuery( getQueryArgs )
                    .Select( a => a.Id )
                    .ToList();
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
