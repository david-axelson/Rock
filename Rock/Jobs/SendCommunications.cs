﻿// <copyright>
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
using System.ComponentModel;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Rock.Attribute;
using Rock.Data;
using Rock.Logging;
using Rock.Model;
using Rock.Utility;

namespace Rock.Jobs
{
    /// <summary>
    /// Job to process communications
    /// </summary>
    [DisplayName( "Send Communications" )]
    [Description( "Job to send any future communications or communications not sent immediately by Rock." )]

    #region Job Attributes

    [IntegerField(
        "Delay Period",
        Key = AttributeKey.DelayPeriod,
        Description = "The number of minutes to wait before sending any new communication (If the communication block's 'Send When Approved' option is turned on, then a delay should be used here to prevent a send overlap).",
        IsRequired = false,
        DefaultIntegerValue = 30,
        Category = "",
        Order = 0 )]

    [IntegerField(
        "Expiration Period",
        Key = AttributeKey.ExpirationPeriod,
        Description = "The number of days after a communication was created or scheduled to be sent when it should no longer be sent.",
        IsRequired = false,
        DefaultIntegerValue = 3,
        Category = "",
        Order = 1 )]

    [IntegerField(
        "Parallel Communications",
        Key = AttributeKey.ParallelCommunications,
        Description = "The number of communications that can be sent at the same time.",
        IsRequired = false,
        DefaultIntegerValue = 3,
        Category = "",
        Order = 2 )]

    #endregion

    public class SendCommunications : RockJob
    {
        #region Keys

        /// <summary>
        /// Keys to use for job Attributes.
        /// </summary>
        private static class AttributeKey
        {
            public const string DelayPeriod = "DelayPeriod";
            public const string ExpirationPeriod = "ExpirationPeriod";
            public const string ParallelCommunications = "ParallelCommunications";
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SendCommunications"/> class.
        /// </summary>
        public SendCommunications()
        {
        }

        /// <inheritdoc cref="RockJob.Execute()" />
        public override void Execute()
        {
            int expirationDays = this.GetAttributeValue( "ExpirationPeriod" ).AsInteger();
            int delayMinutes = this.GetAttributeValue( "DelayPeriod" ).AsInteger();
            int maxParallelization = this.GetAttributeValue( "ParallelCommunications" ).AsInteger();

            List<Model.Communication> sendCommunications = null;
            var startDateTime = RockDateTime.Now;
            var stopWatch = Stopwatch.StartNew();
            using ( var rockContext = new RockContext() )
            {
                sendCommunications = new CommunicationService( rockContext )
                    .GetQueued( expirationDays, delayMinutes, false, false )
                    .AsNoTracking()
                    .ToList()
                    .OrderBy( c => c.Id )
                    .ToList();
            }

            Log( LogLevel.Information, $"Retrieved {sendCommunications.Count} queued communications.", startDateTime, stopWatch.ElapsedMilliseconds );

            if ( sendCommunications == null )
            {
                this.Result = "No communications to send";
            }

            var exceptionMsgs = new List<string>();
            int communicationsSent = 0;

            startDateTime = RockDateTime.Now;
            stopWatch = Stopwatch.StartNew();
            var sendCommunicationTasks = new List<Task<SendCommunicationAsyncResult>>();

            using ( var mutex = new SemaphoreSlim( maxParallelization ) )
            {
                for ( var i = 0; i < sendCommunications.Count(); i++ )
                {
                    mutex.Wait();
                    var comm = sendCommunications[i];

                    sendCommunicationTasks.Add( Task.Run<SendCommunicationAsyncResult>( async () => await SendCommunicationAsync( comm, mutex ).ConfigureAwait( false ) ) );
                }

                /*
                 * Now that we have fired off all of the task, we need to wait for them to complete, get their results,
                 * and then process that result. Once all of the task have been completed we can continue.
                 */
                while ( sendCommunicationTasks.Count > 0 )
                {
                    // Wait for a task to complete using WhenAny and then return the completed task. Since we are not running in an asynchronous method we need to use RunSync.
                    var completedTask = AsyncHelper.RunSync( () => Task.WhenAny<SendCommunicationAsyncResult>( sendCommunicationTasks.ToArray() ) );

                    // Get and process the result of the completed task.
                    var communicationResult = completedTask.Result;
                    if ( communicationResult.Exception != null )
                    {
                        var agException = communicationResult.Exception as AggregateException;
                        if ( agException == null )
                        {
                            exceptionMsgs.Add( $"Exception occurred sending communication ID:{communicationResult.Communication.Id}:{Environment.NewLine}    {communicationResult.Exception.Messages().AsDelimited( Environment.NewLine + "   " )}" );
                        }
                        else
                        {
                            var allExceptions = agException.Flatten();
                            foreach ( var ex in allExceptions.InnerExceptions )
                            {
                                exceptionMsgs.Add( $"Exception occurred sending communication ID:{communicationResult.Communication.Id}:{Environment.NewLine}    {ex.Messages().AsDelimited( Environment.NewLine + "   " )}" );
                            }
                        }

                        ExceptionLogService.LogException( communicationResult.Exception, System.Web.HttpContext.Current );
                    }
                    else
                    {
                        communicationsSent++;
                    }

                    sendCommunicationTasks.Remove( completedTask );
                }
            }

            Log( LogLevel.Information, $"Sent {communicationsSent} communications.", startDateTime, stopWatch.ElapsedMilliseconds );

            if ( communicationsSent > 0 )
            {
                this.Result = string.Format( "Sent {0} {1}", communicationsSent, "communication".PluralizeIf( communicationsSent > 1 ) );
            }
            else
            {
                this.Result = "No communications to send";
            }

            if ( exceptionMsgs.Any() )
            {
                throw new Exception( "One or more exceptions occurred sending communications..." + Environment.NewLine + exceptionMsgs.AsDelimited( Environment.NewLine ) );
            }

            // check for communications that have not been sent but are past the expire date. Mark them as failed and set a warning.
            var expireDateTimeEndWindow = RockDateTime.Now.AddDays( 0 - expirationDays );

            // limit the query to only look a week prior to the window to avoid performance issue (it could be slow to query at ALL the communication recipient before the expire date, as there could several years worth )
            var expireDateTimeBeginWindow = expireDateTimeEndWindow.AddDays( -7 );

            startDateTime = RockDateTime.Now;
            stopWatch = Stopwatch.StartNew();
            using ( var rockContext = new RockContext() )
            {
                var qryExpiredRecipients = new CommunicationRecipientService( rockContext ).Queryable()
                    .Where( cr =>
                        cr.Communication.Status == CommunicationStatus.Approved &&
                        cr.Status == CommunicationRecipientStatus.Pending &&
                        (
                            ( !cr.Communication.FutureSendDateTime.HasValue && cr.Communication.ReviewedDateTime.HasValue && cr.Communication.ReviewedDateTime < expireDateTimeEndWindow && cr.Communication.ReviewedDateTime > expireDateTimeBeginWindow )
                            || ( cr.Communication.FutureSendDateTime.HasValue && cr.Communication.FutureSendDateTime < expireDateTimeEndWindow && cr.Communication.FutureSendDateTime > expireDateTimeBeginWindow )
                        ) );

                rockContext.BulkUpdate( qryExpiredRecipients, c => new CommunicationRecipient { Status = CommunicationRecipientStatus.Failed, StatusNote = "Communication was not sent before the expire window (possibly due to delayed approval)." } );
            }

            Log( LogLevel.Information, @"Updated expired communication recipients' status to ""Failed"".", startDateTime, stopWatch.ElapsedMilliseconds );
        }

        /// <summary>
        /// The resulte of the Send communication request.
        /// </summary>
        private class SendCommunicationAsyncResult
        {
            /// <summary>
            /// Gets or sets the exception.
            /// </summary>
            /// <value>
            /// The exception.
            /// </value>
            public Exception Exception { get; set; }

            /// <summary>
            /// Gets or sets the communication.
            /// </summary>
            /// <value>
            /// The communication.
            /// </value>
            public Model.Communication Communication { get; set; }
        }

        /// <summary>
        /// Sends the communication asynchronous.
        /// </summary>
        /// <param name="comm">The comm.</param>
        /// <param name="mutex">The mutex.</param>
        /// <returns></returns>
        private async Task<SendCommunicationAsyncResult> SendCommunicationAsync( Model.Communication comm, SemaphoreSlim mutex )
        {
            var communicationResult = new SendCommunicationAsyncResult
            {
                Communication = comm
            };

            var startDateTime = RockDateTime.Now;
            var communicationStopWatch = Stopwatch.StartNew();
            Log( LogLevel.Debug, $"Starting to send {comm.Name}.", startDateTime );
            try
            {
                await Model.Communication.SendAsync( comm ).ConfigureAwait( false );
            }
            catch ( Exception ex )
            {
                communicationResult.Exception = ex;
            }

            Log( LogLevel.Information, $"{comm.Name} sent.", startDateTime, communicationStopWatch.ElapsedMilliseconds );
            mutex.Release();
            return communicationResult;
        }
    }
}