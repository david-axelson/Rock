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

namespace Rock.ViewModels.CheckIn
{
    /// <summary>
    /// Defines a single group that can be used during check-in.
    /// </summary>
    public class CheckInGroupItemBag : CheckInItemBag
    {
        /// <summary>
        /// Gets or sets the area unique identifier that this group belongs to.
        /// </summary>
        /// <value>The area unique identifier.</value>
        public Guid AreaGuid { get; set; }

        /// <summary>
        /// Gets or sets the ability level unique identifier required to
        /// attend this group.
        /// </summary>
        /// <value>The required ability level unique identifier; or <c>null</c> if not required.</value>
        public Guid? AbilityLevelGuid { get; set; }
    }
}
