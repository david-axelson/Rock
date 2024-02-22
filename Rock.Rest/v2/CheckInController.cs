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

using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;

using Rock.CheckIn.v2;
using Rock.Data;
using Rock.Model;
using Rock.Rest.Filters;
using Rock.ViewModels.CheckIn;
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
                var configData = configuration.GetCheckInConfiguration( _rockContext );

                var familyMembersQry = director.GetFamilyMembersForCheckInQuery( options.FamilyGuid, configuration );
                var familyMembers = director.GetFamilyMemberBags( options.FamilyGuid, familyMembersQry );
                var checkInOptions = director.GetAllCheckInOptions( areas, kiosk, null );

                var people = familyMembers
                    .Select( fm =>
                    {
                        var person = new CheckInFamilyMemberItem
                        {
                            Person = fm,
                            Options = checkInOptions.Clone()
                        };

                        director.FilterPersonOptions( person, configData );

                        return person;
                    } )
                    .ToList();

                director.SetDefaultSelectionsForPeople( people, configData );

                return Ok( new
                {
                    People = people
                } );
            }
            catch ( CheckInMessageException ex )
            {
                return BadRequest( ex.Message );
            }
        }

        #region Temporary Benchmark

        /// <summary>
        /// Performs a set of benchmark runs to determine timings.
        /// </summary>
        /// <returns>The results of the benchmarks.</returns>
        [HttpPost]
        [Authenticate]
        [Route( "Benchmark" )]
        [ProducesResponseType( HttpStatusCode.OK, Type = typeof( object ) )]
        [SystemGuid.RestActionGuid( "1eb635a9-6a6a-4445-a0a2-bb59a5a08982" )]
        public IActionResult PostBenchmark( [FromBody] BenchmarkOptionsBag options )
        {
            var configuration = GroupTypeCache.Get( options.ConfigurationGuid, _rockContext );
            var kiosk = DeviceCache.Get( options.KioskGuid, _rockContext );
            var areas = options.AreaGuids.Select( guid => GroupTypeCache.Get( guid, _rockContext ) ).ToList();
            var bench = new Rock.Utility.Performance.MicroBench();
            var validBenchmarks = new List<string> { "empty", "familySearch", "getFamilyMembers", "getFamilyMemberBags", "getAllCheckInOptions", "cloneOptions", "filterOptions" };

            bench.RepititionMode = Rock.Utility.Performance.RepititionMode.Fast;

            if ( configuration == null )
            {
                return BadRequest( "Configuration was not found." );
            }

            if ( kiosk == null )
            {
                return BadRequest( "Kiosk was not found." );
            }

            if ( options.Benchmarks.Count == 1 && options.Benchmarks[0] == "all" )
            {
                options.Benchmarks = validBenchmarks;
            }

            if ( options.Benchmarks.Any( b => !validBenchmarks.Contains( b ) ) )
            {
                return BadRequest( "Invalid benchmark specified." );
            }

            var results = new Dictionary<string, object>();

            foreach ( var benchmark in options.Benchmarks )
            {
                if ( benchmark == "empty" )
                {
                    var result = bench.Benchmark( () =>
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            var director = new CheckInDirector( rockContext );
                        }
                    } );

                    results.Add( benchmark, result.NormalizedStatistics.ToString() );
                }
                else if ( benchmark == "familySearch" )
                {
                    var result = bench.Benchmark( () =>
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            var director = new CheckInDirector( rockContext );

                            var families = director.SearchForFamilies( "5553322",
                                Enums.CheckIn.FamilySearchMode.PhoneNumber,
                                configuration,
                                null );
                        }
                    } );

                    results.Add( benchmark, result.NormalizedStatistics.ToString() );
                }
                else if ( benchmark == "getFamilyMembers" )
                {
                    var result = bench.Benchmark( () =>
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            var director = new CheckInDirector( rockContext );
                            var familyMembersQry = director.GetFamilyMembersForCheckInQuery( options.FamilyGuid, configuration );
                        }
                    } );

                    results.Add( benchmark, result.NormalizedStatistics.ToString() );
                }
                else if ( benchmark == "getFamilyMemberBags" )
                {
                    IEnumerable<GroupMember> familyMembers;
                    FamilyMemberBag familyMemberBag;

                    using ( var rockContext = new RockContext() )
                    {
                        var director = new CheckInDirector( rockContext );
                        var familyMembersQry = director.GetFamilyMembersForCheckInQuery( options.FamilyGuid, configuration );

                        familyMembers = familyMembersQry
                            .Include( fm => fm.Person )
                            .Include( fm => fm.Person.PrimaryFamily )
                            .Include( fm => fm.GroupRole )
                            .ToList();

                        familyMemberBag = director.GetFamilyMemberBags( options.FamilyGuid, familyMembers ).First( fm => fm.FirstName == "Noah" );
                    }

                    var result = bench.Benchmark( () =>
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            var director = new CheckInDirector( rockContext );

                            var bags = director.GetFamilyMemberBags( options.FamilyGuid, familyMembers );
                        }
                    } );

                    results.Add( benchmark, result.NormalizedStatistics.ToString() );
                }
                else if ( benchmark == "getAllCheckInOptions" )
                {
                    var result = bench.Benchmark( () =>
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            var director = new CheckInDirector( rockContext );

                            var checkInOptions = director.GetAllCheckInOptions( areas, kiosk, null );
                        }
                    } );

                    results.Add( benchmark, result.NormalizedStatistics.ToString() );
                }
                else if ( benchmark == "cloneOptions" )
                {
                    CheckInOptions mainCheckInOptions;

                    using ( var rockContext = new RockContext() )
                    {
                        var director = new CheckInDirector( rockContext );

                        mainCheckInOptions = director.GetAllCheckInOptions( areas, kiosk, null );
                    }

                    var result = bench.Benchmark( () =>
                    {
                        var clonedOptions = mainCheckInOptions.Clone();
                    } );

                    results.Add( benchmark, result.NormalizedStatistics.ToString() );
                }
                else if ( benchmark == "filterOptions" )
                {
                    CheckInOptions mainCheckInOptions;
                    var configData = configuration.GetCheckInConfiguration( _rockContext );
                    FamilyMemberBag familyMemberBag;

                    using ( var rockContext = new RockContext() )
                    {
                        var director = new CheckInDirector( rockContext );
                        var familyMembersQry = director.GetFamilyMembersForCheckInQuery( options.FamilyGuid, configuration );

                        familyMemberBag = director.GetFamilyMemberBags( options.FamilyGuid, familyMembersQry ).First( fm => fm.FirstName == "Noah" );
                        mainCheckInOptions = director.GetAllCheckInOptions( areas, kiosk, null );
                    }

                    var result = bench.Benchmark( () =>
                    {
                        using ( var rockContext = new RockContext() )
                        {
                            var director = new CheckInDirector( rockContext );

                            var person = new CheckInFamilyMemberItem
                            {
                                Person = familyMemberBag,
                                Options = mainCheckInOptions.Clone()
                            };

                            director.FilterPersonOptions( person, configData );
                        }
                    } );

                    results.Add( benchmark, result.NormalizedStatistics.ToString() );
                }
            }

            return Ok( results );
        }

        /// <summary>
        /// Temporary, used by benchmark action.
        /// </summary>
        public class BenchmarkOptionsBag : ListFamilyMembersOptionsBag
        {
            /// <summary>
            /// Gets or sets the benchmarks.
            /// </summary>
            /// <value>The benchmarks.</value>
            public List<string> Benchmarks { get; set; }
        }

        #endregion
    }
}