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

using Rock.Utility;
using Rock.ViewModels.CheckIn;

namespace Rock.CheckIn.v2.Filters
{
    /// <summary>
    /// A basic check-in filter that includes the person being filtered for.
    /// </summary>
    internal abstract class CheckInPersonOptionsFilter : CheckInOptionsFilter, ICheckInPersonOptionsFilter
    {
        #region Properties

        /// <inheritdoc/>
        public FamilyMemberBag Person { get; set; }

        /// <summary>
        /// Gets the person identifier.
        /// </summary>
        /// <value>The person identifier.</value>
        protected Lazy<int> PersonId { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInPersonOptionsFilter"/> class.
        /// </summary>
        public CheckInPersonOptionsFilter()
        {
            PersonId = new Lazy<int>( () => IdHasher.Instance.GetId( Person.IdKey ) ?? 0 );
        }

        #endregion
    }
}
