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
import { SignUpOccurrenceBag } from "@Obsidian/ViewModels/Blocks/Group/Scheduling/GroupScheduleToolbox/signUpOccurrenceBag";
import { SignUpsBag } from "@Obsidian/ViewModels/Blocks/Group/Scheduling/GroupScheduleToolbox/signUpsBag";

/** A bag that contains information about the outcome of a "save sign-up" request for the group schedule toolbox block. */
export type SaveSignUpResponseBag = {
    /** Gets or sets a friendly error message to describe any problems encountered while saving. */
    saveError?: string | null;

    /** Gets or sets the selected location unique identifier; will only be provided if the save succeeded. */
    selectedLocationGuid?: Guid | null;

    /** Gets or sets the updated sign-up occurrence; will only be provided if the save succeeded. */
    signUpOccurrence?: SignUpOccurrenceBag | null;

    /** Gets or sets the current sign-ups; will only be provided if the save failed. */
    signUps?: SignUpsBag | null;
};
