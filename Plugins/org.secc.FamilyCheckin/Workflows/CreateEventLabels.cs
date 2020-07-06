// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
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
using System.ComponentModel.Composition;
using System.Data.Entity;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using Rock.Workflow.Action.CheckIn;

namespace org.secc.FamilyCheckin
{
    /// <summary>
    /// Creates Check-in Labels
    /// </summary>
    [ActionCategory( "SECC > Check-In" )]
    [Description( "Creates Check-in Labels For Events" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Create Event Labels" )]
    [TextField( "Group Attribute Key", "Attribute key for the check-in group which refers to the registration group." )]
    public class CreateEventLabels : CheckInActionComponent
    {

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

            if ( checkInState != null )
            {
                var family = checkInState.CheckIn.CurrentFamily;
                if ( family != null )
                {
                    var commonMergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                    var groupMemberService = new GroupMemberService( rockContext );

                    var familyLabels = new List<Guid>();

                    var people = family.GetPeople( true );
                    foreach ( var person in people )
                    {
                        var personGroupTypes = person.GetGroupTypes( true );
                        var groupTypes = new List<CheckInGroupType>();

                        // Get Primary area group types first
                        personGroupTypes.Where( t => checkInState.ConfiguredGroupTypes.Contains( t.GroupType.Id ) ).ToList().ForEach( t => groupTypes.Add( t ) );

                        // Then get additional areas
                        personGroupTypes.Where( t => !checkInState.ConfiguredGroupTypes.Contains( t.GroupType.Id ) ).ToList().ForEach( t => groupTypes.Add( t ) );

                        var personLabels = new List<Guid>();

                        foreach ( var groupType in groupTypes )
                        {
                            groupType.Labels = new List<CheckInLabel>();

                            var groupTypeLabels = GetGroupTypeLabels( groupType.GroupType );

                            var PrinterIPs = new Dictionary<int, string>();

                            foreach ( var labelCache in groupTypeLabels )
                            {
                                foreach ( var group in groupType.GetGroups( true ) )
                                {
                                    foreach ( var location in group.GetLocations( true ) )
                                    {
                                        if ( labelCache.LabelType == KioskLabelType.Family )
                                        {
                                            if ( familyLabels.Contains( labelCache.Guid ) ||
                                                personLabels.Contains( labelCache.Guid ) )
                                            {
                                                break;
                                            }
                                            else
                                            {
                                                familyLabels.Add( labelCache.Guid );
                                            }
                                        }
                                        else if ( labelCache.LabelType == KioskLabelType.Person )
                                        {
                                            if ( personLabels.Contains( labelCache.Guid ) )
                                            {
                                                break;

                                            }
                                            else
                                            {
                                                personLabels.Add( labelCache.Guid );
                                            }
                                        }

                                        var mergeObjects = new Dictionary<string, object>();
                                        foreach ( var keyValue in commonMergeFields )
                                        {
                                            mergeObjects.Add( keyValue.Key, keyValue.Value );
                                        }

                                        group.Group.LoadAttributes();
                                        var groupGuid = group.Group.GetAttributeValue( GetAttributeValue( action, "GroupAttributeKey" ) );
                                        if ( !string.IsNullOrWhiteSpace( groupGuid ) )
                                        {
                                            var registrationGroup = new GroupService( rockContext ).Get( groupGuid.AsGuid() );
                                            if ( registrationGroup != null )
                                            {
                                                mergeObjects.Add( "RegistrationGroup", registrationGroup );
                                                var registrationGroupMember = registrationGroup.Members.Where( gm => gm.PersonId == person.Person.Id ).FirstOrDefault();
                                                if ( registrationGroupMember != null )
                                                {
                                                    mergeObjects.Add( "RegistrationGroupMember", registrationGroupMember );
                                                }
                                            }
                                        }

                                        mergeObjects.Add( "Location", location );
                                        mergeObjects.Add( "Group", group );
                                        mergeObjects.Add( "Person", person );
                                        mergeObjects.Add( "People", people );
                                        mergeObjects.Add( "GroupType", groupType );

                                        var groupMembers = groupMemberService.Queryable().AsNoTracking()
                                            .Where( m =>
                                                m.PersonId == person.Person.Id &&
                                                m.GroupId == group.Group.Id )
                                            .ToList();
                                        mergeObjects.Add( "GroupMembers", groupMembers );

                                        var label = new CheckInLabel( labelCache, mergeObjects );
                                        label.FileGuid = labelCache.Guid;
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
                                        else if ( label.PrintTo == PrintTo.Location )
                                        {
                                            var deviceId = location.Location.PrinterDeviceId;
                                            if ( deviceId != null )
                                            {
                                                label.PrinterDeviceId = deviceId;
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
                    }
                }

                return true;

            }

            return false;
        }

        private List<KioskLabel> GetGroupTypeLabels( GroupTypeCache groupType )
        {
            var labels = new List<KioskLabel>();

            //groupType.LoadAttributes();
            foreach ( var attribute in groupType.Attributes.OrderBy( a => a.Value.Order ) )
            {
                if ( attribute.Value.FieldType.Guid == Rock.SystemGuid.FieldType.LABEL.AsGuid() &&
                    attribute.Value.QualifierValues.ContainsKey( "binaryFileType" ) &&
                    attribute.Value.QualifierValues["binaryFileType"].Value.Equals( Rock.SystemGuid.BinaryFiletype.CHECKIN_LABEL, StringComparison.OrdinalIgnoreCase ) )
                {
                    Guid? binaryFileGuid = groupType.GetAttributeValue( attribute.Key ).AsGuidOrNull();
                    if ( binaryFileGuid != null )
                    {
                        var labelCache = KioskLabel.Get( binaryFileGuid.Value );
                        labelCache.Order = attribute.Value.Order;
                        if ( labelCache != null )
                        {
                            labels.Add( labelCache );
                        }
                    }
                }
            }

            return labels;
        }
    }
}