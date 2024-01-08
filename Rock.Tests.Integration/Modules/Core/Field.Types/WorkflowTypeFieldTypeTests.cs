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

using System.Collections.Generic;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rock.Field.Types;
using Rock.ViewModels.Utility;

namespace Rock.Tests.Integration.Core.Field.Types
{
    [TestClass]
    public class WorkflowTypeFieldTypeTests : FieldTypeTestsBase<WorkflowTypeFieldType>
    {
        protected override List<FieldTypeTestValue> OnGetExpectedFieldValues()
        {
            var listItem1 = new ListItemBag { Value = SystemGuid.WorkflowType.UNATTENDED_CHECKIN, Text = "Unattended Check-in" };
            var listItem2 = new ListItemBag { Value = SystemGuid.WorkflowType.REQUEST_ASSESSMENT, Text = "Request Assessment" };

            var items = new List<FieldTypeTestValue>
            {
                new FieldTypeTestValue
                {
                    PrivateValue = listItem1.Value,
                    PublicEditValue = listItem1.ToCamelCaseJson( true, false ),
                    PublicDisplayValue = listItem1.Text,
                },
                new FieldTypeTestValue
                {
                    PrivateValue = listItem2.Value,
                    PublicEditValue = listItem2.ToCamelCaseJson( true, false ),
                    PublicDisplayValue = listItem2.Text,
                }
            };

            return items;
        }
    }
}
