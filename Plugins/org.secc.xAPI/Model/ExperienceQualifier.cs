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
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration;
using System.Runtime.Serialization;
using Rock.Data;
using Rock.Model;

namespace org.secc.xAPI.Model
{
    [Table( "_org_secc_xAPI_ExperienceQualifier" )]
    [DataContract]
    public class ExperienceQualifier : QualifiableModel<ExperienceQualifier>, IRockEntity
    {
        [DataMember]
        [Index]
        public int ParentId { get; set; }

        [DataMember]
        [Index]
        public int EntityTypeId { get; set; }

        [LavaInclude]
        public EntityType EntityType { get; set; }

        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public string Value { get; set; }
    }

    public partial class ExperienceQualifierConfiguration : EntityTypeConfiguration<ExperienceQualifier>
    {
        public ExperienceQualifierConfiguration()
        {
            this.HasRequired<EntityType>( t => t.EntityType ).WithMany().HasForeignKey( x => x.EntityTypeId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "Objects" );
        }
    }
}
