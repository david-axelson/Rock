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

using Rock.Model;

namespace Rock.ViewModels.CheckIn
{
    /// <summary>
    /// A search result item that represents a single family member in check-in
    /// family search.
    /// </summary>
    public class FamilyMemberSearchItemBag
    {
        /// <summary>
        /// Gets or sets the unique identifier of the Person.
        /// </summary>
        /// <value>The unique identifier of the Person.</value>
        public Guid Guid { get; set; }

        /// <summary>
        /// Gets or sets the person's nick name.
        /// </summary>
        /// <value>The person's nick name.</value>
        public string NickName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>The last name.</value>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the birth year.
        /// </summary>
        /// <value>The birth year.</value>
        public int? BirthYear { get; set; }

        /// <summary>
        /// Gets or sets the birth month.
        /// </summary>
        /// <value>The birth month.</value>
        public int? BirthMonth { get; set; }

        /// <summary>
        /// Gets or sets the birth day.
        /// </summary>
        /// <value>The birth day.</value>
        public int? BirthDay { get; set; }

        /// <summary>
        /// Gets or sets the birth date. If this date is used, the time zone
        /// information should be thrown away so that just the raw date is
        /// left. Otherwise the birthdate might shift by one day depending
        /// on the time zone.
        /// </summary>
        /// <value>The birth date.</value>
        public DateTimeOffset? BirthDate { get; set; }

        /// <summary>
        /// Gets or sets the group role order.
        /// </summary>
        /// <value>The group role order.</value>
        public int RoleOrder { get; set; }

        /// <summary>
        /// Gets or sets the person's gender.
        /// </summary>
        /// <value>The person's gender.</value>
        public Gender Gender { get; set; }
    }
}
