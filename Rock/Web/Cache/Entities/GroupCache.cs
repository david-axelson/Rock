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
using System.Collections.Generic;
using System.Runtime.Serialization;

using Rock.Data;

namespace Rock.Web.Cache
{
    /// <summary>
    /// Contains minimal information about a group in cache for a short
    /// period of time.
    /// </summary>
    [Serializable]
    [DataContract]
    public class GroupCache : ModelCache<GroupCache, Rock.Model.Group>
    {
        #region Properties

        /// <inheritdoc cref="Rock.Model.Group.Name" />
        [DataMember]
        public string Name { get; private set; }

        /// <inheritdoc cref="Rock.Model.Group.IsActive" />
        [DataMember]
        public bool IsActive { get; private set; }

        /// <inheritdoc cref="Rock.Model.Group.GroupTypeId" />
        [DataMember]
        public int GroupTypeId { get; private set; }

        /// <inheritdoc cref="Rock.Model.Group.GroupType" />
        public GroupTypeCache GroupType => GroupTypeCache.Get( GroupTypeId );

        #endregion Properties

        #region Public Methods

        /// <inheritdoc/>
        public override TimeSpan? Lifespan
        {
            // Currently, we only check-in related groups for any period of time.
            // This is a 3ns check assuming the group types are already cached.
            get => GroupType?.GetCheckInConfigurationType() == null ? new TimeSpan( 0, 10, 0 ) : base.Lifespan;
        }

        /// <summary>
        /// Not supported on GroupCache.
        /// </summary>
        /// <returns>A list of all groups in their cache form.</returns>
        public static new List<GroupCache> All()
        {
            return All( null );
        }

        /// <summary>
        /// Not supported on GroupCache.
        /// </summary>
        /// <returns>A list of all groups in their cache form.</returns>
        public static new List<GroupCache> All( RockContext rockContext )
        {
            // Since there will be a very large number of groups in the
            // database, we don't support loading all of them.
            throw new NotSupportedException( "GroupCache does not support All()" );
        }

        /// <inheritdoc/>
        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            if ( !( entity is Rock.Model.Group group ) )
            {
                return;
            }

            Name = group.Name;
            IsActive = group.IsActive;
            GroupTypeId = group.GroupTypeId;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }

        #endregion Public Methods
    }
}
