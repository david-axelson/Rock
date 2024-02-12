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
    /// A family member that identifies a single individual for check-in.
    /// </summary>
    public class FamilyMemberBag : FamilyMemberSearchItemBag
    {
        /// <summary>
        /// Gets or sets the primary family unique identifier this person
        /// belongs to.
        /// </summary>
        /// <value>The family unique identifier.</value>
        public Guid FamilyGuid { get; set; }

        /// <summary>
        /// Gets or sets the first name.
        /// </summary>
        /// <value>The first name.</value>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <value>The full name.</value>
        public string FullName { get; set; }

        /// <summary>
        /// Gets or sets the photo URL.
        /// </summary>
        /// <value>The photo URL.</value>
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Gets or sets the age.
        /// </summary>
        /// <value>The age.</value>
        public int? Age { get; set; }

        /// <summary>
        /// Gets or sets the precise age as a floating point number.
        /// </summary>
        /// <value>The precise age.</value>
        public double? AgePrecise { get; set; }

        /// <summary>
        /// Gets or sets the grade offset, this should only be used for sorting
        /// purposes. To display the grade use the GradeFormatted property.
        /// </summary>
        /// <value>The grade offset.</value>
        public int? GradeOffset { get; set; }

        /// <summary>
        /// Gets or sets the grade formatted for display purposes.
        /// </summary>
        /// <value>The grade formatted for display purposes.</value>
        public string GradeFormatted { get; set; }
    }
}
