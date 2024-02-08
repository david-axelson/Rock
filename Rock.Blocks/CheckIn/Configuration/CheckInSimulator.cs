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
using System.ComponentModel;
using System.Linq;

using Rock.Attribute;
using Rock.CheckIn.v2;
using Rock.Data;
using Rock.Enums.CheckIn;
using Rock.ViewModels.CheckIn;
using Rock.ViewModels.Utility;
using Rock.Web.Cache;

namespace Rock.Blocks.CheckIn.Configuration
{
    /// <summary>
    /// Designs a check-in label with a nice drag and drop experience.
    /// </summary>

    [DisplayName( "Check-in Simulator" )]
    [Category( "Check-in > Configuration" )]
    [Description( "Simulates the check-in process in a UI that can be used to quickly test different configuration settings." )]
    [IconCssClass( "fa fa-vial" )]
    [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "23316388-ec1d-495a-8efb-c1b5f6806041" )]
    [Rock.SystemGuid.BlockTypeGuid( "30002636-494b-4fdc-848c-a816f9291764" )]
    public class CheckInSimulator : RockBlockType
    {
        public override object GetObsidianBlockInitialization()
        {
            using ( var rockContext = new RockContext() )
            {
                var kioskDeviceValueId = DefinedValueCache.Get( SystemGuid.DefinedValue.DEVICE_TYPE_CHECKIN_KIOSK.AsGuid(), rockContext ).Id;
                var director = new CheckInDirector( rockContext );

                return new CheckInSimulatorOptionsBag
                {
                    Configurations = director.GetConfigurationSummaries(),
                    Kiosks = DeviceCache.All()
                        .Where( d => d.DeviceTypeValueId == kioskDeviceValueId )
                        .OrderBy( d => d.Name )
                        .Select( d => new ListItemBag
                        {
                            Value = d.Guid.ToString(),
                            Text = d.Name
                        } )
                        .ToList()
                };
            }
        }

        [BlockAction]
        public BlockActionResult GetAreas( Guid kioskGuid )
        {
            using ( var rockContext = new RockContext() )
            {
                var director = new CheckInDirector( rockContext );
                var kiosk = DeviceCache.Get( kioskGuid, rockContext );

                try
                {
                    return ActionOk( director.GetCheckInAreaSummaries( kiosk, null ) );
                }
                catch ( CheckInDirectorException ex )
                {
                    return ActionBadRequest( ex.Message );
                }
            }
        }

        [BlockAction]
        public BlockActionResult SearchForFamilies( string searchTerm, FamilySearchMode searchType, Guid configurationGuid, Guid kioskGuid )
        {
            using ( var rockContext = new RockContext() )
            {
                var director = new CheckInDirector( rockContext );
                var configuration = GroupTypeCache.Get( configurationGuid, rockContext );
                var kiosk = DeviceCache.Get( kioskGuid, rockContext );
                var campusId = kiosk.GetCampusId();
                CampusCache sortByCampus = campusId.HasValue ? CampusCache.Get( campusId.Value, rockContext ) : null;

                try
                {
                    return ActionOk( director.SearchForFamilies( searchTerm, searchType, configuration, sortByCampus ) );
                }
                catch ( CheckInDirectorException ex )
                {
                    return ActionBadRequest( ex.Message );
                }
            }
        }

        private class CheckInSimulatorOptionsBag
        {
            public List<ConfigurationItemSummaryBag> Configurations { get; set; }

            public List<ListItemBag> Kiosks { get; set; }
        }
    }
}
