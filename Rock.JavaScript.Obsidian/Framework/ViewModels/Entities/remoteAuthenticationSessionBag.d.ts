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

/** RemoteAuthenticationSession View Model */
export type RemoteAuthenticationSessionBag = {
    /** Gets or sets the attributes. */
    attributes?: Record<string, PublicAttributeBag> | null;

    /** Gets or sets the attribute values. */
    attributeValues?: Record<string, string> | null;

    /** Gets or sets the authentication ip address. */
    authenticationIpAddress?: string | null;

    /** Gets or sets the authorized person alias identifier. */
    authorizedPersonAliasId?: number | null;

    /** Gets or sets the client ip address. */
    clientIpAddress?: string | null;

    /** Gets or sets the code. */
    code?: string | null;

    /** Gets or sets the created by person alias identifier. */
    createdByPersonAliasId?: number | null;

    /** Gets or sets the created date time. */
    createdDateTime?: string | null;

    /** Gets or sets the device unique identifier. */
    deviceUniqueIdentifier?: string | null;

    /** Gets or sets the identifier key of this entity. */
    idKey?: string | null;

    /** Gets or sets the modified by person alias identifier. */
    modifiedByPersonAliasId?: number | null;

    /** Gets or sets the modified date time. */
    modifiedDateTime?: string | null;

    /** Gets or sets the session authenticated date time. */
    sessionAuthenticatedDateTime?: string | null;

    /** Gets or sets the session end date time. */
    sessionEndDateTime?: string | null;

    /** Gets or sets the session start date time. */
    sessionStartDateTime?: string | null;

    /** Gets or sets the site identifier. */
    siteId?: number | null;
};