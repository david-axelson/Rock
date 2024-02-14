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

using Rock.Attribute;
using Rock.Data;
using Rock.Web.Cache;

namespace Rock.CheckIn.v2
{
    /// <summary>
    /// This provides the server check-in data for a single check-in group.
    /// This should not be sent down to clients as it contains additional
    /// data they should not see.
    /// </summary>
    internal class CheckInGroupData
    {
        #region Properties

        /// <summary>
        /// Gets the minimum age requirement or <c>null</c> if there is no
        /// minimum. The person's age must be greater than or equal to
        /// this value.
        /// </summary>
        /// <value>The minimum age requirement.</value>
        public decimal? MinimumAge { get; }

        /// <summary>
        /// Gets the maximum age requirement or <c>null</c> if there is no
        /// maximum. The person's age must be less than this value.
        /// </summary>
        /// <value>The maximum age requirement.</value>
        public decimal? MaximumAge { get; }

        /// <summary>
        /// Gets the minimum birthdate requirement or <c>null</c> if there
        /// is no minimum. The person's birthdate must be greater than or
        /// equal to this value.
        /// </summary>
        /// <value>The minimum birthdate requirement.</value>
        public DateTime? MinimumBirthdate { get; }

        /// <summary>
        /// Gets the maximum birthdate requirement or <c>null</c> if there
        /// is no maximum. The person's birthdate must be less than this value.
        /// </summary>
        /// <value>The maximum birthdate requirement.</value>
        public DateTime? MaximumBirthdate { get; }

        /// <summary>
        /// Gets the minimum grade offset requirement or <c>null</c> if there
        /// is no minimum. The person's grade offset must be greater than or
        /// equal to this value.
        /// </summary>
        /// <value>The minimum grade offset requirement.</value>
        public int? MinimumGradeOffset { get; }

        /// <summary>
        /// Gets the maximum grade offset requirement or <c>null</c> if there
        /// is no maximum. The person's grade offset must be less than or
        /// equal to this value.
        /// </summary>
        /// <value>The maximum grade offset requirement.</value>
        public int? MaximumGradeOffset { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInGroupData"/> class.
        /// </summary>
        /// <param name="groupCache">The group cache.</param>
        /// <param name="rockContext">The rock context.</param>
        internal CheckInGroupData( GroupCache groupCache, RockContext rockContext )
        {
            (MinimumAge, MaximumAge) = GetAgeRange( groupCache );
            (MinimumBirthdate, MaximumBirthdate) = GetBirthdateRange( groupCache );
            (MinimumGradeOffset, MaximumGradeOffset) = GetGradeOffsetRange( groupCache );
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the age range specified by the group attribute value.
        /// </summary>
        /// <param name="groupCache">The group cache.</param>
        /// <returns>A tuple that contains the minimum and maximum values.</returns>
        private static (decimal? MinimumAge, decimal? MaximumAge) GetAgeRange( GroupCache groupCache )
        {
            // Get the age ranges.
            var ageRange = groupCache.GetAttributeValue( "AgeRange" ).ToStringSafe();
            var ageRangePair = ageRange.Split( new char[] { ',' }, StringSplitOptions.None );

            if ( ageRangePair.Length != 2 )
            {
                return (null, null);
            }

            return (ageRangePair[0].AsDecimalOrNull(), ageRangePair[1].AsDecimalOrNull());
        }

        /// <summary>
        /// Gets the birthdate range specified by the group attribute value.
        /// </summary>
        /// <param name="groupCache">The group cache.</param>
        /// <returns>A tuple that contains the minimum and maximum values.</returns>
        private static (DateTime? MinimumBirthdate, DateTime? MaximumBirthdate) GetBirthdateRange( IHasAttributes groupCache )
        {
            var birthdateRange = groupCache.GetAttributeValue( "BirthdateRange" ).ToStringSafe();
            var birthdateRangePair = birthdateRange.Split( new char[] { ',' }, StringSplitOptions.None );

            if ( birthdateRangePair.Length != 2 )
            {
                return (null, null);
            }

            return (birthdateRangePair[0].AsDateTime(), birthdateRangePair[1].AsDateTime());
        }

        /// <summary>
        /// Gets the grade range specified by the group attribute value.
        /// </summary>
        /// <param name="groupCache">The group cache.</param>
        /// <returns>A tuple that contains the minimum and maximum values.</returns>
        private static (int? MinimumGradeOffset, int? MaximumGradeOffset) GetGradeOffsetRange( IHasAttributes groupCache )
        {
            string gradeOffsetRange = groupCache.GetAttributeValue( "GradeRange" ) ?? string.Empty;
            var gradeOffsetRangePair = gradeOffsetRange.Split( new char[] { ',' }, StringSplitOptions.None ).AsGuidOrNullList().ToArray();

            if ( gradeOffsetRangePair.Length != 2 )
            {
                return (null, null);
            }

            var minGradeDefinedValue = gradeOffsetRangePair[0].HasValue
                ? DefinedValueCache.Get( gradeOffsetRangePair[0].Value )
                : null;

            var maxGradeDefinedValue = gradeOffsetRangePair[1].HasValue
                ? DefinedValueCache.Get( gradeOffsetRangePair[1].Value )
                : null;

            // NOTE: the grade offsets are actually reversed because the range
            // defined values then specify the grade offset as the value, which
            // is the number of years until graduation. So the UI says "4th to 6th"
            // but the offset numbers are "8 to 6".
            return (maxGradeDefinedValue?.Value.AsIntegerOrNull(), minGradeDefinedValue?.Value.AsIntegerOrNull());
        }

        #endregion
    }
}
