//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by the Rock.CodeGeneration project
//     Changes to this file will be lost when the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------
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


namespace Rock.Client
{
    /// <summary>
    /// Base client model for AnalyticsSourceZipCode that only includes the non-virtual fields. Use this for PUT/POSTs
    /// </summary>
    public partial class AnalyticsSourceZipCodeEntity
    {
        /// <summary />
        public string City { get; set; }

        /// <summary />
        public decimal Families100kTo149kPercent { get; set; }

        /// <summary />
        public decimal Families10kTo14kPercent { get; set; }

        /// <summary />
        public decimal Families150kTo199kPercent { get; set; }

        /// <summary />
        public decimal Families15kTo24kPercent { get; set; }

        /// <summary />
        public decimal Families25kTo34kPercent { get; set; }

        /// <summary />
        public decimal Families35kTo49kPercent { get; set; }

        /// <summary />
        public decimal Families50kTo74kPercent { get; set; }

        /// <summary />
        public decimal Families75kTo99kPercent { get; set; }

        /// <summary />
        public decimal FamiliesMeanIncome { get; set; }

        /// <summary />
        public decimal FamiliesMeanIncomeMarginOfError { get; set; }

        /// <summary />
        public decimal FamiliesMedianIncome { get; set; }

        /// <summary />
        public decimal FamiliesMedianIncomeMarginOfError { get; set; }

        /// <summary />
        public decimal FamiliesMore200kPercent { get; set; }

        /// <summary />
        public int? FamiliesTotal { get; set; }

        /// <summary />
        public decimal FamiliesUnder10kPercent { get; set; }

        /// <summary />
        public object GeoFence { get; set; }

        /// <summary />
        public int? HouseholdsTotal { get; set; }

        /// <summary />
        public int? LastUpdate { get; set; }

        /// <summary />
        public int? MarriedCouplesTotal { get; set; }

        /// <summary />
        public int? NonFamilyHouseholdsTotal { get; set; }

        /// <summary />
        public decimal SquareMiles { get; set; }

        /// <summary />
        public string State { get; set; }

        /// <summary>
        /// Copies the base properties from a source AnalyticsSourceZipCode object
        /// </summary>
        /// <param name="source">The source.</param>
        public void CopyPropertiesFrom( AnalyticsSourceZipCode source )
        {
            this.City = source.City;
            this.Families100kTo149kPercent = source.Families100kTo149kPercent;
            this.Families10kTo14kPercent = source.Families10kTo14kPercent;
            this.Families150kTo199kPercent = source.Families150kTo199kPercent;
            this.Families15kTo24kPercent = source.Families15kTo24kPercent;
            this.Families25kTo34kPercent = source.Families25kTo34kPercent;
            this.Families35kTo49kPercent = source.Families35kTo49kPercent;
            this.Families50kTo74kPercent = source.Families50kTo74kPercent;
            this.Families75kTo99kPercent = source.Families75kTo99kPercent;
            this.FamiliesMeanIncome = source.FamiliesMeanIncome;
            this.FamiliesMeanIncomeMarginOfError = source.FamiliesMeanIncomeMarginOfError;
            this.FamiliesMedianIncome = source.FamiliesMedianIncome;
            this.FamiliesMedianIncomeMarginOfError = source.FamiliesMedianIncomeMarginOfError;
            this.FamiliesMore200kPercent = source.FamiliesMore200kPercent;
            this.FamiliesTotal = source.FamiliesTotal;
            this.FamiliesUnder10kPercent = source.FamiliesUnder10kPercent;
            this.GeoFence = source.GeoFence;
            this.HouseholdsTotal = source.HouseholdsTotal;
            this.LastUpdate = source.LastUpdate;
            this.MarriedCouplesTotal = source.MarriedCouplesTotal;
            this.NonFamilyHouseholdsTotal = source.NonFamilyHouseholdsTotal;
            this.SquareMiles = source.SquareMiles;
            this.State = source.State;

        }
    }

    /// <summary>
    /// Client model for AnalyticsSourceZipCode that includes all the fields that are available for GETs. Use this for GETs (use AnalyticsSourceZipCodeEntity for POST/PUTs)
    /// </summary>
    public partial class AnalyticsSourceZipCode : AnalyticsSourceZipCodeEntity
    {
        /// <summary />
        public string ZipCode { get; set; }

    }
}
