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
using System.Linq;
using System.Runtime.Serialization;

using Rock.Data;
using Rock.Model;

namespace Rock.Web.Cache
{
    /// <summary>
    /// Information about a named <see cref="Rock.Model.Schedule"/> that is required by the rendering engine.
    /// This information will be cached by the engine.
    /// </summary>
    [Serializable]
    [DataContract]
    public class NamedScheduleCache : ModelCache<NamedScheduleCache, Rock.Model.Schedule>
    {
        #region Fields

        private Ical.Net.CalendarComponents.CalendarEvent _calendarEvent;

        #endregion

        #region Properties

        /// <inheritdoc cref="Rock.Model.Schedule.Name" />
        [DataMember]
        public string Name { get; private set; }

        /// <inheritdoc cref="Rock.Model.Schedule.CategoryId" />
        [DataMember]
        public int? CategoryId { get; private set; }

        /// <inheritdoc cref="Rock.Model.Schedule.IsActive" />
        [DataMember]
        public bool IsActive { get; private set; }

        /// <inheritdoc cref="Rock.Model.Schedule.FriendlyScheduleText" />
        [DataMember]
        public string FriendlyScheduleText { get; private set; }

        /// <inheritdoc cref="Rock.Model.Schedule.StartTimeOfDay" />
        [DataMember]
        public TimeSpan StartTimeOfDay { get; private set; }

        /// <inheritdoc cref="Rock.Model.Schedule.Category" />
        public CategoryCache Category => this.CategoryId.HasValue ? CategoryCache.Get( CategoryId.Value ) : null;

        /// <inheritdoc cref="Rock.Model.Schedule.CheckInStartOffsetMinutes" />
        public int? CheckInStartOffsetMinutes { get; private set; }

        /// <inheritdoc cref="Rock.Model.Schedule.CheckInEndOffsetMinutes" />
        public int? CheckInEndOffsetMinutes { get; private set; }

        /// <inheritdoc cref="Rock.Model.Schedule.iCalendarContent" />
        private string CalendarContent { get; set; }

        #endregion Properties

        #region Public Methods

        /// <summary>
        /// The amount of time that this item will live in the cache before expiring. If null, then the
        /// default lifespan is used.
        /// </summary>
        public override TimeSpan? Lifespan
        {
            get
            {
                if ( Name.IsNullOrWhiteSpace() )
                {
                    // just in case this isn't a named Schedule, expire after 10 minutes
                    return new TimeSpan( 0, 10, 0 );
                }

                return base.Lifespan;
            }
        }
        
        /// <summary>
        /// Set's the cached objects properties from the model/entities properties.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public override void SetFromEntity( IEntity entity )
        {
            base.SetFromEntity( entity );

            if ( !( entity is Schedule schedule ) )
            {
                return;
            }

            Name = schedule.Name;
            CategoryId = schedule.CategoryId;
            IsActive = schedule.IsActive;
            FriendlyScheduleText = schedule.ToFriendlyScheduleText();
            CalendarContent = schedule.iCalendarContent;
            StartTimeOfDay = schedule.StartTimeOfDay;
            CheckInStartOffsetMinutes = schedule.CheckInStartOffsetMinutes;
            CheckInEndOffsetMinutes = schedule.CheckInEndOffsetMinutes;
        }

        /// <inheritdoc cref="Rock.Model.Schedule.GetICalOccurrences(DateTime, DateTime?, DateTime?)" />
        public IList<Ical.Net.DataTypes.Occurrence> GetICalOccurrences( DateTime beginDateTime, DateTime? endDateTime, DateTime? scheduleStartDateTimeOverride )
        {
            return InetCalendarHelper.GetOccurrences( CalendarContent, beginDateTime, endDateTime, scheduleStartDateTimeOverride );
        }

        /// <summary>
        /// Returns value indicating if check-in was active at the specified time.
        /// </summary>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        public bool WasCheckInActive( DateTime time )
        {
            if ( !( CheckInStartOffsetMinutes.HasValue && IsActive ) )
            {
                return false;
            }

            if ( _calendarEvent == null )
            {
                _calendarEvent = InetCalendarHelper.CreateCalendarEvent( CalendarContent );
            }

            var calEvent = _calendarEvent;

            if ( calEvent != null && calEvent.DtStart != null )
            {
                // Is the current time earlier the event's allowed check-in window?
                var checkInStart = calEvent.DtStart.AddMinutes( 0 - CheckInStartOffsetMinutes.Value );
                if ( time.TimeOfDay.TotalSeconds < checkInStart.Value.TimeOfDay.TotalSeconds )
                {
                    return false;
                }

                var checkInEnd = calEvent.DtEnd;
                if ( CheckInEndOffsetMinutes.HasValue )
                {
                    checkInEnd = calEvent.DtStart.AddMinutes( CheckInEndOffsetMinutes.Value );
                }

                // If compare is greater than zero, then check-in offset end resulted in an end time in next day, in 
                // which case, don't need to compare time
                int checkInEndDateCompare = checkInEnd.Date.CompareTo( checkInStart.Date );

                if ( checkInEndDateCompare < 0 )
                {
                    // End offset is prior to start (Would have required a neg number entered)
                    return false;
                }

                // Is the current time later then the event's allowed check-in window?
                if ( checkInEndDateCompare == 0 && time.TimeOfDay.TotalSeconds > checkInEnd.Value.TimeOfDay.TotalSeconds )
                {
                    // Same day, but end time has passed
                    return false;
                }

                return GetICalOccurrences( time.Date, null, null ).Count > 0;
            }

            return false;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return this.FriendlyScheduleText;
        }

        #endregion Public Methods
    }
}
