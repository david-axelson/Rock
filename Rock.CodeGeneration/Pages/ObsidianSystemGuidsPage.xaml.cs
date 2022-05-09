﻿using Rock.CodeGeneration.FileGenerators;
using Rock.CodeGeneration.Utility;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Rock.CodeGeneration.Pages
{
    /// <summary>
    /// Interaction logic for ObsidianSystemGuidsPage.xaml
    /// </summary>
    public partial class ObsidianSystemGuidsPage : Page
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ObsidianSystemGuidsPage"/> class.
        /// </summary>
        public ObsidianSystemGuidsPage()
        {
            InitializeComponent();

            // Check if the Rock DLL is out of date and the project needs to be built.
            RockOutOfDateAlert.Visibility = SupportTools.IsSourceNewer( typeof( Data.IEntity ).Assembly.Location, "Rock" )
                ? Visibility.Visible
                : Visibility.Collapsed;

            var types = GetSystemGuidTypes();

            // Order the types so that any invalid types are at the top of the list.
            var typeItems = types.Select( t => new TypeItem( t ) )
                .OrderBy( t => !t.IsInvalid )
                .ThenBy( t => t.Name )
                .ToList();

            SystemGuidsListBox.ItemsSource = typeItems;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the types that make up the SystemGuids.
        /// </summary>
        /// <returns>A collection of types in the <see cref="SystemGuid"/> namespace.</returns>
        private List<Type> GetSystemGuidTypes()
        {
            return typeof( SystemGuid.DefinedType ).Assembly
                .GetExportedTypes()
                .Where( t => t.Namespace == "Rock.SystemGuid" )
                .ToList();
        }

        /// <summary>
        /// Gets the path to use when writing the generated files.
        /// </summary>
        /// <returns>A string that represents the target path.</returns>
        private string GetPath()
        {
            return Path.Combine( "Rock.JavaScript.Obsidian", "Framework", "SystemGuids", "CodeGenerated" );
        }

        /// <summary>
        /// Gets the file name to use for the type.
        /// </summary>
        /// <param name="type">The SystemGuid type.</param>
        /// <returns>A string that represents the file name.</returns>
        private string GetFileNameForType( Type type )
        {
            return $"{type.Name.CamelCase()}.d.ts";
        }

        /// <summary>
        /// Gets the selected types that are selected from the list.
        /// </summary>
        /// <returns>A collection of selected types that should be exported.</returns>
        private IList<Type> GetSelectedTypes()
        {
            return SystemGuidsListBox.ItemsSource
                .Cast<TypeItem>()
                .Where( t => t.IsExporting )
                .Select( t => t.Type )
                .ToList();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Click event of the SelectAll control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void SelectAll_Click( object sender, RoutedEventArgs e )
        {
            SystemGuidsListBox.ItemsSource
                .Cast<TypeItem>()
                .Where( i => !i.IsInvalid )
                .ToList()
                .ForEach( i => i.IsExporting = true );
        }

        /// <summary>
        /// Handles the Click event of the SelectNone control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void SelectNone_Click( object sender, RoutedEventArgs e )
        {
            SystemGuidsListBox.ItemsSource
                .Cast<TypeItem>()
                .Where( i => !i.IsInvalid )
                .ToList()
                .ForEach( i => i.IsExporting = false );
        }

        /// <summary>
        /// Handles the Click event of the Preview control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void Preview_Click( object sender, RoutedEventArgs e )
        {
            var files = new List<GeneratedFile>();

            // Generate each file that will provide SystemGuid
            // information to Obsidian.
            var generator = new TypeScriptViewModelGenerator();
            foreach ( var type in GetSelectedTypes() )
            {
                var source = generator.GenerateSystemGuidForType( type );
                files.Add( new GeneratedFile( GetFileNameForType( type ), GetPath(), source ) );
            }

            // Generate an index file that references all the types.
            var indexSource = generator.GenerateSystemGuidIndexForTypes( GetSystemGuidTypes() );
            files.Add( new GeneratedFile( "generated-index.d.ts", GetPath(), indexSource ) );

            await this.Navigation().PushPageAsync( new GeneratedFilePreviewPage( files ) );
        }

        #endregion

        #region Support Classes

        /// <summary>
        /// Contains the types to be shown in the list.
        /// </summary>
        private class TypeItem : INotifyPropertyChanged
        {
            #region Events

            /// <summary>
            /// Occurs when a property value changes.
            /// </summary>
            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Properties

            /// <summary>
            /// Gets the type this item represents.
            /// </summary>
            /// <value>The type this item represents.</value>
            public Type Type { get; }

            /// <summary>
            /// Gets the display name of this item.
            /// </summary>
            /// <value>The display name of this item.</value>
            public string Name { get; }

            /// <summary>
            /// Gets or sets a value indicating whether this type is being exported.
            /// </summary>
            /// <value><c>true</c> if this type is being exported; otherwise, <c>false</c>.</value>
            public bool IsExporting
            {
                get => _isExporting;
                set
                {
                    _isExporting = value;
                    OnPropertyChanged();
                }
            }
            private bool _isExporting;

            /// <summary>
            /// Gets a value indicating whether this item is invalid.
            /// </summary>
            /// <value><c>true</c> if this item is invalid; otherwise, <c>false</c>.</value>
            public bool IsInvalid => InvalidReason.IsNotNullOrWhiteSpace();

            /// <summary>
            /// Gets or sets the reason this item is invalid.
            /// </summary>
            /// <value>The reason this item is invalid.</value>
            public string InvalidReason { get; set; }

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="TypeItem"/> class.
            /// </summary>
            /// <param name="type">The type to be represented by this item.</param>
            public TypeItem( Type type )
            {
                Type = type;
                Name = type.Name;
                IsExporting = true;

                if ( Name.StartsWith( "Rock.ViewModels." ) )
                {
                    Name = Name.Substring( 15 );
                }
            }

            #endregion

            #region Methods

            /// <summary>
            /// Called when a property value has changed.
            /// </summary>
            /// <param name="propertyName">The name of the property.</param>
            protected virtual void OnPropertyChanged( [CallerMemberName] string propertyName = null )
            {
                PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( propertyName ) );
            }

            #endregion
        }

        #endregion
    }
}