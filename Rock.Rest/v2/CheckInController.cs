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
using System.IO;
using System.Linq;
using System.Net;

using Microsoft.AspNetCore.Hosting;

using Rock.Rest.Filters;
using Rock.ViewModels.Utility;

namespace Rock.Rest.v2.Controllers
{
#if WEBFORMS
    using FromBodyAttribute = System.Web.Http.FromBodyAttribute;
    using IActionResult = System.Web.Http.IHttpActionResult;
    using RoutePrefixAttribute = System.Web.Http.RoutePrefixAttribute;
    using RouteAttribute = System.Web.Http.RouteAttribute;
    using HttpPostAttribute = System.Web.Http.HttpPostAttribute;
#endif

    /// <summary>
    /// Provides API interfaces for the Check-in system in Rock.
    /// </summary>
    /// <seealso cref="Rock.Rest.ApiControllerBase" />
    [RoutePrefix( "api/v2/checkin" )]
    [Rock.SystemGuid.RestControllerGuid( "52b3c68a-da8d-4374-a199-8bc8368a22bc" )]
    public sealed class CheckInController : ApiControllerBase
    {
        private readonly IWebHostEnvironment _webHostEnvironment;

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInController"/> class.
        /// </summary>
        /// <param name="webHostEnvironment">The web host environment.</param>
        public CheckInController( IWebHostEnvironment webHostEnvironment )
        {
            _webHostEnvironment = webHostEnvironment;
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
            var themeDirectory = Path.Combine( _webHostEnvironment.WebRootPath, "Themes" );
            var themeDirectoryInfo = new DirectoryInfo( themeDirectory );
            var themes = new List<ListItemBag>();

            foreach ( var themeDir in themeDirectoryInfo.EnumerateDirectories().OrderBy( a => a.Name ) )
            {
                themes.Add( new ListItemBag { Value = themeDir.Name.ToLower(), Text = themeDir.Name } );
            }

            return Ok( new GetConfigurationListResponseBag
            {
                Themes = themes
            } );
        }
    }

    public class GetConfigurationListOptionsBag
    {
    }

    public class GetConfigurationListResponseBag
    {
        public List<ListItemBag> Themes { get; set; }

        public List<CheckInItemBag> Kiosks { get; set; }

        public List<CheckInItemBag> Configurations { get; set; }

        public List<CheckInAreaBag> Areas { get; set; }
    }

    public class CheckInItemBag
    {
        public Guid Guid { get; set; }

        public string Name { get; set; }
    }

    public class CheckInAreaBag : CheckInItemBag
    {
        public Guid ConfigurationGuid { get; set; }
    }
}