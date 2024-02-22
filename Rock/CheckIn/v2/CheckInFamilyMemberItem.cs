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

using Rock.ViewModels.CheckIn;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// Represents a person and all the check-in related values that will be
    /// used during the check-in process.
    /// </summary>
    internal class CheckInFamilyMemberItem
    {
        /// <summary>
        /// Gets or sets the person.
        /// </summary>
        /// <value>The person.</value>
        public FamilyMemberBag Person { get; set; }

        /// <summary>
        /// Gets or sets the options that are available to be selected from.
        /// </summary>
        /// <value>The options that are available to be selected from.</value>
        public CheckInOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the selected options that were automatically made.
        /// </summary>
        /// <value>The selected options that were automatically made.</value>
        public SelectedOptions SelectedOptions { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this person is pre-selected
        /// for check-in.
        /// </summary>
        /// <value><c>true</c> if this person is pre-selected; otherwise, <c>false</c>.</value>
        public bool IsPreSelected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this person is disabled.
        /// </summary>
        /// <value><c>true</c> if this person is disabled; otherwise, <c>false</c>.</value>
        public bool IsDisabled { get; set; }

        /// <summary>
        /// Gets or sets the message describing why the person is disabled.
        /// </summary>
        /// <value>The message describing why the person is disabled.</value>
        public string DisabledMessage { get; set; }

        /// <summary>
        /// Gets or sets the last date and time the person checked in.
        /// </summary>
        /// <value>The last date and time the person checked in.</value>
        public DateTime? LastCheckIn { get; set; }
    }
}
