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
using System.Linq.Expressions;

using Rock.Data;
using Rock.Model;
using Rock.Observability;
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
        /// Gets the configuration summary bags for all valid check-in
        /// configurations.
        /// </summary>
        /// <returns>A colleciton of <see cref="ConfigurationItemSummaryBag"/> objects.</returns>
        public virtual List<ConfigurationItemSummaryBag> GetConfigurationSummaries()
        {
            return GetConfigurationTemplates( RockContext )
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
        /// a <paramref name="kiosk"/> or <paramref name="checkinTemplate"/> are
        /// provided then they will be used to filter the results to only areas
        /// valid for those items.
        /// </summary>
        /// <param name="kiosk">The optional kiosk to filter the results for.</param>
        /// <param name="checkinTemplate">The optional check-in template to filter all areas to.</param>
        /// <returns>A collection of <see cref="AreaItemSummaryBag"/> objects.</returns>
        public virtual List<AreaItemSummaryBag> GetCheckInAreaSummaries( DeviceCache kiosk, GroupTypeCache checkinTemplate )
        {
            var areas = new Dictionary<Guid, AreaItemSummaryBag>();
            List<GroupTypeCache> configurations;
            HashSet<int> kioskGroupTypeIds = null;

            // If the caller specified a configuration, then we return areas for
            // only that primary configuration. Otherwise we include areas from
            // all configurations.
            if ( checkinTemplate != null )
            {
                configurations = new List<GroupTypeCache> { checkinTemplate };
            }
            else
            {
                configurations = GetConfigurationTemplates( RockContext ).ToList();
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
        /// <returns>An instance of <see cref="CheckInOpportunities"/> that describes the available options.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="possibleAreas"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="kiosk"/> - Kiosk must be specified unless locations are specified.</exception>
        public CheckInOpportunities GetAllCheckInOptions( IReadOnlyCollection<GroupTypeCache> possibleAreas, DeviceCache kiosk, IReadOnlyCollection<NamedLocationCache> locations )
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

                return CheckInOpportunities.Create( possibleAreas, kiosk, locations, RockContext );
            }
        }

        /// <summary>
        /// Gets the check in coordinator that will be used for the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>DefaultCheckInCoordinator.</returns>
        public virtual DefaultCheckInCoordinator GetCheckInCoordinator( CheckInConfigurationData configuration )
        {
            return new DefaultCheckInCoordinator( this, configuration );
        }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Gets the group type areas that are valid for the kiosk device. Only group
        /// types associated via group and location to the kiosk will be returned.
        /// </summary>
        /// <param name="kiosk">The kiosk device.</param>
        /// <returns>An enumeration of <see cref="GroupTypeCache" /> objects.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="kiosk"/> is <c>null</c>.</exception>
        protected virtual IEnumerable<GroupTypeCache> GetKioskAreas( DeviceCache kiosk )
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
        /// Gets the configuration group types that are defined in the system.
        /// </summary>
        /// <param name="rockContext">The rock context to use if database access is required.</param>
        /// <returns>An enumeration of <see cref="GroupTypeCache"/> objects.</returns>
        /// <exception cref="Exception">Check-in Template Purpose was not found in the database, please check your installation.</exception>
        protected virtual IEnumerable<GroupTypeCache> GetConfigurationTemplates( RockContext rockContext )
        {
            var checkinTemplateTypeId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid(), rockContext )?.Id;

            if ( !checkinTemplateTypeId.HasValue )
            {
                throw new Exception( "Check-in Template Purpose was not found in the database, please check your installation." );
            }

            return GroupTypeCache.All( rockContext )
                .Where( t => t.GroupTypePurposeValueId.HasValue && t.GroupTypePurposeValueId == checkinTemplateTypeId.Value );
        }

        #endregion

        #region Internal Methods

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
    }
}
