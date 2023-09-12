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
    /// FinancialStatementTemplate View Model
    /// </summary>
    public partial class FinancialStatementTemplateBag : EntityBagBase
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>
        /// The description.
        /// </value>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the JSON for Rock.Model.FinancialStatementTemplate.FooterSettings
        /// </summary>
        /// <value>
        /// The footer template.
        /// </value>
        public string FooterSettingsJson { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating if this is an active financial statement template. This value is required.
        /// </summary>
        /// <value>
        /// A System.Boolean value that is true if this financial statement template is active, otherwise false.
        /// </value>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the image file identifier for the Logo Image
        /// </summary>
        /// <value>
        /// The Logo file identifier.
        /// </value>
        public int? LogoBinaryFileId { get; set; }

        /// <summary>
        /// Gets or sets the name of the Financial Statement Template
        /// </summary>
        /// <value>
        /// A System.String that represents the name of the Financial Statement Template
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the JSON for Rock.Model.FinancialStatementTemplate.ReportSettings
        /// </summary>
        /// <value>
        /// The report settings.
        /// </value>
        public string ReportSettingsJson { get; set; }

        /// <summary>
        /// Gets or sets the report template.
        /// </summary>
        /// <value>
        /// The report template.
        /// </value>
        public string ReportTemplate { get; set; }

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