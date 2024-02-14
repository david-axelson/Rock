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

using System.Linq;
using System.Net;

using Rock.CheckIn.v2;
using Rock.Data;
using Rock.Rest.Filters;
using Rock.ViewModels.Rest.CheckIn;
using Rock.Web.Cache;

namespace Rock.Rest.v2.Controllers
{
#if WEBFORMS
    using FromBodyAttribute = System.Web.Http.FromBodyAttribute;
    using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
    using IActionResult = System.Web.Http.IHttpActionResult;
    using RouteAttribute = System.Web.Http.RouteAttribute;
    using RoutePrefixAttribute = System.Web.Http.RoutePrefixAttribute;
#endif

    /// <summary>
    /// Provides API interfaces for the Check-in system in Rock.
    /// </summary>
    /// <seealso cref="Rock.Rest.ApiControllerBase" />
    [RoutePrefix( "api/v2/checkin" )]
    [Rock.SystemGuid.RestControllerGuid( "52b3c68a-da8d-4374-a199-8bc8368a22bc" )]
    public sealed class CheckInController : ApiControllerBase
    {
        private readonly RockContext _rockContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInController"/> class.
        /// </summary>
        /// <param name="rockContext">The database context to use for this request.</param>
        public CheckInController( RockContext rockContext )
        {
            _rockContext = rockContext;
        }

        /// <summary>
        /// Gets the configuration items available to be selected.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>A bag that contains all the configuration items.</returns>
        [HttpPost]
        [Authenticate]
        //[Secured]
        [Route( "ListConfigurations" )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( ListConfigurationsResponseBag ) )]
        [SystemGuid.RestActionGuid( "200dd82f-6532-4437-9ba4-a289408b0eb8" )]
        public IActionResult PostListConfigurations( [FromBody] ListConfigurationsOptionsBag options )
        {
            var director = new CheckInDirector( _rockContext );
            DeviceCache kiosk = null;

            if ( options.KioskGuid.HasValue )
            {
                kiosk = DeviceCache.Get( options.KioskGuid.Value );

                if ( kiosk == null )
                {
                    return BadRequest( "Kiosk was not found." );
                }
            }

            try
            {
                return Ok( new ListConfigurationsResponseBag
                {
                    Configurations = director.GetConfigurationSummaries(),
                    Areas = director.GetCheckInAreaSummaries( kiosk, null )
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Performs a search for matching families that are valid for check-in.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>A bag that contains all the matched families.</returns>
        [HttpPost]
        [Authenticate]
        //[Secured]
        [Route( "SearchForFamilies" )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( SearchForFamiliesResponseBag ) )]
        [SystemGuid.RestActionGuid( "2c587733-0e08-4e93-8f2b-3e2518362768" )]
        public IActionResult PostSearchForFamilies( [FromBody] SearchForFamiliesOptionsBag options )
        {
            var configuration = GroupTypeCache.Get( options.ConfigurationGuid, _rockContext );
            var director = new CheckInDirector( _rockContext );
            CampusCache sortByCampus = null;

            if ( configuration == null )
            {
                return BadRequest( "Configuration was not found." );
            }

            if ( options.KioskGuid.HasValue && options.PrioritizeKioskCampus )
            {
                var kiosk = DeviceCache.Get( options.KioskGuid.Value );

                if ( kiosk == null )
                {
                    return BadRequest( "Kiosk was not found." );
                }

                var campusId = kiosk.GetCampusId();

                if ( campusId.HasValue )
                {
                    sortByCampus = CampusCache.Get( campusId.Value, _rockContext );
                }
            }

            try
            {
                var families = director.SearchForFamilies( options.SearchTerm,
                    options.SearchType,
                    configuration,
                    sortByCampus );

                return Ok( new SearchForFamiliesResponseBag
                {
                    Families = families
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        /// <summary>
        /// Performs a search for matching families that are valid for check-in.
        /// </summary>
        /// <param name="options">The options that describe the request.</param>
        /// <returns>A bag that contains all the matched families.</returns>
        [HttpPost]
        [Authenticate]
        //[Secured]
        [Route( "ListFamilyMembers" )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( object ) )]
        [SystemGuid.RestActionGuid( "2bd5afdf-da57-48bb-a6db-7dd9ad1ab8da" )]
        public IActionResult PostListFamilyMembers( [FromBody] ListFamilyMembersOptionsBag options )
        {
            var configuration = GroupTypeCache.Get( options.ConfigurationGuid, _rockContext );
            var kiosk = DeviceCache.Get( options.KioskGuid, _rockContext );
            var director = new CheckInDirector( _rockContext );

            if ( configuration == null )
            {
                return BadRequest( "Configuration was not found." );
            }

            if ( kiosk == null )
            {
                return BadRequest( "Kiosk was not found." );
            }

            try
            {
                var areas = options.AreaGuids.Select( guid => GroupTypeCache.Get( guid, _rockContext ) ).ToList();
                var familyMembersQry = director.GetFamilyMembersForCheckInQuery( options.FamilyGuid, configuration );
                var familyMembers = director.GetFamilyMemberBags( options.FamilyGuid, familyMembersQry );

                var checkInOptions = director.GetAllCheckInOptions( areas, kiosk, null );

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var configData = configuration.GetCheckInConfiguration( _rockContext );
                for ( int i = 0; i < 1000; i++ )
                {
                    var clonedOptions = director.CloneOptions( checkInOptions );
                    director.FilterOptionsForPerson( clonedOptions, familyMembers[0], configData );
                }
                sw.Stop();

                return Ok( new
                {
                    Time = sw.Elapsed.TotalMilliseconds,
                    Members = familyMembers,
                    Options = checkInOptions
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }
    }
}