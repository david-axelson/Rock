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
using System.Linq;

using Rock.ViewModels.Utility;

namespace Rock.ViewModels.Entities
{
    /// <summary>
    /// ContentCollection View Model
    /// </summary>
    public partial class ContentCollectionBag : EntityBagBase
    {
        /// <summary>
        /// Gets or sets the collection key.
        /// </summary>
        /// <value>
        /// The collection key.
        /// </value>
        public string CollectionKey { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether personalization request
        /// filters should be enabled.
        /// </summary>
        /// <value>
        ///   true if personalization request filters should be enabled; otherwise, false.
        /// </value>
        public bool EnableRequestFilters { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether personalization segments
        /// should be enabled.
        /// </summary>
        /// <value>
        ///   true if personalization segments should be enabled; otherwise, false.
        /// </value>
        public bool EnableSegments { get; set; }

        /// <summary>
        /// Gets or sets the filter settings.
        /// </summary>
        /// <value>
        /// The filter settings.
        /// </value>
        public string FilterSettings { get; set; }

        /// <summary>
        /// Gets or sets the last index date time.
        /// </summary>
        /// <value>
        /// The last index date time.
        /// </value>
        public DateTime? LastIndexDateTime { get; set; }

        /// <summary>
        /// Gets or sets the last index item count.
        /// </summary>
        /// <value>
        /// The last index index item count.
        /// </value>
        public int? LastIndexItemCount { get; set; }

        /// <summary>
        /// Gets or sets the name of the Rock.Model.ContentCollection.
        /// </summary>
        /// <value>
        /// A System.String representing the name of the Rock.Model.ContentCollection.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether trending is enabled for this
        /// content collection.
        /// </summary>
        /// <value>
        ///   true if trending is enabled; otherwise, false.
        /// </value>
        public bool TrendingEnabled { get; set; }

        /// <summary>
        /// Gets or sets the trending gravity to apply more weight to items that
        /// are newer.
        /// </summary>
        /// <value>
        /// The trending gravity to apply more weight to items that are newer.
        /// </value>
        public decimal TrendingGravity { get; set; }

        /// <summary>
        /// Gets or sets the trending max items.
        /// </summary>
        /// <value>
        /// The trending max items.
        /// </value>
        public int TrendingMaxItems { get; set; }

        /// <summary>
        /// Gets or sets the trending window day.
        /// </summary>
        /// <value>
        /// The trending window day.
        /// </value>
        public int TrendingWindowDay { get; set; }

        /// <summary>
        /// Gets or sets the created date time.
        /// </summary>
        /// <value>
        /// The created date time.
        /// </value>
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the modified date time.
        /// </summary>
        /// <value>
        /// The modified date time.
        /// </value>
        public DateTime? ModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the created by person alias identifier.
        /// </summary>
        /// <value>
        /// The created by person alias identifier.
        /// </value>
        public int? CreatedByPersonAliasId { get; set; }

        /// <summary>
        /// Gets or sets the modified by person alias identifier.
        /// </summary>
        /// <value>
        /// The modified by person alias identifier.
        /// </value>
        public int? ModifiedByPersonAliasId { get; set; }

    }
}