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
    [Table( "_org_secc_xAPI_Experience" )]
    [DataContract]
    public class Experience : QualifiableModel<Experience>, IRockEntity
    {
        [DataMember]
        [Index]
        public int PersonAliasId { get; set; }

        [LavaInclude]
        public virtual PersonAlias PersonAlias { get; set; }

        [DataMember]
        [Index]
        public int VerbValueId { get; set; }

        [LavaInclude]
        public virtual DefinedValue VerbValue { get; set; }

        [DataMember]
        [Index]
        public int? xObjectId { get; set; }

        [LavaInclude]
        public ExperienceObject xObject { get; set; }

        [DataMember]
        [Index]
        public int? ResultId { get; set; }

        [LavaInclude]
        public virtual ExperienceResult Result { get; set; }
    }

    public partial class ExperienceConfiguration : EntityTypeConfiguration<Experience>
    {
        public ExperienceConfiguration()
        {
            this.HasRequired<PersonAlias>( t => t.PersonAlias ).WithMany().HasForeignKey( x => x.PersonAliasId ).WillCascadeOnDelete( false );
            this.HasRequired<DefinedValue>( t => t.VerbValue ).WithMany().HasForeignKey( x => x.VerbValueId ).WillCascadeOnDelete( false );
            this.HasOptional<ExperienceObject>( t => t.xObject ).WithMany().HasForeignKey( x => x.xObjectId ).WillCascadeOnDelete( false );
            this.HasOptional<ExperienceResult>( t => t.Result ).WithMany().HasForeignKey( x => x.ResultId ).WillCascadeOnDelete( false );
            this.HasEntitySetName( "Experiences" );
        }
    }
}
