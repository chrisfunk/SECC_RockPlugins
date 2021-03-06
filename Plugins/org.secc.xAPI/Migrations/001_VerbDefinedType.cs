﻿// <copyright>
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
namespace org.secc.xAPI.Migrations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Web.Hosting;
    using org.secc.xAPI.Utilities;
    using Rock;
    using Rock.Plugin;

    [MigrationNumber( 1, "1.10.2" )]
    public partial class VerbDefinedType : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddDefinedType( "Learning Management",
                "xAPI Verbs", "A list of verbs from the xAPI verb registry. (https://registry.tincanapi.com/)",
                Constants.DEFINED_TYPE_VERBS );
            RockMigrationHelper.AddDefinedTypeAttribute( Constants.DEFINED_TYPE_VERBS, Rock.SystemGuid.FieldType.TEXT,
                "Name", Constants.DEFINED_VALUE_VERBS_ATTRIBUTE_KEY_NAME, "The human readable name for the verb.", 0, true, "",
                 false, true, Constants.DEFINED_VALUE_VERBS_ATTRIBUTE_GUID_NAME );

            //I would like to thank Moores Law for making the following code possible.
            var verbJson = File.ReadAllText( HostingEnvironment.MapPath( "~/Plugins/org_secc/xAPI/verbList.json" ) );
            verbJson = verbJson.Replace( "en-us", "en-US" ).Replace( "en-Us", "en-US" );
            dynamic dyn = verbJson.FromJsonDynamic();
            foreach ( var obj in dyn )
            {
                try
                {

                    var uri = obj.uri as string;
                    var metadata = obj.metadata.metadata;
                    var name = ( string ) ( metadata.name as IDictionary<string, object> )["en-US"];
                    var description = ( string ) ( metadata.description as IDictionary<string, object> )["en-US"];

                    //use a deterministic guid just in case (this isn't standard)
                    Guid guid = GenerateGuid( uri );
                    RockMigrationHelper.AddDefinedValue( Constants.DEFINED_TYPE_VERBS, uri, description, guid.ToString() );
                    RockMigrationHelper.AddDefinedValueAttributeValue( guid.ToString(), Constants.DEFINED_VALUE_VERBS_ATTRIBUTE_GUID_NAME, name );
                }
                catch ( Exception e )
                {

                }
            }
        }

        private Guid GenerateGuid( string uri )
        {
            using ( MD5 md5 = MD5.Create() )
            {
                byte[] hash = md5.ComputeHash( Encoding.Default.GetBytes( uri ) );
                return new Guid( hash );
            }
        }

        public override void Down()
        {

        }
    }
}