using Rock.Attribute;
using Rock.ClientService.Connection.ConnectionType;
using Rock.Data;
using Rock.Mobile;
using Rock.Model.Connection.ConnectionType.Options;
using Rock.Model;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Rock.Common.Mobile.ViewModel;
using System.Linq;
using Rock.ClientService.Connection.ConnectionOpportunity;
using Rock.Model.Connection.ConnectionOpportunity.Options;
using Rock.Utility;
using Rock.Security;
using Rock.Common.Mobile.Blocks.Connection.AddConnectionRequest;
using Rock.Web.Cache;
using Rock.ViewModels.Utility;
using Rock.Common.Mobile.Blocks.Connection.ConnectionRequestDetail;
using GroupMemberStatus = Rock.Model.GroupMemberStatus;

namespace Rock.Blocks.Types.Mobile.Connection
{
    /// <summary>
    /// A block used to create a new Connection Request.
    /// </summary>
    /// <seealso cref="Rock.Blocks.RockBlockType" />
    /// 
    [DisplayName( "Add Connection Request" )]
    [Category( "Mobile > Connection" )]
    [Description( "Allows an individual to create and add a new Connection Request." )]
    [IconCssClass( "fa fa-plus" )]
    [SupportedSiteTypes( Model.SiteType.Mobile )]

    #region Block Attributes

    [ConnectionTypesField( "Connection Types",
        Description = "The connection types to limit this block to. Will only display if the person has access to see them. None selected means all will be available.",
        Key = AttributeKey.ConnectionTypes,
        Order = 0 )]

    [MobileNavigationActionField( "Post Save Action",
        Description = "The navigation action to perform on save. 'ConnectionRequestIdKey' will be passed as a query string parameter.",
        IsRequired = true,
        DefaultValue = MobileNavigationActionFieldAttribute.PushPageValue,
        Key = AttributeKey.PostSaveAction,
        Order = 1 )]

    [MobileNavigationActionField( "Post Cancel Action",
        Description = "The navigation action to perform when the cancel button is pressed.",
        IsRequired = false,
        DefaultValue = MobileNavigationActionFieldAttribute.NoneValue,
        Key = AttributeKey.PostCancelAction,
        Order = 2 )]

    #endregion

    [Rock.SystemGuid.EntityTypeGuid( Rock.SystemGuid.EntityType.MOBILE_CONNECTION_ADD_CONNECTION_REQUEST )]
    [Rock.SystemGuid.BlockTypeGuid( Rock.SystemGuid.BlockType.MOBILE_CONNECTION_ADD_CONNECTION_REQUEST )]
    public class AddConnectionRequest : RockBlockType
    {
        #region Keys

        /// <summary>
        /// The page parameter keys for this block.
        /// </summary>
        public static class PageParameterKey
        {
            /// <summary>
            /// The requester id key key.
            /// </summary>
            public const string RequesterIdKey = "RequesterIdKey";
        }

        /// <summary>
        /// The attribute keys for the <see cref="AddConnectionRequest"/> block.
        /// </summary>
        public static class AttributeKey
        {
            /// <summary>
            /// The connection types key.
            /// </summary>
            public const string ConnectionTypes = "ConnectionTypes";

            /// <summary>
            /// The post save action key.
            /// </summary>
            public const string PostSaveAction = "PostSaveAction";

            /// <summary>
            /// The post cancel action key.
            /// </summary>
            public const string PostCancelAction = "PostCancelAction";
        }

        #endregion

        #region Block Attributes

        /// <summary>
        /// The list of connection types that this block is limited to.
        /// </summary>
        protected List<Guid> ConnectionTypes => GetAttributeValue( AttributeKey.ConnectionTypes ).SplitDelimitedValues().AsGuidList();

        /// <summary>
        /// The post save navigation action.
        /// </summary>
        internal MobileNavigationAction DetailNavigationAction => GetAttributeValue( AttributeKey.PostSaveAction ).FromJsonOrNull<MobileNavigationAction>() ?? new MobileNavigationAction();

        /// <summary>
        /// The post cancel navigation action.
        /// </summary>
        internal MobileNavigationAction CancelNavigationAction => GetAttributeValue( AttributeKey.PostSaveAction ).FromJsonOrNull<MobileNavigationAction>() ?? new MobileNavigationAction();

        #endregion

        #region IRockMobileBlockType Implementation

        #endregion

        #region Methods

        /// <summary>
        /// Gets the connection types for the current person.
        /// </summary>
        /// <param name="rockContext"></param>
        /// <returns></returns>
        private List<ListItemViewModel> GetConnectionTypes( RockContext rockContext )
        {
            var connectionTypeService = new ConnectionTypeService( rockContext );
            var filterOptions = new ConnectionTypeQueryOptions
            {
                IncludeInactive = false
            };

            // Get the connection types.
            var qry = connectionTypeService.GetConnectionTypesQuery( filterOptions );
            var types = connectionTypeService.GetViewAuthorizedConnectionTypes( qry, RequestContext.CurrentPerson );

            return types.Select( ct => new ListItemViewModel
            {
                Text = ct.Name,
                Value = ct.IdKey
            } ).ToList();
        }

        /// <summary>
        /// Gets the connection opportunities for the specified connection type.
        /// </summary>
        /// <param name="connectionTypeIdKey"></param>
        /// <returns></returns>
        private List<ListItemViewModel> GetConnectionOpportunities( string connectionTypeIdKey )
        {
            using ( var rockContext = new RockContext() )
            {
                var opportunityService = new ConnectionOpportunityService( rockContext );
                var opportunityClientService = new ConnectionOpportunityClientService( rockContext, RequestContext.CurrentPerson );
                var connectionType = new ConnectionTypeService( rockContext ).GetNoTracking( connectionTypeIdKey );

                var filterOptions = new ConnectionOpportunityQueryOptions
                {
                    IncludeInactive = false,
                    ConnectionTypeGuids = new List<Guid> { connectionType.Guid }
                };

                // Put all the opportunities in memory so we can check security.
                var qry = opportunityService.GetConnectionOpportunitiesQuery( filterOptions );
                var opportunities = qry.ToList()
                    .Where( o => o.IsAuthorized( Authorization.VIEW, RequestContext.CurrentPerson ) );

                return opportunities.ToList().Select( o => new ListItemViewModel
                {
                    Text = o.Name,
                    Value = o.IdKey
                } ).ToList();
            }
        }

        /// <summary>
        /// Gets the opportunity status list items for the given connection type.
        /// </summary>
        /// <param name="connectionType">Connection type to query.</param>
        /// <returns>A list of list items that can be displayed.</returns>
        private static List<ListItemViewModel> GetOpportunityStatusListItems( ConnectionType connectionType )
        {
            return connectionType.ConnectionStatuses
                .OrderBy( s => s.Order )
                .OrderByDescending( s => s.IsDefault )
                .ThenBy( s => s.Name )
                .Select( s => new ListItemViewModel
                {
                    Value = s.Guid.ToString(),
                    Text = s.Name
                } )
                .ToList();
        }

        /// <summary>
        /// Gets the possible connectors for the specified connection request.
        /// All possible connectors are returned, campus filtering is not applied.
        /// </summary>
        /// <param name="connectionOpportunityId">The connection opportunity id.</param>
        /// <param name="rockContext">The Rock database context.</param>
        /// <returns>A list of connectors that are valid for the request.</returns>
        private List<ListItemViewModel> GetAvailableConnectors( int connectionOpportunityId, RockContext rockContext )
        {
            var additionalConnectorAliasIds = new List<int>();

            // Add the logged in person.
            if ( RequestContext.CurrentPerson != null )
            {
                additionalConnectorAliasIds.Add( RequestContext.CurrentPerson.PrimaryAliasId.Value );
            }

            return GetConnectionOpportunityConnectors( connectionOpportunityId, null, additionalConnectorAliasIds, rockContext )
                .Select( connector => new ListItemViewModel
                {
                    Text = connector.FullName,
                    Value = connector.Guid.ToString()
                } ).ToList();
        }

        /// <summary>
        /// Gets a list of connectors that match the specified criteria.
        /// </summary>
        /// <param name="connectionOpportunityId">The connection opportunity identifier.</param>
        /// <param name="campusGuid">The campus to limit connectors to.</param>
        /// <param name="additionalPersonAliasIds">The additional person alias ids.</param>
        /// <param name="rockContext">The Rock database context.</param>
        /// <returns>A list of connectors that are valid for the request.</returns>
        private static List<ConnectorItemViewModel> GetConnectionOpportunityConnectors( int connectionOpportunityId, Guid? campusGuid, List<int> additionalPersonAliasIds, RockContext rockContext )
        {
            var connectorGroupService = new ConnectionOpportunityConnectorGroupService( rockContext );
            var personAliasService = new PersonAliasService( rockContext );

            // Get the primary list of connectors for this connection opportunity.
            // Include all the currently active members of the groups and then
            // build the connector view model
            var connectorList = connectorGroupService.Queryable()
                .Where( a => a.ConnectionOpportunityId == connectionOpportunityId
                    && ( !campusGuid.HasValue || a.ConnectorGroup.Campus.Guid == campusGuid ) )
                .SelectMany( g => g.ConnectorGroup.Members )
                .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                .Select( m => new ConnectorItemViewModel
                {
                    Guid = m.Person.Guid,
                } )
                .ToList();

            // If they specified any additional people to load then execute
            // a query to find just those people.
            if ( additionalPersonAliasIds != null && additionalPersonAliasIds.Any() )
            {
                var additionalPeople = personAliasService.Queryable()
                    .Where( pa => additionalPersonAliasIds.Contains( pa.Id ) )
                    .Select( pa => new ConnectorItemViewModel
                    {
                        Guid = pa.Person.Guid,
                        FirstName = pa.Person.NickName,
                        LastName = pa.Person.LastName,
                        CampusGuid = null
                    } )
                    .ToList();

                connectorList.AddRange( additionalPeople );
            }

            // Distinct by both the person Guid and the CampusGuid. We could
            // still have duplicate people, but that will be up to the client
            // to sort out. Then apply final sorting.
            return connectorList.GroupBy( c => new { c.Guid, c.CampusGuid } )
                .Select( g => g.First() )
                .OrderBy( c => c.LastName )
                .ThenBy( c => c.FirstName )
                .ToList();
        }

        /// <summary>
        /// Gets all the attributes and values for the entity in a form
        /// suitable to use for editing.
        /// </summary>
        /// <param name="request">The connection request.</param>
        /// <returns>A list of editable attribute values.</returns>
        private List<ConnectionRequestDetail.PublicEditableAttributeValueViewModel> GetPublicEditableAttributeValues( IHasAttributes request )
        {
            var attributes = request.GetPublicAttributesForEdit( RequestContext.CurrentPerson )
                .ToDictionary( kvp => kvp.Key, kvp => new ConnectionRequestDetail.PublicEditableAttributeValueViewModel
                {
                    AttributeGuid = kvp.Value.AttributeGuid,
                    Categories = kvp.Value.Categories,
                    ConfigurationValues = kvp.Value.ConfigurationValues,
                    Description = kvp.Value.Description,
                    FieldTypeGuid = kvp.Value.FieldTypeGuid,
                    IsRequired = kvp.Value.IsRequired,
                    Key = kvp.Value.Key,
                    Name = kvp.Value.Name,
                    Order = kvp.Value.Order,
                    Value = ""
                } );

            request.GetPublicAttributeValuesForEdit( RequestContext.CurrentPerson )
                .ToList()
                .ForEach( kvp =>
                {
                    if ( attributes.ContainsKey( kvp.Key ) )
                    {
                        attributes[kvp.Key].Value = kvp.Value;
                    }
                } );

            return attributes.Select( kvp => kvp.Value ).OrderBy( a => a.Order ).ToList();
        }

        #endregion

        #region Block Actions

        /// <summary>
        /// Gets the connection types for the current person.
        /// </summary>
        /// <returns></returns>
        [BlockAction]
        public BlockActionResult GetConnectionTypes( GetConnectionTypesRequestBag requestBag )
        {
            using ( var rockContext = new RockContext() )
            {
                var personService = new PersonService( rockContext );

                // Get the connection types and grab the name and guid.
                var connectionTypes = GetConnectionTypes( rockContext );

                // Get the requester.
                var requester = new PersonService( rockContext ).Get( requestBag.RequesterIdKey );

                if ( requester == null )
                {
                    return ActionNotFound( "The requester was not found." );
                }

                var requesterViewModel = new ListItemViewModel
                {
                    Text = requester.FullName,
                    Value = requester.IdKey
                };

                return ActionOk( new GetConnectionTypesResponseBag
                {
                    ConnectionTypes = connectionTypes.ToList(),
                    Requester = requesterViewModel,
                } );
            }
        }

        /// <summary>
        /// Gets the connection opportunities for the specified connection type.
        /// </summary>
        /// <param name="requestBag"></param>
        /// <returns></returns>
        [BlockAction]
        public BlockActionResult GetConnectionOpportunities( GetConnectionOpportunitiesRequestBag requestBag )
        {
            if ( requestBag.ConnectionTypeIdKey.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "The connection type identifier key is required." );
            }

            return ActionOk( new GetConnectionOpportunitiesResponseBag
            {
                ConnectionOpportunities = GetConnectionOpportunities( requestBag.ConnectionTypeIdKey )
            } );
        }

        /// <summary>
        /// Gets additional data for the connection request.
        /// </summary>
        /// <param name="requestBag"></param>
        /// <returns></returns>
        [BlockAction]
        public BlockActionResult GetConnectionRequestData( GetConnectionRequestDataRequestBag requestBag )
        {
            if ( requestBag.ConnectionOpportunityIdKey.IsNullOrWhiteSpace() )
            {
                return ActionBadRequest( "The connection opportunity identifier key is required." );
            }

            using ( var rockContext = new RockContext() )
            {
                var opportunity = new ConnectionOpportunityService( new RockContext() ).Get( requestBag.ConnectionOpportunityIdKey );

                if ( opportunity == null )
                {
                    return ActionNotFound( "The opportunity for that Id Key was not found." );
                }

                var statuses = GetOpportunityStatusListItems( opportunity.ConnectionType );
                var connectors = GetAvailableConnectors( opportunity.Id, rockContext );

                // Create a new in-memory connection request to load the editable attributes.
                var connectionRequest = new Rock.Model.ConnectionRequest();
                var attributes = GetPublicEditableAttributeValues( connectionRequest );

                return ActionOk( new GetConnectionRequestDataResponseBag
                {
                    Statuses = statuses,
                    Connectors = connectors,
                    Attributes = attributes
                } );
            }
        }

        #endregion

        public class GetConnectionRequestDataRequestBag
        {
            public string ConnectionOpportunityIdKey { get; set; }
        }

        public class GetConnectionRequestDataResponseBag
        {
            public List<ListItemViewModel> Statuses { get; set; }

            public List<ListItemViewModel> Connectors { get; set; }

            /// <summary>
            /// Gets or sets the attributes.
            /// </summary>
            /// <value>
            /// The attributes.
            /// </value>
            public List<ConnectionRequestDetail.PublicEditableAttributeValueViewModel> Attributes { get; set; }
        }

    }
}
