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

using Rock.Model;

namespace Rock.CheckIn.v2.Filters
{
    /// <summary>
    /// Performs filtering of check-in options based on the person's membership
    /// in the group.
    /// </summary>
    internal class CheckInByMembershipOptionsFilter : CheckInPersonOptionsFilter
    {
        #region Properties

        /// <summary>
        /// Gets the group unique identifiers that the person is an active
        /// member of.
        /// </summary>
        /// <value>The group unique identifiers.</value>
        public Lazy<HashSet<Guid>> GroupGuids { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInByMembershipOptionsFilter"/> class.
        /// </summary>
        public CheckInByMembershipOptionsFilter()
        {
            // Make sure we only load the group unique identifiers once. Because
            // this is lazy initialized, it will only load the data if we come
            // across any group with an AlreadyBelongs attendance rule.
            GroupGuids = new Lazy<HashSet<Guid>>( () =>
            {
                var groupGuidQry = new GroupMemberService( RockContext )
                    .Queryable()
                    .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active && m.Person.Guid == Person.Guid )
                    .Select( m => m.Group.Guid );

                return new HashSet<Guid>( groupGuidQry );
            } );
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override bool IsGroupValid( CheckInGroupItem group )
        {
            if ( group.CheckInAreaData.AttendanceRule == AttendanceRule.AlreadyBelongs )
            {
                return GroupGuids.Value.Contains( group.Guid );
            }

            return true;
        }

        #endregion
    }
}
