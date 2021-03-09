﻿// <auto-generated />
using System;
using HomeAutomation.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HomeAutomation.Migrations
{
    [DbContext(typeof(DefaultContext))]
    [Migration("20191101204832_191101_01")]
    partial class _191101_01
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.2.6-servicing-10079");

            modelBuilder.Entity("HomeAutomation.Database.SensorValue", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("TellstickID");

                    b.Property<DateTime>("Timestamp");

                    b.Property<int>("Type");

                    b.Property<string>("Value");

                    b.HasKey("ID");

                    b.ToTable("SensorValues");
                });

            modelBuilder.Entity("HomeAutomation.Database.SunData", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Date");

                    b.Property<DateTime>("Sunrise");

                    b.Property<DateTime>("Sunset");

                    b.HasKey("ID");

                    b.HasIndex("Date")
                        .IsUnique();

                    b.ToTable("SunData");
                });

            modelBuilder.Entity("HomeAutomation.Database.WeatherForecast", b =>
                {
                    b.Property<int>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("Date");

                    b.Property<int>("Period");

                    b.Property<double>("Rain");

                    b.Property<string>("SymbolID");

                    b.Property<double>("Temperature");

                    b.Property<string>("WindDirection");

                    b.Property<double>("WindSpeed");

                    b.HasKey("ID");

                    b.ToTable("WeatherForecast");
                });
#pragma warning restore 612, 618
        }
    }
}
