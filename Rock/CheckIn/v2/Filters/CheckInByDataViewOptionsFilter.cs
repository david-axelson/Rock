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

using System;
using System.Linq;
using System.Linq.Dynamic.Core;

using Rock.Model;
using Rock.Utility;

namespace Rock.CheckIn.v2.Filters
{
    /// <summary>
    /// Performs filtering of check-in options based on any data views that
    /// the person must be a member of.
    /// </summary>
    internal class CheckInByDataViewOptionsFilter : CheckInPersonOptionsFilter
    {
        #region Properties

        /// <summary>
        /// Gets the person identifier.
        /// </summary>
        /// <value>The person identifier.</value>
        private Lazy<int> PersonId { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CheckInByDataViewOptionsFilter"/> class.
        /// </summary>
        public CheckInByDataViewOptionsFilter()
        {
            PersonId = new Lazy<int>( () => IdHasher.Instance.GetId( Person.IdKey ) ?? 0 );
        }

        #endregion

        #region Methods

        /// <inheritdoc/>
        public override bool IsGroupValid( CheckInGroupItem group )
        {
            if ( group.CheckInData.DataViewGuids.Count == 0 )
            {
                return true;
            }

            var dataViewService = new DataViewService( RockContext );

            foreach ( var dataViewGuid in group.CheckInData.DataViewGuids )
            {
                var dataView = dataViewService.Get( dataViewGuid );

                if ( dataView == null )
                {
                    continue;
                }

                if ( !IsPersonInDataView( dataView ) )
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether the filtered person is in the <see cref="DataView"/>.
        /// </summary>
        /// <param name="dataView">The data view to query.</param>
        /// <returns><c>true</c> if the person is in the data view; otherwise, <c>false</c>.</returns>
        private bool IsPersonInDataView( DataView dataView )
        {
            if ( dataView.IsPersisted() && dataView.PersistedLastRefreshDateTime.HasValue )
            {
                return RockContext.Set<DataViewPersistedValue>()
                    .Any( pv => pv.DataViewId == dataView.Id
                        && pv.EntityId == PersonId.Value );
            }
            else
            {
                var getQueryArgs = new DataViewGetQueryArgs
                {
                    DatabaseTimeoutSeconds = 30,
                    DbContext = RockContext
                };

                var dataViewQry = dataView.GetQuery( getQueryArgs );

                // If the data view isn't working, then assume they are in it.
                if ( dataViewQry == null )
                {
                    return true;
                }

                return dataViewQry.Any( a => a.Id == PersonId.Value );
            }
        }

        #endregion
    }
}
