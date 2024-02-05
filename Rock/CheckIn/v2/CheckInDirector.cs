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

using Rock.Data;
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

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the configuration group types that are defined in the system.
        /// </summary>
        /// <returns>An enumeration of <see cref="GroupTypeCache"/> objects.</returns>
        /// <exception cref="Exception">Check-in Template Purpose was not found in the database, please check your installation.</exception>
        internal IEnumerable<GroupTypeCache> GetConfigurationGroupTypes()
        {
            var checkinTemplateTypeId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid() );

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
                .SelectMany( id => GroupLocationCache.AllForLocationId( id ) )
                .DistinctBy( glc => glc.Id )
                .ToList();

            // Get the distinct group types for those group locations that have
            // attendance enabled.
            return groupLocations
                .Select( gl => GroupCache.Get( gl.GroupId )?.GroupTypeId )
                .Where( id => id.HasValue )
                .Distinct()
                .Select( id => GroupTypeCache.Get( id.Value ) )
                .Where( gt => gt != null && gt.TakesAttendance )
                .ToList();
        }

        #endregion
    }
}
