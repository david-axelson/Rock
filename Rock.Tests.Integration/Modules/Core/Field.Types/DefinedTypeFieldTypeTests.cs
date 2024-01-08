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

namespace Rock.Tests.Integration.Core.Field.Types
{
    [TestClass]
    public class DefinedTypeFieldTypeTests : FieldTypeTestsBase<DefinedTypeFieldType>
    {
        protected override List<FieldTypeTestValue> OnGetExpectedFieldValues()
        {
            var items = new List<FieldTypeTestValue>
            {
                new FieldTypeTestValue
                {
                    PrivateValue = SystemGuid.DefinedType.BACKGROUND_CHECK_TYPES,
                    PublicDisplayValue = "Background Check Types"
                },
                new FieldTypeTestValue
                {
                    PrivateValue = SystemGuid.DefinedType.BENEVOLENCE_REQUEST_STATUS,
                    PublicDisplayValue = "Benevolence Request Status"
                }
            };

            return items;
        }

        protected override bool HasDefaultPublicValueImplementation => true;

        protected override bool HasDefaultPublicEditValueImplementation => true;
    }
}
