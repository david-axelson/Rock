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

using System.ComponentModel;

using Rock.Attribute;

namespace Rock.Blocks.CheckIn.Configuration
{
    /// <summary>
    /// Designs a check-in label with a nice drag and drop experience.
    /// </summary>

    [DisplayName( "Label Designer" )]
    [Category( "Check-inn > Configuration" )]
    [Description( "Designs a check-in label with a nice drag and drop experience." )]
    [IconCssClass( "fa fa-question" )]
    // [SupportedSiteTypes( Model.SiteType.Web )]

    #region Block Attributes

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( "3f477b52-6062-4af4-abb7-b8c153f6242a" )]
    [Rock.SystemGuid.BlockTypeGuid( "8c4ad18f-9f81-4145-8ad0-ab90e451d0d6" )]
    public class LabelDesigner : RockBlockType
    {
    }
}
