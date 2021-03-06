// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using VL.Components.Database;

#nullable disable

namespace VL.Components.Migrations
{
    [DbContext(typeof(JobContext))]
    [Migration("20220306020341_AddedSubProcresses")]
    partial class AddedSubProcresses
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.2");

            modelBuilder.Entity("VL.Components.Database.FileEntry", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Cmd")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Filename")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<ulong?>("JobEntryId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ProcessId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Size")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("JobEntryId");

                    b.HasIndex("ProcessId");

                    b.ToTable("FileEntry");
                });

            modelBuilder.Entity("VL.Components.Database.JobEntry", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Args")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Cmd")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("ErrorCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ExitCode")
                        .HasColumnType("INTEGER");

                    b.Property<long>("FileRead")
                        .HasColumnType("INTEGER");

                    b.Property<long>("FileWrite")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Finish")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsFinished")
                        .HasColumnType("INTEGER");

                    b.Property<long>("NetworkReceive")
                        .HasColumnType("INTEGER");

                    b.Property<long>("NetworkSend")
                        .HasColumnType("INTEGER");

                    b.Property<long>("NumSubProcesses")
                        .HasColumnType("INTEGER");

                    b.Property<int>("OutputCount")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProcessID")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Start")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Jobs");
                });

            modelBuilder.Entity("VL.Components.Database.LogEntry", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("Channel")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("JobEntryId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("JobId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Message")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("JobEntryId");

                    b.ToTable("Logs");
                });

            modelBuilder.Entity("VL.Components.Database.NetworkEntry", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Address")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<int>("Cmd")
                        .HasColumnType("INTEGER");

                    b.Property<ulong?>("JobEntryId")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ProcessId")
                        .HasColumnType("INTEGER");

                    b.Property<long>("Size")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("JobEntryId");

                    b.HasIndex("ProcessId");

                    b.ToTable("NetworkEntry");
                });

            modelBuilder.Entity("VL.Components.Database.ProcessEntry", b =>
                {
                    b.Property<ulong>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Args")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Cmd")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Finish")
                        .HasColumnType("TEXT");

                    b.Property<ulong?>("JobEntryId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ParentProcessID")
                        .HasColumnType("INTEGER");

                    b.Property<ulong>("ParentUniqueId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("ProcessID")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Start")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("JobEntryId");

                    b.ToTable("ProcessEntry");
                });

            modelBuilder.Entity("VL.Components.Database.FileEntry", b =>
                {
                    b.HasOne("VL.Components.Database.JobEntry", null)
                        .WithMany("FileStats")
                        .HasForeignKey("JobEntryId");

                    b.HasOne("VL.Components.Database.ProcessEntry", "Process")
                        .WithMany()
                        .HasForeignKey("ProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Process");
                });

            modelBuilder.Entity("VL.Components.Database.LogEntry", b =>
                {
                    b.HasOne("VL.Components.Database.JobEntry", null)
                        .WithMany("Logs")
                        .HasForeignKey("JobEntryId");
                });

            modelBuilder.Entity("VL.Components.Database.NetworkEntry", b =>
                {
                    b.HasOne("VL.Components.Database.JobEntry", null)
                        .WithMany("NetworkStats")
                        .HasForeignKey("JobEntryId");

                    b.HasOne("VL.Components.Database.ProcessEntry", "Process")
                        .WithMany()
                        .HasForeignKey("ProcessId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Process");
                });

            modelBuilder.Entity("VL.Components.Database.ProcessEntry", b =>
                {
                    b.HasOne("VL.Components.Database.JobEntry", null)
                        .WithMany("SubProcesses")
                        .HasForeignKey("JobEntryId");
                });

            modelBuilder.Entity("VL.Components.Database.JobEntry", b =>
                {
                    b.Navigation("FileStats");

                    b.Navigation("Logs");

                    b.Navigation("NetworkStats");

                    b.Navigation("SubProcesses");
                });
#pragma warning restore 612, 618
        }
    }
}
