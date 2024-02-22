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

using Rock.Data;
using Rock.Utility;

namespace Rock.CheckIn.v2.Filters
{
    /// <summary>
    /// A basic check-in filter.
    /// </summary>
    internal abstract class CheckInOptionsFilter : ICheckInOptionsFilter
    {
        #region Properties

        /// <inheritdoc/>
        public CheckInConfigurationData Configuration { get; set; }

        /// <inheritdoc/>
        public RockContext RockContext { get; set; }

        /// <inheritdoc/>
        public CheckInAttendeeItem Person { get; set; }

        /// <summary>
        /// Gets the person identifier.
        /// </summary>
        /// <value>The person identifier.</value>
        protected Lazy<int> PersonId { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInOptionsFilter"/> class.
        /// </summary>
        public CheckInOptionsFilter()
        {
            PersonId = new Lazy<int>( () => IdHasher.Instance.GetId( Person.Person.IdKey ) ?? 0 );
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public virtual bool IsGroupValid( CheckInGroupItem group )
        {
            return true;
        }

        /// <inheritdoc/>
        public virtual bool IsLocationValid( CheckInLocationItem location )
        {
            return true;
        }

        /// <inheritdoc/>
        public virtual bool IsScheduleValid( CheckInScheduleItem schedule )
        {
            return true;
        }

        #endregion
    }
}
