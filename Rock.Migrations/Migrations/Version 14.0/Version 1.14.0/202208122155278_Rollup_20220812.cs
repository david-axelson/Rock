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
namespace Rock.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    /// <summary>
    ///
    /// </summary>
    public partial class Rollup_20220812 : Rock.Migrations.RockMigration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            AddFollowIconShortcode();
            UpdateContentCollectionDefaultValues();
            UpdateAppleTvBlockFileName();
            AddAdminChecklistItemForRedis();
            ShowFormBuilderPages();
            SetWorkflowLogIsSecured();
            CleanupMigrationHistory();
        }
        
        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
        }

        /// <summary>
        /// Garrett: Add Follow Icon Shortcode
        /// </summary>
        private void AddFollowIconShortcode()
        {
            Sql( MigrationSQL._202208122155278_Rollup_20220812_Follow20Shortcode );
        }

        /// <summary>
        /// GJ: Update Content Collection Default Values
        /// </summary>
        private void UpdateContentCollectionDefaultValues()
        {
            // Attribute for BlockType
            //   BlockType: Content Library View
            //   Category: CMS
            //   Attribute: Results Template
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "CC387575-3530-4CD6-97E0-1F449DCA1869", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Results Template", "ResultsTemplate", "Results Template", @"The lava template to use to render the results container. It must contain an element with the class 'result-items'.", 0, @"<div class=""panel panel-default"">
    {% if SourceEntity and SourceEntity != empty %}
        <div class=""panel-heading"">
            <h2 class=""panel-title"">
                <i class=""{{ SourceEntity.IconCssClass }}""></i> {{ SourceName }}
            </h2>
        </div>
    {% endif %}
    <div class=""list-group"">
        <div class=""result-items""></div>
    </div>
</div>
<div class=""actions"">
   <a href=""#"" class=""btn btn-default show-more"">Show More</a>
</div>", "085A7213-9C0B-41FD-909B-D74804ABA12E" );

            // Attribute for BlockType
            //   BlockType: Content Library View
            //   Category: CMS
            //   Attribute: Item Template
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "CC387575-3530-4CD6-97E0-1F449DCA1869", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Item Template", "ItemTemplate", "Item Template", @"The lava template to use to render a single result.", 0, @"<div class=""result-item"">
    <a href=""#"" class=""list-group-item"">
        <h4>{{ Item.Name }}</h4>
        <p>Posted on {{ Item.RelevanceDateTime  | AsDateTime | Date:'MMM dd, yyyy' }}</p>
        {{ Item.Content | StripHtml | Truncate:100 }}
        <span class=""pull-right pt-4 pl-2 text-primary"">
            <i class=""fa fa-arrow-right""></i>
        </span>
        <span class=""text-muted""></span>
    </a>
</div>", "CE511E4F-E2DE-4C62-877E-DCE1323F1FC9" );

            // Attribute for BlockType
            //   BlockType: Content Library View
            //   Category: CMS
            //   Attribute: Pre-Search Template
            RockMigrationHelper.AddOrUpdateBlockTypeAttribute( "CC387575-3530-4CD6-97E0-1F449DCA1869", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Pre-Search Template", "PreSearchTemplate", "Pre-Search Template", @"The lava template to use to render the content displayed before a search happens. This will not be used if Search on Load is enabled.", 0, @"<div class=""panel panel-default"">
    <div class=""panel-body"">Discover content that matches your preferences.</div>
</div>", "5AF8D107-2BB1-4E71-B1F0-E0641ABFE80B" );
        }

        /// <summary>
        /// Jon: Update block file name for v14.0
        /// </summary>
        private void UpdateAppleTvBlockFileName()
        {
            Sql( @"UPDATE [BlockType]
                SET [Path] = '~/Blocks/Tv/AppleTvPageList.ascx'
                WHERE [Guid] = '7bd1b79c-bf27-42c6-8359-f80ec7fee397'" );
        }

        /// <summary>
        /// NA: Data Migration to Add note about Redis support ending
        /// </summary>
        private void AddAdminChecklistItemForRedis()
        {
            // Now check to see if Redis is enabled and create a checklist item if it is.
            if ( Rock.Web.SystemSettings.GetValueFromWebConfig( Rock.SystemKey.SystemSetting.REDIS_ENABLE_CACHE_CLUSTER ).AsBoolean() )
            {
                Sql( @"

                DECLARE @AdminChecklistDefinedTypeId INT = (SELECT [Id] FROM [DefinedType] WHERE [Guid] = '4BF34677-37E9-4E71-BD03-252B66C9373D')

	            -- Make room at the top of the list.
                UPDATE DefinedValue SET [Order] = [Order] + 1 WHERE DefinedTypeId = @AdminChecklistDefinedTypeId

	            -- Insert the row
	            INSERT INTO [dbo].[DefinedValue] (
		                [IsSystem]
                    , [DefinedTypeId]
                    , [Order]
                    , [Value]
                    , [Description]
                    , [Guid]
                    , [CreatedDateTime]
                    , [ModifiedDateTime]
                    , [IsActive])
	            VALUES (
		                1
                    , @AdminChecklistDefinedTypeId
                    , 0
                    , 'Redis Support Will End in Rock v15'
                    , '<div class=""alert alert-warning"">Rock is still configured to use Redis.  Starting with Rock v15, Redis will no longer be supported.</div>'
                    , '911001E1-D7B6-41BE-A840-6AAFEDFA827D'
                    , GETDATE()
                    , GETDATE()
                    , 1)" );
            }
        }

        /// <summary>
        /// Shows the form builder pages.
        /// </summary>
        private void ShowFormBuilderPages()
        {
            Sql( @"-- Form Builder Templates
                -- DisplayInNavWhen.WhenAllowed = 0,

                UPDATE [Page]
                SET [DisplayInNavWhen] = 0
                WHERE [Guid] = '316E8E0C-9714-4DAF-896F-1154D52D095B'

                -- Form Builder (main page)
                UPDATE [Page]
                SET [DisplayInNavWhen] = 0
                WHERE [Guid] = '4F77819C-8F69-4418-933E-08F63E7FC4F9'" );
        }
    
        /// <summary>
        /// Cleanups the migration history records except the last one.
        /// </summary>
        private void CleanupMigrationHistory()
        {
        Sql( @"
        UPDATE [dbo].[__MigrationHistory]
        SET [Model] = 0x
        WHERE MigrationId < (SELECT TOP 1 MigrationId FROM __MigrationHistory ORDER BY MigrationId DESC)" );
        }

        /// <summary>
        /// KA: Migration to Set IsSecured column of WorkflowLog to false.
        /// </summary>
        private void SetWorkflowLogIsSecured()
        {
            RockMigrationHelper.UpdateEntityType( "Rock.Model.WorkflowLog", Rock.SystemGuid.EntityType.WORKFLOW_LOG, true, false );
        }
    }
}