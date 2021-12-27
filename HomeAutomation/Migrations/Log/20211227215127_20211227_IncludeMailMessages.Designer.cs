﻿// <auto-generated />
using System;
using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace HomeAutomation.Migrations.Log
{
    [DbContext(typeof(LogContext))]
    [Migration("20211227215127_20211227_IncludeMailMessages")]
    partial class _20211227_IncludeMailMessages
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.0");

            modelBuilder.Entity("HomeAutomation.Database.LogRow", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Category")
                        .HasColumnType("TEXT");

                    b.Property<int>("EventID")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Exception")
                        .HasColumnType("TEXT");

                    b.Property<int>("Level")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Message")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("Rows");
                });

            modelBuilder.Entity("HomeAutomation.Database.MailMessage", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("DeviceSource")
                        .HasColumnType("TEXT");

                    b.Property<string>("DeviceSourceID")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("EmlData")
                        .HasColumnType("BLOB");

                    b.Property<string>("MessageID")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("ID");

                    b.ToTable("MailMessages");
                });
#pragma warning restore 612, 618
        }
    }
}
