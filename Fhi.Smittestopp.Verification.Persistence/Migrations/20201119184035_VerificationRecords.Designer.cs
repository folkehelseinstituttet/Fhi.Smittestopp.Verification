﻿// <auto-generated />
using System;
using Fhi.Smittestopp.Verification.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fhi.Smittestopp.Verification.Persistence.Migrations
{
    [DbContext(typeof(VerificationDbContext))]
    [Migration("20201119184035_VerificationRecords")]
    partial class VerificationRecords
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .UseIdentityColumns()
                .HasAnnotation("Relational:MaxIdentifierLength", 128)
                .HasAnnotation("ProductVersion", "5.0.0");

            modelBuilder.Entity("Fhi.Smittestopp.Verification.Persistence.Entities.VerificationRecordEntity", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .UseIdentityColumn();

                    b.Property<string>("Pseudonym")
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTimeOffset>("VerifiedAtTime")
                        .HasColumnType("datetimeoffset");

                    b.HasKey("Id");

                    b.ToTable("VerificationRecords");
                });
#pragma warning restore 612, 618
        }
    }
}
