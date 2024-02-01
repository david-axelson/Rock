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
using System.Collections.Concurrent;
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
    public class GroupLocationCache : ModelCache<GroupLocationCache, Rock.Model.GroupLocation>
    {
        #region Fields

        /// <summary>
        /// <c>true</c> if this is for a named location.
        /// </summary>
        private bool _isNamedLocation;

        /// <summary>
        /// Tracks the cached "all item ids" lists per location.
        /// </summary>
        private static readonly AlternateIdListCache<GroupLocationCache, int> _byLocationIdCache = new AlternateIdListCache<GroupLocationCache, int>( "location" );

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
        /// Gets all cache objects for the specified location.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns>A list of <see cref="GroupLocationCache"/> objects.</returns>
        public static List<GroupLocationCache> AllForLocationId( int locationId, RockContext rockContext = null )
        {
            if ( rockContext != null )
            {
                var keys = _byLocationIdCache.GetOrAddKeys( locationId, locId => QueryDbForLocationId( locId, rockContext ) );

                return GetMany( keys.AsIntegerList(), rockContext ).ToList();
            }
            else
            {
                using ( var newRockContext = new RockContext() )
                {
                    var keys = _byLocationIdCache.GetOrAddKeys( locationId, locId => QueryDbForLocationId( locId, newRockContext ) );

                    return GetMany( keys.AsIntegerList(), rockContext ).ToList();
                }
            }
        }

        /// <summary>
        /// Queries the database for all group location keys for the
        /// given location.
        /// </summary>
        /// <param name="locationId">The location identifier.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <returns>A collection of group locations.</returns>
        private static List<string> QueryDbForLocationId( int locationId, RockContext rockContext )
        {
            var service = new GroupLocationService( rockContext );

            return service.Queryable()
                .AsNoTracking()
                .Include( gl => gl.Location )
                .Include( gl => gl.Schedules )
                .Where( gl => gl.LocationId == locationId )
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

        /// <summary>
        /// Removes or invalidates the CachedItem based on EntityState
        /// </summary>
        /// <param name="entityId">The entity identifier.</param>
        /// <param name="entityState">State of the entity. If unknown, use <see cref="EntityState.Detached" /></param>
        public static new void UpdateCachedEntity( int entityId, EntityState entityState )
        {
            throw new NotSupportedException( "Do not call UpdateCachedEntity on GroupLocationCache with an entity identifier." );
        }

        /// <summary>
        /// Removes or invalidates the GroupLocationCache based on <paramref name="entityState"/>.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="entityState">State of the entity. If unknown, use <see cref="EntityState.Detached" /></param>
        public static void UpdateCachedEntity( GroupLocation entity, EntityState entityState )
        {
            if ( entityState == EntityState.Deleted )
            {
                Remove( entity );
            }
            else if ( entityState == EntityState.Added )
            {
                // add this entity to All Ids, but don't fetch it into cache until somebody asks for it
                AddToAllIds( entity );
            }
            else
            {
                FlushItem( entity.Id );
            }
        }

        /// <summary>
        /// This method is not supported on GroupLocationCache, call the method
        /// that takes a <see cref="GroupLocation"/> parameter.
        /// </summary>
        /// <param name="key">The key.</param>
        public static new void Remove( int key )
        {
            throw new NotSupportedException( "Do not call Remove on GroupLocationCache with an entity identifier." );
        }

        /// <summary>
        /// This method is not supported on GroupLocationCache, call the method
        /// that takes a <see cref="GroupLocation"/> parameter.
        /// </summary>
        /// <param name="key">The key.</param>
        public static new void Remove( string key )
        {
            throw new NotSupportedException( "Do not call Remove on GroupLocationCache with a cache key." );
        }

        /// <summary>
        /// Removes the related cache for the entity.
        /// </summary>
        /// <param name="entity">The entity whose cache data should be removed.</param>
        public static void Remove( GroupLocation entity )
        {
            var key = entity.Id.ToString();

            ItemCache<GroupLocationCache>.Remove( key );
            _byLocationIdCache.Remove( key, entity.LocationId );
        }

        /// <summary>
        /// Adds a new entity to the "all ids" lists.
        /// </summary>
        /// <param name="entity">The entity whose cache data should be added.</param>
        public static void AddToAllIds( GroupLocation entity )
        {
            var key = entity.Id.ToString();

            ItemCache<GroupLocationCache>.AddToAllIds( key );
            _byLocationIdCache.Add( key, entity.LocationId );
        }

        //private static void AddToAllIds( string key, string allKey, Func<AllIdList> keyFactory = null )
        //{
        //    // Get the list of all item ids.
        //    var allKeys = RockCacheManager<AllIdList>.Instance.Get( allKey );

        //    if ( allKeys != null && allKeys.Contains( key ) )
        //    {
        //        // Already has it so no need to update the cache.
        //        return;
        //    }

        //    if ( allKeys == null )
        //    {
        //        // If the list doesn't exist then see if we can get it using the delegate
        //        if ( keyFactory != null )
        //        {
        //            allKeys = keyFactory();

        //            if ( allKeys != null )
        //            {
        //                RockCacheManager<AllIdList>.Instance.AddOrUpdate( allKey, allKeys );
        //            }
        //        }

        //        // At this point the method has all the data that is possible
        //        // to get if there are no current keys stored in the cache, so return.
        //        return;
        //    }

        //    // The key is not part of the list so add it and update the cache
        //    lock ( _obj )
        //    {
        //        // Add it.
        //        allKeys.Add( key, true );
        //        RockCacheManager<List<string>>.Instance.AddOrUpdate( AllKey, _AllRegion, allKeys );
        //    }
        //}

        /// <summary>
        /// Removes all items of this type from cache.
        /// </summary>
        public static new void Clear()
        {
            ItemCache<GroupLocationCache>.Clear();
            _byLocationIdCache.Clear();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Group {GroupId} at {Location.ToStringSafe()}";
        }

        #endregion Public Methods

        /// <summary>
        /// Handles tracking an alternate list of identifiers beyond the standard
        /// "all items" cached list.
        /// </summary>
        /// <typeparam name="TCache">The type of cached object.</typeparam>
        /// <typeparam name="TListKey">The type of alternate identifier key.</typeparam>
        internal class AlternateIdListCache<TCache, TListKey>
        {
            /// <summary>
            /// The key prefix that will be used for the set of alternate
            /// identifier lists.
            /// </summary>
            private readonly string _keyPrefix;

            /// <summary>
            /// The lock object that will be used for synchronizing the
            /// modification of the key lists.
            /// </summary>
            private readonly object _keyListLock = new object();

            /// <summary>
            /// Initializes a new instance of the <see cref="AlternateIdListCache{TCache, TListKey}"/> class.
            /// </summary>
            /// <param name="keyPrefix">The key prefix for these alternate identifier lists.</param>
            public AlternateIdListCache( string keyPrefix )
            {
                _keyPrefix = keyPrefix;
            }

            /// <summary>
            /// Adds the key to the id list.
            /// </summary>
            /// <param name="key">The key to be added.</param>
            /// <param name="listKey">The key for the specific alternate identifier list.</param>
            /// <param name="keyFactory">The key factory to load all keys if it has not already been cached.</param>
            public void Add( string key, TListKey listKey, Func<TListKey, List<string>> keyFactory = null )
            {
                // Get the list of all item ids.
                var allKeys = RockCacheManager<AllIdList<TCache>>.Instance.Get( $"{_keyPrefix}:{listKey}" );

                if ( allKeys != null && allKeys.Keys.Contains( key ) )
                {
                    // Already has it so no need to update the cache.
                    return;
                }

                if ( allKeys == null )
                {
                    // If the list doesn't exist then see if we can get it using the delegate
                    if ( keyFactory != null )
                    {
                        allKeys = new AllIdList<TCache>( keyFactory( listKey ) );

                        if ( allKeys != null )
                        {
                            RockCacheManager<AllIdList<TCache>>.Instance.AddOrUpdate( _keyPrefix, allKeys );
                        }
                    }

                    // At this point the method has all the data that is possible
                    // to get if there are no current keys stored in the cache, so return.
                    return;
                }

                // The key is not part of the list so add it and update the cache.
                lock ( _keyListLock )
                {
                    // Add it.
                    allKeys.Keys.Add( key, true );
                    RockCacheManager<AllIdList<TCache>>.Instance.AddOrUpdate( _keyPrefix, allKeys );
                }
            }

            /// <summary>
            /// Adds the key to the id list.
            /// </summary>
            /// <param name="listKey">The key for the specific alternate identifier list.</param>
            /// <param name="keyFactory">The key factory to load all keys if it has not already been cached.</param>
            public IReadOnlyCollection<string> GetOrAddKeys( TListKey listKey, Func<TListKey, List<string>> keyFactory )
            {
                // Get the list of all item ids.
                var allKeys = RockCacheManager<AllIdList<TCache>>.Instance.Get( $"{_keyPrefix}:{listKey}" );

                if ( allKeys != null )
                {
                    return allKeys.Keys;
                }

                var keys = keyFactory( listKey );

                if ( keys != null )
                {
                    RockCacheManager<AllIdList<TCache>>.Instance.AddOrUpdate( $"{_keyPrefix}:{listKey}", new AllIdList<TCache>( keys ) );

                    return keys;
                }

                return new List<string>();
            }

            /// <summary>
            /// Removes the specified key from id list. This should be called
            /// when the key is no longer valid in the database and will not
            /// return.
            /// </summary>
            /// <param name="key">The key to be removed.</param>
            /// <param name="listKey">The key for the specific alternate identifier list.</param>
            public void Remove( string key, TListKey listKey )
            {
                var allIds = RockCacheManager<AllIdList<TCache>>.Instance.Get( $"{_keyPrefix}:{listKey}" );

                if ( allIds == null || !allIds.Keys.Contains( key ) )
                {
                    return;
                }

                lock ( _keyListLock )
                {
                    allIds.Keys.Remove( key );
                    RockCacheManager<AllIdList<TCache>>.Instance.AddOrUpdate( $"{_keyPrefix}:{listKey}", allIds );
                }
            }

            /// <summary>
            /// Clears all alternate list keys.
            /// </summary>
            public void Clear()
            {
                RockCacheManager<AllIdList<TCache>>.Instance.Clear();
            }

            /// <summary>
            /// Clears the list of keys from the cached list.
            /// </summary>
            /// <param name="listKey">The key for the specific alternate identifier list.</param>
            public void Clear( TListKey listKey )
            {
                RockCacheManager<AllIdList<TCache>>.Instance.Remove( $"{_keyPrefix}:{listKey}" );
            }

            /// <summary>
            /// This is a helper class to ensure that all our cached items are
            /// scoped to us. If we just cached a generic List&lt;string&gt;
            /// we would conflict with other cached items.
            /// </summary>
            /// <typeparam name="TItemCache">The type of the cached item.</typeparam>
            private class AllIdList<TItemCache>
            {
                public List<string> Keys { get; }

                public AllIdList()
                {
                    Keys = new List<string>();
                }

                public AllIdList( List<string> keys )
                {
                    Keys = keys;
                }
            }
        }
    }
}
