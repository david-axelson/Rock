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
namespace Rock.ViewModels.CheckIn
{
    /// <summary>
    /// A Family Member item bag.
    /// </summary>
    public class FamilyMemberItemBag
    {
        /// <summary>
        /// Gets or sets the person represented by this item.
        /// </summary>
        /// <value>The person.</value>
        public FamilyMemberBag Person { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this person is disabled.
        /// </summary>
        /// <value><c>true</c> if this person is disabled; otherwise, <c>false</c>.</value>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is already selected.
        /// </summary>
        /// <value><c>true</c> if this instance is already selected; otherwise, <c>false</c>.</value>
        public bool IsPreSelected { get; set; }

        /// <summary>
        /// Gets or sets the message describing why this person is not available.
        /// </summary>
        /// <value>The disabled reason message.</value>
        public string DisabledMessage { get; set; }

        // TODO: SelectedOptions
    }
}
