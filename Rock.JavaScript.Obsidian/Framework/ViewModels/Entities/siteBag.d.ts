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

import { Guid } from "@Obsidian/Types";
import { PublicAttributeBag } from "@Obsidian/ViewModels/Utility/publicAttributeBag";

/** Site View Model */
export type SiteBag = {
    /** Gets or sets the AdditionalSettings. */
    additionalSettings?: string | null;

    /** Gets or sets the AllowedFrameDomains. */
    allowedFrameDomains?: string | null;

    /** Gets or sets the AllowIndexing. */
    allowIndexing: boolean;

    /** Gets or sets the ChangePasswordPageId. */
    changePasswordPageId?: number | null;

    /** Gets or sets the ChangePasswordPageRouteId. */
    changePasswordPageRouteId?: number | null;

    /** Gets or sets the CommunicationPageId. */
    communicationPageId?: number | null;

    /** Gets or sets the CommunicationPageRouteId. */
    communicationPageRouteId?: number | null;

    /** Gets or sets the ConfigurationMobilePhoneBinaryFileId. */
    configurationMobilePhoneBinaryFileId?: number | null;

    /** Gets or sets the ConfigurationMobileTabletBinaryFileId. */
    configurationMobileTabletBinaryFileId?: number | null;

    /** Gets or sets the DefaultPageId. */
    defaultPageId?: number | null;

    /** Gets or sets the DefaultPageRouteId. */
    defaultPageRouteId?: number | null;

    /** Gets or sets the Description. */
    description?: string | null;

    /** Gets or sets the DisablePredictableIds. */
    disablePredictableIds: boolean;

    /** Gets or sets the EnabledForShortening. */
    enabledForShortening: boolean;

    /** Gets or sets the EnableExclusiveRoutes. */
    enableExclusiveRoutes: boolean;

    /** Gets or sets the EnableMobileRedirect. */
    enableMobileRedirect: boolean;

    /** Gets or sets the EnablePageViewGeoTracking. */
    enablePageViewGeoTracking: boolean;

    /** Gets or sets the EnablePageViews. */
    enablePageViews: boolean;

    /** Gets or sets the ErrorPage. */
    errorPage?: string | null;

    /** Gets or sets the ExternalUrl. */
    externalUrl?: string | null;

    /** Gets or sets the FavIconBinaryFileId. */
    favIconBinaryFileId?: number | null;

    /** Gets or sets the GoogleAnalyticsCode. */
    googleAnalyticsCode?: string | null;

    /** Gets or sets the IndexStartingLocation. */
    indexStartingLocation?: string | null;

    /** Gets or sets the IsActive. */
    isActive: boolean;

    /** Gets or sets the IsIndexEnabled. */
    isIndexEnabled: boolean;

    /** Gets or sets the IsSystem. */
    isSystem: boolean;

    /** Gets or sets the LatestVersionDateTime. */
    latestVersionDateTime?: string | null;

    /** Gets or sets the LoginPageId. */
    loginPageId?: number | null;

    /** Gets or sets the LoginPageRouteId. */
    loginPageRouteId?: number | null;

    /** Gets or sets the MobilePageId. */
    mobilePageId?: number | null;

    /** Gets or sets the Name. */
    name?: string | null;

    /** Gets or sets the PageHeaderContent. */
    pageHeaderContent?: string | null;

    /** Gets or sets the PageNotFoundPageId. */
    pageNotFoundPageId?: number | null;

    /** Gets or sets the PageNotFoundPageRouteId. */
    pageNotFoundPageRouteId?: number | null;

    /** Gets or sets the RedirectTablets. */
    redirectTablets: boolean;

    /** Gets or sets the RegistrationPageId. */
    registrationPageId?: number | null;

    /** Gets or sets the RegistrationPageRouteId. */
    registrationPageRouteId?: number | null;

    /** Gets or sets the RequiresEncryption. */
    requiresEncryption: boolean;

    /** Gets or sets the SiteLogoBinaryFileId. */
    siteLogoBinaryFileId?: number | null;

    /** Gets or sets the SiteType. */
    siteType: number;

    /** Gets or sets the Theme. */
    theme?: string | null;

    /** Gets or sets the ThumbnailBinaryFileId. */
    thumbnailBinaryFileId?: number | null;

    /** Gets or sets the CreatedDateTime. */
    createdDateTime?: string | null;

    /** Gets or sets the ModifiedDateTime. */
    modifiedDateTime?: string | null;

    /** Gets or sets the CreatedByPersonAliasId. */
    createdByPersonAliasId?: number | null;

    /** Gets or sets the ModifiedByPersonAliasId. */
    modifiedByPersonAliasId?: number | null;

    /** Gets or sets the Id. */
    id: number;

    /** Gets or sets the Guid. */
    guid?: Guid | null;

    /** Gets or sets the identifier key of this entity. */
    idKey?: string | null;

    /** Gets or sets the attributes. */
    attributes?: Record<string, PublicAttributeBag> | null;

    /** Gets or sets the attribute values. */
    attributeValues?: Record<string, string> | null;
};