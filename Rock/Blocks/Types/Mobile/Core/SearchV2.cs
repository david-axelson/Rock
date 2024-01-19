using Rock.Attribute;
using Rock.Common.Mobile.Blocks.Core.SearchV2;
using Rock.Data;
using Rock.Mobile;
using Rock.Model;
using Rock.Search;
using Rock.Web.Cache;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Rock.Blocks.Types.Mobile.Core
{
    /// <summary>
    /// Performs a search using any of the configured search components and displays the results.
    /// </summary>
    /// <remarks>
    ///     <para>This block only supports some search components. It heavily relies on templates stored on the mobile shell.</para>
    ///     <para>Supported person entity search components: <see cref="SearchV2Constants.SupportedPersonSearchComponents"/></para>
    ///     <para>Supported group entity search components: <see cref="SearchV2Constants.SupportedGroupSearchComponents"/></para>
    /// </remarks>
    /// <seealso cref="Rock.Blocks.RockBlockType" />

    [DisplayName( "Search (V2)" )]
    [Category( "Mobile > Core" )]
    [Description( "Performs a search using the configured search components and displays the results." )]
    [IconCssClass( "fa fa-search" )]
    [SupportedSiteTypes( Model.SiteType.Mobile )]

    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.MOBILE_CORE_SEARCH_V2_BLOCK_TYPE )]
    [Rock.SystemGuid.BlockTypeGuid( Rock.SystemGuid.BlockType.MOBILE_CORE_SEARCH_V2 )]

    #region Block Attributes

    [ComponentsField( "Rock.Search.SearchContainer, Rock",
        Name = "Search Component(s)",
        Description = "The search components to offer for searches.",
        IsRequired = true,
        Key = AttributeKey.SearchComponents,
        Order = 0 )]

    [CodeEditorField( "Header Content",
        Key = AttributeKey.HeaderContent,
        Description = "The content to display for the header.",
        IsRequired = false,
        DefaultValue = "",
        Order = 1 )]

    [CodeEditorField( "Footer Content",
        Key = AttributeKey.FooterContent,
        Description = "The content to display for the header.",
        IsRequired = false,
        DefaultValue = "",
        Order = 2 )]

    [IntegerField( "Result Size",
        Key = AttributeKey.ResultSize,
        Description = "The amount of results to initially return and with each sequential load (as you scroll down).",
        IsRequired = true,
        DefaultIntegerValue = 20,
        Order = 3 )]

    [BooleanField( "Auto Focus Keyboard",
        Description = "Determines if the keyboard should auto-focus into the search field when the page is attached.",
        IsRequired = false,
        DefaultBooleanValue = true,
        ControlType = Field.Types.BooleanFieldType.BooleanControlType.Toggle,
        Key = AttributeKey.AutoFocusKeyboard,
        Order = 4 )]

    [BooleanField( "Show Search History",
        Description = "Determines if search history should be stored (locally in the shell) and displayed per-component.",
        IsRequired = false,
        DefaultBooleanValue = true,
        ControlType = Field.Types.BooleanFieldType.BooleanControlType.Toggle,
        Key = AttributeKey.ShowSearchHistory,
        Order = 5 )]

    [IntegerField( "Stopped Typing Behavior Threshold",
        Description = "Changes the amount of time (in milliseconds) that a user must stop typing for the search command to execute. Set to 0 to disable entirely.",
        IsRequired = true,
        DefaultIntegerValue = 200,
        Key = AttributeKey.StoppedTypingBehaviorThreshold,
        Order = 6 )]

    //
    // Person-specific attributes
    //

    [BooleanField( "Show Birthdate",
        Description = "Determines if the person's birthdate should be displayed in the search results.",
        IsRequired = false,
        DefaultBooleanValue = false,
        ControlType = Field.Types.BooleanFieldType.BooleanControlType.Toggle,
        Key = AttributeKey.ShowBirthdate,
        Category = AttributeCategory.PersonSearch,
        Order = 7 )]

    [BooleanField( "Show Age",
        Description = "Determines if the person's age should be displayed in the search results.",
        IsRequired = false,
        DefaultBooleanValue = true,
        ControlType = Field.Types.BooleanFieldType.BooleanControlType.Toggle,
        Key = AttributeKey.ShowAge,
        Category = AttributeCategory.PersonSearch,
        Order = 8 )]

    [LinkedPage(
        "Person Detail Page",
        Description = "Page to link to when a person taps on a Person search result. 'PersonGuid' is passed as the query string.",
        IsRequired = false,
        Key = AttributeKey.PersonDetailPage,
        Category = AttributeCategory.PersonSearch,
        Order = 9 )]

    //
    // Group-specific attributes
    //

    [LinkedPage(
        "Group Detail Page",
        Description = "Page to link to when a person taps on a Group search result. 'GroupGuid' is passed as the query string.",
        IsRequired = false,
        Key = AttributeKey.GroupDetailPage,
        Category = AttributeCategory.GroupSearch,
        Order = 10 )]

    #endregion

    public class SearchV2 : RockBlockType
    {
        #region Properties

        /// <summary>
        /// Gets the search components configured for this block.
        /// </summary>
        protected List<Guid> SearchComponents => GetAttributeValue( AttributeKey.SearchComponents )?.SplitDelimitedValues().AsGuidList();

        /// <summary>
        /// Gets the content to display above the search results.
        /// </summary>
        protected string HeaderContent => GetAttributeValue( AttributeKey.HeaderContent );

        /// <summary>
        /// Gets the content to display below the search results.
        /// </summary>
        protected string FooterContent => GetAttributeValue( AttributeKey.FooterContent );

        /// <summary>
        /// Gets the result size from the block attribute.
        /// </summary>
        protected int ResultSize => GetAttributeValue( AttributeKey.ResultSize ).AsInteger();

        /// <summary>
        /// Gets whether or not the keyboard should auto-focus into the search field when the page is attached.
        /// </summary>
        protected bool AutoFocusKeyboard => GetAttributeValue( AttributeKey.AutoFocusKeyboard ).AsBoolean();

        /// <summary>
        /// Gets whether or not search history should be stored (locally in the shell) and displayed per-component.
        /// </summary>
        protected bool ShowSearchHistory => GetAttributeValue( AttributeKey.ShowSearchHistory ).AsBoolean();

        /// <summary>
        /// Gets the amount of time (in milliseconds) that a user must stop typing for the search command to execute.
        /// </summary>
        protected int StoppedTypingBehaviorThreshold => GetAttributeValue( AttributeKey.StoppedTypingBehaviorThreshold ).AsInteger();

        /// <summary>
        /// Gets whether or not the person's birthdate should be displayed in the search results for a Person search.
        /// </summary>
        protected bool ShowBirthdate => GetAttributeValue( AttributeKey.ShowBirthdate ).AsBoolean();

        /// <summary>
        /// Gets whether or not the person's age should be displayed in the search results for a Person search.
        /// </summary>
        protected bool ShowAge => GetAttributeValue( AttributeKey.ShowAge ).AsBoolean();

        #endregion

        #region Constants/Keys

        /// <summary>
        /// A class used to hold attribute category keys for this block.
        /// </summary>
        private static class AttributeCategory
        {
            /// <summary>
            /// The person search attribute category.
            /// </summary>
            public const string PersonSearch = "Person Search";

            /// <summary>
            /// The group search attribute category.
            /// </summary>
            public const string GroupSearch = "Group Search";
        }

        /// <summary>
        /// A class used to hold attribute keys for this block.
        /// </summary>
        private static class AttributeKey
        {
            /// <summary>
            /// The search component attribute key.
            /// </summary>
            public const string SearchComponents = "SearchComponents";

            /// <summary>
            /// The header content attribute key.
            /// </summary>
            public const string HeaderContent = "HeaderContent";

            /// <summary>
            /// The footer content attribute key.
            /// </summary>
            public const string FooterContent = "FooterContent";

            /// <summary>
            /// The result size attribute key.
            /// </summary>
            public const string ResultSize = "ResultSize";

            /// <summary>
            /// The auto focus keyboard attribute key.
            /// </summary>
            public const string AutoFocusKeyboard = "AutoFocusKeyboard";

            /// <summary>
            /// The show search history attribute key.
            /// </summary>
            public const string ShowSearchHistory = "ShowSearchHistory";

            /// <summary>
            /// The stopped typing behavior threshold attribute key.
            /// </summary>
            public const string StoppedTypingBehaviorThreshold = "StoppedTypingBehaviorThreshold";

            /// <summary>
            /// The show birthdate attribute key.
            /// </summary>
            public const string ShowBirthdate = "ShowBirthdate";

            /// <summary>
            /// The show age attribute key.
            /// </summary>
            public const string ShowAge = "ShowAge";

            /// <summary>
            /// The person detail page attribute key.
            /// </summary>
            public const string PersonDetailPage = "PersonDetailPage";

            /// <summary>
            /// The group detail page attribute key.
            /// </summary>
            public const string GroupDetailPage = "GroupDetailPage";
        }

        /// <summary>
        /// A class used to hold template keys that are available on the mobile shell for this block.
        /// </summary>
        private static class TemplateKey
        {
            public const string PersonSearchResultItem = "PersonSearchResultItem";
            public const string GroupSearchResultItem = "GroupSearchResultItem";
        }

        #endregion

        #region IRockMobileBlockType Implementation

        /// <inheritdoc />
        public override object GetMobileConfigurationValues()
        {
            return new
            {
                HeaderContent = HeaderContent,
                FooterContent = FooterContent,
                ResultSize = ResultSize,
                AutoFocusKeyboard = AutoFocusKeyboard,
                ShowSearchHistory = ShowSearchHistory,
                StoppedTypingBehaviorThreshold = StoppedTypingBehaviorThreshold,
                ShowBirthdate = ShowBirthdate,
                ShowAge = ShowAge,
                SearchComponents = GetComponents()
            };
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the configured search components for this block.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<SearchComponentBag> GetComponents()
        {
            //
            // This block only supports certain search components (listed below).
            // The template key is used to determine which template to use for the search results.
            // The templates for this block are stored within the mobile shell.
            //

            var components = new List<SearchComponentBag>();

            foreach ( var guid in SearchComponents )
            {
                var templateKey = GetTemplateKeyForComponent( guid );

                if ( templateKey.IsNullOrWhiteSpace() )
                {
                    continue;
                }

                var componentEntityType = EntityTypeCache.Get( guid );

                if ( componentEntityType == null )
                {
                    continue;
                }

                var componentBag = new SearchComponentBag
                {
                    Guid = guid,
                    TemplateKey = templateKey,
                    Name = componentEntityType.FriendlyName
                };

                components.Add( componentBag );
            }

            return components;
        }

        /// <summary>
        /// Gets the template key for a search component.
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        private string GetTemplateKeyForComponent( Guid guid )
        {
            var templateKey = string.Empty;

            if ( SearchV2Constants.SupportedPersonSearchComponents.Contains( guid ) )
            {
                templateKey = TemplateKey.PersonSearchResultItem;
            }
            else if ( SearchV2Constants.SupportedGroupSearchComponents.Contains( guid ) )
            {
                templateKey = TemplateKey.GroupSearchResultItem;
            }

            return templateKey;
        }

        /// <summary>
        /// Gets the results as a <see cref="SearchResultUnionItemBag"/> object.
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        private SearchResultUnionItemBag GetSearchResultItems( List<object> results )
        {
            var unionBag = new SearchResultUnionItemBag();

            results.ForEach( o =>
            {
                if ( o is IEntity entity )
                {
                    var type = entity.GetType();

                    if ( type.IsDynamicProxyType() )
                    {
                        type = type.BaseType;
                    }

                    var searchResultBag = new SearchResultBag
                    {
                        Guid = entity.Guid,
                        DetailKey = entity.TypeName,
                    };

                    if ( entity is Person personEntity )
                    {
                        if ( unionBag.PersonResults == null )
                        {
                            unionBag.PersonResults = new List<PersonSearchItemResultBag>();
                        }

                        unionBag.PersonResults.Add( PopulatePersonSearchResultBag( searchResultBag, personEntity ) );
                    }
                    else if ( entity is Group groupEntity )
                    {
                        if ( unionBag.GroupResults == null )
                        {
                            unionBag.GroupResults = new List<GroupSearchItemResultBag>();
                        }
                    }
                }
            } );

            return unionBag;
        }

        /// <summary>
        /// Gets the search result bag for a person.
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        private PersonSearchItemResultBag PopulatePersonSearchResultBag( SearchResultBag bag, Person person )
        {
            PersonSearchItemResultBag itemBag = ( PersonSearchItemResultBag ) bag;
            itemBag.NickName = person.NickName;
            itemBag.LastName = person.LastName;
            itemBag.PhotoUrl = MobileHelper.BuildPublicApplicationRootUrl( person.PhotoUrl );
            itemBag.Email = person.Email;

            return itemBag;
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Gets the search results that match the term.
        /// </summary>

        /// <returns>A view model that represents the results of the search.</returns>
        [BlockAction( "Search" )]
        public BlockActionResult GetSearchResults( SearchRequestBag requestBag )
        {
            var searchComponent = SearchContainer.Instance.Components
                    .Select( c => c.Value.Value )
                    .FirstOrDefault( c => c.TypeGuid == requestBag.SearchComponentGuid );

            var hasMore = false;

            if ( searchComponent == null || !searchComponent.IsActive )
            {
                return ActionInternalServerError( "Search component is not configured or not active." );
            }

            // Perform the search, take one more than configured so we can
            // determine if there are more items.
            var results = searchComponent.SearchQuery( requestBag.SearchTerm )
                .Skip( requestBag.Offset )
                .Take( ResultSize + 1 )
                .ToList();

            // Check if we have more results than we will send, if so then set
            // the flag to tell the client there are more results available.
            if ( results.Count > ResultSize )
            {
                hasMore = true;
                results = results.Take( ResultSize ).ToList();
            }

            // Convert the results into view models.
            var result = GetSearchResultItems( results );

            return ActionOk( new SearchResponseBag
            {
                Result = result,
                HasMore = hasMore
            } );
        }

        #endregion

    }
}
