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

using Rock.CheckIn.v2.Filters;
using Rock.Observability;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Provides the logic for filtering options in check-in.
    /// </summary>
    internal class DefaultOptionsFilterProvider
    {
        #region Fields

        /// <summary>
        /// The default group filter types.
        /// </summary>
        private static readonly List<Type> _defaultGroupFilterTypes = new List<Type>
        {
            typeof( CheckInByAgeOptionsFilter ),
            typeof( CheckInByGradeOptionsFilter ),
            typeof( CheckInByGenderOptionsFilter ),
            typeof( CheckInByMembershipOptionsFilter ),
            typeof( CheckInByDataViewOptionsFilter )
        };

        /// <summary>
        /// The default location filter types.
        /// </summary>
        private static readonly List<Type> _defaultLocationFilterTypes = new List<Type>
        {
            typeof( CheckInThresholdOptionsFilter )
        };

        /// <summary>
        /// The default schedule filter types.
        /// </summary>
        private static readonly List<Type> _defaultScheduleFilterTypes = new List<Type>
        {
            typeof( CheckInOptionsDuplicateCheckInFilter )
        };

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the check-in configuration in effect during filtering.
        /// </summary>
        /// <value>The check-in configuration.</value>
        protected CheckInConfigurationData Configuration { get; }

        /// <summary>
        /// Gets or sets the check-in director.
        /// </summary>
        /// <value>The check-in director.</value>
        protected CheckInDirector Director { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultOptionsFilterProvider"/> class.
        /// </summary>
        /// <param name="director">The check-in director.</param>
        /// <param name="configuration">The check-in configuration.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="director"/> is <c>null</c>.</exception>
        /// <exception cref="System.ArgumentNullException"><paramref name="configuration"/> is <c>null</c>.</exception>
        public DefaultOptionsFilterProvider( CheckInDirector director, CheckInConfigurationData configuration )
        {
            if ( director == null )
            {
                throw new ArgumentNullException( nameof( director ) );
            }

            if ( configuration == null )
            {
                throw new ArgumentNullException( nameof( configuration ) );
            }

            Director = director;
            Configuration = configuration;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Filters the check-in options for a single person.
        /// </summary>
        /// <param name="person">The person to use when filtering options.</param>
        public virtual void FilterPersonOptions( CheckInAttendeeItem person )
        {
            using ( var activity = ObservabilityHelper.StartActivity( $"Get Options For {person.Person.NickName}" ) )
            {
                var groupFilters = GetGroupFilters( person );

                if ( groupFilters.Count > 0 )
                {
                    person.Options.Groups
                        .RemoveAll( g => groupFilters.Any( f => !f.IsGroupValid( g ) ) );
                }

                var locationFilters = GetLocationFilters(  person );

                if ( locationFilters.Count > 0 )
                {
                    person.Options.Locations
                        .RemoveAll( l => locationFilters.Any( f => !f.IsLocationValid( l ) ) );
                }

                var scheduleFilters = GetScheduleFilters(  person );

                if ( scheduleFilters.Count > 0 )
                {
                    person.Options.Schedules
                        .RemoveAll( l => scheduleFilters.Any( f => !f.IsScheduleValid( l ) ) );
                }
            }
        }

        /// <summary>
        /// Removes any option items that are "empty". Meaning, if a group has
        /// no locations then it can't be available as a choice so it will be
        /// removed.
        /// </summary>
        /// <param name="person">The person whose options should be cleaned up.</param>
        public virtual void RemoveEmptyOptions( CheckInAttendeeItem person )
        {
            person.Options.RemoveEmptyOptions();
        }

        /// <summary>
        /// Gets the filter type definitions to use when filtering options for
        /// groups.
        /// </summary>
        /// <returns>A collection of <see cref="Type"/> objects.</returns>
        protected virtual IReadOnlyCollection<Type> GetGroupFilterTypes()
        {
            return _defaultGroupFilterTypes;
        }

        /// <summary>
        /// Gets the filter type definitions to use when filtering options for
        /// locations.
        /// </summary>
        /// <returns>A collection of <see cref="Type"/> objects.</returns>
        protected virtual IReadOnlyCollection<Type> GetLocationFilterTypes()
        {
            return _defaultLocationFilterTypes;
        }

        /// <summary>
        /// Gets the filter type definitions to use when filtering options for
        /// schedules.
        /// </summary>
        /// <returns>A collection of <see cref="Type"/> objects.</returns>
        protected virtual IReadOnlyCollection<Type> GetScheduleFilterTypes()
        {
            return _defaultScheduleFilterTypes;
        }

        /// <summary>
        /// Gets the filters to use when filtering options for a specific group.
        /// </summary>
        /// <param name="person">The person to filter options for.</param>
        /// <returns>A list of <see cref="ICheckInOptionsFilter"/> objects that will perform filtering logic.</returns>
        private List<ICheckInOptionsFilter> GetGroupFilters( CheckInAttendeeItem person )
        {
            var types = GetGroupFilterTypes();

            return CreateOptionsFilters( types, person );
        }

        /// <summary>
        /// Gets the filters to use when filtering options for a specific location.
        /// </summary>
        /// <param name="person">The person to filter options for.</param>
        /// <returns>A list of <see cref="ICheckInOptionsFilter"/> objects that will perform filtering logic.</returns>
        private List<ICheckInOptionsFilter> GetLocationFilters( CheckInAttendeeItem person )
        {
            var types = GetLocationFilterTypes();

            return CreateOptionsFilters( types, person );
        }

        /// <summary>
        /// Gets the filters to use when filtering options for a specific
        /// schedule.
        /// </summary>
        /// <param name="person">The person to filter options for.</param>
        /// <returns>A list of <see cref="ICheckInOptionsFilter"/> objects that will perform filtering logic.</returns>
        private List<ICheckInOptionsFilter> GetScheduleFilters( CheckInAttendeeItem person )
        {
            var types = GetScheduleFilterTypes();

            return CreateOptionsFilters( types, person );
        }

        /// <summary>
        /// Creates the options filters specified by the types. This filters will
        /// be properly initialized before returning.
        /// </summary>
        /// <param name="filterTypes">The filter types.</param>
        /// <param name="person">The person to filter for.</param>
        /// <returns>A collection of filter instances.</returns>
        private List<ICheckInOptionsFilter> CreateOptionsFilters( IReadOnlyCollection<Type> filterTypes, CheckInAttendeeItem person )
        {
            var expectedType = typeof( ICheckInOptionsFilter );

            return filterTypes
                .Where( t => expectedType.IsAssignableFrom( t ) )
                .Select( t =>
                {
                    var filter = ( ICheckInOptionsFilter ) Activator.CreateInstance( t );

                    filter.Configuration = Configuration;
                    filter.RockContext = Director.RockContext;
                    filter.Person = person;

                    return filter;
                } )
                .ToList();
        }

        #endregion
    }
}
