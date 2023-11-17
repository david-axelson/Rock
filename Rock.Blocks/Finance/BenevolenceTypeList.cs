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
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Obsidian.UI;
using Rock.Security;
using Rock.ViewModels.Blocks;
using Rock.ViewModels.Blocks.Finance.BenevolenceTypeList;
using Rock.Web.Cache;

namespace Rock.Blocks.Finance
{
    /// <summary>
    /// Displays a list of benevolence types.
    /// </summary>

    [DisplayName( "Benevolence Type List" )]
    [Category( "Finance" )]
    [Description( "Displays a list of benevolence types." )]
    [IconCssClass( "fa fa-list" )]
    // [SupportedSiteTypes( Model.SiteType.Web )]

    [LinkedPage( "Detail Page",
        Description = "The page that will show the benevolence type details.",
        Key = AttributeKey.DetailPage )]

    [Rock.SystemGuid.EntityTypeGuid( "3db4d87e-ca48-47ab-a1c3-99be7c026b00" )]
    [Rock.SystemGuid.BlockTypeGuid( "5df12843-63f1-4884-af56-420aca869e45" )]
    [CustomizedGrid]
    public class BenevolenceTypeList : RockEntityListBlockType<BenevolenceType>
    {
        #region Keys

        private static class AttributeKey
        {
            public const string DetailPage = "DetailPage";
        }

        private static class NavigationUrlKey
        {
            public const string DetailPage = "DetailPage";
        }

        #endregion Keys

        #region Methods

        /// <inheritdoc/>
        public override object GetObsidianBlockInitialization()
        {
            var box = new ListBlockBox<BenevolenceTypeListOptionsBag>();
            var builder = GetGridBuilder();

            box.IsAddEnabled = GetIsAddEnabled();
            box.IsDeleteEnabled = true;
            box.ExpectedRowCount = null;
            box.NavigationUrls = GetBoxNavigationUrls();
            box.Options = GetBoxOptions();
            box.GridDefinition = builder.BuildDefinition();

            return box;
        }

        /// <summary>
        /// Gets the box options required for the component to render the list.
        /// </summary>
        /// <returns>The options that provide additional details to the block.</returns>
        private BenevolenceTypeListOptionsBag GetBoxOptions()
        {
            var options = new BenevolenceTypeListOptionsBag();

            return options;
        }

        /// <summary>
        /// Determines if the add button should be enabled in the grid.
        /// <summary>
        /// <returns>A boolean value that indicates if the add button should be enabled.</returns>
        private bool GetIsAddEnabled()
        {
            return BlockCache.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson );
        }

        /// <summary>
        /// Gets the box navigation URLs required for the page to operate.
        /// </summary>
        /// <returns>A dictionary of key names and URL values.</returns>
        private Dictionary<string, string> GetBoxNavigationUrls()
        {
            return new Dictionary<string, string>
            {
                [NavigationUrlKey.DetailPage] = this.GetLinkedPageUrl( AttributeKey.DetailPage, "BenevolenceTypeId", "((Key))" )
            };
        }

        /// <inheritdoc/>
        protected override IQueryable<BenevolenceType> GetListQueryable( RockContext rockContext )
        {
            return base.GetListQueryable( rockContext );
        }

        /// <inheritdoc/>
        protected override GridBuilder<BenevolenceType> GetGridBuilder()
        {
            return new GridBuilder<BenevolenceType>()
                .WithBlock( this )
                .AddField("id", a => a.Id)
                .AddTextField( "idKey", a => a.IdKey )
                .AddTextField( "name", a => a.Name )
                .AddTextField( "description", a => a.Description )
                .AddField( "showFinancialResults", a => a.ShowFinancialResults )
                .AddField( "isActive", a => a.IsActive )
                .AddField( "isSecurityDisabled", a => !a.IsAuthorized( Authorization.ADMINISTRATE, RequestContext.CurrentPerson ) )
                .AddAttributeFields( GetGridAttributes() );
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Deletes the specified entity.
        /// </summary>
        /// <param name="key">The identifier of the entity to be deleted.</param>
        /// <returns>An empty result that indicates if the operation succeeded.</returns>
        [BlockAction]
        public BlockActionResult Delete( string key )
        {
            using ( var rockContext = new RockContext() )
            {
                var entityService = new BenevolenceTypeService( rockContext );
                var entity = entityService.Get( key, !PageCache.Layout.Site.DisablePredictableIds );

                if ( entity == null )
                {
                    return ActionBadRequest( $"{BenevolenceType.FriendlyTypeName} not found." );
                }

                if ( !entity.IsAuthorized( Authorization.EDIT, RequestContext.CurrentPerson ) )
                {
                    return ActionBadRequest( $"Not authorized to delete ${BenevolenceType.FriendlyTypeName}." );
                }

                if ( !entityService.CanDelete( entity, out var errorMessage ) )
                {
                    return ActionBadRequest( errorMessage );
                }

                entityService.Delete( entity );
                rockContext.SaveChanges();

                return ActionOk();
            }
        }

        /// <summary>
        /// Changes the ordered position of a single benevolence type in the list.
        /// </summary>
        /// <param name="key">The key identifier of the BenevolenceType entity.</param>
        /// <param name="guid">The GUID of the benevolence type that will be moved.</param>
        /// <param name="beforeGuid">The GUID of the benevolence type it will be placed before, or null if placed at the end.</param>
        /// <returns>An empty result indicating if the operation succeeded.</returns>
        [BlockAction]
        public BlockActionResult ReorderBenevolenceType( string key, Guid guid, Guid? beforeGuid )
        {
            using ( var rockContext = new RockContext() )
            {
                var benevolenceTypeService = new BenevolenceTypeService( rockContext );

                // Get all benevolence types
                var allBenevolenceTypes = benevolenceTypeService.Queryable().ToList();

                // Find the current and new index for the moved item
                int currentIndex = allBenevolenceTypes.FindIndex( bt => bt.Guid == guid );
                int newIndex = beforeGuid.HasValue ? allBenevolenceTypes.FindIndex( bt => bt.Guid == beforeGuid.Value ) : allBenevolenceTypes.Count - 1;

                // Perform the reordering
                if ( currentIndex != newIndex )
                {
                    benevolenceTypeService.Reorder( allBenevolenceTypes, currentIndex, newIndex );
                    rockContext.SaveChanges();
                }

                return ActionOk();
            }
        }
        #endregion
    }
}
