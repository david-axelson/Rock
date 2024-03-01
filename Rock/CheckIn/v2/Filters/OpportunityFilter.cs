﻿// <copyright>
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

using Rock.Data;
using Rock.Utility;

namespace Rock.CheckIn.v2.Filters
{
    /// <summary>
    /// A basic check-in filter.
    /// </summary>
    internal abstract class OpportunityFilter
    {
        #region Properties

        /// <summary>
        /// Gets or sets the check-in template configuration.
        /// </summary>
        /// <value>The check-in template configuration.</value>
        public TemplateConfigurationData TemplateConfiguration { get; set; }

        /// <summary>
        /// Gets or sets the check-in session.
        /// </summary>
        /// <value>The check-in session.</value>
        public CheckInSession Session { get; set; }

        /// <summary>
        /// Gets or sets the person to filter the opportunities for.
        /// </summary>
        /// <value>The person to filter the opportunities for.</value>
        public Attendee Person { get; set; }

        /// <summary>
        /// Gets the context to use if database access is needed.
        /// </summary>
        /// <value>The context to use if database access is needed.</value>
        public RockContext RockContext => Session.RockContext;

        /// <summary>
        /// Gets the person identifier.
        /// </summary>
        /// <value>The person identifier.</value>
        protected Lazy<int> PersonId { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="OpportunityFilter"/> class.
        /// </summary>
        public OpportunityFilter()
        {
            PersonId = new Lazy<int>( () => IdHasher.Instance.GetId( Person.Person.IdKey ) ?? 0 );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Determines whether the specified group is valid for check-in.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns><c>true</c> if the group is valid; otherwise, <c>false</c>.</returns>
        public virtual bool IsGroupValid( GroupOpportunity group )
        {
            return true;
        }

        /// <summary>
        /// Determines whether the specified location is valid for check-in.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns><c>true</c> if the location is valid; otherwise, <c>false</c>.</returns>
        public virtual bool IsLocationValid( LocationOpportunity location )
        {
            return true;
        }

        /// <summary>
        /// Determines whether the specified schedule is valid for check-in.
        /// </summary>
        /// <param name="schedule">The schedule.</param>
        /// <returns><c>true</c> if the schedule is valid; otherwise, <c>false</c>.</returns>
        public virtual bool IsScheduleValid( ScheduleOpportunity schedule )
        {
            return true;
        }

        #endregion
    }
}
