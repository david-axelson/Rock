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

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Newtonsoft.Json;

using Rock.Field;
using Rock.Tests.Shared;
using Rock.ViewModels.Utility;

namespace Rock.Tests.Integration.Core.Field.Types
{
    /// <summary>
    /// A collection of utility methods suitable for testing FieldType components that have an ItemPicker edit control.
    /// </summary>
    /// <remarks>
    /// Refer to https://triumph.slab.com/posts/how-field-types-work-9cks18kh#hm2my-c-reference for further information
    /// about the implementation of FieldTypes.
    /// </remarks>
    /// 
    public abstract class FieldTypeTestsBase<TFieldType>
        where TFieldType : class, IFieldType, new()
    {
        #region Configuration

        /// <summary>
        /// Gets the subset of configuration values that should be present if this field is operating correctly.
        /// To provide a set of valid values, override the <see cref="OnGetExpectedFieldValues"/> method.
        /// </summary>
        /// <returns></returns>
        protected virtual List<FieldTypeTestValue> GetExpectedFieldValues()
        {
            var expectedListItems = OnGetExpectedFieldValues();

            Assert.That.IsTrue( expectedListItems.Any(), "Expected configuration values are not configured." );

            return expectedListItems;
        }

        /// <summary>
        /// Override this method to provide a set of test values for this field type.
        /// </summary>
        /// <returns></returns>
        protected abstract List<FieldTypeTestValue> OnGetExpectedFieldValues();

        /// <summary>
        /// Gets the configuration values for the field.
        /// To provide a set of valid values, override the <see cref="OnGetFieldConfigurationValues"/> method.
        /// </summary>
        /// <returns></returns>
        protected Dictionary<string, string> GetFieldConfigurationValues()
        {
            var configurationValues = OnGetFieldConfigurationValues() ?? new Dictionary<string, string>();

            return configurationValues;
        }

        /// <summary>
        /// Override this method to provide configuration values for the field.
        /// </summary>
        /// <returns></returns>
        protected virtual Dictionary<string, string> OnGetFieldConfigurationValues()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// A flag indicating if a selection value that does not match an available selection should be returned as the field value description.
        /// </summary>
        /// <returns></returns>
        protected virtual bool ShouldReturnInvalidSelectionAsDescription
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// A flag indicating that the public display value of this field is expected to match the default behavior.
        /// By default this flag is to set to false, because most fields will require a custom implementation
        /// of the GetPublicValue() method.
        /// </summary>
        /// <remarks>
        /// The default implementation of GetPublicValue() returns the private field value.
        /// </remarks>
        /// <returns></returns>
        protected virtual bool HasDefaultPublicValueImplementation
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// A flag indicating that the public edit value of this field is expected to match the default behavior.
        /// By default this flag is to set to false, because most fields will require a custom implementation
        /// of the GetPublicEditValue() method.
        /// </summary>
        /// <remarks>
        /// The default implementation of GetPublicEditValue() returns the public display value of the field.
        /// </remarks>
        /// <returns></returns>
        protected virtual bool HasDefaultPublicEditValueImplementation
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// A flag indicating that the private edit value of this field is expected to match the default behavior.
        /// By default this flag is to set to false, because most fields will require a custom implementation
        /// of the GetPrivateEditValue() method.
        /// </summary>
        /// <remarks>
        /// The default implementation of GetPrivateEditValue() returns the public field value.
        /// </remarks>
        /// <returns></returns>
        protected virtual bool HasDefaultPrivateEditValueImplementation
        {
            get
            {
                return false;
            }
        }

        #endregion

        #region Webforms

        /// <summary>
        /// Verify the implementation of the configuration properties and methods for the Field Type.
        /// </summary>
        [TestMethod]
        public void VerifyWebFormsFieldConfigurationProcessing()
        {
            var fieldType = new TFieldType();

            // Test: Create Configuration Controls.,
            var configurationControls = fieldType.ConfigurationControls();
            if ( !configurationControls.Any() )
            {
                return;
            }
            Assert.That.IsNotEmpty( configurationControls, "Verify ConfigurationControls failed. Controls expected but none found." );

            // Test: Read Configuration Values from the controls.
            var configurationValues = fieldType.ConfigurationValues( configurationControls );

            // Test: Push Configuration Values to the controls.
            fieldType.SetConfigurationValues( configurationControls, configurationValues );
        }

        /// <summary>
        /// Verify the implementation requirements for viewing the field value.
        /// </summary>
        /// <remarks>
        /// Refer to https://triumph.slab.com/posts/how-field-types-work-9cks18kh#hm2my-c-reference for information
        /// about the required implementation for a FieldType in Obsidian.
        /// </remarks>
        [TestMethod]
        public void VerifyWebformsViewValueProcessing()
        {
            var expectedValues = GetExpectedFieldValues();

            var fieldType = new TFieldType();
            var configurationValues = new Dictionary<string, ConfigurationValue>();

            foreach ( var expectedValue in expectedValues )
            {
                // Test: FormatValue().
                // Verify that the field correctly converts the private value to a text value.
                var editControl = fieldType.EditControl( configurationValues, "ctl" );

                var actualFormattedValue = fieldType.FormatValue( editControl, expectedValue.PrivateValue, configurationValues, condensed: false );

                var expectedFormattedValue = expectedValue.PublicDisplayValue;

                Assert.That.AreEqual( expectedFormattedValue,
                    actualFormattedValue,
                    $"GetTextValue verification failed. [Expected:{expectedFormattedValue}, Actual:{actualFormattedValue}] " );
            }
        }

        /// <summary>
        /// Verify that setting a valid edit value causes the field to return the correct selection value.
        /// </summary>
        [TestMethod]
        public void VerifyWebFormsEditValueProcessing()
        {
            var expectedListItems = GetExpectedFieldValues();

            // Test 1: GetEditValue/SetEditValue/EditControl
            // Verify that the field returns the expected item text for each of the specified values.
            foreach ( var expectedListItem in expectedListItems )
            {
                // For a list of values, the input value and selected value should be identical.
                Assert_WebFormsSetEditValue_ReturnsExpectedSelectionValue( expectedListItem.PrivateValue, expectedListItem.PrivateValue );
            }

            // Test 2: Verify that the field returns an empty selection when attempting to set an invalid values.
            Assert_WebFormsSetEditValue_ReturnsExpectedSelectionValue( "00000000-0000-0000-0000-000000000000", string.Empty );
            Assert_WebFormsSetEditValue_ReturnsExpectedSelectionValue( null, string.Empty );
            Assert_WebFormsSetEditValue_ReturnsExpectedSelectionValue( "1234", string.Empty );
            Assert_WebFormsSetEditValue_ReturnsExpectedSelectionValue( "not_a_guid", string.Empty );
        }

        /// <summary>
        /// Verify that retrieving a public value for a specified private value returns the expected output.
        /// The public value of a field is the user-friendly description of the field value.
        /// The default FieldType implementation of GetPublicValue() returns the private value unaltered.
        /// </summary>
        protected void xxAssert_GetTextValue_ForPrivateValue_ReturnsExpectedValue( string privateValue, string expectedPublicValue )
        {
            var fieldType = new TFieldType();
            var configurationValues = GetFieldConfigurationValues();

            var publicValue = fieldType.GetPublicValue( privateValue, configurationValues );

            Assert.That.AreEqual( expectedPublicValue ?? string.Empty, publicValue ?? string.Empty,
                $"GetPublicValue verification failed. [Expected:{expectedPublicValue}, Actual:{publicValue}] " );
        }

        /// <summary>
        /// Verify that retrieving a public value for a specified private value returns the expected output.
        /// The public value of a field is the user-friendly description of the field value.
        /// The default FieldType implementation of GetPublicValue() returns the private value unaltered.
        /// </summary>
        protected void xxAssert_GetPublicValue_ForPrivateValue_ReturnsExpectedValue( string privateValue, string expectedPublicValue )
        {
            var fieldType = new TFieldType();
            var configurationValues = GetFieldConfigurationValues();

            var publicValue = fieldType.GetPublicValue( privateValue, configurationValues );

            Assert.That.AreEqual( expectedPublicValue ?? string.Empty, publicValue ?? string.Empty,
                $"GetPublicValue verification failed. [Expected:{expectedPublicValue}, Actual:{publicValue}] " );
        }

        /// <summary>
        /// Verify that retrieving a public value for a specified private value returns the expected output.
        /// The public value of a field is the user-friendly description of the field value.
        /// </summary>
        protected void xxAssert_GetPublicEditValue_ForPrivateValue_ReturnsExpectedValue( string privateValue, string expectedPublicEditValue )
        {
            var fieldType = new TFieldType();
            var configurationValues = GetFieldConfigurationValues();

            var publicEditValue = fieldType.GetPublicEditValue( privateValue, configurationValues );

            var compareExpectedValue = expectedPublicEditValue.ToStringSafe().ToUpper().RemoveWhiteSpace();
            var compareActualValue = publicEditValue.ToStringSafe().ToUpper().RemoveWhiteSpace();

            Assert.That.AreEqual( compareExpectedValue,
                compareActualValue,
                $"GetPublicEditValue verification failed. [Expected:{expectedPublicEditValue}, Actual:{publicEditValue}]" );
        }

        /// <summary>
        /// Verify that a public edit value provided by a client is correctly converted to a corresponding private edit value.
        /// The private edit value of a field is the internal representation of the field data that can be persisted in the datastore.
        /// </summary>
        protected void xxAssert_GetPrivateEditValue_FromPublicEditValue_ReturnsExpectedValue( string publicValue, string expectedPrivateValue )
        {
            var fieldType = new TFieldType();
            var configurationValues = GetFieldConfigurationValues();

            var privateEditValue = fieldType.GetPrivateEditValue( publicValue, configurationValues );

            var compareExpectedValue = expectedPrivateValue.ToStringSafe().RemoveWhiteSpace();
            var compareActualValue = privateEditValue.ToStringSafe().RemoveWhiteSpace();

            Assert.That.AreEqual( compareExpectedValue,
                compareActualValue,
                $"GetPrivateEditValue verification failed. [PublicValue=\"{publicValue}\", ExpectedPrivateValue=\"{expectedPrivateValue}\"]" );
        }

        /// <summary>
        /// Verify that setting a valid selection value causes the field to display the selected item description.
        /// </summary>
        protected void xxAssert_GetTextValue_ForSelectedValue_ReturnsExpectedText( string selectedValue, string expectedTextValue )
        {
            var fieldType = new TFieldType();
            var configurationValues = GetFieldConfigurationValues();

            var textValue = fieldType.GetTextValue( selectedValue, configurationValues );

            Assert.That.AreEqual( expectedTextValue ?? string.Empty, textValue ?? string.Empty );
        }

        /// <summary>
        /// Verify that if the edit control is supplied with valid input, the output selection value is correct.
        /// </summary>
        /// <typeparam name="T">The FieldType to be tested.</typeparam>
        /// <param name="inputValue"></param>
        /// <param name="expectedSelectionValue"></param>
        protected void Assert_WebFormsSetEditValue_ReturnsExpectedSelectionValue( string inputValue, string expectedSelectionValue )
        {
            var fieldType = new TFieldType();

            var configurationValues = new Dictionary<string, ConfigurationValue>();
            var editControl = fieldType.EditControl( configurationValues, "ctl" );

            fieldType.SetEditValue( editControl, configurationValues, inputValue );
            var selectionValue = fieldType.GetEditValue( editControl, null );

            var expectedSelectionValueAsGuid = expectedSelectionValue.AsGuidOrNull();
            if ( expectedSelectionValueAsGuid != null )
            {
                // The selection value is returned as Guid, so force a Guid comparison.
                Assert.That.AreEqual( expectedSelectionValueAsGuid, selectionValue.AsGuidOrNull() );
            }
            else
            {
                var compareExpectedValue = expectedSelectionValue.ToStringSafe().RemoveWhiteSpace().ToUpper();
                var compareActualValue = selectionValue.ToStringSafe().RemoveWhiteSpace().ToUpper();

                Assert.That.AreEqual( compareExpectedValue,
                    compareActualValue,
                    $"GetPrivateEditValue verification failed. [PublicValue=\"{inputValue}\", ExpectedPrivateValue=\"{expectedSelectionValue}\"]" );
            }
        }

        #endregion

        #region Obsidian

        private const string CLIENT_VALUES = "values";

        /// <summary>
        /// Verify that a request to provide public configuration values returns the correct list of selection values for the control.
        /// </summary>
        [TestMethod]
        public void VerifyObsidianFieldConfigurationProcessing()
        {
            var fieldType = new TFieldType();
            var expectedValues = GetExpectedFieldValues();

            // Placeholder values for required parameters.
            // Do these values need to be passed?
            var privateConfigurationValuesParam = new Dictionary<string, string>();
            var valueParam = "???";

            // Test 1: GetPublicConfigurationValues()
            // Verify that the public values include the expected values.
            var publicValuesDictionary = fieldType.GetPublicConfigurationValues( privateConfigurationValuesParam,
                ConfigurationValueUsage.Configure,
                valueParam );

            // If the values dictionary does not contain any items, assume it is an ItemPicker that requires
            // data supplied by the editor at runtime. This configuration requires a separate test.
            if ( !publicValuesDictionary.Any() )
            {
                return;
            }

            // Get list items and convert all values to uppercase for comparison purposes.
            var listItems = JsonConvert.DeserializeObject<List<ListItemBag>>( publicValuesDictionary[CLIENT_VALUES] );
            foreach ( var listItem in listItems )
            {
                listItem.Text = listItem.Text.ToUpper();
                listItem.Value = listItem.Value.ToUpper();
            }

            foreach ( var expectedValue in expectedValues )
            {
                var matchedItem = listItems.FirstOrDefault( i => i.Value == expectedValue.PrivateValue.ToUpper()
                    && i.Text == expectedValue.PublicDisplayValue.ToUpper() );

                Assert.That.IsNotNull( matchedItem,
                    $"Expected Value Not Found. [Value=\"{expectedValue.PrivateValue}\", Text=\"{expectedValue.PublicDisplayValue}\"]" );
            }

            // Test: GetPrivateConfigurationValues().
            // Verify that the private dictionary does not contain client values.
            var privateValuesDictionary = fieldType.GetPrivateConfigurationValues( publicValuesDictionary );

            Assert.That.IsFalse( privateValuesDictionary.ContainsKey( CLIENT_VALUES ) );
        }

        /// <summary>
        /// Verify that setting a valid edit value causes the field to return the correct selection value.
        /// </summary>
        [TestMethod]
        public void VerifyObsidianEditValueProcessing()
        {
            var expectedValues = GetExpectedFieldValues();

            var fieldType = new TFieldType();
            var configurationValues = GetFieldConfigurationValues();

            foreach ( var expectedValue in expectedValues )
            {
                string expectedOutputValue;
                string inputValue;

                // Test: GetPrivateEditValue.
                // Verify that a public edit value can be converted to the expected private edit value.
                // The public edit value is suitable for transmission to and from an external field editor.
                // The private edit value is an internal representation that is suitable for persisting to a datastore.
                if ( this.HasDefaultPrivateEditValueImplementation )
                {
                    // The default implementation of GetPrivateEditValue() returns the input value unaltered.
                    inputValue = expectedValue.PrivateValue;
                    expectedOutputValue = expectedValue.PrivateValue;
                }
                else
                {
                    // If no public edit value is specified, assume that it is the same as the private value.
                    inputValue = expectedValue.PublicEditValue ?? expectedValue.PrivateValue;
                    expectedOutputValue = expectedValue.PrivateValue;
                }

                expectedOutputValue = expectedOutputValue.ToStringSafe().RemoveWhiteSpace();

                var actualValue = fieldType.GetPrivateEditValue( inputValue, configurationValues );
                actualValue = actualValue.ToStringSafe().RemoveWhiteSpace();

                Assert.That.AreEqual( expectedOutputValue,
                    actualValue,
                    $"GetPrivateEditValue verification failed. [PublicValue=\"{inputValue}\", ExpectedPrivateValue=\"{expectedValue.PrivateValue}\"]" );

                // Test: GetPublicEditValue.
                // Verify that the field correctly converts a private value to a public edit value.
                // The public edit value is intended for use by an external field editor, and may contain configuration information
                // that is not intended for display to the end-user.
                // It is typically the same as the private value, but some sensitive information may be excluded.
                inputValue = expectedValue.PrivateValue;

                if ( this.HasDefaultPublicEditValueImplementation )
                {
                    if ( this.HasDefaultPublicValueImplementation )
                    {
                        // The default implementation of GetPublicValue() returns the private value.
                        expectedOutputValue = expectedValue.PrivateValue;
                    }
                    else
                    {
                        // The default implementation of GetPublicEditValue() returns the public display value.
                        expectedOutputValue = expectedValue.PublicDisplayValue;
                    }
                }
                else
                {
                    expectedOutputValue = expectedValue.PublicEditValue;
                }

                expectedOutputValue = expectedOutputValue.ToStringSafe().ToUpper().RemoveWhiteSpace();

                actualValue = fieldType.GetPublicEditValue( inputValue, configurationValues );
                actualValue = actualValue.ToStringSafe().ToUpper().RemoveWhiteSpace();

                Assert.That.AreEqual( expectedOutputValue,
                    actualValue,
                    $"GetPublicEditValue verification failed. [Expected:{expectedOutputValue}, Actual:{actualValue}]" );
            }
        }

        /// <summary>
        /// Verify the Obsidian implementation requirements for viewing the field value.
        /// </summary>
        [TestMethod]
        public void VerifyObsidianViewValueProcessing()
        {
            var expectedValues = GetExpectedFieldValues();

            var fieldType = new TFieldType();
            var configurationValues = GetFieldConfigurationValues();

            foreach ( var expectedValue in expectedValues )
            {
                // Test: GetTextValue.
                // Verify that the field correctly converts the private value to a text value.
                var actualTextValue = fieldType.GetTextValue( expectedValue.PrivateValue, configurationValues );
                var expectedTextValue = expectedValue.PublicDisplayValue;

                Assert.That.AreEqual( expectedTextValue, actualTextValue,
                    $"GetTextValue verification failed. [Expected:{expectedTextValue}, Actual:{actualTextValue}] " );
            }
        }

        #endregion
    }

    #region Support Classes

    /// <summary>
    /// A set of test values for a FieldType.
    /// </summary>
    public class FieldTypeTestValue
    {
        /// <summary>
        /// The internal representation of the field value that is persisted to the datastore.
        /// </summary>
        public string PrivateValue { get; set; }

        /// <summary>
        /// The user-friendly representation of the field value that is suitable for public display.
        /// The default FieldType implementation of this value returns the private value.
        /// </summary>
        public string PublicDisplayValue { get; set; }

        /// <summary>
        /// The value that can be relayed to and from an external editor.
        /// This is similar to the PrivateValue, but omits any data that is intended for internal use only.
        /// </summary>
        public string PublicEditValue { get; set; }
    }

    #endregion
}
