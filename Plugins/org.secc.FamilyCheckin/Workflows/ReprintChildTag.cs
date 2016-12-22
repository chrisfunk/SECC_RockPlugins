﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;

using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;
using Rock;
using System.Runtime.Caching;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Creates Check-in Labels
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Reprints Child Tag" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Reprint ChildTag" )]
    [BinaryFileField( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, "Child Label", "Label to reprint", true )]
    [GroupTypeField( "Breakout GroupType", "The grouptype which represents elementary breakout groups." )]

    public class ReprintChildTag : CheckInActionComponent
    {

        string cacheKey = "org_secc:familycheckin:breakoutgroups";

        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The workflow action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            var checkInState = GetCheckInState( entity, out errorMessages );

            CheckInPerson checkinPerson = checkInState.CheckIn.CurrentFamily.People.Where( p => p.Selected ).FirstOrDefault();

            if ( checkInState != null )
            {
                var globalAttributes = Rock.Web.Cache.GlobalAttributesCache.Read( rockContext );
                var globalMergeValues = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                var groupMemberService = new GroupMemberService( rockContext );

                AttendanceService attendanceService = new AttendanceService( rockContext );
                PersonService personService = new PersonService( rockContext );

                foreach ( var family in checkInState.CheckIn.Families.Where( f => f.Selected ) )
                {
                    foreach ( var person in family.People.Where( p => p.Selected && p.GroupTypes.Any() ) )
                    {
                        //we have to load the person entity in because the person.Person doesn't load enough information
                        var personEntity = personService.Get( person.Person.Id );
                        var attendanceCode = attendanceService.Queryable()
                            .Where( a => a.CreatedDateTime >= Rock.RockDateTime.Today && personEntity.PrimaryAliasId == a.PersonAliasId )
                            .DistinctBy( a => a.AttendanceCode ).Select( a => a.AttendanceCode.ToString() ).ToList();

                        if ( attendanceCode.Any() )
                        {
                            var groupType = person.GroupTypes.FirstOrDefault();
                            groupType.Selected = true;
                            groupType.Labels = new List<CheckInLabel>();
                            var PrinterIPs = new Dictionary<int, string>();

                            person.SecurityCode = attendanceCode.FirstOrDefault();
                            Dictionary<string, object> mergeObjects = new Dictionary<string, object>();
                            mergeObjects.Add( "Person", person );
                            mergeObjects.Add( "GroupTypes", person.GroupTypes.Where( g => g.Selected ).ToList() );
                            List<Group> mergeGroups = new List<Group>();
                            List<Location> mergeLocations = new List<Location>();
                            List<Schedule> mergeSchedules = new List<Schedule>();

                            var sets = new AttendanceService( rockContext )
                                .Queryable().AsNoTracking().Where( a =>
                                     a.PersonAlias.Person.Id == person.Person.Id
                                     && a.StartDateTime >= Rock.RockDateTime.Today
                                     && a.EndDateTime == null
                                    )
                                    .Select( a =>
                                         new
                                         {
                                             Group = a.Group,
                                             Location = a.Location,
                                             Schedule = a.Schedule
                                         }
                                    )
                                    .ToList()
                                    .OrderBy( a => a.Schedule.StartTimeOfDay );

                            //Load breakout group
                            var breakoutGroups = GetBreakoutGroups( person.Person, rockContext, action );

                            //Add in an empty object as a placeholder for our breakout group
                            mergeObjects.Add( "BreakoutGroup", "" );


                            foreach ( var set in sets )
                            {
                                mergeGroups.Add( set.Group );
                                mergeLocations.Add( set.Location );
                                mergeSchedules.Add( set.Schedule );

                                //Add the breakout group mergefield
                                if ( breakoutGroups.Any() )
                                {
                                    var breakoutGroup = breakoutGroups.Where( g => g.ScheduleId == set.Schedule.Id ).FirstOrDefault();
                                    if ( breakoutGroup != null )
                                    {
                                        breakoutGroup.LoadAttributes();
                                        mergeObjects["BreakoutGroup"] = breakoutGroup.GetAttributeValue( "Letter" );
                                    }
                                }
                            }
                            mergeObjects.Add( "Groups", mergeGroups );
                            mergeObjects.Add( "Locations", mergeLocations );
                            mergeObjects.Add( "Schedules", mergeSchedules );

                            var labelCache = KioskLabel.Read( new Guid( GetAttributeValue( action, "ChildLabel" ) ) );
                            if ( labelCache != null )
                            {
                                var label = new CheckInLabel( labelCache, mergeObjects );
                                label.FileGuid = new Guid( GetAttributeValue( action, "ChildLabel" ) );
                                label.PrintFrom = checkInState.Kiosk.Device.PrintFrom;
                                label.PrintTo = checkInState.Kiosk.Device.PrintToOverride;

                                if ( label.PrintTo == PrintTo.Default )
                                {
                                    label.PrintTo = groupType.GroupType.AttendancePrintTo;
                                }

                                if ( label.PrintTo == PrintTo.Kiosk )
                                {
                                    var device = checkInState.Kiosk.Device;
                                    if ( device != null )
                                    {
                                        label.PrinterDeviceId = device.PrinterDeviceId;
                                    }
                                }

                                if ( label.PrinterDeviceId.HasValue )
                                {
                                    if ( PrinterIPs.ContainsKey( label.PrinterDeviceId.Value ) )
                                    {
                                        label.PrinterAddress = PrinterIPs[label.PrinterDeviceId.Value];
                                    }
                                    else
                                    {
                                        var printerDevice = new DeviceService( rockContext ).Get( label.PrinterDeviceId.Value );
                                        if ( printerDevice != null )
                                        {
                                            PrinterIPs.Add( printerDevice.Id, printerDevice.IPAddress );
                                            label.PrinterAddress = printerDevice.IPAddress;
                                        }
                                    }
                                }
                                groupType.Labels.Add( label );
                            }
                        }
                    }
                }
                return true;
            }
            return false;
        }
        private List<Group> GetBreakoutGroups( Person person, RockContext rockContext, WorkflowAction action )
        {
            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( action, "BreakoutGroupType" ) ) )
            {
                ObjectCache cache = Rock.Web.Cache.RockMemoryCache.Default;
                List<Group> allBreakoutGroups = cache[cacheKey] as List<Group>;
                if ( allBreakoutGroups == null || !allBreakoutGroups.Any() )
                {
                    //If the cache is empty, fill it up!
                    Guid breakoutGroupTypeGuid = GetAttributeValue( action, "BreakoutGroupType" ).AsGuid();
                    var breakoutGroupType = new GroupTypeService( rockContext ).Queryable()
                        .Where( gt => gt.Guid == breakoutGroupTypeGuid )
                        .FirstOrDefault();
                    if ( breakoutGroupType != null )
                    {
                        allBreakoutGroups = breakoutGroupType.Groups.ToList();
                        var cachePolicy = new CacheItemPolicy();
                        cachePolicy.AbsoluteExpiration = DateTimeOffset.Now.AddMinutes( 10 );
                        cache.Set( cacheKey, allBreakoutGroups, cachePolicy );
                    }
                    else
                    {
                        return new List<Group>();
                    }
                }
                return allBreakoutGroups.Where( g => g.Members.Where( gm => gm.PersonId == person.Id ).Any() ).ToList();
            }
            else
            {
                return new List<Group>();
            }
        }
    }
}