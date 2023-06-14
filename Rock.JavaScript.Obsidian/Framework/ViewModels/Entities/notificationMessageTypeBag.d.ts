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

import { PublicAttributeBag } from "@Obsidian/ViewModels/Utility/publicAttributeBag";

/** NotificationMessageType View Model */
export type NotificationMessageTypeBag = {
    /** Gets or sets the attributes. */
    attributes?: Record<string, PublicAttributeBag> | null;

    /** Gets or sets the attribute values. */
    attributeValues?: Record<string, string> | null;

    /**
     * Gets or sets the component data json. This data is only understood
     * by the component itself and should not be modified elsewhere.
     */
    componentDataJson?: string | null;

    /** Gets or sets the created by person alias identifier. */
    createdByPersonAliasId?: number | null;

    /** Gets or sets the created date time. */
    createdDateTime?: string | null;

    /**
     * Gets or sets the Id of the Rock.Model.EntityType component
     * that handles logic for this instance.
     */
    entityTypeId: number;

    /** Gets or sets the identifier key of this entity. */
    idKey?: string | null;

    /**
     * Gets or sets a value indicating whether messages are deleted instead
     * of being marked as read.
     */
    isDeletedOnRead: boolean;

    /**
     * Gets or sets a value indicating whether messages are supported
     * on mobile applications.
     */
    isMobileApplicationSupported: boolean;

    /**
     * Gets or sets a value indicating whether messages are supported
     * on TV applications.
     */
    isTvApplicationSupported: boolean;

    /**
     * Gets or sets a value indicating whether messages are supported
     * on web sites.
     */
    isWebSupported: boolean;

    /** Gets or sets the key that identifies this instance to the component. */
    key?: string | null;

    /** Gets or sets the modified by person alias identifier. */
    modifiedByPersonAliasId?: number | null;

    /** Gets or sets the modified date time. */
    modifiedDateTime?: string | null;

    /**
     * Gets or sets the related mobile site identifier. If specified then
     * messages will only show up on this mobile application. Otherwise
     * messages will show up on all mobile applications. This does not
     * affect other site types.
     */
    relatedMobileApplicationSiteId?: number | null;

    /**
     * Gets or sets the related TV site identifier. If specified then
     * messages will only show up on this TV application. Otherwise
     * messages will show up on all TV applications. This does not
     * affect other site types.
     */
    relatedTvApplicationSiteId?: number | null;

    /**
     * Gets or sets the related web site identifier. If specified then
     * messages will only show up on this website. Otherwise messages
     * will show up on all websites. This does not affect other site types.
     */
    relatedWebSiteId?: number | null;
};