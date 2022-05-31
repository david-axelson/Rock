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
    /// MediaFolder View Model
    /// </summary>
    public partial class MediaFolderBag : EntityBagBase
    {
        /// <summary>
        /// Gets or sets the synced content channel item attribute identifier
        /// to store the Guid value into.
        /// </summary>
        /// <value>
        /// The synced content channel item attribute identifier.
        /// </value>
        public int? ContentChannelAttributeId { get; set; }

        /// <summary>
        /// Gets or sets the content channel identifier.
        /// </summary>
        /// <value>
        /// The content channel identifier.
        /// </value>
        public int? ContentChannelId { get; set; }

        /// <summary>
        /// Gets or sets the Rock.Model.ContentChannelItemStatus Content channel Item status.
        /// </summary>
        /// <value>
        /// A Rock.Model.ContentChannelItemStatus enumeration value that represents the status of the ContentItem.
        /// </value>
        public int? ContentChannelItemStatus { get; set; }

        /// <summary>
        /// Gets or sets a description of the MediaFolder.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the content channel sync is enabled.
        /// </summary>
        /// <value>
        /// true if the content channel sync is enabled; otherwise, false.
        /// </value>
        public bool IsContentChannelSyncEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating if this Media Folder is public.
        /// </summary>
        /// <value>
        /// A System.Boolean that is true if this Media Folder is public, otherwise false.
        /// </value>
        public bool? IsPublic { get; set; }

        /// <summary>
        /// Gets or sets the MediaAccountId of the Rock.Model.MediaAccount that this MediaFolder belongs to. This property is required.
        /// </summary>
        /// <value>
        /// A System.Int32 representing the MediaAccountId of the Rock.Model.MediaAccount that this MediaFolder belongs to.
        /// </value>
        public int MediaAccountId { get; set; }

        /// <summary>
        /// Gets or sets the custom provider metric data for this instance.
        /// </summary>
        /// <value>
        /// The custom provider metric data for this instance.
        /// </value>
        public string MetricData { get; set; }

        /// <summary>
        /// Gets or sets the Name of the MediaFolder. This property is required.
        /// </summary>
        /// <value>
        /// A System.String representing the name of the MediaFolder.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the custom provider data for this instance.
        /// </summary>
        /// <value>
        /// The custom provider data for this instance.
        /// </value>
        public string SourceData { get; set; }

        /// <summary>
        /// Gets or sets the provider's unique identifier for this instance.
        /// </summary>
        /// <value>
        /// The provider's unique identifier for this instance.
        /// </value>
        public string SourceKey { get; set; }

        /// <summary>
        /// Gets or sets the workflow type identifier. This workflow is
        /// launched whenever a new Rock.Model.MediaElement is added to
        /// the system. The Rock.Model.MediaElement is passed as the Entity
        /// object to the workflow.
        /// </summary>
        /// <value>
        /// The workflow type identifier.
        /// </value>
        public int? WorkflowTypeId { get; set; }

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