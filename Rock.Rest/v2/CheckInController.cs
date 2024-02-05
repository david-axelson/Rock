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
        [Route( "GetConfigurationList" )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( GetConfigurationListResponseBag ) )]
        [SystemGuid.RestActionGuid( "200dd82f-6532-4437-9ba4-a289408b0eb8" )]
        public IActionResult PostGetConfigurationList( [FromBody] GetConfigurationListOptionsBag options )
        {
            var director = new CheckInDirector( _rockContext );
            DeviceCache kiosk = null;

            if ( options.Kiosk.HasValue )
            {
                kiosk = DeviceCache.Get( options.Kiosk.Value );

                if ( kiosk == null )
                {
                    return BadRequest( "Kiosk was not found." );
                }
            }

            return Ok( new GetConfigurationListResponseBag
            {
                Configurations = director.GetConfigurationSummaries(),
                Areas = director.GetCheckInAreaSummaries( kiosk, null )
            } );
        }
    }
}