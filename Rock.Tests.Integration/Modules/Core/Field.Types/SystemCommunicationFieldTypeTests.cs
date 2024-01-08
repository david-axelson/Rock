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
using System.Linq;
using System.Web.UI.WebControls;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Rock.Field;
using Rock.Field.Types;
using Rock.Tests.Shared;
using Rock.Web.UI.Controls;

namespace Rock.Tests.Integration.Core.Field.Types
{
    [TestClass]
    public class SystemCommunicationFieldTypeTests : FieldTypeTestsBase<SystemCommunicationFieldType>
    {
        protected override List<FieldTypeTestValue> OnGetExpectedFieldValues()
        {
            var items = new List<FieldTypeTestValue>
            {
                new FieldTypeTestValue
                {
                    PrivateValue =  SystemGuid.SystemCommunication.SECURITY_ACCOUNT_CREATED,
                    PublicDisplayValue = "Account Created"
                },
                new FieldTypeTestValue
                {
                    PrivateValue = SystemGuid.SystemCommunication.SECURITY_CONFIRM_ACCOUNT,
                    PublicDisplayValue = "Confirm Account"
                }
            };

            return items;
        }

        protected override bool HasDefaultPublicEditValueImplementation => true;
        protected override bool HasDefaultPrivateEditValueImplementation => true;


        #region WebForms Tests

        /// <summary>
        /// Verify that the default edit control for this field contains the correct list of items.
        /// </summary>
        /// <param name="expectedItemValue"></param>
        /// <param name="expectedItemText"></param>
        [DataTestMethod]
        [DataRow( SystemGuid.SystemCommunication.SECURITY_ACCOUNT_CREATED, "Account Created" )]
        [DataRow( SystemGuid.SystemCommunication.SECURITY_CONFIRM_ACCOUNT, "Confirm Account" )]
        public void EditControl_WithDefaultConfiguration_ContainsExpectedListItem( string expectedItemValue, string expectedItemText )
        {
            var fieldType = new SystemCommunicationFieldType();

            var configurationValues = new Dictionary<string, ConfigurationValue>();
            var editControl = fieldType.EditControl( configurationValues, "ctl" );
            var ddl = editControl as RockDropDownList;

            var items = ddl.Items.Cast<ListItem>().ToList();

            Assert.That.IsNotNull( items.FirstOrDefault( i => i.Value == expectedItemValue
                && i.Text == expectedItemText ) );
        }

        #endregion
    }
}
